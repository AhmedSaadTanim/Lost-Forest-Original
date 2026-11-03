using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace LostForest.Dialogue
{
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineDialogue : MonoBehaviour
    {
        [SerializeField] private DialogueUI dialogueUI;

        private PlayableDirector director;
        private PlayerInput input;
        private double[] previousSpeeds;
        private bool waitingForInput;
        private int openedFrame;
        private int advanceBindingIndex;
        
        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            input = new PlayerInput();
        }

        private void OnEnable()
        {
            input.UI.AdvanceDialogue.performed += OnAdvance;
            director.stopped += OnDirectorStopped;
        }

        public void ShowLine(DialogueLine line)
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            if (dialogueUI == null || line == null)
            {
                Debug.LogError("Assign Dialogue UI and a valid dialogue line.", this);
                return;
            }

            if (waitingForInput || director.state != PlayState.Playing)
            {
                return;
            }

            dialogueUI.SetContinueButton(input.UI.AdvanceDialogue, advanceBindingIndex);
            dialogueUI.ShowLine(line);
            FreezeTimeline();

            waitingForInput = true;
            openedFrame = Time.frameCount;
            input.UI.AdvanceDialogue.Enable();
        }

        private void OnAdvance(InputAction.CallbackContext context)
        {
            if (!waitingForInput || Time.frameCount == openedFrame)
            {
                return;
            }

            int bindingIndex = context.action.GetBindingIndexForControl(context.control);
            
            if (bindingIndex >= 0)
            {
                advanceBindingIndex = bindingIndex;
                dialogueUI.SetContinueButton(context.action, advanceBindingIndex);
            }
            
            if (dialogueUI.IsTyping)
            {
                dialogueUI.CompleteTyping();
                return;
            }

            waitingForInput = false;
            input.UI.AdvanceDialogue.Disable();
            dialogueUI.Hide();
            RestoreTimelineSpeed();
        }

        private void FreezeTimeline()
        {
            PlayableGraph graph = director.playableGraph;
            previousSpeeds = new double[graph.GetRootPlayableCount()];

            for (int i = 0; i < previousSpeeds.Length; i++)
            {
                Playable root = graph.GetRootPlayable(i);
                previousSpeeds[i] = root.GetSpeed();
                root.SetSpeed(0d);
            }
        }

        private void RestoreTimelineSpeed()
        {
            PlayableGraph graph = director.playableGraph;

            if (!graph.IsValid() || previousSpeeds == null)
            {
                return;
            }

            int count = Mathf.Min(graph.GetRootPlayableCount(), previousSpeeds.Length);

            for (int i = 0; i < count; i++)
            {
                graph.GetRootPlayable(i).SetSpeed(previousSpeeds[i]);
            }

            previousSpeeds = null;
        }

        private void OnDirectorStopped(PlayableDirector stoppedDirector)
        {
            RestoreTimelineSpeed();
            waitingForInput = false;
            input.UI.AdvanceDialogue.Disable();

            if (dialogueUI != null)
            {
                dialogueUI.Hide();
            }
        }

        private void OnDisable()
        {
            input.UI.AdvanceDialogue.performed -= OnAdvance;
            input.UI.AdvanceDialogue.Disable();

            // Stopping also lets SequenceController restore player control.
            if (waitingForInput)
            {
                director.Stop();
            }

            director.stopped -= OnDirectorStopped;
        }

        private void OnDestroy()
        {
            input.Dispose();
        }
    }
}