using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Tracks discovered clues and story flags (e.g. "ritual_done", "met_landlady"). Endings read from here.</summary>
    public class ClueManager : MonoBehaviour
    {
        public static ClueManager Instance { get; private set; }

        readonly List<ClueData> discovered = new List<ClueData>();
        readonly HashSet<string> clueIds = new HashSet<string>();
        readonly HashSet<string> flags = new HashSet<string>();

        public IReadOnlyList<ClueData> Discovered => discovered;
        public int ClueCount => discovered.Count;

        public event Action<ClueData> ClueDiscovered;
        /// <summary>(flag, value)</summary>
        public event Action<string, bool> FlagChanged;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning("[ClueManager] Duplicate found, destroying this one.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Returns true if this clue is new.</summary>
        public bool Discover(ClueData clue)
        {
            if (!clue || !clueIds.Add(clue.Id)) return false;
            discovered.Add(clue);
            ClueDiscovered?.Invoke(clue);
            return true;
        }

        public bool HasClue(string clueId) => !string.IsNullOrEmpty(clueId) && clueIds.Contains(clueId);
        public bool HasClue(ClueData clue) => clue && clueIds.Contains(clue.Id);

        public void SetFlag(string flag, bool value = true)
        {
            if (string.IsNullOrEmpty(flag)) return;
            bool changed = value ? flags.Add(flag) : flags.Remove(flag);
            if (changed) FlagChanged?.Invoke(flag, value);
        }

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

        public void ResetAll()
        {
            discovered.Clear();
            clueIds.Clear();
            flags.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
