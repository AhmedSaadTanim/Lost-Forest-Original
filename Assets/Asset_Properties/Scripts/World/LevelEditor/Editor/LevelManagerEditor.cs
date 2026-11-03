using System.Collections.Generic;
using LostForest.World;
using UnityEditor;
using UnityEngine;

namespace LostForest.Editor
{
    [CustomEditor(typeof(LevelManager))]
    public class LevelManagerEditor : UnityEditor.Editor
    {
        private const string DataFolder = "Assets/Asset_Properties/Data/RoomData";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Build Room Layout"))
                {
                    BuildRoomLayout();
                }

                if (GUILayout.Button("Bake Positions Only"))
                {
                    TryRunStage("Bake Room Layout", TryBakeRoomLayout);
                }
            }
        }

        private void BuildRoomLayout()
        {
            if (!TryRunStage("Generate Room Data", RoomDataGenerator.TryGenerateRoomData))
            {
                return;
            }

            if (!TryRunStage("Bake Room Layout", TryBakeRoomLayout))
            {
                return;
            }

            if (!TryRunStage("Update Room Catalog", RoomCatalogGenerator.TryUpdateRoomCatalog))
            {
                return;
            }

            Debug.Log("Build Room Layout completed successfully: room data generated, positions baked, and catalog updated.", target);
        }
        
        private bool TryRunStage(string stageName, System.Func<bool> stage)
        {
            try
            {
                if (!stage())
                {
                    Debug.LogError($"Build Room Layout failed at stage: '{stageName}'. Check the preceding Console messages for details.", target);
                    return false;
                }

                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Build Room Layout failed at stage: '{stageName}'. Exception: {exception.Message}", target);
                Debug.LogException(exception, target);
                return false;
            }
        }
        
        private bool TryBakeRoomLayout()
        {
            serializedObject.Update();

            Transform container = serializedObject.FindProperty("roomContainer").objectReferenceValue as Transform;

            if (container == null)
            {
                Debug.LogError("Assign Room Container first.", target);
                return false;
            }

            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                Debug.LogError("Generate Room Data before baking the layout.", target);
                return false;
            }

            var dataByPrefab = new Dictionary<GameObject, RoomData>();
            string[] dataGuids = AssetDatabase.FindAssets("t:RoomData", new[] { DataFolder });

            foreach (string guid in dataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path);

                if (data == null || data.Prefab == null)
                {
                    continue;
                }

                if (dataByPrefab.ContainsKey(data.Prefab))
                {
                    Debug.LogError($"Multiple RoomData assets reference '{data.Prefab.name}'. Resolve the duplicate first.", data);
                    return false;
                }

                dataByPrefab.Add(data.Prefab, data);
            }

            var placements = new Dictionary<RoomData, Vector3>();

            // Validate every placement before changing any data.
            foreach (Transform child in container)
            {
                GameObject room = child.gameObject;

                if (!PrefabUtility.IsAnyPrefabInstanceRoot(room))
                {
                    Debug.LogError($"'{room.name}' must be a room prefab instance.", room);
                    return false;
                }

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(room);

                if (prefab == null || !dataByPrefab.TryGetValue(prefab, out RoomData data))
                {
                    Debug.LogError($"No generated RoomData found for '{room.name}'. Run Generate Room Data first.", room);
                    return false;
                }

                if (Quaternion.Angle(child.rotation, Quaternion.identity) > 0.01f ||
                    (child.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
                {
                    Debug.LogError($"'{room.name}' must have zero world rotation and unit world scale.", room);
                    return false;
                }

                if (placements.ContainsKey(data))
                {
                    Debug.LogError($"'{prefab.name}' appears more than once. The current data format stores one position per prefab.", room);
                    return false;
                }

                placements.Add(data, child.position);
            }

            if (placements.Count == 0)
            {
                Debug.LogWarning("Room Container has no rooms to bake.", target);
                return false;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bake Room Layout");

            foreach (KeyValuePair<RoomData, Vector3> placement in placements)
            {
                SerializedObject serializedData = new SerializedObject(placement.Key);
                serializedData.Update();
                serializedData.FindProperty("worldPosition").vector3Value = placement.Value;
                serializedData.ApplyModifiedProperties();
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();

            Debug.Log($"Saved positions into {placements.Count} RoomData assets.", target);

            return true;
        }
    }
}