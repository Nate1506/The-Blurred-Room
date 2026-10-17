using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Hold the Ritual key while looking at an IRitualTarget to perform it.</summary>
    public class PlayerRitual : MonoBehaviour
    {
        [SerializeField] PlayerInputSource input;
        [SerializeField] PlayerSight sight;
        [SerializeField] bool lockMovementWhilePerforming = true;

        IRitualTarget current;
        float progress;

        public bool IsPerforming => current != null;
        public float Progress => progress;
        public IRitualTarget Focus { get; private set; }

        public event Action<IRitualTarget> FocusChanged;
        public event Action<IRitualTarget> Started;
        public event Action<float> ProgressChanged;
        public event Action<IRitualTarget> Completed;
        public event Action<IRitualTarget> Cancelled;

        void Awake()
        {
            if (!input) input = GetComponentInParent<PlayerInputSource>();
            if (!sight) sight = this.FindInRoot<PlayerSight>();
        }

        void OnDisable()
        {
            if (current != null) Cancel();
        }

        void Update()
        {
            IRitualTarget target = sight ? sight.Get<IRitualTarget>() : null;
            if (target != null && !target.CanPerformRitual(gameObject)) target = null;
            if (!ReferenceEquals(target, Focus))
            {
                Focus = target;
                FocusChanged?.Invoke(Focus);
            }

            bool holding = input && input.RitualHeld && target != null
                           && (current == null || ReferenceEquals(target, current));

            if (!holding)
            {
                if (current != null) Cancel();
                return;
            }

            if (current == null)
            {
                current = target;
                progress = 0f;
                if (lockMovementWhilePerforming && input) input.LockMovement(this);
                Started?.Invoke(current);
            }

            progress = Mathf.Clamp01(progress + Time.deltaTime / Mathf.Max(0.01f, current.RitualDuration));
            current.OnRitualProgress(progress);
            ProgressChanged?.Invoke(progress);

            if (progress >= 1f)
            {
                IRitualTarget done = current;
                Clear();
                done.OnRitualCompleted(gameObject);
                Completed?.Invoke(done);
            }
        }

        void Cancel()
        {
            IRitualTarget t = current;
            Clear();
            t.OnRitualCancelled();
            Cancelled?.Invoke(t);
        }

        void Clear()
        {
            current = null;
            progress = 0f;
            if (input) input.UnlockMovement(this);
        }
    }
}
