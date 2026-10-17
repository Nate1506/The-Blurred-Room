using UnityEngine;

namespace BlurredRoom
{
    [CreateAssetMenu(fileName = "Clue", menuName = "Blurred Room/Clue")]
    public class ClueData : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] string title;
        [SerializeField, TextArea(3, 8)] string description;
        [SerializeField] Sprite image;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => title;
        public string Description => description;
        public Sprite Image => image;
    }
}
