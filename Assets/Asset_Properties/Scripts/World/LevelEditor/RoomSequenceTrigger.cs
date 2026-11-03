using LostForest.Gameplay;
using UnityEngine;
using UnityEngine.Playables;

namespace LostForest.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomSequenceTrigger : MonoBehaviour
    {
        [SerializeField] private PlayableAsset sequence;

        private SequenceController sequenceController;
        private bool hasTriggered;

        private void Awake()
        {
            sequenceController = FindFirstObjectByType<SequenceController>();
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (hasTriggered || sequence == null)
            {
                return;
            }

            Rigidbody2D body = other.attachedRigidbody;

            if (body == null || !body.TryGetComponent(out Player player))
            {
                return;
            }

            if (!player.IsPlayerControlEnabled || !player.IsPlayerMovementEnabled)
            {
                return;
            }

            if (sequenceController == null)
            {
                sequenceController = FindFirstObjectByType<SequenceController>();
            }

            if (sequenceController == null || sequenceController.IsPlaying)
            {
                return;
            }

            if (sequenceController.PlaySequenceAfterLanding(sequence))
            {
                hasTriggered = true;
            }
        }
    }
}