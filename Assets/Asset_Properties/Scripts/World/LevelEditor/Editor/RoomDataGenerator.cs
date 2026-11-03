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
            TryGenerateRoomData();
        }
        
        [MenuItem("Lost Forest/Rooms/Generate Room Data")]
        public static bool TryGenerateRoomData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                Debug.LogError($"Prefab folder not found: {PrefabFolder}");
                return false;
            }

            EnsureFolder(DataFolder);
            
            if (!TryCollectRoomPrefabs(out var roomPrefabs))
            {
                return false;
            }

            var existingData = CollectExistingData();
            
            if (!TryCollectAssignments(roomPrefabs, existingData, out var assignments, out var assignedData))
            {
                return false;
            }
            
            if (!TryReuseEmptyAssets(roomPrefabs, assignments, assignedData))
            {
                return false;
            }

            if (!TryStageExistingAssets(assignments))
            {
                return false;
            }

            if (!TryWriteRoomData(roomPrefabs, assignments, out int created))
            {
                return false;
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"Generated {roomPrefabs.Count} room data assets; {created} created.");

            return true;
        }

        #region Helper

        private static bool TryReuseEmptyAssets(SortedDictionary<int, GameObject> roomPrefabs, Dictionary<int, RoomData> assignments, HashSet<RoomData> assignedData)
        {
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
                return false;
            }

            return true;
        }

        private static bool TryStageExistingAssets(Dictionary<int, RoomData> assignments)
        {
            foreach (KeyValuePair<int, RoomData> entry in assignments)
            {
                RoomData data = entry.Value;
                string oldPath = AssetDatabase.GetAssetPath(data);
                string destinationPath = GetDataPath(entry.Key);

                // Assets already at their correct path do not need staging.
                if (oldPath == destinationPath)
                {
                    continue;
                }

                string temporaryPath = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/RoomData_Temporary.asset");

                if (!TryMoveAsset(data, temporaryPath))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryWriteRoomData(SortedDictionary<int, GameObject> roomPrefabs, Dictionary<int, RoomData> assignments, out int created)
        {
            created = 0;

            foreach (KeyValuePair<int, GameObject> entry in roomPrefabs)
            {
                string path = GetDataPath(entry.Key);

                if (!assignments.TryGetValue(entry.Key, out RoomData data))
                {
                    data = ScriptableObject.CreateInstance<RoomData>();
                    AssetDatabase.CreateAsset(data, path);
                    created++;
                }
                else if (!TryMoveAsset(data, path))
                {
                    return false;
                }

                UpdatePrefabReference(data, entry.Value);
            }

            return true;
        }

        private static bool TryMoveAsset(RoomData data, string destinationPath)
        {
            string currentPath = AssetDatabase.GetAssetPath(data);

            if (currentPath == destinationPath)
            {
                return true;
            }

            string error = AssetDatabase.MoveAsset(currentPath, destinationPath);

            if (string.IsNullOrEmpty(error))
            {
                return true;
            }

            Debug.LogError(error, data);
            return false;
        }

        private static void UpdatePrefabReference(RoomData data, GameObject prefab)
        {
            SerializedObject serializedData = new SerializedObject(data);
            serializedData.Update();
            serializedData.FindProperty("prefab").objectReferenceValue = prefab;
            serializedData.ApplyModifiedProperties();

            EditorUtility.SetDirty(data);
        }

        private static bool TryCollectAssignments(SortedDictionary<int, GameObject> roomPrefabs, List<RoomData> existingData,
            out Dictionary<int, RoomData> assignments, out HashSet<RoomData> assignedData)
        {
            assignments = new Dictionary<int, RoomData>();
            assignedData = new HashSet<RoomData>();

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
                        return false;
                    }

                    match = data;
                }

                if (match != null)
                {
                    assignments.Add(entry.Key, match);
                    assignedData.Add(match);
                }
            }

            return true;
        }
        
        private static bool TryCollectRoomPrefabs(out SortedDictionary<int, GameObject> roomPrefabs)
        {
            roomPrefabs = new SortedDictionary<int, GameObject>();
            
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
                    return false;
                }

                if (roomPrefabs.ContainsKey(serial))
                {
                    Debug.LogError($"Multiple room prefabs map to r{serial:D2}. Give each room a unique number.", prefab);
                    return false;
                }

                roomPrefabs.Add(serial, prefab);
            }

            return true;
        }

        private static List<RoomData> CollectExistingData()
        {
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

            return existingData;
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

        #endregion
    }
}