using System.Collections.Generic;
using UnityEngine;

namespace LostForest.World
{
    [DefaultExecutionOrder(1000)]
    public class LevelManager : MonoBehaviour
    {
        private sealed class CachedRoom
        {
            public GameObject Instance;
            public Rect Bounds;
        }

        [Header("References")]
        [SerializeField] private Transform roomContainer;
        [SerializeField] private Transform player;
        [SerializeField] private Camera streamingCamera;
        [SerializeField] private RoomCatalog catalog;

        [Header("Streaming Buffers")]
        [SerializeField, Min(0f)] private float loadBuffer = 5f;
        [SerializeField, Min(0f)] private float unloadBuffer = 10f;

        private readonly List<CachedRoom> cachedRooms = new List<CachedRoom>();
        private bool initialized;

        private void Start()
        {
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            ClearPreviewRooms();
            CreateCachedRooms();

            initialized = true;
            RefreshRooms();
        }

        private void LateUpdate()
        {
            RefreshRooms();
        }

        public void RefreshRooms()
        {
            if (!initialized)
            {
                return;
            }

            Rect cameraBounds = GetCameraBounds();
            Rect loadBounds = Expand(cameraBounds, loadBuffer);
            Rect unloadBounds = Expand(cameraBounds, unloadBuffer);
            Vector2 playerPosition = player.position;

            foreach (CachedRoom room in cachedRooms)
            {
                if (room.Instance == null)
                {
                    continue;
                }

                // Keep rooms containing the player active during camera lookaways.
                bool containsPlayer = room.Bounds.Contains(playerPosition);

                if (!room.Instance.activeSelf)
                {
                    if (containsPlayer || room.Bounds.Overlaps(loadBounds))
                    {
                        room.Instance.SetActive(true);
                    }
                }
                else if (!containsPlayer && !room.Bounds.Overlaps(unloadBounds))
                {
                    room.Instance.SetActive(false);
                }
            }
        }

        private bool ValidateSetup()
        {
            if (roomContainer == null || player == null || streamingCamera == null || catalog == null)
            {
                Debug.LogError("Assign Room Container, Player, Streaming Camera, and Catalog.", this);
                return false;
            }

            if (!roomContainer.gameObject.activeInHierarchy)
            {
                Debug.LogError("Room Container must be active.", roomContainer);
                return false;
            }

            if (player == roomContainer || player.IsChildOf(roomContainer) ||
                streamingCamera.transform == roomContainer || streamingCamera.transform.IsChildOf(roomContainer) ||
                transform == roomContainer || transform.IsChildOf(roomContainer))
            {
                Debug.LogError("Keep the player, streaming camera, and LevelManager outside Room Container.", this);
                return false;
            }

            if (!streamingCamera.orthographic ||
                Quaternion.Angle(streamingCamera.transform.rotation, Quaternion.identity) > 0.01f)
            {
                Debug.LogError("Streaming requires an unrotated orthographic camera.", streamingCamera);
                return false;
            }

            if (Quaternion.Angle(roomContainer.rotation, Quaternion.identity) > 0.01f ||
                (roomContainer.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            {
                Debug.LogError("Room Container requires zero world rotation and unit world scale.", roomContainer);
                return false;
            }

            if (catalog.Rooms.Count == 0)
            {
                Debug.LogError("Room Catalog is empty. Update it before playing.", catalog);
                return false;
            }

            var uniqueData = new HashSet<RoomData>();

            foreach (RoomData data in catalog.Rooms)
            {
                if (data == null || data.Prefab == null)
                {
                    Debug.LogError("Catalog contains missing room data or a missing prefab.", catalog);
                    return false;
                }

                if (!uniqueData.Add(data))
                {
                    Debug.LogError($"Catalog contains duplicate data: {data.name}.", catalog);
                    return false;
                }

                if (data.Prefab.GetComponent<RoomBounds>() == null ||
                    (data.Prefab.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
                {
                    Debug.LogError($"{data.Prefab.name} requires RoomBounds on its root and unit root scale.", data.Prefab);
                    return false;
                }
            }

            return true;
        }

        private void ClearPreviewRooms()
        {
            // Deactivate immediately because Destroy completes later in the frame.
            for (int i = roomContainer.childCount - 1; i >= 0; i--)
            {
                GameObject preview = roomContainer.GetChild(i).gameObject;
                preview.SetActive(false);
                preview.transform.SetParent(null, true);
                Destroy(preview);
            }
        }

        private void CreateCachedRooms()
        {
            // Instantiate under an inactive parent to avoid activating every room.
            GameObject staging = new GameObject("RoomStaging");
            staging.SetActive(false);

            foreach (RoomData data in catalog.Rooms)
            {
                RoomBounds bounds = data.Prefab.GetComponent<RoomBounds>();

                Vector2 center = new Vector2(
                    data.WorldPosition.x + bounds.Center.x,
                    data.WorldPosition.y + bounds.Center.y);

                GameObject instance = Instantiate(data.Prefab, data.WorldPosition, Quaternion.identity, staging.transform);
                instance.name = $"{data.name}_{data.Prefab.name}";
                instance.SetActive(false);
                instance.transform.SetParent(roomContainer, true);

                cachedRooms.Add(new CachedRoom
                {
                    Instance = instance,
                    Bounds = new Rect(center - bounds.Size * 0.5f, bounds.Size)
                });
            }

            Destroy(staging);
        }

        private Rect GetCameraBounds()
        {
            float height = streamingCamera.orthographicSize * 2f;
            float width = height * streamingCamera.aspect;
            Vector2 center = streamingCamera.transform.position;
            Vector2 size = new Vector2(width, height);

            return new Rect(center - size * 0.5f, size);
        }

        private static Rect Expand(Rect bounds, float buffer)
        {
            return Rect.MinMaxRect(
                bounds.xMin - buffer,
                bounds.yMin - buffer,
                bounds.xMax + buffer,
                bounds.yMax + buffer);
        }

        private void OnValidate()
        {
            loadBuffer = Mathf.Max(0f, loadBuffer);
            unloadBuffer = Mathf.Max(unloadBuffer, loadBuffer + 1f);
        }
    }
}