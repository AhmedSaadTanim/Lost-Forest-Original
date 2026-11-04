using UnityEngine;

namespace LostForest.UI
{
    [CreateAssetMenu(fileName = "ObjectiveLine", menuName = "Lost Forest/UI/Line")]
    public class ObjectiveLine : ScriptableObject
    {
        [TextArea(2, 6)]
        [SerializeField] private string text;

        public string Text => text;
    }
}
