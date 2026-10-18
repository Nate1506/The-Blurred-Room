#if BR_INPUT_SYSTEM_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlurredRoom.Tests
{
    /// <summary>Simulated keyboard / mouse / gamepad through the Input System's InputTestFixture.</summary>
    public class InputSystemPlayerInputTests : InputTestFixture
    {
        Keyboard keyboard;
        Mouse mouse;
        Gamepad gamepad;
        GameObject go;
        InputSystemPlayerInput input;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            go = new GameObject("InputSystemPlayerInput");
            input = go.AddComponent<InputSystemPlayerInput>();
        }

        public override void TearDown()
        {
            if (go) Object.DestroyImmediate(go);
            base.TearDown();
        }

        static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(0.001f), $"expected {expected} but was {actual}");
        }

        [Test]
        public void Move_WASD()
        {
            AssertVector(Vector2.zero, input.Move);

            Press(keyboard.wKey);
            AssertVector(Vector2.up, input.Move);
            Release(keyboard.wKey);

            Press(keyboard.sKey);
            AssertVector(Vector2.down, input.Move);
            Release(keyboard.sKey);

            Press(keyboard.aKey);
            AssertVector(Vector2.left, input.Move);
            Release(keyboard.aKey);

            Press(keyboard.dKey);
            AssertVector(Vector2.right, input.Move);
            Release(keyboard.dKey);

            AssertVector(Vector2.zero, input.Move);
        }

        [Test]
        public void Move_Diagonal_IsNormalized()
        {
            Press(keyboard.wKey);
            Press(keyboard.dKey);
            Assert.That(input.Move.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.Greater(input.Move.x, 0f);
            Assert.Greater(input.Move.y, 0f);
        }

        [Test]
        public void Look_MouseDelta()
        {
            Set(mouse.delta, new Vector2(4f, -2f));
            AssertVector(new Vector2(4f, -2f), input.Look);
        }

        [Test]
        public void RunHeld_LeftShift()
        {
            Assert.IsFalse(input.RunHeld);
            Press(keyboard.leftShiftKey);
            Assert.IsTrue(input.RunHeld);
            InputSystem.Update();
            Assert.IsTrue(input.RunHeld, "held, not just pressed");
            Release(keyboard.leftShiftKey);
            Assert.IsFalse(input.RunHeld);
        }

        [Test]
        public void InteractPressed_E_OnlyOnPressUpdate()
        {
            Assert.IsFalse(input.InteractPressed);
            Press(keyboard.eKey);
            Assert.IsTrue(input.InteractPressed);
            InputSystem.Update();
            Assert.IsFalse(input.InteractPressed, "still held, but no longer 'pressed this frame'");
        }

        [Test]
        public void PickupPressed_F_OnlyOnPressUpdate()
        {
            Assert.IsFalse(input.PickupPressed);
            Press(keyboard.fKey);
            Assert.IsTrue(input.PickupPressed);
            InputSystem.Update();
            Assert.IsFalse(input.PickupPressed);
        }

        [Test]
        public void PeepholeHeld_Q()
        {
            Press(keyboard.qKey);
            Assert.IsTrue(input.PeepholeHeld);
            InputSystem.Update();
            Assert.IsTrue(input.PeepholeHeld);
            Release(keyboard.qKey);
            Assert.IsFalse(input.PeepholeHeld);
        }

        [Test]
        public void RitualHeld_R()
        {
            Press(keyboard.rKey);
            Assert.IsTrue(input.RitualHeld);
            InputSystem.Update();
            Assert.IsTrue(input.RitualHeld);
            Release(keyboard.rKey);
            Assert.IsFalse(input.RitualHeld);
        }

        [Test]
        public void OtherKeys_DoNothing()
        {
            Press(keyboard.xKey);
            AssertVector(Vector2.zero, input.Move);
            Assert.IsFalse(input.RunHeld);
            Assert.IsFalse(input.InteractPressed);
            Assert.IsFalse(input.PickupPressed);
            Assert.IsFalse(input.PeepholeHeld);
            Assert.IsFalse(input.RitualHeld);
        }

        [Test]
        public void Gamepad_Bindings()
        {
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Assert.Greater(input.Move.y, 0.9f);
            Set(gamepad.leftStick, Vector2.zero);

            Set(gamepad.rightStick, new Vector2(1f, 0f));
            Assert.Greater(input.Look.x, 1.5f, "right stick is scaled up to mouse-delta size");
            Set(gamepad.rightStick, Vector2.zero);

            Press(gamepad.buttonSouth);
            Assert.IsTrue(input.InteractPressed);
            Press(gamepad.buttonWest);
            Assert.IsTrue(input.PickupPressed);
            Press(gamepad.leftShoulder);
            Assert.IsTrue(input.PeepholeHeld);
            Press(gamepad.rightShoulder);
            Assert.IsTrue(input.RitualHeld);
            Press(gamepad.leftStickButton);
            Assert.IsTrue(input.RunHeld);
        }

        [Test]
        public void Disabled_ReadsNothing()
        {
            input.enabled = false;
            Press(keyboard.wKey);
            Press(keyboard.eKey);
            Press(keyboard.rKey);
            AssertVector(Vector2.zero, input.Move);
            Assert.IsFalse(input.InteractPressed);
            Assert.IsFalse(input.RitualHeld);

            input.enabled = true;
            Release(keyboard.wKey);
            Press(keyboard.wKey);
            AssertVector(Vector2.up, input.Move);
        }

        [Test]
        public void MovementLock_IsIndependentOfDevices()
        {
            input.LockMovement(this);
            Press(keyboard.wKey);
            Assert.IsTrue(input.MovementLocked);
            AssertVector(Vector2.up, input.Move); // systems decide to ignore Move while locked
            input.UnlockMovement(this);
            Assert.IsFalse(input.MovementLocked);
        }
    }
}
#endif
