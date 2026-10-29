using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Serializable]
    private class RoomPlacement
    {
        public GameObject prefab;
        public Vector3 worldPosition;
        
        [NonSerialized] public GameObject instance;
        [NonSerialized] public Bounds worldBounds;
        [NonSerialized] public bool valid;
    }
    
    [SerializeField] private Transform roomContainer;
    [SerializeField] private Transform player;
    [SerializeField] private RoomPlacement[] rooms;

    [Header("Streaming Distances")]
    [SerializeField, Min(0f)] private float loadDistance = 25f;
    [SerializeField, Min(0f)] private float unloadDistance = 40f;
    
    private void Awake()
    {
        if (roomContainer == null || player == null || rooms == null)
        {
            Debug.LogError("Assign RoomContainer, Player, and Rooms.", this);
            enabled = false;
            return;
        }

        foreach (RoomPlacement room in rooms)
        {
            if (room == null || room.prefab == null)
                continue;

            RoomBounds bounds = room.prefab.GetComponent<RoomBounds>();

            if (bounds == null)
            {
                Debug.LogError(
                    $"{room.prefab.name} needs RoomBounds on its root.",
                    room.prefab
                );
                continue;
            }

            Vector3 center = room.worldPosition +
                new Vector3(bounds.Center.x, bounds.Center.y, 0f);

            room.worldBounds = new Bounds(
                center,
                new Vector3(bounds.Size.x, bounds.Size.y, 1f)
            );

            room.valid = true;
        }

        UpdateRooms();
    }

    private void Update()
    {
        UpdateRooms();
    }

    private void UpdateRooms()
    {
        foreach (RoomPlacement room in rooms)
        {
            if (room == null || !room.valid)
                continue;

            // Measure distance in 2D, ignoring the player's Z position.
            Vector3 point = new Vector3(
                player.position.x,
                player.position.y,
                room.worldBounds.center.z
            );

            float distanceSquared = room.worldBounds.SqrDistance(point);

            if (room.instance == null)
            {
                if (distanceSquared <= loadDistance * loadDistance)
                {
                    room.instance = Instantiate(
                        room.prefab,
                        room.worldPosition,
                        Quaternion.identity,
                        roomContainer
                    );
                }
            }
            else if (distanceSquared > unloadDistance * unloadDistance)
            {
                Destroy(room.instance);
                room.instance = null;
            }
        }
    }

    private void OnValidate()
    {
        unloadDistance = Mathf.Max(unloadDistance, loadDistance + 1f);
    }
}