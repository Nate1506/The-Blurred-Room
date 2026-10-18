using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    public class PickupItemTests : NateTestBase
    {
        ClueManager clues;
        int eventCount;

        PickupItem Make(ClueData clue = null, bool destroyOnPickup = true)
        {
            eventCount = 0;
            return NewComponent<PickupItem>("Item", i =>
            {
                TestReflection.Set(i, "itemId", "photo");
                TestReflection.Set(i, "clue", clue);
                TestReflection.Set(i, "destroyOnPickup", destroyOnPickup);
                var ev = new UnityEvent();
                ev.AddListener(() => eventCount++);
                TestReflection.Set(i, "onPickedUp", ev);
            });
        }

        [SetUp]
        public void SetUp() => clues = NewComponent<ClueManager>("Clues");

        [UnityTest]
        public IEnumerator OnPickedUp_DiscoversClue_FiresEvent_AndDestroys()
        {
            ClueData clue = NewClue("old_photo");
            PickupItem item = Make(clue);
            Assert.AreEqual("photo", item.ItemId);
            Assert.IsTrue(item.CanPickup(null));

            item.OnPickedUp(null);
            Assert.IsTrue(clues.HasClue(clue));
            Assert.AreEqual(1, eventCount);

            yield return null;
            Assert.IsTrue(item == null, "destroyed");
        }

        [UnityTest]
        public IEnumerator OnPickedUp_Deactivates_WhenNotDestroyOnPickup()
        {
            PickupItem item = Make(destroyOnPickup: false);
            item.OnPickedUp(null);
            yield return null;
            Assert.IsTrue(item != null, "not destroyed");
            Assert.IsFalse(item.gameObject.activeSelf);
            Assert.IsFalse(item.CanPickup(null), "can't be picked up again while inactive");
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void NoClue_NoError()
        {
            PickupItem item = Make(destroyOnPickup: false);
            Assert.DoesNotThrow(() => item.OnPickedUp(null));
            Assert.AreEqual(0, clues.ClueCount);
        }

        [Test]
        public void CanPickup_FalseWhenDisabled()
        {
            PickupItem item = Make();
            item.enabled = false;
            Assert.IsFalse(item.CanPickup(null));
        }
    }

    public class RitualPointTests : PlayerSightTestBase
    {
        ClueManager clues;
        TestPlayer player;
        int completedEvents, cancelledEvents;
        List<float> progressEvents;

        RitualPoint Make(string ritualId = "seal", string requiredItem = "", bool consume = true, float sanityCost = 0f,
                         string flag = "", bool oneShot = true, float duration = 0.2f, Vector3? pos = null)
        {
            completedEvents = cancelledEvents = 0;
            progressEvents = new List<float>();
            GameObject go = NewBox("RitualPoint", pos ?? Away, new Vector3(0.6f, 0.6f, 0.2f), active: false);
            var r = go.AddComponent<RitualPoint>();
            TestReflection.Set(r, "ritualId", ritualId);
            TestReflection.Set(r, "requiredItemId", requiredItem);
            TestReflection.Set(r, "consumeItem", consume);
            TestReflection.Set(r, "sanityCost", sanityCost);
            TestReflection.Set(r, "flagOnComplete", flag);
            TestReflection.Set(r, "oneShot", oneShot);
            TestReflection.Set(r, "duration", duration);
            var onProgress = new UnityEvent<float>();
            onProgress.AddListener(progressEvents.Add);
            var onCompleted = new UnityEvent();
            onCompleted.AddListener(() => completedEvents++);
            var onCancelled = new UnityEvent();
            onCancelled.AddListener(() => cancelledEvents++);
            TestReflection.Set(r, "onProgress", onProgress);
            TestReflection.Set(r, "onCompleted", onCompleted);
            TestReflection.Set(r, "onCancelled", onCancelled);
            go.SetActive(true);
            return r;
        }

        [SetUp]
        public void SetUp()
        {
            clues = NewComponent<ClueManager>("Clues");
            player = NewPlayer(Vector3.zero);
        }

        [Test]
        public void RequiredItem_IsChecked()
        {
            RitualPoint r = Make(requiredItem: "candle");
            Assert.IsFalse(r.CanPerformRitual(player.Root));
            player.Inventory.TryAdd("candle");
            Assert.IsTrue(r.CanPerformRitual(player.Root));
        }

        [Test]
        public void RequiredItem_CheckedFromAnyPartOfThePlayer()
        {
            RitualPoint r = Make(requiredItem: "candle");
            player.Inventory.TryAdd("candle");
            Assert.IsTrue(r.CanPerformRitual(player.Camera.gameObject));
        }

        [Test]
        public void NoRequiredItem_CanAlwaysPerform()
        {
            Assert.IsTrue(Make().CanPerformRitual(player.Root));
        }

        [Test]
        public void Completed_ConsumesItem()
        {
            RitualPoint r = Make(requiredItem: "candle");
            player.Inventory.TryAdd("candle");
            player.Inventory.TryAdd("candle");
            r.OnRitualCompleted(player.Root);
            Assert.AreEqual(1, player.Inventory.Count("candle"), "consumes exactly one");
        }

        [Test]
        public void Completed_KeepsItem_WhenConsumeFalse()
        {
            RitualPoint r = Make(requiredItem: "candle", consume: false);
            player.Inventory.TryAdd("candle");
            r.OnRitualCompleted(player.Root);
            Assert.AreEqual(1, player.Inventory.Count("candle"));
        }

        [Test]
        public void Completed_AppliesSanityCost()
        {
            RitualPoint r = Make(sanityCost: 20f);
            r.OnRitualCompleted(player.Root);
            Assert.AreEqual(80f, player.Sanity.Current);
        }

        [Test]
        public void Completed_SetsRitualIdFlag_WhenNoFlagGiven()
        {
            RitualPoint r = Make(ritualId: "seal_door");
            r.OnRitualCompleted(player.Root);
            Assert.IsTrue(clues.HasFlag("seal_door"));
            Assert.IsTrue(r.IsDone);
            Assert.AreEqual(1, completedEvents);
        }

        [Test]
        public void Completed_SetsFlagOnComplete_WhenGiven()
        {
            RitualPoint r = Make(ritualId: "seal_door", flag: "door_sealed");
            r.OnRitualCompleted(player.Root);
            Assert.IsTrue(clues.HasFlag("door_sealed"));
            Assert.IsFalse(clues.HasFlag("seal_door"));
        }

        [Test]
        public void OneShot_BlocksSecondTime()
        {
            RitualPoint r = Make(oneShot: true);
            r.OnRitualCompleted(player.Root);
            Assert.IsFalse(r.CanPerformRitual(player.Root));
        }

        [Test]
        public void NotOneShot_CanRepeat()
        {
            RitualPoint r = Make(oneShot: false);
            r.OnRitualCompleted(player.Root);
            Assert.IsTrue(r.CanPerformRitual(player.Root));
        }

        [Test]
        public void ProgressAndCancel_ForwardToUnityEvents()
        {
            RitualPoint r = Make();
            r.OnRitualProgress(0.5f);
            r.OnRitualCancelled();
            CollectionAssert.AreEqual(new[] { 0.5f }, progressEvents);
            Assert.AreEqual(1, cancelledEvents);
        }

        [Test]
        public void Disabled_CannotPerform()
        {
            RitualPoint r = Make();
            r.enabled = false;
            Assert.IsFalse(r.CanPerformRitual(player.Root));
        }

        [UnityTest]
        public IEnumerator FullFlow_WithPlayerRitual()
        {
            RitualPoint r = Make(requiredItem: "candle", sanityCost: 10f, duration: 0.2f, pos: InFront);
            player.Inventory.TryAdd("candle");
            int completed = 0;
            player.Ritual.Completed += _ => completed++;

            player.Input.RitualHeldValue = true;
            yield return WaitUntilOrTimeout(() => completed > 0, 3f);
            yield return Frames(3);
            player.Input.RitualHeldValue = false;

            Assert.AreEqual(1, completed, "oneShot: holding on doesn't start it again");
            Assert.IsTrue(r.IsDone);
            Assert.IsTrue(clues.HasFlag("seal"));
            Assert.IsFalse(player.Inventory.Has("candle"));
            Assert.AreEqual(90f, player.Sanity.Current);
            Assert.Greater(progressEvents.Count, 0);
            Assert.AreEqual(1f, progressEvents[progressEvents.Count - 1]);
            Assert.IsFalse(player.Input.MovementLocked);
        }
    }
}
