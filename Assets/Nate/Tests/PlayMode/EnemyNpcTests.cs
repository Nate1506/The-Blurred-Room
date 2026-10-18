#if BR_AI_NAVIGATION_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace BlurredRoom.Tests
{
    /// <summary>
    /// A 40x40 NavMesh is baked at runtime with NavMeshSurface for every test.
    /// The enemy stands at the origin. chaseSpeed defaults to 0 here so it doesn't walk during detection tests.
    /// </summary>
    public class EnemyNpcTests : NateTestBase
    {
        static readonly Quaternion FacingBack = Quaternion.Euler(0f, 180f, 0f);

        [SetUp]
        public void BuildNavMesh()
        {
            GameObject root = NewGO("NavRoot");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);

            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            Physics.SyncTransforms();
        }

        EnemyNpc MakeEnemy(Vector3 pos, Quaternion rot, TestPlayer player, System.Action<EnemyNpc> configure = null)
        {
            GameObject go = NewGO("Enemy", pos, false);
            go.transform.rotation = rot;
            var e = go.AddComponent<EnemyNpc>();
            TestReflection.Set(e, "player", player.Transform);
            TestReflection.Set(e, "sightRange", 10f);
            TestReflection.Set(e, "sightAngle", 120f);
            TestReflection.Set(e, "loseTargetAfter", 0.3f);
            TestReflection.Set(e, "chaseSpeed", 0f);
            configure?.Invoke(e);
            go.SetActive(true);
            return e;
        }

        TestPlayer MakePlayer(Vector3 pos, Quaternion rot) => NewPlayer(pos, rot);

        [UnityTest]
        public IEnumerator NavMeshAgent_IsOnNavMesh()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, -15f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            yield return null;
            Assert.IsTrue(e.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh);
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_RespectsRange()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            yield return null;
            Assert.IsTrue(e.CanSeePlayer());

            MoveTo(p.Root, new Vector3(0f, 0f, 15f));
            Assert.IsFalse(e.CanSeePlayer());
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_RespectsAngle()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, -5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            yield return null;
            Assert.IsFalse(e.CanSeePlayer(), "behind");

            MoveTo(p.Root, new Vector3(4f, 0f, 4f));
            Assert.IsTrue(e.CanSeePlayer(), "45 degrees, inside a 120 degree cone");

            MoveTo(p.Root, new Vector3(5f, 0f, 0.5f));
            Assert.IsFalse(e.CanSeePlayer(), "about 84 degrees, outside the cone");
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_BlockedByWalls()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            yield return null;
            NewBox("Wall", new Vector3(0f, 1.5f, 2.5f), new Vector3(4f, 3f, 0.2f));
            Assert.IsFalse(e.CanSeePlayer());
        }

        [UnityTest]
        public IEnumerator CanSeePlayer_FalseWithoutPlayer()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p, c => TestReflection.Set(c, "player", null));
            p.Root.tag = "Untagged";
            yield return null;
            Assert.IsFalse(e.CanSeePlayer());
        }

        [UnityTest]
        public IEnumerator FindsPlayerByTag_WhenNotAssigned()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p, c => TestReflection.Set(c, "player", null));
            yield return null;
            Assert.IsTrue(e.CanSeePlayer());
        }

        [UnityTest]
        public IEnumerator Idle_ToChase_WhenItSeesThePlayer()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            var states = new List<EnemyNpc.EnemyState>();
            e.StateChanged += states.Add;
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.IsFalse(e.IsBusy);

            yield return Frames(2);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, e.State);
            Assert.IsTrue(e.IsBusy);
            CollectionAssert.AreEqual(new[] { EnemyNpc.EnemyState.Chase }, states);
        }

        [UnityTest]
        public IEnumerator Chase_BackToIdle_AfterLosingSight()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);
            yield return Frames(2);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, e.State);

            MoveTo(p.Root, new Vector3(0f, 0f, 30f)); // out of range
            float elapsed = 0f;
            while (e.State == EnemyNpc.EnemyState.Chase && elapsed < 3f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (e.State == EnemyNpc.EnemyState.Chase)
                    Assert.Less(elapsed, 0.3f + 1e-4f, "should have given up after loseTargetAfter");
            }
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.GreaterOrEqual(elapsed, 0.3f - 1e-4f, "kept chasing for loseTargetAfter seconds");
        }

        [UnityTest]
        public IEnumerator Chase_SeeingAgainResetsLoseTimer()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p, c => TestReflection.Set(c, "loseTargetAfter", 0.5f));
            yield return Frames(2);

            MoveTo(p.Root, new Vector3(0f, 0f, 30f));
            yield return new WaitForSeconds(0.3f);
            MoveTo(p.Root, new Vector3(0f, 0f, 5f));
            yield return Frames(2);
            MoveTo(p.Root, new Vector3(0f, 0f, 30f));
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, e.State, "0.3s + 0.3s unseen, but the timer was reset in between");
        }

        [UnityTest]
        public IEnumerator Caught_WithinRange_FiresPlayerCaught()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 1f), FacingBack);
            var caught = new List<EnemyNpc>();
            NpcRegistry.PlayerCaught += caught.Add;
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p);

            yield return Frames(3);
            Assert.AreEqual(EnemyNpc.EnemyState.Caught, e.State);
            CollectionAssert.AreEqual(new[] { e }, caught);

            yield return Frames(3);
            Assert.AreEqual(1, caught.Count, "PlayerCaught fires once");
        }

        [UnityTest]
        public IEnumerator Chase_MovesTowardsPlayer()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 8f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p, c => TestReflection.Set(c, "chaseSpeed", 4f));
            yield return new WaitForSeconds(0.8f);
            Assert.Greater(e.transform.position.z, 0.5f);
        }

        [UnityTest]
        public IEnumerator Sanity_DrainsWhileSeen_StopsWhenNot()
        {
            // Enemy faces away from the player (so it never chases); the player looks at it.
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, FacingBack, p);
            yield return Frames(2);
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.IsTrue(e.IsSeenByPlayer);
            float v = p.Sanity.Current;
            yield return Frames(5);
            Assert.Less(p.Sanity.Current, v, "drains while the player sees the enemy");

            p.Transform.rotation = Quaternion.identity; // look away
            yield return Frames(2);
            Assert.IsFalse(e.IsSeenByPlayer);
            v = p.Sanity.Current;
            yield return Frames(5);
            Assert.AreEqual(v, p.Sanity.Current, "no drain when not seen");
        }

        [UnityTest]
        public IEnumerator Sanity_NotDrained_WhenWallBlocksView()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            NewBox("Wall", new Vector3(0f, 1.5f, 2.5f), new Vector3(4f, 3f, 0.2f));
            EnemyNpc e = MakeEnemy(Vector3.zero, FacingBack, p);
            yield return Frames(5);
            Assert.IsFalse(e.IsSeenByPlayer);
            Assert.AreEqual(p.Sanity.Max, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator Sanity_NotDrained_BeyondSeenMaxDistance()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, FacingBack, p, c => TestReflection.Set(c, "seenMaxDistance", 3f));
            yield return Frames(5);
            Assert.IsFalse(e.IsSeenByPlayer);
            Assert.AreEqual(p.Sanity.Max, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator Sanity_DrainCleared_WhenEnemyDisabled()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, FacingBack, p);
            yield return Frames(3);
            Assert.IsTrue(e.IsSeenByPlayer);

            e.gameObject.SetActive(false);
            Assert.IsFalse(e.IsSeenByPlayer);
            float v = p.Sanity.Current;
            yield return Frames(5);
            Assert.AreEqual(v, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator Sanity_DrainCleared_WhenComponentDisabled()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, FacingBack, p);
            yield return Frames(3);
            e.enabled = false;
            float v = p.Sanity.Current;
            yield return Frames(5);
            Assert.AreEqual(v, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator DoorOpenedToEnemy_AppearsHitsSanityAndChases()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, -18f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(new Vector3(5f, 0f, 5f), Quaternion.identity, p);
            GameObject doorGo = NewGO("Door", new Vector3(0f, 0f, 9f), false);
            var door = doorGo.AddComponent<Door>();
            var stand = new GameObject("Stand").transform;
            stand.SetParent(doorGo.transform, false);
            stand.localPosition = new Vector3(0f, 0f, -1f);
            stand.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TestReflection.Set(door, "visitorStandPoint", stand);
            doorGo.SetActive(true);
            yield return Frames(2);
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.AreEqual(100f, p.Sanity.Current);

            Assert.IsTrue(door.BeginVisit(e));
            Assert.AreSame(door, e.VisitingDoor);
            door.TryOpen();

            Assert.That(Vector3.Distance(new Vector3(0f, 0f, 8f), e.transform.position), Is.LessThan(0.2f));
            Assert.That(Quaternion.Angle(stand.rotation, e.transform.rotation), Is.LessThan(0.5f));
            Assert.That(p.Sanity.Current, Is.EqualTo(85f).Within(0.001f));
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, e.State);
            Assert.IsNull(e.VisitingDoor);
        }

        [UnityTest]
        public IEnumerator DoorIgnored_EnemyStaysPut()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, -18f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(new Vector3(5f, 0f, 5f), Quaternion.identity, p);
            Door door = NewComponent<Door>("Door", new Vector3(0f, 0f, 9f));
            yield return Frames(2);
            Vector3 pos = e.transform.position;

            door.BeginVisit(e);
            door.DeclineVisitor();

            Assert.That(Vector3.Distance(pos, e.transform.position), Is.LessThan(0.001f));
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.AreEqual(100f, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator InactivePhase_DoesNotChaseOrDrain()
        {
            NewComponent<TimeOfDayManager>("Time", t => TestReflection.Set(t, "autoAdvance", false));
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 5f), FacingBack);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p,
                c => TestReflection.Set(c, "activePhases", DayPhaseMask.Night));
            yield return Frames(3);
            Assert.IsFalse(e.IsActive);
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.IsFalse(e.IsSeenByPlayer);
            Assert.AreEqual(p.Sanity.Max, p.Sanity.Current);
        }

        [UnityTest]
        public IEnumerator ResetToIdle_StopsChase()
        {
            TestPlayer p = MakePlayer(new Vector3(0f, 0f, 8f), Quaternion.identity);
            EnemyNpc e = MakeEnemy(Vector3.zero, Quaternion.identity, p, c => TestReflection.Set(c, "chaseSpeed", 4f));
            var agent = e.GetComponent<UnityEngine.AI.NavMeshAgent>();
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, e.State);
            Assert.Greater(e.transform.position.z, 0.1f, "was moving");

            MoveTo(p.Root, new Vector3(0f, 0f, 30f));
            Vector3 atReset = e.transform.position;
            e.ResetToIdle();
            Assert.AreEqual(EnemyNpc.EnemyState.Idle, e.State);
            Assert.IsFalse(agent.hasPath);
            Assert.IsTrue(agent.isStopped);

            // The agent brakes with its acceleration (it is not frozen instantly), then stays at rest.
            yield return new WaitForSeconds(1f);
            Assert.That(agent.velocity.magnitude, Is.LessThan(0.01f), "came to rest");
            Assert.IsTrue(agent.isStopped, "stays stopped while idle");
            float braking = 4f * 4f / (2f * agent.acceleration) + 0.2f;
            Assert.That(Vector3.Distance(atReset, e.transform.position), Is.LessThan(braking),
                "only the braking distance, no further pursuit");
            Vector3 rest = e.transform.position;
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(rest, e.transform.position), Is.LessThan(0.001f));
        }
    }
}
#endif
