using System;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>Drains while running, regenerates after a short delay. Hitting 0 = exhausted until it recovers.</summary>
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] float max = 100f;
        [SerializeField] float drainPerSecond = 18f;
        [SerializeField] float regenPerSecond = 14f;
        [SerializeField] float regenDelay = 1f;
        [Tooltip("When exhausted, the player can run again only after stamina is back above this value.")]
        [SerializeField] float recoverThreshold = 30f;

        bool running;
        float regenTimer;

        public float Current { get; private set; }
        public float Max => max;
        public float Normalized => max > 0f ? Current / max : 0f;
        public bool IsExhausted { get; private set; }
        public bool CanRun => !IsExhausted && Current > 0f;

        /// <summary>(current, max)</summary>
        public event Action<float, float> Changed;
        public event Action Exhausted;
        public event Action Recovered;

        void Awake() => Current = max;

        /// <summary>Called by PlayerMovement each frame.</summary>
        public void SetRunning(bool isRunning) => running = isRunning;

        void Update()
        {
            float dt = Time.deltaTime;
            if (running)
            {
                regenTimer = regenDelay;
                Spend(drainPerSecond * dt);
            }
            else if (regenTimer > 0f)
            {
                regenTimer -= dt;
            }
            else if (Current < max)
            {
                Restore(regenPerSecond * dt);
            }
        }

        public void Spend(float amount)
        {
            if (amount <= 0f || Current <= 0f) return;
            Set(Current - amount);
            if (Current <= 0f && !IsExhausted)
            {
                IsExhausted = true;
                Exhausted?.Invoke();
            }
        }

        public void Restore(float amount)
        {
            if (amount <= 0f || Current >= max) return;
            Set(Current + amount);
            if (IsExhausted && Current >= recoverThreshold)
            {
                IsExhausted = false;
                Recovered?.Invoke();
            }
        }

        void Set(float value)
        {
            Current = Mathf.Clamp(value, 0f, max);
            Changed?.Invoke(Current, max);
        }
    }
}
