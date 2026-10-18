using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class PlayerStaminaTests : NateTestBase
    {
        PlayerStamina Make(float drain = 5f, float regen = 0f, float delay = 0.5f, float recover = 30f)
        {
            return NewComponent<PlayerStamina>("Stamina", s =>
            {
                TestReflection.Set(s, "max", 100f);
                TestReflection.Set(s, "drainPerSecond", drain);
                TestReflection.Set(s, "regenPerSecond", regen);
                TestReflection.Set(s, "regenDelay", delay);
                TestReflection.Set(s, "recoverThreshold", recover);
            });
        }

        [Test]
        public void StartsFull()
        {
            PlayerStamina s = Make();
            Assert.AreEqual(100f, s.Current);
            Assert.AreEqual(1f, s.Normalized);
            Assert.IsTrue(s.CanRun);
            Assert.IsFalse(s.IsExhausted);
        }

        [UnityTest]
        public IEnumerator DrainsWhileRunning()
        {
            PlayerStamina s = Make(drain: 5f);
            s.SetRunning(true);
            float elapsed = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Assert.That(s.Current, Is.EqualTo(100f - 5f * elapsed).Within(0.01f));
            Assert.Less(s.Current, 100f);
        }

        [UnityTest]
        public IEnumerator Regen_WaitsForDelay()
        {
            PlayerStamina s = Make(drain: 5f, regen: 50f, delay: 0.5f);
            s.Spend(50f);
            s.SetRunning(true);
            yield return null;
            s.SetRunning(false);
            float afterRun = s.Current;

            float elapsed = 0f;
            while (true)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed >= 0.5f) break;
                Assert.AreEqual(afterRun, s.Current, $"no regen before the delay (t={elapsed:F3})");
            }

            yield return new WaitForSeconds(0.2f);
            Assert.Greater(s.Current, afterRun);
        }

        [Test]
        public void HittingZero_FiresExhaustedOnce_AndBlocksRunning()
        {
            PlayerStamina s = Make();
            int exhausted = 0;
            s.Exhausted += () => exhausted++;

            s.Spend(60f);
            Assert.AreEqual(0, exhausted);
            s.Spend(60f);
            Assert.AreEqual(1, exhausted);
            Assert.AreEqual(0f, s.Current);
            Assert.IsTrue(s.IsExhausted);
            Assert.IsFalse(s.CanRun);

            s.Spend(10f);
            Assert.AreEqual(1, exhausted);
        }

        [Test]
        public void Recovered_FiresOnlyAtRecoverThreshold()
        {
            PlayerStamina s = Make(recover: 30f);
            int recovered = 0;
            s.Recovered += () => recovered++;

            s.Spend(100f);
            s.Restore(20f);
            Assert.AreEqual(0, recovered);
            Assert.IsTrue(s.IsExhausted);
            Assert.IsFalse(s.CanRun, "still exhausted below the recover threshold even though stamina > 0");

            s.Restore(15f);
            Assert.AreEqual(1, recovered);
            Assert.IsFalse(s.IsExhausted);
            Assert.IsTrue(s.CanRun);

            s.Restore(10f);
            Assert.AreEqual(1, recovered);
        }

        [Test]
        public void Recovered_DoesNotFireWhenNeverExhausted()
        {
            PlayerStamina s = Make();
            int recovered = 0;
            s.Recovered += () => recovered++;
            s.Spend(50f);
            s.Restore(40f);
            Assert.AreEqual(0, recovered);
        }

        [Test]
        public void Values_ClampToRange()
        {
            PlayerStamina s = Make();
            s.Restore(50f);
            Assert.AreEqual(100f, s.Current);
            s.Spend(500f);
            Assert.AreEqual(0f, s.Current);
            s.Restore(500f);
            Assert.AreEqual(100f, s.Current);
        }

        [Test]
        public void NegativeAmounts_AreIgnored()
        {
            PlayerStamina s = Make();
            var changes = new List<float>();
            s.Changed += (c, m) => changes.Add(c);
            s.Spend(-10f);
            s.Restore(-10f);
            Assert.AreEqual(100f, s.Current);
            Assert.AreEqual(0, changes.Count);
        }

        [Test]
        public void Changed_ReportsCurrentAndMax()
        {
            PlayerStamina s = Make();
            float cur = -1f, max = -1f;
            s.Changed += (c, m) => { cur = c; max = m; };
            s.Spend(25f);
            Assert.AreEqual(75f, cur);
            Assert.AreEqual(100f, max);
        }

        [UnityTest]
        public IEnumerator Exhausted_RecoversByRegenOverTime()
        {
            PlayerStamina s = Make(regen: 400f, delay: 0f, recover: 30f);
            int recovered = 0;
            s.Recovered += () => recovered++;
            s.Spend(100f);
            yield return WaitUntilOrTimeout(() => recovered > 0, 2f);
            Assert.AreEqual(1, recovered);
            Assert.IsTrue(s.CanRun);
        }
    }

    public class PlayerSanityTests : NateTestBase
    {
        PlayerSanity Make(float passive = 0f)
        {
            return NewComponent<PlayerSanity>("Sanity", s =>
            {
                TestReflection.Set(s, "max", 100f);
                TestReflection.Set(s, "passiveRegenPerSecond", passive);
            });
        }

        [Test]
        public void StartsFullAndStable()
        {
            PlayerSanity s = Make();
            Assert.AreEqual(100f, s.Current);
            Assert.AreEqual(SanityLevel.Stable, s.Level);
            Assert.IsFalse(s.IsDepleted);
        }

        [Test]
        public void DrainAndRestore_Clamp()
        {
            PlayerSanity s = Make();
            s.Drain(30f);
            Assert.AreEqual(70f, s.Current);
            s.Restore(500f);
            Assert.AreEqual(100f, s.Current);
            s.Drain(500f);
            Assert.AreEqual(0f, s.Current);
            Assert.IsTrue(s.IsDepleted);
        }

        [Test]
        public void Changed_FiresOnlyOnRealChange()
        {
            PlayerSanity s = Make();
            var changes = new List<float>();
            s.Changed += (c, m) => changes.Add(c);

            s.Restore(10f);       // already full
            s.Drain(0f);
            s.Drain(-5f);
            s.Restore(-5f);
            Assert.AreEqual(0, changes.Count);

            s.Drain(10f);
            CollectionAssert.AreEqual(new[] { 90f }, changes);

            s.Drain(1000f);
            s.Drain(10f);         // already empty
            CollectionAssert.AreEqual(new[] { 90f, 0f }, changes);
        }

        [Test]
        public void LevelChanged_AtEachThreshold()
        {
            PlayerSanity s = Make();
            var levels = new List<SanityLevel>();
            s.LevelChanged += levels.Add;

            s.Drain(30f); // 70 = exactly the Uneasy threshold, still Stable
            Assert.AreEqual(SanityLevel.Stable, s.Level);
            s.Drain(1f);  // 69
            s.Drain(30f); // 39
            s.Drain(25f); // 14
            s.Restore(100f);

            CollectionAssert.AreEqual(new[]
            {
                SanityLevel.Uneasy, SanityLevel.Panicked, SanityLevel.Breaking, SanityLevel.Stable
            }, levels);
        }

        [Test]
        public void LevelChanged_JumpingSeveralLevels_FiresOnce()
        {
            PlayerSanity s = Make();
            var levels = new List<SanityLevel>();
            s.LevelChanged += levels.Add;
            s.Drain(95f);
            CollectionAssert.AreEqual(new[] { SanityLevel.Breaking }, levels);
        }

        [Test]
        public void Depleted_FiresOnce_AndAgainOnlyAfterRestore()
        {
            PlayerSanity s = Make();
            int depleted = 0;
            s.Depleted += () => depleted++;

            s.Drain(100f);
            Assert.AreEqual(1, depleted);
            s.Drain(10f);
            Assert.AreEqual(1, depleted);

            s.Restore(1f);
            s.Drain(1f);
            Assert.AreEqual(2, depleted);
        }

        [UnityTest]
        public IEnumerator SetDrain_TwoSourcesAddUp()
        {
            PlayerSanity s = Make();
            object a = new object(), b = new object();
            s.SetDrain(a, 2f);
            s.SetDrain(b, 3f);
            float start = s.Current;
            float elapsed = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            float expected = 5f * elapsed;
            Assert.That(start - s.Current, Is.EqualTo(expected).Within(0.001f + expected * 0.01f));
        }

        [UnityTest]
        public IEnumerator SetDrain_SameSourceReplacesRate()
        {
            PlayerSanity s = Make();
            object a = new object();
            s.SetDrain(a, 50f);
            s.SetDrain(a, 2f);
            float start = s.Current;
            float elapsed = 0f;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Assert.That(start - s.Current, Is.EqualTo(2f * elapsed).Within(0.001f + 0.02f * elapsed));
        }

        [UnityTest]
        public IEnumerator ClearDrain_StopsIt()
        {
            PlayerSanity s = Make();
            object a = new object();
            s.SetDrain(a, 10f);
            yield return Frames(3);
            Assert.Less(s.Current, 100f);

            s.ClearDrain(a);
            float v = s.Current;
            yield return Frames(5);
            Assert.AreEqual(v, s.Current);
        }

        [UnityTest]
        public IEnumerator SetDrainZero_RemovesSource()
        {
            PlayerSanity s = Make();
            object a = new object();
            s.SetDrain(a, 10f);
            yield return Frames(2);
            s.SetDrain(a, 0f);
            float v = s.Current;
            yield return Frames(5);
            Assert.AreEqual(v, s.Current);
        }

        [UnityTest]
        public IEnumerator DestroyedSource_IsIgnored()
        {
            PlayerSanity s = Make();
            var source = new GameObject("DrainSource");
            s.SetDrain(source, 10f);
            yield return Frames(2);
            Assert.Less(s.Current, 100f);

            Object.DestroyImmediate(source);
            float v = s.Current;
            yield return Frames(5);
            Assert.AreEqual(v, s.Current);
        }

        [Test]
        public void NullSource_IsIgnored()
        {
            PlayerSanity s = Make();
            Assert.DoesNotThrow(() => s.SetDrain(null, 10f));
            Assert.DoesNotThrow(() => s.ClearDrain(null));
        }

        [UnityTest]
        public IEnumerator PassiveRegen_OnlyWhenNothingDrains()
        {
            PlayerSanity s = Make(passive: 20f);
            s.Drain(50f);
            yield return Frames(5);
            Assert.Greater(s.Current, 50f, "regenerates while nothing drains");

            object a = new object();
            s.SetDrain(a, 1f);
            yield return null;
            float v = s.Current;
            yield return Frames(5);
            Assert.Less(s.Current, v, "no regen while a drain is active");

            s.ClearDrain(a);
            v = s.Current;
            yield return Frames(5);
            Assert.Greater(s.Current, v, "regen resumes after the drain is cleared");
        }

        [UnityTest]
        public IEnumerator NoPassiveRegen_WhenZero()
        {
            PlayerSanity s = Make(passive: 0f);
            s.Drain(50f);
            yield return Frames(5);
            Assert.AreEqual(50f, s.Current);
        }
    }
}
