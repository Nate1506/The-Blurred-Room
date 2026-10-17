using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    public enum SanityLevel { Stable, Uneasy, Panicked, Breaking }

    /// <summary>
    /// Sanity 0..max. Other systems call Drain/Restore for one-off changes, or SetDrain/ClearDrain
    /// for continuous effects (e.g. an enemy in view). Reaching 0 raises Depleted.
    /// </summary>
    public class PlayerSanity : MonoBehaviour
    {
        [SerializeField] float max = 100f;
        [Tooltip("Recovered per second while nothing is draining. 0 = no passive recovery.")]
        [SerializeField] float passiveRegenPerSecond = 0f;
        [SerializeField, Range(0f, 1f)] float uneasyBelow = 0.7f;
        [SerializeField, Range(0f, 1f)] float panickedBelow = 0.4f;
        [SerializeField, Range(0f, 1f)] float breakingBelow = 0.15f;

        readonly Dictionary<object, float> drains = new Dictionary<object, float>();
        bool depletedRaised;

        public float Current { get; private set; }
        public float Max => max;
        public float Normalized => max > 0f ? Current / max : 0f;
        public SanityLevel Level { get; private set; }
        public bool IsDepleted => Current <= 0f;

        /// <summary>(current, max)</summary>
        public event Action<float, float> Changed;
        public event Action<SanityLevel> LevelChanged;
        public event Action Depleted;

        void Awake()
        {
            Current = max;
            Level = Evaluate();
        }

        void Update()
        {
            float perSecond = 0f;
            foreach (KeyValuePair<object, float> kv in drains)
            {
                if (kv.Key is UnityEngine.Object o && !o) continue; // source destroyed without clearing
                perSecond += kv.Value;
            }

            if (perSecond > 0f) Drain(perSecond * Time.deltaTime);
            else if (passiveRegenPerSecond > 0f) Restore(passiveRegenPerSecond * Time.deltaTime);
        }

        public void Drain(float amount)
        {
            if (amount > 0f) Set(Current - amount);
        }

        public void Restore(float amount)
        {
            if (amount > 0f) Set(Current + amount);
        }

        /// <summary>Continuous drain from a source, per second. Call ClearDrain when it stops.</summary>
        public void SetDrain(object source, float perSecond)
        {
            if (source == null) return;
            if (perSecond <= 0f) drains.Remove(source);
            else drains[source] = perSecond;
        }

        public void ClearDrain(object source)
        {
            if (source != null) drains.Remove(source);
        }

        void Set(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, max);
            if (Mathf.Approximately(clamped, Current)) return;
            Current = clamped;
            Changed?.Invoke(Current, max);

            SanityLevel level = Evaluate();
            if (level != Level)
            {
                Level = level;
                LevelChanged?.Invoke(level);
            }

            if (Current <= 0f && !depletedRaised)
            {
                depletedRaised = true;
                Depleted?.Invoke();
            }
            else if (Current > 0f)
            {
                depletedRaised = false;
            }
        }

        SanityLevel Evaluate()
        {
            float n = Normalized;
            if (n < breakingBelow) return SanityLevel.Breaking;
            if (n < panickedBelow) return SanityLevel.Panicked;
            if (n < uneasyBelow) return SanityLevel.Uneasy;
            return SanityLevel.Stable;
        }
    }
}
