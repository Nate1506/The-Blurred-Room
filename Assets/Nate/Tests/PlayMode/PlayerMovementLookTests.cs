using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class PlayerMovementTests : NateTestBase
    {
        // Batchmode runs uncapped (thousands of FPS). Per-frame moves then fall below
        // CharacterController.minMoveDistance (0.001) and are skipped, so simulate a normal 60 FPS game.
        [SetUp]
        public void FixTimestep() => Time.captureDeltaTime = 1f / 60f;

        [TearDown]
        public void ResetTimestep() => Time.captureDeltaTime = 0f;

        TestPlayer MakeOnFloor(System.Action<TestPlayer> configure = null)
        {
            NewBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 60f));
            return NewPlayer(new Vector3(0f, 0.05f, 0f), enableMovement: true, configure: configure);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        [UnityTest]
        public IEnumerator MovesForwardWithInput()
        {
            TestPlayer p = MakeOnFloor();
            yield return Frames(2);
            Vector3 start = p.Transform.position;

            p.Input.MoveValue = Vector2.up;
            yield return new WaitForSeconds(0.5f);

            Vector3 delta = p.Transform.position - start;
            Assert.Greater(delta.z, 0.3f);
            Assert.That(Mathf.Abs(delta.x), Is.LessThan(0.05f));
            Assert.IsTrue(p.Movement.IsMoving);
            Assert.IsFalse(p.Movement.IsRunning);
        }

        [UnityTest]
        public IEnumerator MovesRelativeToFacing()
        {
            TestPlayer p = MakeOnFloor();
            p.Transform.rotation = Quaternion.Euler(0f, 90f, 0f); // facing +X
            yield return Frames(2);
            Vector3 start = p.Transform.position;

            p.Input.MoveValue = Vector2.up;
            yield return new WaitForSeconds(0.4f);

            Vector3 delta = p.Transform.position - start;
            Assert.Greater(delta.x, 0.2f);
            Assert.That(Mathf.Abs(delta.z), Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator Run_IsFasterThanWalk()
        {
            TestPlayer p = MakeOnFloor();
            p.Input.MoveValue = Vector2.up;
            yield return new WaitForSeconds(0.5f);
            float walkSpeed = p.Movement.CurrentSpeed;
            Vector3 w0 = p.Transform.position;
            yield return new WaitForSeconds(0.3f);
            float walkDist = Flat(p.Transform.position - w0).magnitude;
            float walkTime = 0.3f;

            p.Input.RunHeldValue = true;
            yield return new WaitForSeconds(0.5f);
            float runSpeed = p.Movement.CurrentSpeed;
            Vector3 r0 = p.Transform.position;
            yield return new WaitForSeconds(0.3f);
            float runDist = Flat(p.Transform.position - r0).magnitude;

            Assert.IsTrue(p.Movement.IsRunning);
            Assert.That(walkSpeed, Is.EqualTo(2.2f).Within(0.05f));
            Assert.That(runSpeed, Is.EqualTo(4.5f).Within(0.05f));
            Assert.Greater(runDist / walkTime, walkDist / walkTime * 1.5f, "actual displacement is faster when running");
        }

        [UnityTest]
        public IEnumerator Running_DrainsStamina()
        {
            TestPlayer p = MakeOnFloor();
            p.Input.MoveValue = Vector2.up;
            p.Input.RunHeldValue = true;
            yield return new WaitForSeconds(0.3f);
            Assert.Less(p.Stamina.Current, p.Stamina.Max);
        }

        [UnityTest]
        public IEnumerator RunHeldWithoutMoving_IsNotRunning()
        {
            TestPlayer p = MakeOnFloor();
            p.Input.RunHeldValue = true;
            yield return Frames(5);
            Assert.IsFalse(p.Movement.IsRunning);
            Assert.AreEqual(p.Stamina.Max, p.Stamina.Current);
        }

        [UnityTest]
        public IEnumerator CannotRun_WhenExhausted()
        {
            TestPlayer p = MakeOnFloor(c => TestReflection.Set(c.Stamina, "regenPerSecond", 0f));
            p.Stamina.Spend(1000f);
            Assert.IsTrue(p.Stamina.IsExhausted);

            p.Input.MoveValue = Vector2.up;
            p.Input.RunHeldValue = true;
            yield return new WaitForSeconds(0.5f);

            Assert.IsTrue(p.Movement.IsMoving);
            Assert.IsFalse(p.Movement.IsRunning);
            Assert.That(p.Movement.CurrentSpeed, Is.EqualTo(2.2f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator NoMovement_WhenLocked()
        {
            TestPlayer p = MakeOnFloor();
            yield return Frames(2);
            p.Input.LockMovement(this);
            Vector3 start = p.Transform.position;

            p.Input.MoveValue = Vector2.up;
            p.Input.RunHeldValue = true;
            yield return new WaitForSeconds(0.3f);

            Assert.That(Flat(p.Transform.position - start).magnitude, Is.LessThan(0.001f));
            Assert.IsFalse(p.Movement.IsMoving);
            Assert.IsFalse(p.Movement.IsRunning);

            p.Input.UnlockMovement(this);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(p.Transform.position.z - start.z, 0.1f, "moves again after unlock");
        }

        [UnityTest]
        public IEnumerator FallsWithGravity()
        {
            TestPlayer p = NewPlayer(new Vector3(0f, 10f, 0f), enableMovement: true);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(p.Transform.position.y, 9.8f);
        }

        [UnityTest]
        public IEnumerator StaysOnFloor()
        {
            TestPlayer p = MakeOnFloor();
            yield return new WaitForSeconds(0.5f);
            Assert.That(p.Transform.position.y, Is.GreaterThan(-0.1f).And.LessThan(0.2f));
            Assert.IsTrue(p.Controller.isGrounded);
        }

        [UnityTest]
        public IEnumerator DiagonalInput_IsClampedToWalkSpeed()
        {
            TestPlayer p = MakeOnFloor();
            p.Input.MoveValue = new Vector2(1f, 1f);
            yield return new WaitForSeconds(0.5f);
            Assert.That(p.Movement.CurrentSpeed, Is.EqualTo(2.2f).Within(0.05f));
        }
    }

    public class PlayerLookTests : NateTestBase
    {
        const float DegPerUnit = 0.1f;

        TestPlayer Make()
        {
            GameSettings.LookSensitivity = 1f;
            GameSettings.InvertY = false;
            return NewPlayer(Vector3.zero);
        }

        static float Pitch(TestPlayer p)
        {
            float x = p.Camera.transform.localEulerAngles.x;
            return x > 180f ? x - 360f : x;
        }

        [UnityTest]
        public IEnumerator Yaw_RotatesBody()
        {
            TestPlayer p = Make();
            p.Input.LookValue = new Vector2(100f, 0f);
            yield return null;
            p.Input.LookValue = Vector2.zero;

            Assert.That(p.Transform.eulerAngles.y, Is.EqualTo(100f * DegPerUnit).Within(0.01f));
            Assert.That(Pitch(p), Is.EqualTo(0f).Within(0.01f), "yaw doesn't pitch the camera");
        }

        [UnityTest]
        public IEnumerator Sensitivity_ScalesRotation()
        {
            TestPlayer p = Make();
            GameSettings.LookSensitivity = 2f;
            p.Input.LookValue = new Vector2(50f, 0f);
            yield return null;
            p.Input.LookValue = Vector2.zero;
            Assert.That(p.Transform.eulerAngles.y, Is.EqualTo(50f * 2f * DegPerUnit).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Pitch_RotatesCameraOnly()
        {
            TestPlayer p = Make();
            p.Input.LookValue = new Vector2(0f, 100f); // mouse up = look up = negative pitch
            yield return null;
            p.Input.LookValue = Vector2.zero;

            Assert.That(Pitch(p), Is.EqualTo(-10f).Within(0.01f));
            Assert.That(Quaternion.Angle(p.Transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Pitch_IsClamped()
        {
            TestPlayer p = Make();
            p.Input.LookValue = new Vector2(0f, -10000f);
            yield return null;
            Assert.That(Pitch(p), Is.EqualTo(85f).Within(0.01f));

            p.Input.LookValue = new Vector2(0f, 20000f);
            yield return null;
            p.Input.LookValue = Vector2.zero;
            Assert.That(Pitch(p), Is.EqualTo(-85f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator InvertY_FlipsPitch()
        {
            TestPlayer p = Make();
            GameSettings.InvertY = true;
            p.Input.LookValue = new Vector2(0f, 100f);
            yield return null;
            p.Input.LookValue = Vector2.zero;
            Assert.That(Pitch(p), Is.EqualTo(10f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator NoRotation_WhenLocked()
        {
            TestPlayer p = Make();
            p.Input.LockMovement(this);
            p.Input.LookValue = new Vector2(100f, 100f);
            yield return Frames(3);
            Assert.That(Quaternion.Angle(p.Transform.rotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(p.Camera.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator FieldOfView_AppliedOnEnableAndOnChange()
        {
            GameSettings.FieldOfView = 60f;
            TestPlayer p = Make();
            Assert.That(p.Camera.fieldOfView, Is.EqualTo(60f).Within(0.01f));

            GameSettings.FieldOfView = 90f;
            Assert.That(p.Camera.fieldOfView, Is.EqualTo(90f).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FieldOfView_NotAppliedWhileDisabled_ButOnReEnable()
        {
            TestPlayer p = Make();
            p.Look.enabled = false;
            GameSettings.FieldOfView = 55f;
            Assert.That(p.Camera.fieldOfView, Is.Not.EqualTo(55f).Within(0.01f));
            p.Look.enabled = true;
            Assert.That(p.Camera.fieldOfView, Is.EqualTo(55f).Within(0.01f));
            yield return null;
        }
    }
}
