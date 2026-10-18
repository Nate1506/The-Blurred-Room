using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class TimeOfDayManagerTests : NateTestBase
    {
        readonly List<DayPhase> phases = new List<DayPhase>();
        readonly List<int> days = new List<int>();

        TimeOfDayManager Make(bool autoAdvance = false, float duration = 100f, DayPhase start = DayPhase.Day, int startDay = 1)
        {
            phases.Clear();
            days.Clear();
            var t = NewComponent<TimeOfDayManager>("Time", m =>
            {
                TestReflection.Set(m, "autoAdvance", autoAdvance);
                TestReflection.Set(m, "startPhase", start);
                TestReflection.Set(m, "startDay", startDay);
                TestReflection.Set(m, "dayDuration", duration);
                TestReflection.Set(m, "eveDuration", duration);
                TestReflection.Set(m, "nightDuration", duration);
            });
            t.PhaseChanged += phases.Add;
            t.DayChanged += days.Add;
            return t;
        }

        [Test]
        public void Awake_SetsInstanceAndStartValues()
        {
            TimeOfDayManager t = Make(start: DayPhase.Eve, startDay: 3);
            Assert.AreSame(t, TimeOfDayManager.Instance);
            Assert.AreEqual(DayPhase.Eve, t.Phase);
            Assert.AreEqual(3, t.DayNumber);
        }

        [Test]
        public void StartDayBelowOne_IsClampedToOne()
        {
            Assert.AreEqual(1, Make(startDay: 0).DayNumber);
        }

        [Test]
        public void AdvancePhase_DayEveNightDay_IncrementsDay()
        {
            TimeOfDayManager t = Make();
            t.AdvancePhase();
            Assert.AreEqual(DayPhase.Eve, t.Phase);
            t.AdvancePhase();
            Assert.AreEqual(DayPhase.Night, t.Phase);
            Assert.AreEqual(1, t.DayNumber);
            Assert.AreEqual(0, days.Count);

            t.AdvancePhase();
            Assert.AreEqual(DayPhase.Day, t.Phase);
            Assert.AreEqual(2, t.DayNumber);
            CollectionAssert.AreEqual(new[] { DayPhase.Eve, DayPhase.Night, DayPhase.Day }, phases);
            CollectionAssert.AreEqual(new[] { 2 }, days);
        }

        [Test]
        public void DayChanged_FiresEachFullCycle()
        {
            TimeOfDayManager t = Make();
            for (int i = 0; i < 9; i++) t.AdvancePhase();
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, days);
            Assert.AreEqual(4, t.DayNumber);
        }

        [Test]
        public void SetPhase_SamePhase_NoEvent()
        {
            TimeOfDayManager t = Make();
            t.SetPhase(DayPhase.Day);
            Assert.AreEqual(0, phases.Count);
            t.SetPhase(DayPhase.Night);
            t.SetPhase(DayPhase.Night);
            CollectionAssert.AreEqual(new[] { DayPhase.Night }, phases);
            Assert.AreEqual(1, t.DayNumber, "SetPhase never changes the day");
        }

        [UnityTest]
        public IEnumerator AutoAdvance_ByDuration()
        {
            TimeOfDayManager t = Make(autoAdvance: true, duration: 0.4f);
            yield return null;
            Assert.AreEqual(DayPhase.Day, t.Phase);
            Assert.That(t.PhaseProgress, Is.GreaterThan(0f).And.LessThan(1f));

            yield return WaitUntilOrTimeout(() => t.Phase == DayPhase.Eve, 2f);
            Assert.AreEqual(DayPhase.Eve, t.Phase);

            yield return WaitUntilOrTimeout(() => t.DayNumber == 2, 3f);
            Assert.AreEqual(2, t.DayNumber);
            Assert.AreEqual(DayPhase.Day, t.Phase);
            CollectionAssert.AreEqual(new[] { 2 }, days);
        }

        [UnityTest]
        public IEnumerator AutoAdvance_UsesEachPhaseDuration()
        {
            TimeOfDayManager t = Make(autoAdvance: true, duration: 100f);
            TestReflection.Set(t, "dayDuration", 0.1f);
            yield return WaitUntilOrTimeout(() => t.Phase == DayPhase.Eve, 2f);
            Assert.AreEqual(DayPhase.Eve, t.Phase);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(DayPhase.Eve, t.Phase, "the long Eve duration is respected");
        }

        [UnityTest]
        public IEnumerator Paused_StopsAutoAdvance()
        {
            TimeOfDayManager t = Make(autoAdvance: true, duration: 0.1f);
            t.Paused = true;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(DayPhase.Day, t.Phase);
            Assert.AreEqual(0, phases.Count);

            t.Paused = false;
            yield return WaitUntilOrTimeout(() => phases.Count > 0, 2f);
            Assert.AreEqual(DayPhase.Eve, t.Phase);
        }

        [UnityTest]
        public IEnumerator AutoAdvanceFalse_StopsIt()
        {
            TimeOfDayManager t = Make(autoAdvance: true, duration: 0.1f);
            t.AutoAdvance = false;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(DayPhase.Day, t.Phase);
            Assert.AreEqual(0, phases.Count);
        }

        [UnityTest]
        public IEnumerator SetPhase_ResetsPhaseTimer()
        {
            TimeOfDayManager t = Make(autoAdvance: true, duration: 0.5f);
            yield return new WaitForSeconds(0.3f);
            t.SetPhase(DayPhase.Day);
            Assert.AreEqual(0f, t.PhaseProgress);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(DayPhase.Day, t.Phase, "timer restarted, so no advance yet");
        }

        [UnityTest]
        public IEnumerator DuplicateManager_IsDestroyed()
        {
            TimeOfDayManager first = Make();
            LogAssert.Expect(LogType.Warning, new Regex(@"\[TimeOfDayManager\] Duplicate"));
            var second = NewGO("Time2").AddComponent<TimeOfDayManager>();
            yield return null;

            Assert.IsTrue(second == null, "the second manager is destroyed");
            Assert.AreSame(first, TimeOfDayManager.Instance);
            Assert.IsTrue(first != null);
        }

        [Test]
        public void DestroyingInstance_ClearsInstance()
        {
            TimeOfDayManager t = Make();
            Object.DestroyImmediate(t.gameObject);
            Assert.IsTrue(TimeOfDayManager.Instance == null);
        }
    }
}
