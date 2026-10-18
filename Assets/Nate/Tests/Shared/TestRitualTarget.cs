using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom.Tests
{
    public class TestRitualTarget : MonoBehaviour, IRitualTarget
    {
        public float duration = 0.3f;
        public bool canPerform = true;
        public readonly List<float> progress = new List<float>();
        public int completedCount;
        public int cancelledCount;

        public string Prompt => "Test ritual";
        public float RitualDuration => duration;
        public bool CanPerformRitual(GameObject performer) => canPerform;
        public void OnRitualProgress(float normalized) => progress.Add(normalized);
        public void OnRitualCompleted(GameObject performer) => completedCount++;
        public void OnRitualCancelled() => cancelledCount++;
    }
}
