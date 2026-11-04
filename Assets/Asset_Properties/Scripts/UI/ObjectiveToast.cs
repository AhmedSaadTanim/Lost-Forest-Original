using System.Collections;
using TMPro;
using UnityEngine;

namespace LostForest.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ObjectiveToast : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField, Min(0f)] private float visibleSeconds = 4f;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 1f;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        public void ShowObjective(ObjectiveLine message)
        {
            if (!isActiveAndEnabled || objectiveText == null)
            {
                return;
            }

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            objectiveText.text = message.Text;
            canvasGroup.alpha = 1f;
            fadeRoutine = StartCoroutine(FadeAfterDelay());
        }

        private IEnumerator FadeAfterDelay()
        {
            yield return new WaitForSecondsRealtime(visibleSeconds);

            float elapsed = 0f;

            while (elapsed < fadeSeconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
            }

            canvasGroup.alpha = 0f;
            fadeRoutine = null;
        }

        private void OnDisable()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            canvasGroup.alpha = 0f;
        }
    }
}