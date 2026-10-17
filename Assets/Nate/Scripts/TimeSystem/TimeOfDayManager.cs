using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Day → Eve → Night → (next day) Day ... Auto-advances by duration, or call AdvancePhase / SetPhase
    /// from story events. Other systems subscribe to PhaseChanged / DayChanged.
    /// </summary>
    public class TimeOfDayManager : MonoBehaviour
    {
        public static TimeOfDayManager Instance { get; private set; }

        [SerializeField] DayPhase startPhase = DayPhase.Day;
        [SerializeField] int startDay = 1;
        [SerializeField] bool autoAdvance = true;
        [Tooltip("Real seconds per phase.")]
        [SerializeField] float dayDuration = 180f;
        [SerializeField] float eveDuration = 90f;
        [SerializeField] float nightDuration = 240f;

        float elapsed;

        public DayPhase Phase { get; private set; }
        public int DayNumber { get; private set; }
        public bool Paused { get; set; }
        public bool AutoAdvance { get => autoAdvance; set => autoAdvance = value; }
        /// <summary>0..1 through the current phase.</summary>
        public float PhaseProgress => Mathf.Clamp01(elapsed / Mathf.Max(0.01f, DurationOf(Phase)));

        public event Action<DayPhase> PhaseChanged;
        public event Action<int> DayChanged;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogWarning("[TimeOfDayManager] Duplicate found, destroying this one.", this);
                Destroy(this);
                return;
            }
            Instance = this;
            Phase = startPhase;
            DayNumber = Mathf.Max(1, startDay);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Paused || !autoAdvance) return;
            elapsed += Time.deltaTime;
            if (elapsed >= DurationOf(Phase)) AdvancePhase();
        }

        public void AdvancePhase()
        {
            if (Phase == DayPhase.Night)
            {
                DayNumber++;
                SetPhase(DayPhase.Day);
                DayChanged?.Invoke(DayNumber);
            }
            else
            {
                SetPhase(Phase + 1);
            }
        }

        public void SetPhase(DayPhase phase)
        {
            elapsed = 0f;
            if (phase == Phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        public float DurationOf(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Day: return dayDuration;
                case DayPhase.Eve: return eveDuration;
                default: return nightDuration;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
