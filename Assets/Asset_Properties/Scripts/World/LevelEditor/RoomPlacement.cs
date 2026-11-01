using UnityEngine;

namespace LostForest.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RoomBounds))]
    public class RoomPlacement : MonoBehaviour
    {
        [SerializeField] private RoomData data;

        public RoomData Data => data;
    }
}