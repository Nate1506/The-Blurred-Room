using System;
using UnityEngine;

namespace BlurredRoom
{
    public enum NpcKind { Neutral, Enemy }

    /// <summary>
    /// Base for all NPCs. Handles registry, which time phases it is active in, and door-visit callbacks.
    /// </summary>
    public abstract class NpcBase : MonoBehaviour
    {
        [SerializeField] string npcId = "npc";
        [SerializeField] string displayName = "Someone";
        [Tooltip("Phases in which this NPC is present/active.")]
        [SerializeField] DayPhaseMask activePhases = DayPhaseMask.All;
        [Tooltip("Can the DoorVisitScheduler send this NPC to knock on doors?")]
        [SerializeField] bool canBeVisitor = true;
        [Tooltip("Shown/hidden when the NPC becomes active/inactive. Leave empty to handle it yourself.")]
        [SerializeField] GameObject visualRoot;

        TimeOfDayManager time;
        bool activeApplied;

        public abstract NpcKind Kind { get; }
        public string NpcId => npcId;
        public string DisplayName => displayName;
        public bool CanBeVisitor => canBeVisitor;
        public bool IsActive { get; private set; }
        /// <summary>Door this NPC is currently knocking on (null if none).</summary>
        public Door VisitingDoor { get; internal set; }
        /// <summary>Busy NPCs are not picked as visitors (e.g. an enemy mid-chase).</summary>
        public virtual bool IsBusy => false;

        public event Action<bool> ActiveChanged;

        protected virtual void Awake() { }

        protected virtual void OnEnable() => NpcRegistry.Register(this);
        protected virtual void OnDisable() => NpcRegistry.Unregister(this);

        protected virtual void Start()
        {
            time = TimeOfDayManager.Instance;
            if (time)
            {
                time.PhaseChanged += HandlePhaseChanged;
                HandlePhaseChanged(time.Phase);
            }
            else
            {
                SetActiveState(true);
            }
        }

        protected virtual void OnDestroy()
        {
            if (time) time.PhaseChanged -= HandlePhaseChanged;
        }

        void HandlePhaseChanged(DayPhase phase) => SetActiveState(activePhases.Includes(phase));

        public void SetActiveState(bool active)
        {
            if (activeApplied && IsActive == active) return;
            activeApplied = true;
            IsActive = active;
            if (visualRoot) visualRoot.SetActive(active);
            OnActiveChanged(active);
            ActiveChanged?.Invoke(active);
        }

        /// <summary>Teleport here and become active (used when a door is opened to a visitor).</summary>
        public virtual void Appear(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            SetActiveState(true);
        }

        protected virtual void OnActiveChanged(bool active) { }

        /// <summary>Called by Door when the player opens the door or ignores the knocking.</summary>
        public virtual void OnDoorAnswered(Door door, DoorAnswer answer) { }
    }
}
