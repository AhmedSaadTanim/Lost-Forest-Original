using TMPro;
using UnityEngine;

namespace LostForest.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;

        private void Awake()
        {
            Hide();
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
            canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}