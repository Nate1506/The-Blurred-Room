using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public abstract class DoorTestBase : NateTestBase
    {
        protected Door MakeDoor(string name = "Door", System.Action<Door> configure = null)
            => NewComponent(name, configure);

        protected TestNpc MakeNpc(string id = "npc", NpcKind kind = NpcKind.Neutral, bool busy = false,
                                  bool canBeVisitor = true)
        {
            return NewComponent<TestNpc>("Npc_" + id, n =>
            {
                n.kind = kind;
                n.busy = busy;
                TestReflection.Set(n, "npcId", id);
                TestReflection.Set(n, "canBeVisitor", canBeVisitor);
            });
        }

        protected static KnockPattern Pattern(int knocks = 3, float interval = 0.05f, float window = 5f)
            => new KnockPattern { knocks = knocks, interval = interval, answerWindow = window };
    }

    public class DoorTests : DoorTestBase
    {
        [Test]
        public void OpenAndClose_FireEvents()
        {
            Door door = MakeDoor();
            int opened = 0, closed = 0;
            door.Opened += () => opened++;
            door.Closed += () => closed++;

            Assert.IsTrue(door.TryOpen());
            Assert.IsTrue(door.IsOpen);
            Assert.IsTrue(door.TryOpen(), "opening an open door returns true");
            Assert.AreEqual(1, opened);

            door.Close();
            door.Close();
            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(1, closed);
        }

        [Test]
        public void Interact_TogglesDoor()
        {
            Door door = MakeDoor();
            Assert.AreEqual("Open", door.Prompt);
            door.Interact(null);
            Assert.IsTrue(door.IsOpen);
            Assert.AreEqual("Close", door.Prompt);
            door.Interact(null);
            Assert.IsFalse(door.IsOpen);
        }

        [Test]
        public void StartOpen_IsOpen()
        {
            Door door = MakeDoor(configure: d => TestReflection.Set(d, "startOpen", true));
            Assert.IsTrue(door.IsOpen);
        }

        [Test]
        public void LockedDoor_DoesNotOpen_AndFiresLockedAttempt()
        {
            Door door = MakeDoor();
            var lockEvents = new List<bool>();
            int attempts = 0, opened = 0;
            door.LockChanged += lockEvents.Add;
            door.LockedAttempt += () => attempts++;
            door.Opened += () => opened++;

            door.SetLocked(true);
            door.SetLocked(true);
            Assert.IsTrue(door.IsLocked);
            Assert.AreEqual("Locked", door.Prompt);

            Assert.IsFalse(door.TryOpen());
            door.Interact(null);
            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(2, attempts);
            Assert.AreEqual(0, opened);

            door.SetLocked(false);
            Assert.IsTrue(door.TryOpen());
            CollectionAssert.AreEqual(new[] { true, false }, lockEvents);
        }

        [UnityTest]
        public IEnumerator Hinge_RotatesToOpenAngleAndBack()
        {
            Transform hinge = null;
            Door door = MakeDoor(configure: d =>
            {
                hinge = new GameObject("Hinge").transform;
                hinge.SetParent(d.transform, false);
                hinge.localRotation = Quaternion.Euler(0f, 30f, 0f);
                TestReflection.Set(d, "hinge", hinge);
                TestReflection.Set(d, "openAngle", 90f);
                TestReflection.Set(d, "swingSpeed", 900f);
            });
            Quaternion closed = Quaternion.Euler(0f, 30f, 0f);
            Assert.That(Quaternion.Angle(hinge.localRotation, closed), Is.LessThan(0.01f));

            door.TryOpen();
            yield return null;
            Assert.Greater(Quaternion.Angle(hinge.localRotation, closed), 0.01f, "starts swinging");
            yield return new WaitForSeconds(0.3f);
            Assert.That(Quaternion.Angle(hinge.localRotation, closed * Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(0.01f));

            door.Close();
            yield return new WaitForSeconds(0.3f);
            Assert.That(Quaternion.Angle(hinge.localRotation, closed), Is.LessThan(0.01f));
        }

        [Test]
        public void Hinge_StartOpen_IsAtOpenAngleImmediately()
        {
            Transform hinge = null;
            MakeDoor(configure: d =>
            {
                hinge = new GameObject("Hinge").transform;
                hinge.SetParent(d.transform, false);
                TestReflection.Set(d, "hinge", hinge);
                TestReflection.Set(d, "openAngle", 80f);
                TestReflection.Set(d, "startOpen", true);
            });
            Assert.That(Quaternion.Angle(hinge.localRotation, Quaternion.Euler(0f, 80f, 0f)), Is.LessThan(0.01f));
        }

        [Test]
        public void BeginVisit_Fails_WhenDoorOpen()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            door.TryOpen();
            Assert.IsFalse(door.BeginVisit(npc));
            Assert.IsNull(door.Visitor);
            Assert.IsNull(npc.VisitingDoor);
        }

        [Test]
        public void BeginVisit_Fails_WhenBusy()
        {
            Door door = MakeDoor();
            TestNpc a = MakeNpc("a"), b = MakeNpc("b");
            Assert.IsTrue(door.BeginVisit(a, Pattern()));
            Assert.IsFalse(door.BeginVisit(b, Pattern()));
            Assert.AreSame(a, door.Visitor);
            Assert.IsNull(b.VisitingDoor);
        }

        [Test]
        public void BeginVisit_Fails_WhenVisitorNull()
        {
            Door door = MakeDoor();
            int started = 0;
            door.VisitStarted += _ => started++;
            Assert.IsFalse(door.BeginVisit(null));
            Assert.AreEqual(0, started);
            Assert.IsFalse(door.HasPendingVisitor);
        }

        [UnityTest]
        public IEnumerator BeginVisit_StartsVisitAndKnocks()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            var started = new List<NpcBase>();
            var knocks = new List<string>();
            door.VisitStarted += started.Add;
            door.Knocked += (i, n) => knocks.Add(i + "/" + n);

            Assert.IsTrue(door.BeginVisit(npc, Pattern(knocks: 3, interval: 0.05f)));
            CollectionAssert.AreEqual(new NpcBase[] { npc }, started);
            Assert.AreSame(npc, door.Visitor);
            Assert.IsTrue(door.HasPendingVisitor);
            Assert.AreSame(door, npc.VisitingDoor);
            Assert.IsTrue(door.IsKnocking);
            Assert.AreEqual("Open the door", door.Prompt);

            yield return new WaitForSeconds(0.4f);
            CollectionAssert.AreEqual(new[] { "1/3", "2/3", "3/3" }, knocks);
            Assert.IsFalse(door.IsKnocking);
            Assert.IsTrue(door.HasPendingVisitor, "still waiting for an answer");
        }

        [UnityTest]
        public IEnumerator OpeningDuringVisit_AnswersOpened()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            var answers = new List<string>();
            door.Answered += (n, a) => answers.Add((n == npc) + ":" + a);

            door.BeginVisit(npc, Pattern());
            yield return null;
            Assert.IsTrue(door.TryOpen());

            CollectionAssert.AreEqual(new[] { "True:Opened" }, answers);
            CollectionAssert.AreEqual(new[] { DoorAnswer.Opened }, npc.answers);
            Assert.AreSame(door, npc.answeredDoors[0]);
            Assert.IsNull(door.Visitor);
            Assert.IsNull(npc.VisitingDoor);
            Assert.IsFalse(door.IsKnocking);
        }

        [UnityTest]
        public IEnumerator Visitor_ClearedBeforeOnDoorAnswered()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            NpcBase visitorDuringAnswer = npc;
            Door visitingDuringAnswer = door;
            door.Answered += (n, a) => { visitorDuringAnswer = door.Visitor; visitingDuringAnswer = n.VisitingDoor; };
            door.BeginVisit(npc, Pattern());
            yield return null;
            door.TryOpen();
            Assert.IsNull(visitorDuringAnswer);
            Assert.IsNull(visitingDuringAnswer);
        }

        [UnityTest]
        public IEnumerator DeclineVisitor_AnswersIgnored()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            var answers = new List<DoorAnswer>();
            door.Answered += (n, a) => answers.Add(a);

            door.BeginVisit(npc, Pattern());
            yield return null;
            door.DeclineVisitor();

            CollectionAssert.AreEqual(new[] { DoorAnswer.Ignored }, answers);
            CollectionAssert.AreEqual(new[] { DoorAnswer.Ignored }, npc.answers);
            Assert.IsNull(door.Visitor);
            Assert.IsNull(npc.VisitingDoor);
            Assert.IsFalse(door.IsOpen);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(1, answers.Count, "the knock routine was stopped, no second answer");
        }

        [Test]
        public void DeclineVisitor_WithoutVisitor_DoesNothing()
        {
            Door door = MakeDoor();
            int answered = 0;
            door.Answered += (n, a) => answered++;
            door.DeclineVisitor();
            Assert.AreEqual(0, answered);
        }

        [UnityTest]
        public IEnumerator AnswerWindowRunsOut_AnswersIgnored()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            var answers = new List<DoorAnswer>();
            door.Answered += (n, a) => answers.Add(a);

            door.BeginVisit(npc, Pattern(knocks: 2, interval: 0.05f, window: 0.2f));
            yield return WaitUntilOrTimeout(() => answers.Count > 0, 3f);

            CollectionAssert.AreEqual(new[] { DoorAnswer.Ignored }, answers);
            CollectionAssert.AreEqual(new[] { DoorAnswer.Ignored }, npc.answers);
            Assert.IsNull(door.Visitor);
            Assert.IsNull(npc.VisitingDoor);
            Assert.IsFalse(door.IsKnocking);
        }

        [UnityTest]
        public IEnumerator AfterVisit_DoorCanBeVisitedAgain()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            door.BeginVisit(npc, Pattern());
            door.DeclineVisitor();
            yield return null;
            Assert.IsTrue(door.BeginVisit(npc, Pattern()));
        }

        [UnityTest]
        public IEnumerator KnockAgain_RestartsKnockingAndTimer()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            var knocks = new List<int>();
            var answers = new List<DoorAnswer>();
            door.Knocked += (i, n) => knocks.Add(i);
            door.Answered += (n, a) => answers.Add(a);

            KnockPattern p = Pattern(knocks: 2, interval: 0.05f, window: 1f);
            door.BeginVisit(npc, p);
            yield return new WaitForSeconds(0.6f);
            CollectionAssert.AreEqual(new[] { 1, 2 }, knocks);
            Assert.AreEqual(0, answers.Count);

            door.KnockAgain(p);
            Assert.IsTrue(door.IsKnocking);
            Assert.AreEqual(1, knocks[knocks.Count - 1], "knocking restarts from 1");

            yield return new WaitForSeconds(0.7f); // ~1.3s after the first visit started: the old timer would have expired
            Assert.AreEqual(0, answers.Count, "the answer timer was restarted");
            CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, knocks);
            Assert.AreSame(npc, door.Visitor);

            yield return WaitUntilOrTimeout(() => answers.Count > 0, 3f);
            CollectionAssert.AreEqual(new[] { DoorAnswer.Ignored }, answers);
        }

        [Test]
        public void KnockAgain_WithoutVisitor_DoesNothing()
        {
            Door door = MakeDoor();
            int knocks = 0;
            door.Knocked += (i, n) => knocks++;
            door.KnockAgain();
            Assert.AreEqual(0, knocks);
            Assert.IsFalse(door.IsKnocking);
        }

        [UnityTest]
        public IEnumerator LockedDoorDuringVisit_KeepsVisitorWaiting()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            door.SetLocked(true);
            door.BeginVisit(npc, Pattern());
            yield return null;
            Assert.IsFalse(door.TryOpen());
            Assert.AreSame(npc, door.Visitor);
            Assert.AreEqual(0, npc.answers.Count);
        }

        [Test]
        public void CanInteract_FalseWhilePeepholeInUse()
        {
            Door door = MakeDoor();
            var changes = new List<bool>();
            door.PeepholeChanged += changes.Add;
            Assert.IsTrue(door.CanInteract(null));

            door.SetPeepholeInUse(true);
            door.SetPeepholeInUse(true);
            Assert.IsFalse(door.CanInteract(null));
            Assert.IsTrue(door.IsPeepholeInUse);

            door.SetPeepholeInUse(false);
            Assert.IsTrue(door.CanInteract(null));
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void VisitorStandPoint_FallsBackToDoorTransform()
        {
            Door door = MakeDoor();
            Assert.AreSame(door.transform, door.VisitorStandPoint);
            var stand = new GameObject("Stand").transform;
            stand.SetParent(door.transform);
            TestReflection.Set(door, "visitorStandPoint", stand);
            Assert.AreSame(stand, door.VisitorStandPoint);
        }
    }

    public class DoorVisitSchedulerTests : DoorTestBase
    {
        DoorVisitScheduler MakeScheduler(Door[] doors, float enemyChance = 0.5f,
                                         DayPhaseMask phases = DayPhaseMask.All, float interval = 1000f)
        {
            return NewComponent<DoorVisitScheduler>("Scheduler", s =>
            {
                TestReflection.Set(s, "doors", doors);
                TestReflection.Set(s, "enemyChance", enemyChance);
                TestReflection.Set(s, "activePhases", phases);
                TestReflection.Set(s, "secondsBetweenVisits", new Vector2(interval, interval));
            });
        }

        [SetUp]
        public void SeedRandom() => Random.InitState(12345);

        [Test]
        public void TryStartVisit_StartsOnFreeDoorWithAvailableNpc()
        {
            Door door = MakeDoor();
            TestNpc npc = MakeNpc();
            DoorVisitScheduler s = MakeScheduler(new[] { door });
            Assert.IsTrue(s.TryStartVisit());
            Assert.AreSame(npc, door.Visitor);
        }

        [Test]
        public void SkipsOpenAndBusyDoors()
        {
            Door open = MakeDoor("Open");
            open.TryOpen();
            Door busy = MakeDoor("Busy");
            busy.BeginVisit(MakeNpc("already"), Pattern());
            TestNpc npc = MakeNpc("free_npc");
            DoorVisitScheduler s = MakeScheduler(new[] { open, busy });

            Assert.IsFalse(s.TryStartVisit());
            Assert.IsNull(npc.VisitingDoor);

            Door free = MakeDoor("Free");
            TestReflection.Set(s, "doors", new[] { open, busy, free });
            Assert.IsTrue(s.TryStartVisit());
            Assert.AreSame(npc, free.Visitor);
        }

        [Test]
        public void SkipsBusyNpcs_AndNpcsAlreadyVisiting_AndNonVisitors()
        {
            Door a = MakeDoor("A"), b = MakeDoor("B");
            MakeNpc("busy", busy: true);
            MakeNpc("no_visit", canBeVisitor: false);
            TestNpc visiting = MakeNpc("visiting");
            a.BeginVisit(visiting, Pattern());
            DoorVisitScheduler s = MakeScheduler(new[] { b });

            Assert.IsFalse(s.TryStartVisit());
            Assert.IsFalse(b.HasPendingVisitor);
        }

        [Test]
        public void FallsBackToOtherKind_EnemyWanted()
        {
            Door door = MakeDoor();
            TestNpc neutral = MakeNpc("neutral", NpcKind.Neutral);
            DoorVisitScheduler s = MakeScheduler(new[] { door }, enemyChance: 1f);
            Assert.IsTrue(s.TryStartVisit());
            Assert.AreSame(neutral, door.Visitor);
        }

        [Test]
        public void FallsBackToOtherKind_NeutralWanted()
        {
            Door door = MakeDoor();
            TestNpc enemyKind = MakeNpc("enemy", NpcKind.Enemy);
            DoorVisitScheduler s = MakeScheduler(new[] { door }, enemyChance: 0f);
            Assert.IsTrue(s.TryStartVisit());
            Assert.AreSame(enemyKind, door.Visitor);
        }

        [Test]
        public void PrefersChosenKind_WhenAvailable()
        {
            Door door = MakeDoor();
            MakeNpc("neutral", NpcKind.Neutral);
            TestNpc enemyKind = MakeNpc("enemy", NpcKind.Enemy);
            DoorVisitScheduler s = MakeScheduler(new[] { door }, enemyChance: 1f);
            Assert.IsTrue(s.TryStartVisit());
            Assert.AreSame(enemyKind, door.Visitor);
        }

        [Test]
        public void ReturnsFalse_WhenNothingPossible()
        {
            Assert.IsFalse(MakeScheduler(new Door[0]).TryStartVisit(), "no doors");
            Assert.IsFalse(MakeScheduler(null).TryStartVisit(), "null door array");
            Assert.IsFalse(MakeScheduler(new Door[] { null }).TryStartVisit(), "null door entry");
            Assert.IsFalse(MakeScheduler(new[] { MakeDoor() }).TryStartVisit(), "no NPCs");
        }

        [UnityTest]
        public IEnumerator OnlyRunsInActivePhases()
        {
            TimeOfDayManager time = NewComponent<TimeOfDayManager>("Time",
                t => TestReflection.Set(t, "autoAdvance", false));
            Door door = MakeDoor();
            MakeNpc();
            MakeScheduler(new[] { door }, phases: DayPhaseMask.Night, interval: 0.05f);

            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(door.HasPendingVisitor, "no visits during Day");

            time.SetPhase(DayPhase.Night);
            yield return WaitUntilOrTimeout(() => door.HasPendingVisitor, 2f);
            Assert.IsTrue(door.HasPendingVisitor, "visit started during Night");
        }

        [UnityTest]
        public IEnumerator RunsWithoutTimeManager()
        {
            Door door = MakeDoor();
            MakeNpc();
            MakeScheduler(new[] { door }, phases: DayPhaseMask.Night, interval: 0.05f);
            yield return WaitUntilOrTimeout(() => door.HasPendingVisitor, 2f);
            Assert.IsTrue(door.HasPendingVisitor);
        }
    }
}
