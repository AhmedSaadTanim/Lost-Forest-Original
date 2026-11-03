using LostForest.Gameplay;
using UnityEngine;
using UnityEngine.Playables;

namespace LostForest.World
{
    [RequireComponent(typeof(PlayableDirector))]
    public class SequenceController : MonoBehaviour
    {
        [SerializeField] private Player player;

        private PlayableDirector director;
        private bool ownsControlLock;

        public bool IsPlaying => ownsControlLock;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
        }

        private void OnEnable()
        {
            director.stopped += OnDirectorStopped;
        }

        public bool PlaySequence(PlayableAsset sequence)
        {
            if (!isActiveAndEnabled || sequence == null || player == null)
            {
                Debug.LogWarning("Sequence requires an active controller, a Timeline asset, and a Player reference.", this);
                return false;
            }

            // Avoid overlapping sequences or taking over another system's lock.
            if (ownsControlLock || !player.IsPlayerControlEnabled || !player.IsPlayerMovementEnabled)
            {
                return false;
            }

            player.Movement.LockForSequence();
            ownsControlLock = true;

            try
            {
                director.playableAsset = sequence;
                director.time = 0d;
                director.Play();
                Debug.Log($"Sequence started: {sequence.name}. Duration: {director.duration:F2} seconds.", this);
                return true;
            }
            catch
            {
                RestorePlayerControl();
                throw;
            }
        }

        public void StopSequence()
        {
            if (!ownsControlLock)
            {
                return;
            }

            director.Stop();
            RestorePlayerControl();
        }

        private void OnDirectorStopped(PlayableDirector stoppedDirector)
        {
            Debug.Log($"Sequence stopped at {stoppedDirector.time:F2} seconds.", this);
            RestorePlayerControl();
        }

        private void OnDisable()
        {
            director.stopped -= OnDirectorStopped;
            StopSequence();
        }

        private void RestorePlayerControl()
        {
            if (!ownsControlLock)
            {
                return;
            }

            ownsControlLock = false;

            if (player != null)
            {
                player.Movement.UnlockAfterSequence();
            }
        }
    }
}