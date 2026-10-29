using UnityEngine;

namespace LostForest.Gameplay
{
    [RequireComponent(typeof(Player))]
    public class PlayerRespawn : MonoBehaviour
    {
        private Player player;
        private int lastRespawnFrame = -1;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        public void RespawnAt(Vector2 position)
        {
            if (lastRespawnFrame == Time.frameCount)
            {
                return;
            }

            lastRespawnFrame = Time.frameCount;

            player.IsPlayerControlEnabled = false;
            player.IsPlayerMovementEnabled = false;

            player.Body.linearVelocity = Vector2.zero;
            player.Body.angularVelocity = 0f;
            player.Body.position = position;

            player.Movement.ResetAfterRespawn();

            player.IsPlayerMovementEnabled = true;
            player.IsPlayerControlEnabled = true;
        }
    }
}