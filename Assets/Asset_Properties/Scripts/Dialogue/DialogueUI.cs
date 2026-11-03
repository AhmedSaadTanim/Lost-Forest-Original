using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LostForest.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject continuePrompt;
        [SerializeField] private TMP_Text continuePromptText;

        [Header("Typewriter")]
        [Min(1f)]
        [SerializeField] private float charactersPerSecond = 35f;
        
        private float revealedCharacters;
        private int totalCharacters;

        public bool IsTyping { get; private set; }
        
        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            if (!IsTyping)
            {
                return;
            }

            revealedCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            dialogueText.maxVisibleCharacters = Mathf.FloorToInt(revealedCharacters);

            if (revealedCharacters >= totalCharacters)
            {
                CompleteTyping();
            }
        }
        
        public void SetContinueButton(InputAction action, int bindingIndex)
        {
            string button = action.GetBindingDisplayString(bindingIndex);
            continuePromptText.text = $"Press [{button}] to continue";
        }
        
        public void ShowLine(DialogueLine line)
        {
            if (line == null)
            {
                Debug.LogWarning("Cannot display a missing dialogue line.", this);
                return;
            }

            speakerText.text = line.Speaker;
            dialogueText.text = line.Text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            totalCharacters = dialogueText.textInfo.characterCount;
            revealedCharacters = 0f;
            IsTyping = totalCharacters > 0;
            continuePrompt.SetActive(!IsTyping);

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void CompleteTyping()
        {
            IsTyping = false;
            revealedCharacters = totalCharacters;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            continuePrompt.SetActive(true);
        }
        
        public void Hide()
        {
            IsTyping = false;
            continuePrompt.SetActive(false);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        private void OnDisable()
        {
            Hide();
        }
    }
}