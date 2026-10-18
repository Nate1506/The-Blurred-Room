using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class NpcBaseTests : NateTestBase
    {
        GameObject visual;

        TestNpc MakeNpc(DayPhaseMask phases = DayPhaseMask.All, bool withVisual = true, string id = "npc")
        {
            return NewComponent<TestNpc>("Npc", n =>
            {
                TestReflection.Set(n, "npcId", id);
                TestReflection.Set(n, "activePhases", phases);
                if (withVisual)
                {
                    visual = new GameObject("Visual");
                    visual.transform.SetParent(n.transform, false);
                    TestReflection.Set(n, "visualRoot", visual);
                }
            });
        }

        TimeOfDayManager MakeTime(DayPhase start = DayPhase.Day)
        {
            return NewComponent<TimeOfDayManager>("Time", t =>
            {
                TestReflection.Set(t, "autoAdvance", false);
                TestReflection.Set(t, "startPhase", start);
            });
        }

        [Test]
        public void RegistersOnEnable_UnregistersOnDisable()
        {
            TestNpc npc = MakeNpc();
            CollectionAssert.Contains(NpcRegistry.All, npc);
            npc.enabled = false;
            CollectionAssert.DoesNotContain(NpcRegistry.All, npc);
            npc.enabled = true;
            CollectionAssert.Contains(NpcRegistry.All, npc);
            npc.gameObject.SetActive(false);
            CollectionAssert.DoesNotContain(NpcRegistry.All, npc);
        }

        [Test]
        public void Destroyed_IsUnregistered()
        {
            TestNpc npc = MakeNpc();
            Object.DestroyImmediate(npc.gameObject);
            Assert.AreEqual(0, NpcRegistry.All.Count);
        }

        [UnityTest]
        public IEnumerator NoTimeManager_IsActive()
        {
            TestNpc npc = MakeNpc(DayPhaseMask.Night);
            yield return null;
            Assert.IsTrue(npc.IsActive);
            Assert.IsTrue(visual.activeSelf);
        }

        [UnityTest]
        public IEnumerator ActiveState_FollowsActivePhases()
        {
            TimeOfDayManager time = MakeTime(DayPhase.Day);
            TestNpc npc = MakeNpc(DayPhaseMask.Eve | DayPhaseMask.Night);
            var events = new List<bool>();
            npc.ActiveChanged += events.Add;
            yield return null;

            Assert.IsFalse(npc.IsActive);
            Assert.IsFalse(visual.activeSelf);

            time.SetPhase(DayPhase.Eve);
            Assert.IsTrue(npc.IsActive);
            Assert.IsTrue(visual.activeSelf);

            time.SetPhase(DayPhase.Night);
            Assert.IsTrue(npc.IsActive);

            time.AdvancePhase(); // -> Day
            Assert.IsFalse(npc.IsActive);
            Assert.IsFalse(visual.activeSelf);

            CollectionAssert.AreEqual(new[] { false, true, false }, events);
        }

        [UnityTest]
        public IEnumerator ActiveAtStart_WhenPhaseIncluded()
        {
            MakeTime(DayPhase.Night);
            TestNpc npc = MakeNpc(DayPhaseMask.Night);
            yield return null;
            Assert.IsTrue(npc.IsActive);
            Assert.IsTrue(visual.activeSelf);
        }

        [UnityTest]
        public IEnumerator SetActiveState_ShowsAndHidesVisualRoot()
        {
            TestNpc npc = MakeNpc();
            yield return null;
            int events = 0;
            npc.ActiveChanged += _ => events++;

            npc.SetActiveState(false);
            Assert.IsFalse(visual.activeSelf);
            npc.SetActiveState(false);
            Assert.AreEqual(1, events, "no event when the state doesn't change");
            npc.SetActiveState(true);
            Assert.IsTrue(visual.activeSelf);
            Assert.AreEqual(2, events);
            Assert.IsTrue(npc.enabled, "visualRoot is separate from the NPC component");
        }

        [UnityTest]
        public IEnumerator Appear_MovesAndActivates()
        {
            TestNpc npc = MakeNpc();
            yield return null;
            npc.SetActiveState(false);

            Vector3 pos = new Vector3(3f, 0f, -2f);
            Quaternion rot = Quaternion.Euler(0f, 45f, 0f);
            npc.Appear(pos, rot);

            Assert.That(Vector3.Distance(pos, npc.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rot, npc.transform.rotation), Is.LessThan(0.01f));
            Assert.IsTrue(npc.IsActive);
            Assert.IsTrue(visual.activeSelf);
        }

        [UnityTest]
        public IEnumerator Destroyed_UnsubscribesFromTimeManager()
        {
            TimeOfDayManager time = MakeTime();
            TestNpc npc = MakeNpc(DayPhaseMask.Night);
            yield return null;
            Object.DestroyImmediate(npc.gameObject);
            Assert.DoesNotThrow(() => time.SetPhase(DayPhase.Night));
        }
    }

    public class NpcRegistryTests : NateTestBase
    {
        TestNpc Make(string id, NpcKind kind)
        {
            return NewComponent<TestNpc>("Npc_" + id, n =>
            {
                n.kind = kind;
                TestReflection.Set(n, "npcId", id);
            });
        }

        [Test]
        public void Find_ById()
        {
            TestNpc a = Make("landlady", NpcKind.Neutral);
            TestNpc b = Make("shadow", NpcKind.Enemy);
            Assert.AreSame(a, NpcRegistry.Find("landlady"));
            Assert.AreSame(b, NpcRegistry.Find("shadow"));
            Assert.IsNull(NpcRegistry.Find("nobody"));
            Assert.IsNull(NpcRegistry.Find(null));
        }

        [Test]
        public void GetAll_ByKind_ClearsResultList()
        {
            TestNpc n1 = Make("n1", NpcKind.Neutral);
            TestNpc n2 = Make("n2", NpcKind.Neutral);
            TestNpc e1 = Make("e1", NpcKind.Enemy);

            var results = new List<NpcBase> { e1 };
            NpcRegistry.GetAll(NpcKind.Neutral, results);
            CollectionAssert.AreEquivalent(new NpcBase[] { n1, n2 }, results);

            NpcRegistry.GetAll(NpcKind.Enemy, results);
            CollectionAssert.AreEquivalent(new NpcBase[] { e1 }, results);
        }

        [Test]
        public void Register_IgnoresNullAndDuplicates()
        {
            TestNpc a = Make("a", NpcKind.Neutral);
            NpcRegistry.Register(a);
            NpcRegistry.Register(null);
            Assert.AreEqual(1, NpcRegistry.All.Count);
        }

        [Test]
        public void PlayerCaught_IsRaised()
        {
            int caught = 0;
            NpcRegistry.PlayerCaught += _ => caught++;
            TestStatics.RaisePlayerCaught(null);
            Assert.AreEqual(1, caught);
        }
    }

    public class NeutralNpcTests : DoorTestBase
    {
        GameObject visual;
        ClueManager clues;

        NeutralNpc Make(string[] lines, ClueData[] give = null, bool loop = false, bool leaveIfIgnored = true)
        {
            return NewComponent<NeutralNpc>("Neutral", n =>
            {
                TestReflection.Set(n, "lines", lines);
                TestReflection.Set(n, "cluesToGive", give ?? new ClueData[0]);
                TestReflection.Set(n, "loopLines", loop);
                TestReflection.Set(n, "leaveIfIgnored", leaveIfIgnored);
                visual = new GameObject("Visual");
                visual.transform.SetParent(n.transform, false);
                TestReflection.Set(n, "visualRoot", visual);
            });
        }

        [SetUp]
        public void SetUp() => clues = NewComponent<ClueManager>("Clues");

        [UnityTest]
        public IEnumerator Lines_SpokenInOrder_CluesAfterLastLine()
        {
            ClueData clue = NewClue("landlady_key");
            NeutralNpc npc = Make(new[] { "a", "b", "c" }, new[] { clue });
            yield return null;
            var spoken = new List<string>();
            int finished = 0;
            npc.LineSpoken += (n, l) => spoken.Add(l);
            npc.ConversationFinished += _ => finished++;

            npc.SpeakNext();
            npc.SpeakNext();
            Assert.IsFalse(clues.HasClue(clue), "no clue before the last line");
            Assert.AreEqual(0, finished);
            Assert.IsTrue(npc.HasMoreLines);

            npc.SpeakNext();
            Assert.IsTrue(clues.HasClue(clue));
            Assert.AreEqual(1, finished);
            Assert.IsFalse(npc.HasMoreLines);

            npc.SpeakNext();
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, spoken, "nothing more without looping");
            Assert.AreEqual(1, finished);
        }

        [UnityTest]
        public IEnumerator LoopLines_StartsOver()
        {
            NeutralNpc npc = Make(new[] { "a", "b" }, loop: true);
            yield return null;
            var spoken = new List<string>();
            npc.LineSpoken += (n, l) => spoken.Add(l);
            for (int i = 0; i < 5; i++) npc.SpeakNext();
            CollectionAssert.AreEqual(new[] { "a", "b", "a", "b", "a" }, spoken);
            Assert.IsTrue(npc.HasMoreLines);
        }

        [UnityTest]
        public IEnumerator ResetConversation_StartsFromFirstLine()
        {
            NeutralNpc npc = Make(new[] { "a", "b" });
            yield return null;
            var spoken = new List<string>();
            npc.LineSpoken += (n, l) => spoken.Add(l);
            npc.SpeakNext();
            npc.SpeakNext();
            npc.ResetConversation();
            npc.SpeakNext();
            CollectionAssert.AreEqual(new[] { "a", "b", "a" }, spoken);
        }

        [UnityTest]
        public IEnumerator NoLines_GivesCluesImmediately()
        {
            ClueData clue = NewClue("note");
            NeutralNpc npc = Make(new string[0], new[] { clue });
            yield return null;
            int finished = 0;
            npc.ConversationFinished += _ => finished++;
            npc.SpeakNext();
            Assert.IsTrue(clues.HasClue(clue));
            Assert.AreEqual(1, finished);
            Assert.IsFalse(npc.HasMoreLines);
        }

        [UnityTest]
        public IEnumerator Interact_SpeaksNext_OnlyWhenActive()
        {
            NeutralNpc npc = Make(new[] { "hello" });
            yield return null;
            Assert.AreEqual("Talk", npc.Prompt);
            Assert.IsTrue(npc.CanInteract(null));
            var spoken = new List<string>();
            npc.LineSpoken += (n, l) => spoken.Add(l);
            npc.Interact(null);
            CollectionAssert.AreEqual(new[] { "hello" }, spoken);

            npc.SetActiveState(false);
            Assert.IsFalse(npc.CanInteract(null));
        }

        [UnityTest]
        public IEnumerator IgnoredAtDoor_FiresIgnoredAndHides()
        {
            NeutralNpc npc = Make(new[] { "let me in" });
            Door door = MakeDoor();
            yield return null;
            var ignored = new List<Door>();
            npc.Ignored += (n, d) => ignored.Add(d);

            door.BeginVisit(npc, Pattern());
            door.DeclineVisitor();

            CollectionAssert.AreEqual(new[] { door }, ignored);
            Assert.IsFalse(npc.IsActive);
            Assert.IsFalse(visual.activeSelf);
        }

        [UnityTest]
        public IEnumerator IgnoredAtDoor_StaysWhenLeaveIfIgnoredFalse()
        {
            NeutralNpc npc = Make(new[] { "let me in" }, leaveIfIgnored: false);
            Door door = MakeDoor();
            yield return null;
            int ignored = 0;
            npc.Ignored += (n, d) => ignored++;

            door.BeginVisit(npc, Pattern());
            door.DeclineVisitor();

            Assert.AreEqual(1, ignored);
            Assert.IsTrue(npc.IsActive);
        }

        [UnityTest]
        public IEnumerator DoorOpened_AppearsAtStandPointAndSpeaks()
        {
            NeutralNpc npc = Make(new[] { "good evening", "bye" });
            Door door = MakeDoor(configure: d =>
            {
                var stand = new GameObject("Stand").transform;
                stand.SetParent(d.transform, false);
                stand.localPosition = new Vector3(0f, 0f, 1.5f);
                stand.localRotation = Quaternion.Euler(0f, 180f, 0f);
                TestReflection.Set(d, "visitorStandPoint", stand);
            });
            yield return null;
            npc.SetActiveState(false);
            var spoken = new List<string>();
            npc.LineSpoken += (n, l) => spoken.Add(l);

            door.BeginVisit(npc, Pattern());
            door.TryOpen();

            Assert.That(Vector3.Distance(door.VisitorStandPoint.position, npc.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(door.VisitorStandPoint.rotation, npc.transform.rotation), Is.LessThan(0.01f));
            Assert.IsTrue(npc.IsActive);
            Assert.IsTrue(visual.activeSelf);
            CollectionAssert.AreEqual(new[] { "good evening" }, spoken);
        }

        [UnityTest]
        public IEnumerator Clues_NotGivenWithoutClueManager_NoError()
        {
            Object.DestroyImmediate(clues.gameObject);
            ClueData clue = NewClue("x");
            NeutralNpc npc = Make(new[] { "a" }, new[] { clue });
            yield return null;
            Assert.DoesNotThrow(() => npc.SpeakNext());
        }
    }
}
