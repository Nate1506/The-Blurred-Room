using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class ClueManagerInstanceTests : NateTestBase
    {
        [Test]
        public void Awake_SetsInstance()
        {
            ClueManager c = NewComponent<ClueManager>("Clues");
            Assert.AreSame(c, ClueManager.Instance);
        }

        [UnityTest]
        public IEnumerator Duplicate_IsDestroyed()
        {
            ClueManager first = NewComponent<ClueManager>("Clues");
            LogAssert.Expect(LogType.Warning, new Regex(@"\[ClueManager\] Duplicate"));
            ClueManager second = NewComponent<ClueManager>("Clues2");
            yield return null;
            Assert.IsTrue(second == null);
            Assert.AreSame(first, ClueManager.Instance);
        }
    }

    public class EndingManagerTests : NateTestBase
    {
        EndingManager Make(params EndingData[] endings)
        {
            foreach (EndingData e in endings) if (e) Track(e);
            return NewComponent<EndingManager>("Endings", m => TestReflection.Set(m, "endings", endings));
        }

        static EndingData E(string id, string reason, int priority = 0, ClueData[] req = null)
            => TestAssets.Ending(id, reason, priority, req);

        [Test]
        public void HighestPriorityWins()
        {
            EndingData low = E("low", EndingReasons.SanityLost, 1);
            EndingData high = E("high", EndingReasons.SanityLost, 5);
            EndingData other = E("other", EndingReasons.Caught, 10);
            EndingManager m = Make(low, high, other);
            var reached = new List<string>();
            m.EndingReached += (e, r) => reached.Add(e.Id + ":" + r);

            Assert.IsTrue(m.TryEnd(EndingReasons.SanityLost));
            Assert.AreSame(high, m.Current);
            Assert.IsTrue(m.HasEnded);
            CollectionAssert.AreEqual(new[] { "high:SanityLost" }, reached);
        }

        [Test]
        public void HigherPriorityWithUnmetRequirements_IsSkipped()
        {
            NewComponent<ClueManager>("Clues");
            EndingData plain = E("plain", EndingReasons.Solved, 0);
            EndingData good = E("good", EndingReasons.Solved, 10, new[] { Track(TestAssets.Clue("truth")) });
            EndingManager m = Make(plain, good);
            Assert.AreSame(plain, m.Evaluate(EndingReasons.Solved));
        }

        [Test]
        public void EqualPriority_FirstInListWins()
        {
            EndingData a = E("a", EndingReasons.TimeUp, 3);
            EndingData b = E("b", EndingReasons.TimeUp, 3);
            Assert.AreSame(a, Make(a, b).Evaluate(EndingReasons.TimeUp));
        }

        [Test]
        public void NullEntries_AreSkipped()
        {
            EndingData a = E("a", EndingReasons.Caught);
            Assert.AreSame(a, Make(null, a, null).Evaluate(EndingReasons.Caught));
        }

        [Test]
        public void Evaluate_DoesNotEndTheGame()
        {
            EndingManager m = Make(E("a", EndingReasons.Caught));
            Assert.IsNotNull(m.Evaluate(EndingReasons.Caught));
            Assert.IsFalse(m.HasEnded);
            Assert.IsNull(m.Current);
        }

        [Test]
        public void TryEnd_OnlyWorksOnce()
        {
            EndingData a = E("a", EndingReasons.Caught);
            EndingData b = E("b", EndingReasons.SanityLost);
            EndingManager m = Make(a, b);
            int reached = 0;
            m.EndingReached += (e, r) => reached++;

            Assert.IsTrue(m.TryEnd(EndingReasons.Caught));
            Assert.IsFalse(m.TryEnd(EndingReasons.SanityLost));
            Assert.IsFalse(m.TryEnd(EndingReasons.Caught));
            Assert.AreSame(a, m.Current);
            Assert.AreEqual(1, reached);
        }

        [Test]
        public void TryEnd_NoMatch_ReturnsFalseAndWarns()
        {
            EndingManager m = Make(E("a", EndingReasons.Caught));
            int reached = 0;
            m.EndingReached += (e, r) => reached++;
            LogAssert.Expect(LogType.Warning, new Regex(@"\[EndingManager\] No ending matches reason 'Nope'"));

            Assert.IsFalse(m.TryEnd("Nope"));
            Assert.IsFalse(m.HasEnded);
            Assert.AreEqual(0, reached);

            Assert.IsTrue(m.TryEnd(EndingReasons.Caught), "a later matching reason still works");
        }

        [UnityTest]
        public IEnumerator Duplicate_IsDestroyed()
        {
            EndingManager first = Make();
            LogAssert.Expect(LogType.Warning, new Regex(@"\[EndingManager\] Duplicate"));
            EndingManager second = NewComponent<EndingManager>("Endings2");
            yield return null;
            Assert.IsTrue(second == null);
            Assert.AreSame(first, EndingManager.Instance);
        }
    }

    public class SystemHubTests : NateTestBase
    {
        TestPlayer player;
        TimeOfDayManager time;
        EndingManager endings;
        EndingData sanityEnding, caughtEnding, timeUpEnding, solvedEnding;

        IEnumerator Setup(int lastDay = 0, bool lockPlayer = true, bool withEndingManager = true)
        {
            player = NewPlayer(Vector3.zero);
            time = NewComponent<TimeOfDayManager>("Time", t => TestReflection.Set(t, "autoAdvance", false));

            sanityEnding = Track(TestAssets.Ending("insane", EndingReasons.SanityLost));
            caughtEnding = Track(TestAssets.Ending("caught", EndingReasons.Caught));
            timeUpEnding = Track(TestAssets.Ending("timeup", EndingReasons.TimeUp));
            solvedEnding = Track(TestAssets.Ending("solved", EndingReasons.Solved));
            if (withEndingManager)
            {
                endings = NewComponent<EndingManager>("Endings", m => TestReflection.Set(m, "endings",
                    new[] { sanityEnding, caughtEnding, timeUpEnding, solvedEnding }));
            }

            NewComponent<SystemHub>("SystemHub", h =>
            {
                TestReflection.Set(h, "playerInput", player.Input);
                TestReflection.Set(h, "playerSanity", player.Sanity);
                TestReflection.Set(h, "lastDay", lastDay);
                TestReflection.Set(h, "lockPlayerOnEnding", lockPlayer);
            });
            yield return null; // SystemHub.Start
        }

        void AssertEndedWith(EndingData ending)
        {
            Assert.AreSame(ending, endings.Current);
            Assert.IsTrue(player.Input.MovementLocked, "player locked on ending");
            Assert.IsTrue(time.Paused, "time paused on ending");
        }

        [UnityTest]
        public IEnumerator SanityZero_EndsWithSanityLost()
        {
            yield return Setup();
            Assert.IsFalse(endings.HasEnded);
            player.Sanity.Drain(1000f);
            AssertEndedWith(sanityEnding);
        }

        [UnityTest]
        public IEnumerator PlayerCaught_EndsWithCaught()
        {
            yield return Setup();
            TestStatics.RaisePlayerCaught(null);
            AssertEndedWith(caughtEnding);
        }

        [UnityTest]
        public IEnumerator PassingLastDay_EndsWithTimeUp()
        {
            yield return Setup(lastDay: 2);
            for (int i = 0; i < 3; i++) time.AdvancePhase(); // -> day 2
            Assert.AreEqual(2, time.DayNumber);
            Assert.IsFalse(endings.HasEnded, "the last day itself is still playable");

            for (int i = 0; i < 3; i++) time.AdvancePhase(); // -> day 3
            AssertEndedWith(timeUpEnding);
        }

        [UnityTest]
        public IEnumerator LastDayZero_MeansNoLimit()
        {
            yield return Setup(lastDay: 0);
            for (int i = 0; i < 30; i++) time.AdvancePhase();
            Assert.IsFalse(endings.HasEnded);
        }

        [UnityTest]
        public IEnumerator AnyEnding_LocksPlayerAndPausesTime()
        {
            yield return Setup();
            endings.TryEnd(EndingReasons.Solved);
            AssertEndedWith(solvedEnding);
        }

        [UnityTest]
        public IEnumerator LockPlayerOnEndingFalse_DoesNotLock()
        {
            yield return Setup(lockPlayer: false);
            player.Sanity.Drain(1000f);
            Assert.AreSame(sanityEnding, endings.Current);
            Assert.IsFalse(player.Input.MovementLocked);
            Assert.IsTrue(time.Paused);
        }

        [UnityTest]
        public IEnumerator FirstEndingWins()
        {
            yield return Setup();
            player.Sanity.Drain(1000f);
            TestStatics.RaisePlayerCaught(null);
            Assert.AreSame(sanityEnding, endings.Current);
        }

        [UnityTest]
        public IEnumerator WithoutEndingManager_NothingBreaks()
        {
            yield return Setup(withEndingManager: false);
            Assert.DoesNotThrow(() => player.Sanity.Drain(1000f));
            Assert.DoesNotThrow(() => TestStatics.RaisePlayerCaught(null));
            Assert.IsFalse(player.Input.MovementLocked);
        }

        [UnityTest]
        public IEnumerator Destroyed_Unsubscribes()
        {
            yield return Setup();
            Object.DestroyImmediate(Object.FindAnyObjectByType<SystemHub>().gameObject);
            player.Sanity.Drain(1000f);
            TestStatics.RaisePlayerCaught(null);
            Assert.IsFalse(endings.HasEnded);
        }
    }
}
