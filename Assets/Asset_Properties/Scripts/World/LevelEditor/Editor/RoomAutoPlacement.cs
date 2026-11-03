using System.Collections.Generic;
using LostForest.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LostForest.Editor
{
    [InitializeOnLoad]
    public static class RoomAutoPlacement
    {
        private const string DataFolder = "Assets/Asset_Properties/Data/RoomData";

        private static HashSet<(EntityId room, EntityId container)> _knownRooms = new();
        private static bool _initialized;
        private static bool _scanQueued;

        static RoomAutoPlacement()
        {
            EditorApplication.hierarchyChanged += QueueScan;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            QueueScan();
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // Opening a scene should preserve its authored positions.
            _initialized = false;
            QueueScan();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            _initialized = false;
            _knownRooms.Clear();

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                QueueScan();
            }
        }

        private static void QueueScan()
        {
            if (_scanQueued)
            {
                return;
            }

            _scanQueued = true;
            EditorApplication.delayCall += ScanRooms;
        }

        private static void ScanRooms()
        {
            _scanQueued = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var currentRooms = new HashSet<(EntityId room, EntityId container)>();
            var newRooms = new List<GameObject>();

            LevelManager[] managers = Object.FindObjectsByType<LevelManager>(FindObjectsInactive.Include);

            foreach (LevelManager manager in managers)
            {
                if (EditorSceneManager.IsPreviewScene(manager.gameObject.scene))
                {
                    continue;
                }

                SerializedObject serializedManager = new SerializedObject(manager);
                Transform container = serializedManager.FindProperty("roomContainer").objectReferenceValue as Transform;

                if (container == null)
                {
                    continue;
                }

                foreach (Transform child in container)
                {
                    GameObject room = child.gameObject;

                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(room) || room.GetComponent<RoomBounds>() == null)
                    {
                        continue;
                    }

                    var key = (room.GetEntityId(), container.GetEntityId());

                    // Avoid processing a shared container more than once.
                    if (!currentRooms.Add(key))
                    {
                        continue;
                    }

                    if (_initialized && !_knownRooms.Contains(key))
                    {
                        newRooms.Add(room);
                    }
                }
            }

            // Update tracking before moving objects, which can trigger another scan.
            _knownRooms = currentRooms;
            _initialized = true;

            if (newRooms.Count == 0 || !AssetDatabase.IsValidFolder(DataFolder))
            {
                return;
            }

            Dictionary<GameObject, RoomData> dataByPrefab = CollectData();

            foreach (GameObject room in newRooms)
            {
                RestorePosition(room, dataByPrefab);
            }
        }

        private static Dictionary<GameObject, RoomData> CollectData()
        {
            var result = new Dictionary<GameObject, RoomData>();
            string[] guids = AssetDatabase.FindAssets("t:RoomData", new[] { DataFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path);

                if (data == null || data.Prefab == null)
                {
                    continue;
                }

                if (result.ContainsKey(data.Prefab))
                {
                    Debug.LogWarning($"Multiple RoomData assets reference '{data.Prefab.name}'. Auto-placement will skip that prefab.", data);
                    result[data.Prefab] = null;
                    continue;
                }

                result.Add(data.Prefab, data);
            }

            return result;
        }

        private static void RestorePosition(GameObject room, Dictionary<GameObject, RoomData> dataByPrefab)
        {
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(room);

            if (prefab == null || !dataByPrefab.TryGetValue(prefab, out RoomData data) || data == null)
            {
                Debug.LogWarning($"No unique RoomData found for '{room.name}'. Generate its data first.", room);
                return;
            }

            if ((room.transform.position - data.WorldPosition).sqrMagnitude < 0.000001f)
            {
                return;
            }

            Undo.RecordObject(room.transform, "Restore Saved Room Position");
            room.transform.position = data.WorldPosition;

            PrefabUtility.RecordPrefabInstancePropertyModifications(room.transform);
            EditorSceneManager.MarkSceneDirty(room.scene);
        }
    }
}