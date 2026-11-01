using System;
using System.Collections.Generic;
using LostForest.World;
using UnityEditor;
using UnityEngine;

namespace LostForest.Editor
{
    public static class RoomCatalogGenerator
    {
        private const string DataFolder = "Assets/Asset_Properties/Data/RoomData";
        private const string CatalogPath = "Assets/Asset_Properties/Data/RoomCatalog.asset";

        [MenuItem("Lost Forest/Rooms/Update Room Catalog")]
        private static void UpdateRoomCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                Debug.LogError("Generate Room Data first.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:RoomData", new[] { DataFolder });
            var rooms = new List<RoomData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomData data = AssetDatabase.LoadAssetAtPath<RoomData>(path);

                if (data == null || data.Prefab == null)
                {
                    Debug.LogError($"RoomData at '{path}' has no prefab. Fix it before updating the catalog.", data);
                    return;
                }

                rooms.Add(data);
            }

            if (rooms.Count == 0)
            {
                Debug.LogWarning("No room data found. Existing catalog was kept.");
                return;
            }

            rooms.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            RoomCatalog catalog = AssetDatabase.LoadAssetAtPath<RoomCatalog>(CatalogPath);

            if (catalog == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(CatalogPath) != null)
                {
                    Debug.LogError($"Another asset already exists at '{CatalogPath}'.");
                    return;
                }

                catalog = ScriptableObject.CreateInstance<RoomCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.Update();

            SerializedProperty roomsProperty = serializedCatalog.FindProperty("rooms");
            roomsProperty.arraySize = rooms.Count;

            for (int i = 0; i < rooms.Count; i++)
            {
                roomsProperty.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
            }

            serializedCatalog.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"Room catalog updated with {rooms.Count} rooms.", catalog);
        }
    }
}