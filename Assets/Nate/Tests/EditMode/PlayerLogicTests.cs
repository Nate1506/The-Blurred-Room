using System.Collections.Generic;
using NUnit.Framework;

namespace BlurredRoom.Tests
{
    public class PlayerInputSourceTests : NateTestBase
    {
        FakePlayerInput input;

        [SetUp]
        public void SetUp() => input = NewGO("Input").AddComponent<FakePlayerInput>();

        [Test]
        public void StartsUnlocked() => Assert.IsFalse(input.MovementLocked);

        [Test]
        public void StaysLockedUntilAllOwnersUnlock()
        {
            object a = new object(), b = new object(), c = new object();
            input.LockMovement(a);
            input.LockMovement(b);
            input.LockMovement(c);
            Assert.IsTrue(input.MovementLocked);

            input.UnlockMovement(b);
            Assert.IsTrue(input.MovementLocked);
            input.UnlockMovement(a);
            Assert.IsTrue(input.MovementLocked);
            input.UnlockMovement(c);
            Assert.IsFalse(input.MovementLocked);
        }

        [Test]
        public void SameOwnerTwice_NeedsOneUnlock()
        {
            object a = new object();
            input.LockMovement(a);
            input.LockMovement(a);
            input.UnlockMovement(a);
            Assert.IsFalse(input.MovementLocked);
        }

        [Test]
        public void UnlockByUnknownOwner_DoesNothing()
        {
            object a = new object();
            input.LockMovement(a);
            input.UnlockMovement(new object());
            Assert.IsTrue(input.MovementLocked);
        }

        [Test]
        public void NullOwner_IsIgnored()
        {
            input.LockMovement(null);
            Assert.IsFalse(input.MovementLocked);
            input.LockMovement(this);
            input.UnlockMovement(null);
            Assert.IsTrue(input.MovementLocked);
        }
    }

    public class PlayerInventoryTests : NateTestBase
    {
        PlayerInventory inv;
        readonly List<string> added = new List<string>();
        readonly List<string> removed = new List<string>();

        [SetUp]
        public void SetUp()
        {
            added.Clear();
            removed.Clear();
            inv = NewGO("Inventory").AddComponent<PlayerInventory>();
            inv.ItemAdded += added.Add;
            inv.ItemRemoved += removed.Add;
        }

        [Test]
        public void TryAdd_AddsAndFiresEvent()
        {
            Assert.IsTrue(inv.TryAdd("key"));
            Assert.IsTrue(inv.Has("key"));
            CollectionAssert.AreEqual(new[] { "key" }, inv.Items);
            CollectionAssert.AreEqual(new[] { "key" }, added);
        }

        [Test]
        public void TryAdd_NullOrEmpty_Fails()
        {
            Assert.IsFalse(inv.TryAdd(null));
            Assert.IsFalse(inv.TryAdd(""));
            Assert.AreEqual(0, inv.Items.Count);
            Assert.AreEqual(0, added.Count);
        }

        [Test]
        public void Duplicates_AreAllowed_AndCounted()
        {
            inv.TryAdd("candle");
            inv.TryAdd("candle");
            inv.TryAdd("key");
            Assert.AreEqual(2, inv.Count("candle"));
            Assert.AreEqual(1, inv.Count("key"));
            Assert.AreEqual(0, inv.Count("missing"));
            Assert.AreEqual(3, inv.Items.Count);
        }

        [Test]
        public void Capacity_LimitsItems()
        {
            TestReflection.Set(inv, "capacity", 2);
            Assert.IsTrue(inv.TryAdd("a"));
            Assert.IsFalse(inv.IsFull);
            Assert.IsTrue(inv.TryAdd("a"));
            Assert.IsTrue(inv.IsFull);
            Assert.IsFalse(inv.TryAdd("b"));
            Assert.AreEqual(2, inv.Items.Count);
            Assert.AreEqual(2, added.Count);
        }

        [Test]
        public void CapacityZero_IsUnlimited()
        {
            for (int i = 0; i < 200; i++) Assert.IsTrue(inv.TryAdd("item" + i));
            Assert.IsFalse(inv.IsFull);
        }

        [Test]
        public void Remove_RemovesOneAndFiresEvent()
        {
            inv.TryAdd("candle");
            inv.TryAdd("candle");
            Assert.IsTrue(inv.Remove("candle"));
            Assert.AreEqual(1, inv.Count("candle"));
            CollectionAssert.AreEqual(new[] { "candle" }, removed);
        }

        [Test]
        public void Remove_Missing_ReturnsFalseWithoutEvent()
        {
            Assert.IsFalse(inv.Remove("ghost"));
            Assert.IsFalse(inv.Remove(null));
            Assert.AreEqual(0, removed.Count);
        }

        [Test]
        public void Remove_FreesCapacity()
        {
            TestReflection.Set(inv, "capacity", 1);
            inv.TryAdd("a");
            Assert.IsFalse(inv.TryAdd("b"));
            inv.Remove("a");
            Assert.IsTrue(inv.TryAdd("b"));
        }
    }
}
