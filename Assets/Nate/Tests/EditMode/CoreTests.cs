using NUnit.Framework;
using UnityEngine;

namespace BlurredRoom.Tests
{
    public class DayPhaseMaskTests
    {
        [TestCase(DayPhaseMask.None, DayPhase.Day, false)]
        [TestCase(DayPhaseMask.None, DayPhase.Eve, false)]
        [TestCase(DayPhaseMask.None, DayPhase.Night, false)]
        [TestCase(DayPhaseMask.Day, DayPhase.Day, true)]
        [TestCase(DayPhaseMask.Day, DayPhase.Eve, false)]
        [TestCase(DayPhaseMask.Day, DayPhase.Night, false)]
        [TestCase(DayPhaseMask.Eve, DayPhase.Day, false)]
        [TestCase(DayPhaseMask.Eve, DayPhase.Eve, true)]
        [TestCase(DayPhaseMask.Eve, DayPhase.Night, false)]
        [TestCase(DayPhaseMask.Night, DayPhase.Day, false)]
        [TestCase(DayPhaseMask.Night, DayPhase.Eve, false)]
        [TestCase(DayPhaseMask.Night, DayPhase.Night, true)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Eve, DayPhase.Day, true)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Eve, DayPhase.Eve, true)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Eve, DayPhase.Night, false)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Night, DayPhase.Day, true)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Night, DayPhase.Eve, false)]
        [TestCase(DayPhaseMask.Day | DayPhaseMask.Night, DayPhase.Night, true)]
        [TestCase(DayPhaseMask.Eve | DayPhaseMask.Night, DayPhase.Day, false)]
        [TestCase(DayPhaseMask.Eve | DayPhaseMask.Night, DayPhase.Eve, true)]
        [TestCase(DayPhaseMask.Eve | DayPhaseMask.Night, DayPhase.Night, true)]
        [TestCase(DayPhaseMask.All, DayPhase.Day, true)]
        [TestCase(DayPhaseMask.All, DayPhase.Eve, true)]
        [TestCase(DayPhaseMask.All, DayPhase.Night, true)]
        public void Includes(DayPhaseMask mask, DayPhase phase, bool expected)
        {
            Assert.AreEqual(expected, mask.Includes(phase));
        }
    }

    public class SightUtilTests : NateTestBase
    {
        // Far away from anything that may be in the scene the editor has open.
        static readonly Vector3 O = new Vector3(5000f, 5000f, 5000f);
        static readonly Vector3 From = O + new Vector3(0f, 0f, -5f);
        static readonly Vector3 To = O + new Vector3(0f, 0f, 5f);

        GameObject Wall(Vector3 pos) => NewBox("Wall", pos, new Vector3(4f, 4f, 0.2f));

        [Test]
        public void ClearLine_IsVisible()
        {
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, null));
        }

        [Test]
        public void WallInBetween_IsNotVisible()
        {
            Wall(O);
            Assert.IsFalse(SightUtil.HasLineOfSight(From, To, null, null));
        }

        [Test]
        public void WallUnderIgnoreRoot_IsSkipped()
        {
            GameObject ignoreRoot = NewGO("Ignore", O);
            Wall(O).transform.SetParent(ignoreRoot.transform, true);
            Physics.SyncTransforms();
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, ignoreRoot.transform, null));
        }

        [Test]
        public void WallThatIsIgnoreRootItself_IsSkipped()
        {
            GameObject wall = Wall(O);
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, wall.transform, null));
        }

        [Test]
        public void HittingTarget_CountsAsVisible()
        {
            GameObject target = NewBox("Target", To, Vector3.one);
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, target.transform));
        }

        [Test]
        public void HittingTargetChildCollider_CountsAsVisible()
        {
            GameObject targetRoot = NewGO("TargetRoot", To);
            NewBox("TargetPart", To, Vector3.one).transform.SetParent(targetRoot.transform, true);
            Physics.SyncTransforms();
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, targetRoot.transform));
        }

        [Test]
        public void HittingNonTargetCollider_AtEnd_IsNotVisible()
        {
            NewBox("SomethingElse", To, Vector3.one);
            Assert.IsFalse(SightUtil.HasLineOfSight(From, To, null, null));
        }

        [Test]
        public void TargetBehindWall_IsNotVisible()
        {
            Wall(O);
            GameObject target = NewBox("Target", To, Vector3.one);
            Assert.IsFalse(SightUtil.HasLineOfSight(From, To, null, target.transform));
        }

        [Test]
        public void WallBehindTarget_DoesNotMatter()
        {
            GameObject target = NewBox("Target", O, Vector3.one);
            Wall(O + new Vector3(0f, 0f, 2f));
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, target.transform));
        }

        [Test]
        public void TriggerColliders_AreIgnored()
        {
            Wall(O).GetComponent<Collider>().isTrigger = true;
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, null));
        }

        [Test]
        public void LayerMask_ExcludesWallLayer()
        {
            GameObject wall = Wall(O);
            wall.layer = 2;
            Assert.IsTrue(SightUtil.HasLineOfSight(From, To, null, null, ~(1 << 2)));
            Assert.IsFalse(SightUtil.HasLineOfSight(From, To, null, null, ~0));
        }

        [Test]
        public void SamePoint_IsVisible()
        {
            Wall(O);
            Assert.IsTrue(SightUtil.HasLineOfSight(O, O, null, null));
        }
    }

    public class FindInRootTests : NateTestBase
    {
        [Test]
        public void FindsComponentOnSiblingBranch()
        {
            GameObject root = NewGO("Root");
            var a = new GameObject("A"); a.transform.SetParent(root.transform);
            var a1 = new GameObject("A1"); a1.transform.SetParent(a.transform);
            var b = new GameObject("B"); b.transform.SetParent(root.transform);
            var inv = b.AddComponent<PlayerInventory>();

            Assert.AreSame(inv, a1.FindInRoot<PlayerInventory>());
            Assert.AreSame(inv, a1.transform.FindInRoot<PlayerInventory>());
        }

        [Test]
        public void FindsComponentOnRootAndOnChildFromRoot()
        {
            GameObject root = NewGO("Root");
            var rootInv = root.AddComponent<PlayerInventory>();
            var child = new GameObject("Child"); child.transform.SetParent(root.transform);
            var childStamina = child.AddComponent<PlayerStamina>();

            Assert.AreSame(rootInv, child.FindInRoot<PlayerInventory>());
            Assert.AreSame(childStamina, root.FindInRoot<PlayerStamina>());
        }

        [Test]
        public void FindsComponentOnInactiveChild()
        {
            GameObject root = NewGO("Root");
            var child = new GameObject("Child"); child.transform.SetParent(root.transform);
            var inv = child.AddComponent<PlayerInventory>();
            child.SetActive(false);

            Assert.AreSame(inv, root.FindInRoot<PlayerInventory>());
        }

        [Test]
        public void DoesNotFindComponentInAnotherRoot()
        {
            GameObject root = NewGO("Root");
            NewGO("Other").AddComponent<PlayerInventory>();
            Assert.IsNull(root.FindInRoot<PlayerInventory>());
        }

        [Test]
        public void NullInput_ReturnsNull()
        {
            Assert.IsNull(((GameObject)null).FindInRoot<PlayerInventory>());
            Assert.IsNull(((Component)null).FindInRoot<PlayerInventory>());
        }

        [Test]
        public void DestroyedInput_ReturnsNull()
        {
            var go = new GameObject("Destroyed");
            go.AddComponent<PlayerInventory>();
            Object.DestroyImmediate(go);
            Assert.IsNull(go.FindInRoot<PlayerInventory>());
        }
    }
}
