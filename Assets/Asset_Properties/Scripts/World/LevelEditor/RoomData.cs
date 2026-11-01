using UnityEngine;

namespace LostForest.World
{
    [CreateAssetMenu(
        fileName = "RoomData",
        menuName = "Lost Forest/Room Data")]
    public class RoomData : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 worldPosition;

        public GameObject Prefab => prefab;
        public Vector3 WorldPosition => worldPosition;
    }
}