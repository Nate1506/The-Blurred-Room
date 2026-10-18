using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    /// <summary>Player at the origin facing +Z; its camera is at eye height (0, 1.6, 0). Sight range is 2.5.</summary>
    public abstract class PlayerSightTestBase : NateTestBase
    {
        protected static readonly Vector3 InFront = new Vector3(0f, TestPlayer.EyeHeight, 2f);
        protected static readonly Vector3 TooFar = new Vector3(0f, TestPlayer.EyeHeight, 5f);
        protected static readonly Vector3 Away = new Vector3(50f, TestPlayer.EyeHeight, 50f);

        protected T Target<T>(Vector3 pos) where T : Component
        {
            return NewBox(typeof(T).Name, pos, new Vector3(0.6f, 0.6f, 0.2f)).AddComponent<T>();
        }
    }

    public class PlayerSightTests : PlayerSightTestBase
    {
        [UnityTest]
        public IEnumerator Get_ReturnsComponentInViewWithinRange()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestInteractable>(InFront);
            yield return null;

            Assert.AreSame(t, p.Sight.Get<TestInteractable>());
            Assert.AreSame(t, p.Sight.Get<IInteractable>(), "works with interfaces");
            Assert.IsTrue(p.Sight.HasHit);
            Assert.AreSame(t.gameObject, p.Sight.Hit.collider.gameObject);
        }

        [UnityTest]
        public IEnumerator Get_ReturnsNullOutOfRange()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            Target<TestInteractable>(TooFar);
            yield return null;
            Assert.IsNull(p.Sight.Get<TestInteractable>());
            Assert.IsFalse(p.Sight.HasHit);
        }

        [UnityTest]
        public IEnumerator Get_ReturnsNullWhenNothingHit()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            yield return null;
            Assert.IsNull(p.Sight.Get<TestInteractable>());
            Assert.IsFalse(p.Sight.HasHit);
        }

        [UnityTest]
        public IEnumerator Get_ReturnsNullWhenHitObjectHasNoSuchComponent()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            NewBox("Plain", InFront, Vector3.one * 0.5f);
            yield return null;
            Assert.IsTrue(p.Sight.HasHit);
            Assert.IsNull(p.Sight.Get<TestInteractable>());
        }

        [UnityTest]
        public IEnumerator Get_FindsComponentOnParentOfCollider()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            GameObject parent = NewGO("Parent", InFront);
            var t = parent.AddComponent<TestInteractable>();
            NewBox("ChildCollider", InFront, Vector3.one * 0.5f).transform.SetParent(parent.transform, true);
            Physics.SyncTransforms();
            yield return null;
            Assert.AreSame(t, p.Sight.Get<TestInteractable>());
        }

        [UnityTest]
        public IEnumerator Get_IgnoresTriggers()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestInteractable>(InFront);
            t.GetComponent<Collider>().isTrigger = true;
            yield return null;
            Assert.IsNull(p.Sight.Get<TestInteractable>());
        }

        [UnityTest]
        public IEnumerator Get_IsCachedForTheFrame()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestInteractable>(InFront);
            yield return null;
            Assert.AreSame(t, p.Sight.Get<TestInteractable>());

            MoveTo(t.gameObject, Away);
            Assert.AreSame(t, p.Sight.Get<TestInteractable>(), "same frame: cached result, no new raycast");

            yield return null;
            Assert.IsNull(p.Sight.Get<TestInteractable>(), "next frame: raycast again");
        }

        [UnityTest]
        public IEnumerator Get_FollowsCameraPitch()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            p.Look.enabled = false; // PlayerLook would reset the pitch every frame
            var t = Target<TestInteractable>(new Vector3(0f, 0.2f, 1.4f));
            yield return null;
            Assert.IsNull(p.Sight.Get<TestInteractable>());

            p.Camera.transform.localRotation = Quaternion.LookRotation(t.transform.position - p.Camera.transform.position);
            yield return null;
            Assert.AreSame(t, p.Sight.Get<TestInteractable>());
        }
    }

    public class PlayerInteractorTests : PlayerSightTestBase
    {
        [UnityTest]
        public IEnumerator FocusChanged_OnLookAtAndLookAway()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var focus = new List<IInteractable>();
            p.Interactor.FocusChanged += focus.Add;
            var t = Target<TestInteractable>(InFront);

            yield return null;
            Assert.AreSame(t, p.Interactor.Focus);
            Assert.AreEqual(1, focus.Count);
            Assert.AreSame(t, focus[0]);

            yield return Frames(3);
            Assert.AreEqual(1, focus.Count, "no repeated event while the focus stays the same");

            MoveTo(t.gameObject, Away);
            yield return null;
            Assert.IsNull(p.Interactor.Focus);
            Assert.AreEqual(2, focus.Count);
            Assert.IsNull(focus[1]);
        }

        [UnityTest]
        public IEnumerator Interact_OnlyOnPressFrame()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestInteractable>(InFront);
            var interacted = new List<IInteractable>();
            p.Interactor.Interacted += interacted.Add;

            yield return Frames(3);
            Assert.AreEqual(0, t.interactCount, "no interaction without pressing");

            p.Input.PressInteract();
            yield return null;
            Assert.AreEqual(1, t.interactCount);
            Assert.AreSame(p.Root, t.lastInteractor);
            CollectionAssert.AreEqual(new IInteractable[] { t }, interacted);

            yield return Frames(3);
            Assert.AreEqual(1, t.interactCount, "a single press interacts once");
        }

        [UnityTest]
        public IEnumerator CanInteractFalse_MeansNoFocusAndNoInteract()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestInteractable>(InFront);
            t.canInteract = false;
            int focusEvents = 0;
            p.Interactor.FocusChanged += _ => focusEvents++;

            yield return null;
            Assert.IsNull(p.Interactor.Focus);
            Assert.AreEqual(0, focusEvents);

            p.Input.PressInteract();
            yield return Frames(2);
            Assert.AreEqual(0, t.interactCount);

            t.canInteract = true;
            yield return null;
            Assert.AreSame(t, p.Interactor.Focus);
            Assert.AreEqual(1, focusEvents);
        }

        [UnityTest]
        public IEnumerator PressWithNothingInView_DoesNothing()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            int interacted = 0;
            p.Interactor.Interacted += _ => interacted++;
            p.Input.PressInteract();
            yield return Frames(2);
            Assert.AreEqual(0, interacted);
        }
    }

    public class PlayerPickupTests : PlayerSightTestBase
    {
        [UnityTest]
        public IEnumerator FocusChanged_OnLookAtAndLookAway()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var focus = new List<IPickupable>();
            p.Pickup.FocusChanged += focus.Add;
            var t = Target<TestPickupable>(InFront);
            yield return null;
            Assert.AreSame(t, p.Pickup.Focus);

            MoveTo(t.gameObject, Away);
            yield return null;
            Assert.IsNull(p.Pickup.Focus);
            Assert.AreEqual(2, focus.Count);
            Assert.AreSame(t, focus[0]);
            Assert.IsNull(focus[1]);
        }

        [UnityTest]
        public IEnumerator Pickup_OnPressFrame_AddsToInventoryAndCallsOnPickedUp()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestPickupable>(InFront);
            t.itemId = "brass_key";
            var picked = new List<IPickupable>();
            p.Pickup.PickedUp += picked.Add;

            yield return Frames(3);
            Assert.AreEqual(0, t.pickedUpCount, "no pickup without pressing");

            p.Input.PressPickup();
            yield return null;
            Assert.IsTrue(p.Inventory.Has("brass_key"));
            Assert.AreEqual(1, t.pickedUpCount);
            Assert.AreSame(p.Root, t.lastPicker);
            CollectionAssert.AreEqual(new IPickupable[] { t }, picked);

            yield return Frames(3);
            Assert.AreEqual(1, p.Inventory.Count("brass_key"), "one press picks up once");
        }

        [UnityTest]
        public IEnumerator InventoryFull_FiresPickupFailed()
        {
            TestPlayer p = NewPlayer(Vector3.zero, configure: c => TestReflection.Set(c.Inventory, "capacity", 1));
            p.Inventory.TryAdd("rock");
            var t = Target<TestPickupable>(InFront);
            var failed = new List<IPickupable>();
            int picked = 0;
            p.Pickup.PickupFailed += failed.Add;
            p.Pickup.PickedUp += _ => picked++;

            yield return null;
            p.Input.PressPickup();
            yield return null;

            CollectionAssert.AreEqual(new IPickupable[] { t }, failed);
            Assert.AreEqual(0, picked);
            Assert.AreEqual(0, t.pickedUpCount, "OnPickedUp is not called when the inventory is full");
            Assert.AreEqual(1, p.Inventory.Items.Count);
        }

        [UnityTest]
        public IEnumerator CanPickupFalse_MeansNoFocus()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var t = Target<TestPickupable>(InFront);
            t.canPickup = false;
            yield return null;
            Assert.IsNull(p.Pickup.Focus);
            p.Input.PressPickup();
            yield return Frames(2);
            Assert.AreEqual(0, t.pickedUpCount);
            Assert.AreEqual(0, p.Inventory.Items.Count);
        }

        [UnityTest]
        public IEnumerator PickingUpPickupItem_DestroysItAndClearsFocus()
        {
            TestPlayer p = NewPlayer(Vector3.zero);
            var item = Target<PickupItem>(InFront);
            TestReflection.Set(item, "itemId", "candle");
            var focus = new List<IPickupable>();
            p.Pickup.FocusChanged += focus.Add;

            yield return null;
            p.Input.PressPickup();
            yield return Frames(2);

            Assert.IsTrue(p.Inventory.Has("candle"));
            Assert.IsTrue(item == null, "PickupItem destroyed");
            Assert.IsNull(p.Pickup.Focus);
            Assert.AreEqual(2, focus.Count);
            Assert.IsNull(focus[1]);
        }
    }
}
