using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Call TryEnd(reason). Picks the highest-priority EndingData that matches the reason + requirements.
    /// The ending screen/cutscene listens to EndingReached.
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        public static EndingManager Instance { get; private set; }

        [SerializeField] EndingData[] endings = new EndingData[0];

        public bool HasEnded => Current != null;
        public EndingData Current { get; private set; }

        /// <summary>(ending, reason)</summary>
        public event Action<EndingData, string> EndingReached;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning("[EndingManager] Duplicate found, destroying this one.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Returns the ending that would play for this reason right now (or null).</summary>
        public EndingData Evaluate(string reason)
        {
            EndingData best = null;
            ClueManager clues = ClueManager.Instance;
            foreach (EndingData e in endings)
            {
                if (!e || !e.Matches(reason, clues)) continue;
                if (best == null || e.Priority > best.Priority) best = e;
            }
            return best;
        }

        public bool TryEnd(string reason)
        {
            if (HasEnded) return false;
            EndingData ending = Evaluate(reason);
            if (!ending)
            {
                Debug.LogWarning($"[EndingManager] No ending matches reason '{reason}'.", this);
                return false;
            }
            Current = ending;
            EndingReached?.Invoke(ending, reason);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
