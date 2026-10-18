#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BlurredRoom.Sandbox.Tests
{
    /// <summary>
    /// Loads NateSandbox.unity in Play mode and checks that the level is wired correctly.
    /// Any error logged while the scene runs fails the test (Unity Test Framework default).
    /// </summary>
    public class SandboxSceneSmokeTests
    {
        const string ScenePath = "Assets/Nate/Sandbox/NateSandbox.unity";

        GameObject player;
        PlayerInventory inventory;
        PlayerSanity sanity;
        PlayerInputSource input;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.captureDeltaTime = 1f / 60f; // batchmode runs uncapped; play at a normal frame rate
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            player = GameObject.FindWithTag("Player");
            inventory = player.GetComponent<PlayerInventory>();
            sanity = player.GetComponent<PlayerSanity>();
            input = player.GetComponent<PlayerInputSource>();
        }

        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        static Door FindDoor(string name) => GameObject.Find("Doors/" + name).GetComponent<Door>();

        void Teleport(Vector3 position, Quaternion rotation)
        {
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator Scene_IsWired()
        {
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.GetComponent<InputSystemPlayerInput>());
            Assert.IsNotNull(TimeOfDayManager.Instance);
            Assert.IsNotNull(ClueManager.Instance);
            Assert.IsNotNull(EndingManager.Instance);
            Assert.IsNotNull(Object.FindAnyObjectByType<SystemHub>());
            Assert.AreEqual(4, Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(3, NpcRegistry.All.Count);
            Assert.IsTrue(FindDoor("Room 4 Door").IsLocked);
            Assert.IsFalse(FindDoor("Room 1 Door").IsLocked);
            Assert.AreEqual(DayPhase.Day, TimeOfDayManager.Instance.Phase);

            var enemy = Object.FindAnyObjectByType<EnemyNpc>();
            Assert.IsTrue(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "enemy stands on the baked NavMesh");
            Assert.IsFalse(enemy.IsActive, "enemy is Night only");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Night_ShowsEnemy_HidesLandlady()
        {
            TimeOfDayManager.Instance.SetPhase(DayPhase.Night);
            yield return null;
            Assert.IsTrue(Object.FindAnyObjectByType<EnemyNpc>().IsActive);
            Assert.IsFalse(NpcRegistry.Find("npc_landlady").IsActive);
            Assert.IsTrue(NpcRegistry.Find("npc_student").IsActive);
        }

        [UnityTest]
        public IEnumerator Key_UnlocksRoom4()
        {
            Door room4 = FindDoor("Room 4 Door");
            inventory.TryAdd("room_key");
            Assert.IsFalse(room4.IsLocked);
            Assert.IsTrue(room4.TryOpen());
            yield return new WaitForSeconds(1f);
        }

        [UnityTest]
        public IEnumerator Visit_KnocksAndCanBeDeclined()
        {
            Door door = FindDoor("Room 1 Door");
            NpcBase student = NpcRegistry.Find("npc_student");
            int knocks = 0;
            door.Knocked += (i, n) => knocks++;
            Assert.IsTrue(door.BeginVisit(student, SandboxKnock.Create()));
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(SandboxKnock.Knocks, knocks);
            door.DeclineVisitor();
            Assert.IsFalse(door.HasPendingVisitor);
        }

        [UnityTest]
        public IEnumerator OpeningToEnemy_ItAppearsAndChases()
        {
            Door door = FindDoor("Room 1 Door");
            var enemy = Object.FindAnyObjectByType<EnemyNpc>();
            door.BeginVisit(enemy, SandboxKnock.Create());
            yield return null;
            door.TryOpen();
            Assert.Less(Vector3.Distance(door.VisitorStandPoint.position, enemy.transform.position), 0.3f);
            Assert.IsTrue(enemy.IsActive);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, enemy.State);
            Assert.Less(sanity.Current, sanity.Max);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Exit_RanAwayEnding_LocksPlayerAndPausesTime()
        {
            Teleport(new Vector3(18.6f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f));
            yield return null;
            Teleport(new Vector3(19.4f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f)); // into the trigger
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(EndingManager.Instance.HasEnded, "exit trigger fired");
            Assert.AreEqual("Ran away", EndingManager.Instance.Current.Title);
            Assert.IsTrue(input.MovementLocked);
            Assert.IsTrue(TimeOfDayManager.Instance.Paused);
        }

        [UnityTest]
        public IEnumerator Exit_TruthEnding_WithRitualAndThreeClues()
        {
            ClueManager clues = ClueManager.Instance;
            foreach (ClueData c in Resources.FindObjectsOfTypeAll<ClueData>())
                if (c.Id != "clue_old_photo") clues.Discover(c);
            clues.SetFlag("ritual_done");
            Assert.GreaterOrEqual(clues.ClueCount, 3);

            EndingManager.Instance.TryEnd(EndingReasons.Solved);
            Assert.AreEqual("The truth", EndingManager.Instance.Current.Title);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SanityZero_LostInThePast()
        {
            sanity.Drain(1000f);
            yield return null;
            Assert.AreEqual("Lost in the past", EndingManager.Instance.Current.Title);
        }

        [UnityTest]
        public IEnumerator EnemySeesPlayerAtNight_ChasesAndCatches()
        {
            TimeOfDayManager.Instance.SetPhase(DayPhase.Night);
            var enemy = Object.FindAnyObjectByType<EnemyNpc>();
            Teleport(new Vector3(13f, 0.05f, 0f), Quaternion.Euler(0f, 90f, 0f));
            yield return WaitFor(() => enemy.State == EnemyNpc.EnemyState.Chase, 3f);
            Assert.AreEqual(EnemyNpc.EnemyState.Chase, enemy.State);
            Assert.IsTrue(enemy.IsSeenByPlayer, "player looks down the corridor at it");

            yield return WaitFor(() => EndingManager.Instance.HasEnded, 10f);
            Assert.AreEqual("Taken", EndingManager.Instance.Current.Title);
        }

        [UnityTest]
        public IEnumerator Ritual_AtAltar_WithCandle()
        {
            inventory.TryAdd("room_key");
            inventory.TryAdd("candle");
            Door room4 = FindDoor("Room 4 Door");
            room4.TryOpen();
            yield return new WaitForSeconds(1f);
            Teleport(new Vector3(13.5f, 0.05f, 3.9f), Quaternion.identity); // facing the altar
            player.GetComponent<PlayerLook>().enabled = false; // it would reset the pitch every frame
            var cam = player.GetComponentInChildren<Camera>();
            cam.transform.parent.localRotation = Quaternion.Euler(25f, 0f, 0f);
            yield return null;

            var ritual = player.GetComponent<PlayerRitual>();
            Assert.IsNotNull(ritual.Focus, "altar is in view");
            // No keyboard in batchmode: drive the ritual target directly, as PlayerRitual would.
            ritual.Focus.OnRitualCompleted(player);
            Assert.IsTrue(ClueManager.Instance.HasFlag("ritual_done"));
            Assert.IsFalse(inventory.Has("candle"));
            Assert.AreEqual(90f, sanity.Current, 0.01f);
        }

        [UnityTest]
        public IEnumerator Restart_ReloadsCleanly()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Assert.AreEqual(3, NpcRegistry.All.Count, "no NPCs left over from the previous load");
            Assert.IsFalse(EndingManager.Instance.HasEnded);
        }

        static IEnumerator WaitFor(System.Func<bool> condition, float timeout)
        {
            float t = 0f;
            while (!condition() && t < timeout)
            {
                yield return null;
                t += Time.deltaTime;
            }
        }
    }
}
#endif
