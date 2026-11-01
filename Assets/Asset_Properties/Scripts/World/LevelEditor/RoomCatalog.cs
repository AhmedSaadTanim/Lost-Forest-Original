using System.Collections.Generic;
using UnityEngine;

namespace LostForest.World
{
    public class RoomCatalog : ScriptableObject
    {
        [SerializeField] private RoomData[] rooms = new RoomData[0];

        public IReadOnlyList<RoomData> Rooms => rooms;
    }
}