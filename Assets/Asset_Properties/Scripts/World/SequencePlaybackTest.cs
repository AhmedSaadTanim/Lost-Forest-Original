using UnityEngine;
using UnityEngine.Playables;

namespace LostForest.World
{
    [DefaultExecutionOrder(1200)]
    public class SequencePlaybackTest : MonoBehaviour
    {
        [SerializeField] private SequenceController sequenceController;
        [SerializeField] private PlayableAsset sequence;

        private void Start()
        {
            if (sequenceController == null || sequence == null)
            {
                Debug.LogError("Assign Sequence Controller and Sequence.", this);
                return;
            }

            if (!sequenceController.PlaySequence(sequence))
            {
                Debug.LogWarning("The sequence could not start. Check player control flags and controller references.", this);
            }
        }
    }
}