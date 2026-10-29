using LostForest.Gameplay;
using UnityEngine;

namespace LostForest.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class FallZone : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;

            if (body == null || !body.TryGetComponent<Player>(out var player))
            {
                return;
            }

            if (respawnPoint == null)
            {
                Debug.LogError("FallZone has no RespawnPoint assigned.", this);
                return;
            }

            player.Respawn.RespawnAt(respawnPoint.position);
        }
    }
}