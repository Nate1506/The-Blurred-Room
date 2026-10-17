using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Common reasons the game can end. Endings match on these (any custom string also works).</summary>
    public static class EndingReasons
    {
        public const string SanityLost = "SanityLost";
        public const string Caught = "Caught";
        public const string TimeUp = "TimeUp";
        public const string Solved = "Solved";
    }

    [CreateAssetMenu(fileName = "Ending", menuName = "Blurred Room/Ending")]
    public class EndingData : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] string title;
        [SerializeField, TextArea(3, 10)] string text;

        [Header("When")]
        [Tooltip("Reason that triggers this ending, e.g. SanityLost, Caught, TimeUp, Solved.")]
        [SerializeField] string triggerReason = EndingReasons.Solved;
        [Tooltip("Higher wins when several endings match.")]
        [SerializeField] int priority;

        [Header("Requirements")]
        [SerializeField] ClueData[] requiredClues = new ClueData[0];
        [SerializeField] int minimumClueCount;
        [SerializeField] string[] requiredFlags = new string[0];
        [SerializeField] string[] forbiddenFlags = new string[0];

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string Title => title;
        public string Text => text;
        public string TriggerReason => triggerReason;
        public int Priority => priority;

        public bool Matches(string reason, ClueManager clues)
        {
            if (!string.Equals(reason, triggerReason, System.StringComparison.OrdinalIgnoreCase)) return false;

            bool hasRequirements = requiredClues.Length > 0 || minimumClueCount > 0 || requiredFlags.Length > 0;
            if (!clues) return !hasRequirements;

            if (clues.ClueCount < minimumClueCount) return false;
            foreach (ClueData c in requiredClues) if (c && !clues.HasClue(c)) return false;
            foreach (string f in requiredFlags) if (!clues.HasFlag(f)) return false;
            foreach (string f in forbiddenFlags) if (clues.HasFlag(f)) return false;
            return true;
        }
    }
}
