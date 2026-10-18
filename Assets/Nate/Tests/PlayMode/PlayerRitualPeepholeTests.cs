using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class PlayerRitualTests : PlayerSightTestBase
    {
        TestPlayer p;
        TestRitualTarget target;
        int started, completed, cancelled;

        IEnumerator Setup(float duration)
        {
            started = completed = cancelled = 0;
            p = NewPlayer(Vector3.zero);
            target = Target<TestRitualTarget>(InFront);
            target.duration = duration;
            p.Ritual.Started += _ => started++;
            p.Ritual.Completed += _ => completed++;
            p.Ritual.Cancelled += _ => cancelled++;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Holding_CompletesAfterDuration()
        {
            yield return Setup(0.3f);
            Assert.AreSame(target, p.Ritual.Focus);

            p.Input.RitualHeldValue = true;
            float elapsed = 0f;
            while (completed == 0 && elapsed < 3f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (completed == 0)
                {
                    Assert.IsTrue(p.Ritual.IsPerforming);
                    Assert.Less(elapsed, 0.3f + 1e-4f, "should not still be performing after the duration");
                }
            }
            p.Input.RitualHeldValue = false;

            Assert.AreEqual(1, completed);
            Assert.AreEqual(1, target.completedCount);
            Assert.AreEqual(1, started);
            Assert.AreEqual(0, cancelled);
            Assert.AreEqual(0, target.cancelledCount);
            Assert.GreaterOrEqual(elapsed, 0.3f - 1e-4f);
            Assert.IsFalse(p.Ritual.IsPerforming);
            Assert.AreEqual(1f, target.progress[target.progress.Count - 1]);
            for (int i = 1; i < target.progress.Count; i++)
                Assert.GreaterOrEqual(target.progress[i], target.progress[i - 1], "progress only goes up");
        }

        [UnityTest]
        public IEnumerator ReleasingEarly_Cancels()
        {
            yield return Setup(5f);
            p.Input.RitualHeldValue = true;
            yield return Frames(3);
            Assert.IsTrue(p.Ritual.IsPerforming);
            Assert.Greater(p.Ritual.Progress, 0f);

            p.Input.RitualHeldValue = false;
            yield return null;

            Assert.IsFalse(p.Ritual.IsPerforming);
            Assert.AreEqual(0f, p.Ritual.Progress);
            Assert.AreEqual(1, cancelled);
            Assert.AreEqual(1, target.cancelledCount);
            Assert.AreEqual(0, completed);
            Assert.AreEqual(0, target.completedCount);
        }

        [UnityTest]
        public IEnumerator LookingAway_Cancels()
        {
            yield return Setup(5f);
            p.Input.RitualHeldValue = true;
            yield return Frames(2);
            Assert.IsTrue(p.Ritual.IsPerforming);

            MoveTo(target.gameObject, Away);
            yield return null;

            Assert.IsFalse(p.Ritual.IsPerforming);
            Assert.AreEqual(1, target.cancelledCount);
            Assert.AreEqual(1, cancelled);
        }

        [UnityTest]
        public IEnumerator Movement_LockedWhilePerforming_UnlockedAfterComplete()
        {
            yield return Setup(0.2f);
            p.Input.RitualHeldValue = true;
            yield return null;
            Assert.IsTrue(p.Input.MovementLocked);

            yield return WaitUntilOrTimeout(() => completed > 0, 3f);
            p.Input.RitualHeldValue = false;
            Assert.AreEqual(1, completed);
            Assert.IsFalse(p.Input.MovementLocked);
        }

        [UnityTest]
        public IEnumerator Movement_UnlockedAfterCancel()
        {
            yield return Setup(5f);
            p.Input.RitualHeldValue = true;
            yield return null;
            Assert.IsTrue(p.Input.MovementLocked);
            p.Input.RitualHeldValue = false;
            yield return null;
            Assert.IsFalse(p.Input.MovementLocked);
        }

        [UnityTest]
        public IEnumerator Movement_OtherLocksKeptAfterRitual()
        {
            yield return Setup(5f);
            p.Input.LockMovement(this);
            p.Input.RitualHeldValue = true;
            yield return null;
            p.Input.RitualHeldValue = false;
            yield return null;
            Assert.IsTrue(p.Input.MovementLocked, "the ritual only removes its own lock");
        }

        [UnityTest]
        public IEnumerator DisablingMidRitual_Cancels()
        {
            yield return Setup(5f);
            p.Input.RitualHeldValue = true;
            yield return Frames(2);
            Assert.IsTrue(p.Ritual.IsPerforming);

            p.Ritual.enabled = false;

            Assert.IsFalse(p.Ritual.IsPerforming);
            Assert.AreEqual(1, target.cancelledCount);
            Assert.AreEqual(1, cancelled);
            Assert.IsFalse(p.Input.MovementLocked);
        }

        [UnityTest]
        public IEnumerator CanPerformFalse_DoesNotStart()
        {
            yield return Setup(0.2f);
            target.canPerform = false;
            p.Input.RitualHeldValue = true;
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(0, started);
            Assert.AreEqual(0, target.completedCount);
            Assert.IsFalse(p.Input.MovementLocked);
            Assert.IsNull(p.Ritual.Focus);
        }

        [UnityTest]
        public IEnumerator HoldingWithNoTarget_DoesNothing()
        {
            yield return Setup(0.2f);
            MoveTo(target.gameObject, Away);
            p.Input.RitualHeldValue = true;
            yield return Frames(3);
            Assert.AreEqual(0, started);
            Assert.IsFalse(p.Input.MovementLocked);
        }
    }

    public class PlayerPeepholeTests : PlayerSightTestBase
    {
        Camera peepCam;

        Door MakeDoor(bool hasPeephole = true)
        {
            GameObject go = NewBox("Door", InFront, new Vector3(1f, 2f, 0.1f), active: false);
            var door = go.AddComponent<Door>();
            TestReflection.Set(door, "hasPeephole", hasPeephole);
            var camGo = new GameObject("PeepholeCamera");
            camGo.transform.SetParent(go.transform, false);
            peepCam = camGo.AddComponent<Camera>();
            TestReflection.Set(door, "peepholeCamera", peepCam);
            go.SetActive(true);
            return door;
        }

        [UnityTest]
        public IEnumerator Starts_OnClosedDoorWithPeephole()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            var started = new List<Door>();
            p.Peephole.Started += started.Add;
            Assert.IsFalse(peepCam.enabled, "peephole camera starts disabled");

            p.Input.PeepholeHeldValue = true;
            yield return null;

            Assert.IsTrue(p.Peephole.IsViewing);
            Assert.AreSame(door, p.Peephole.Current);
            Assert.IsTrue(door.IsPeepholeInUse);
            Assert.IsTrue(peepCam.enabled);
            Assert.IsTrue(p.Input.MovementLocked);
            CollectionAssert.AreEqual(new[] { door }, started);
        }

        [UnityTest]
        public IEnumerator Ends_OnRelease()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            var ended = new List<Door>();
            p.Peephole.Ended += ended.Add;

            p.Input.PeepholeHeldValue = true;
            yield return Frames(2);
            p.Input.PeepholeHeldValue = false;
            yield return null;

            Assert.IsFalse(p.Peephole.IsViewing);
            Assert.IsFalse(door.IsPeepholeInUse);
            Assert.IsFalse(peepCam.enabled);
            Assert.IsFalse(p.Input.MovementLocked);
            CollectionAssert.AreEqual(new[] { door }, ended);
        }

        [UnityTest]
        public IEnumerator Ends_WhenDoorOpens()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            int ended = 0, started = 0;
            p.Peephole.Ended += _ => ended++;
            p.Peephole.Started += _ => started++;

            p.Input.PeepholeHeldValue = true;
            yield return null;
            door.TryOpen();
            yield return Frames(3);

            Assert.IsFalse(p.Peephole.IsViewing);
            Assert.IsFalse(door.IsPeepholeInUse);
            Assert.IsFalse(p.Input.MovementLocked);
            Assert.AreEqual(1, ended);
            Assert.AreEqual(1, started, "does not restart on an open door while still held");
        }

        [UnityTest]
        public IEnumerator DoesNotStart_WithoutPeephole()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor(hasPeephole: false);
            p.Input.PeepholeHeldValue = true;
            yield return Frames(2);
            Assert.IsFalse(p.Peephole.IsViewing);
            Assert.IsFalse(door.IsPeepholeInUse);
            Assert.IsFalse(p.Input.MovementLocked);
        }

        [UnityTest]
        public IEnumerator DoesNotStart_OnOpenDoor()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            door.TryOpen();
            p.Input.PeepholeHeldValue = true;
            yield return Frames(2);
            Assert.IsFalse(p.Peephole.IsViewing);
            Assert.IsFalse(door.IsPeepholeInUse);
        }

        [UnityTest]
        public IEnumerator DoesNotStart_WhenNotLookingAtDoor()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            MoveTo(door.gameObject, Away);
            p.Input.PeepholeHeldValue = true;
            yield return Frames(2);
            Assert.IsFalse(p.Peephole.IsViewing);
        }

        [UnityTest]
        public IEnumerator DoorCannotBeInteractedWhileViewing()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            yield return null;
            Assert.AreSame(door, p.Interactor.Focus);

            p.Input.PeepholeHeldValue = true;
            yield return Frames(2);
            Assert.IsFalse(door.CanInteract(p.Root));
            Assert.IsNull(p.Interactor.Focus);

            p.Input.PressInteract();
            yield return Frames(2);
            Assert.IsFalse(door.IsOpen, "interact is blocked while looking through the peephole");
        }

        [UnityTest]
        public IEnumerator DisablingMidView_EndsAndUnlocks()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Door door = MakeDoor();
            int ended = 0;
            p.Peephole.Ended += _ => ended++;
            p.Input.PeepholeHeldValue = true;
            yield return null;

            p.Peephole.enabled = false;
            Assert.AreEqual(1, ended);
            Assert.IsFalse(door.IsPeepholeInUse);
            Assert.IsFalse(p.Input.MovementLocked);
        }
    }
}
