using UnityEngine;

namespace LostForest.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueLine", menuName = "Lost Forest/Dialogue/Line")]
    public class DialogueLine : ScriptableObject
    {
        [SerializeField] private string speaker;

        [TextArea(2, 6)]
        [SerializeField] private string text;

        public string Speaker => speaker;
        public string Text => text;
    }
}