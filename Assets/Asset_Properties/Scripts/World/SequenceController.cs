using System.Collections;
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
            if (!TryLockPlayer(sequence))
            {
                return false;
            }

            StartPlayback(sequence);
            return true;
        }

        public bool PlaySequenceAfterLanding(PlayableAsset sequence)
        {
            if (!TryLockPlayer(sequence))
            {
                return false;
            }

            StartCoroutine(WaitForLanding(sequence));
            return true;
        }
        
        private bool TryLockPlayer(PlayableAsset sequence)
        {
            if (!isActiveAndEnabled || sequence == null || player == null)
            {
                Debug.LogWarning("Assign a sequence and Player to an active sequence controller.", this);
                return false;
            }

            if (ownsControlLock ||
                !player.IsPlayerControlEnabled ||
                !player.IsPlayerMovementEnabled)
            {
                return false;
            }

            player.Movement.LockForSequence();
            ownsControlLock = true;
            return true;
        }
        
        private IEnumerator WaitForLanding(PlayableAsset sequence)
        {
            // Allow physics to update after movement was stopped.
            yield return new WaitForFixedUpdate();

            while (player != null && !player.Movement.IsStandingOnGround)
            {
                yield return new WaitForFixedUpdate();
            }

            if (player == null)
            {
                RestorePlayerControl();
                yield break;
            }

            StartPlayback(sequence);
        }
        
        private void StartPlayback(PlayableAsset sequence)
        {
            try
            {
                director.playableAsset = sequence;
                director.time = 0d;
                director.Play();
            }
            catch
            {
                RestorePlayerControl();
                throw;
            }
        }
        
        public void StopSequence()
        {
            StopAllCoroutines();
            
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
            StopAllCoroutines();
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