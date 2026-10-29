using UnityEngine;

namespace LostForest.Gameplay
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerRespawn))]
    public class Player : MonoBehaviour
    {
        public Rigidbody2D Body { get; private set; }
        public Collider2D Collider { get; private set; }
    
        public PlayerMovement Movement { get; private set; }
        public PlayerAnimation Animation { get; private set; }
        public PlayerRespawn Respawn { get; private set; }
    
        public bool IsPlayerControlEnabled { get; set; }
        public bool IsPlayerMovementEnabled { get; set; }

        private void Start()
        {
            Body = GetComponent<Rigidbody2D>();
            Collider = GetComponent<Collider2D>();

            Movement = GetComponent<PlayerMovement>();
            Animation = GetComponent<PlayerAnimation>();
            Respawn = GetComponent<PlayerRespawn>();
        }
    }
}

