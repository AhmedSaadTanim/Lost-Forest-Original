using System.Collections.Generic;
using LostForest.World;
using UnityEditor;
using UnityEngine;

namespace LostForest.Editor
{
    public static class RoomDataGenerator
    {
        private const string PrefabFolder = "Assets/Asset_Properties/Prefab/Levels";
        private const string DataFolder = "Assets/Asset_Properties/Data/RoomData";

        [MenuItem("Lost Forest/Rooms/Generate Room Data")]
        private static void GenerateRoomData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                Debug.LogError($"Prefab folder not found: {PrefabFolder}");
                return;
            }

            EnsureFolder(DataFolder);

            var roomPrefabs = new SortedDictionary<int, GameObject>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });

            // Validate all prefab names before modifying assets.
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null || prefab.GetComponent<RoomBounds>() == null)
                {
                    continue;
                }

                if (!TryGetSerial(prefab.name, out int serial))
                {
                    Debug.LogError($"Expected 'Room' or 'Room <number>', found '{prefab.name}'.", prefab);
                    return;
                }

                if (roomPrefabs.ContainsKey(serial))
                {
                    Debug.LogError($"Multiple room prefabs map to r{serial:D2}. Give each room a unique number.", prefab);
                    return;
                }

                roomPrefabs.Add(serial, prefab);
            }

            var existingData = new List<RoomData>();
            string[] dataGuids = AssetDatabase.FindAssets("t:RoomData", new[] { DataFolder });

            foreach (string guid in dataGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path);

                if (data != null)
                {
                    existingData.Add(data);
                }
            }

            var assignments = new Dictionary<int, RoomData>();
            var assignedData = new HashSet<RoomData>();

            // Prefer assets already linked to the correct prefab.
            foreach (KeyValuePair<int, GameObject> entry in roomPrefabs)
            {
                RoomData match = null;

                foreach (RoomData data in existingData)
                {
                    if (data.Prefab != entry.Value)
                    {
                        continue;
                    }

                    if (match != null)
                    {
                        Debug.LogError($"Multiple RoomData assets reference '{entry.Value.name}'. Resolve the duplicate first.", data);
                        return;
                    }

                    match = data;
                }

                if (match != null)
                {
                    assignments.Add(entry.Key, match);
                    assignedData.Add(match);
                }
            }

            // Reuse empty assets at the expected paths.
            foreach (KeyValuePair<int, GameObject> entry in roomPrefabs)
            {
                string path = GetDataPath(entry.Key);
                Object occupant = AssetDatabase.LoadMainAssetAtPath(path);

                if (occupant == null)
                {
                    continue;
                }

                if (occupant is RoomData data && assignedData.Contains(data))
                {
                    continue;
                }

                if (!assignments.ContainsKey(entry.Key) && occupant is RoomData emptyData && emptyData.Prefab == null)
                {
                    assignments.Add(entry.Key, emptyData);
                    assignedData.Add(emptyData);
                    continue;
                }

                Debug.LogError($"Cannot use '{path}': it contains an unrelated asset. Rename that asset and generate again.", occupant);
                return;
            }

            // Move existing assets aside first to avoid rename collisions.
            foreach (RoomData data in assignments.Values)
            {
                string oldPath = AssetDatabase.GetAssetPath(data);
                string temporaryPath = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/RoomData_Temporary.asset");
                string error = AssetDatabase.MoveAsset(oldPath, temporaryPath);

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error, data);
                    return;
                }
            }

            int created = 0;

            foreach (KeyValuePair<int, GameObject> entry in roomPrefabs)
            {
                string path = GetDataPath(entry.Key);

                if (!assignments.TryGetValue(entry.Key, out RoomData data))
                {
                    data = ScriptableObject.CreateInstance<RoomData>();
                    AssetDatabase.CreateAsset(data, path);
                    created++;
                }
                else
                {
                    string error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(data), path);

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError(error, data);
                        return;
                    }
                }

                SerializedObject serializedData = new SerializedObject(data);
                serializedData.Update();
                serializedData.FindProperty("prefab").objectReferenceValue = entry.Value;
                serializedData.ApplyModifiedProperties();

                // Existing world positions remain unchanged.
                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"Generated {roomPrefabs.Count} room data assets; {created} created. Room → r00, Room 1 → r01, Room 2 → r02.");
        }

        private static bool TryGetSerial(string prefabName, out int serial)
        {
            serial = 0;

            if (prefabName == "Room")
            {
                return true;
            }

            if (!prefabName.StartsWith("Room "))
            {
                return false;
            }

            return int.TryParse(prefabName.Substring(5).Trim(), out serial) && serial >= 0;
        }

        private static string GetDataPath(int serial)
        {
            return $"{DataFolder}/r{serial:D2}.asset";
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}