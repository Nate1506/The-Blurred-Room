using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BlurredRoom.Sandbox
{
    /// <summary>
    /// Debug keys for the sandbox (Keyboard.current). God mode works by removing the SystemHub object
    /// (its OnDestroy unsubscribes everything) and restoring a copy when it's turned off; the systems are untouched.
    /// </summary>
    public class SandboxDebugKeys : MonoBehaviour
    {
        [SerializeField] Door playerRoomDoor;
        [SerializeField] SandboxDebugHud hud;
        [Tooltip("The SystemHub object. God mode removes it; turning god mode off restores a copy.")]
        [SerializeField] GameObject systemHub;
        [Tooltip("Same as SystemHub Last Day: used for TimeUp while god mode has removed the hub.")]
        [SerializeField] int lastDay = 3;
        [SerializeField] float sanityStep = 20f;
        [SerializeField] float fastTimeScale = 4f;

        PlayerSanity sanity;
        PlayerInputSource input;
        TimeOfDayManager time;
        EndingManager endings;
        EnemyNpc enemy;
        Vector3 enemySpawnPosition;
        Quaternion enemySpawnRotation;
        GameObject hubTemplate;
        Transform hubParent;

        public bool GodMode { get; private set; }

        void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player)
            {
                sanity = player.FindInRoot<PlayerSanity>();
                input = player.FindInRoot<PlayerInputSource>();
            }
            time = TimeOfDayManager.Instance;
            endings = EndingManager.Instance;
            enemy = FindAnyObjectByType<EnemyNpc>();
            if (enemy)
            {
                enemySpawnPosition = enemy.transform.position;
                enemySpawnRotation = enemy.transform.rotation;
            }

            if (systemHub)
            {
                // Keep an inactive copy (no Awake/Start runs under an inactive parent) to restore after god mode.
                hubParent = systemHub.transform.parent;
                var holder = new GameObject("SystemHub template (god mode)");
                holder.SetActive(false);
                holder.transform.SetParent(transform, false);
                hubTemplate = Instantiate(systemHub, holder.transform);
                hubTemplate.name = systemHub.name;
            }

            NpcRegistry.PlayerCaught += OnPlayerCaught;
            if (sanity) sanity.Depleted += OnSanityDepleted;
            if (time) time.DayChanged += OnDayChanged;
            if (endings) endings.EndingReached += OnEndingReached;
        }

        void OnDestroy()
        {
            NpcRegistry.PlayerCaught -= OnPlayerCaught;
            if (sanity) sanity.Depleted -= OnSanityDepleted;
            if (time) time.DayChanged -= OnDayChanged;
            if (endings) endings.EndingReached -= OnEndingReached;
        }

        void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame && hud) hud.ToggleHelp();
            if (kb.xKey.wasPressedThisFrame) DeclineVisitors();
            if (kb.f2Key.wasPressedThisFrame && time)
            {
                time.AdvancePhase();
                Message($"Phase -> {time.Phase} (day {time.DayNumber})");
            }
            if (kb.f3Key.wasPressedThisFrame) ForceVisit(NpcKind.Neutral);
            if (kb.f4Key.wasPressedThisFrame) ForceVisit(NpcKind.Enemy);
            if (kb.f5Key.wasPressedThisFrame && sanity) sanity.Drain(sanityStep);
            if (kb.f6Key.wasPressedThisFrame && sanity) sanity.Restore(sanityStep);
            if (kb.f7Key.wasPressedThisFrame) SetGodMode(!GodMode);
            if (kb.f8Key.wasPressedThisFrame)
            {
                Time.timeScale = Mathf.Approximately(Time.timeScale, 1f) ? fastTimeScale : 1f;
                Message($"Time scale x{Time.timeScale:0}");
            }
            if (kb.f9Key.wasPressedThisFrame) Restart();
            if (kb.escapeKey.wasPressedThisFrame)
                PlayerLook.SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
        }

        void DeclineVisitors()
        {
            bool any = false;
            foreach (Door door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                if (!door.HasPendingVisitor) continue;
                door.DeclineVisitor();
                any = true;
            }
            if (!any) Message("Nobody is waiting at a door.");
        }

        void ForceVisit(NpcKind kind)
        {
            Door door = playerRoomDoor;
            if (!door) { Message("No player room door assigned."); return; }
            if (door.IsOpen) { Message($"Close {door.name} first."); return; }
            if (door.HasPendingVisitor) { Message($"{door.name} already has a visitor."); return; }

            NpcBase visitor = null;
            foreach (NpcBase npc in NpcRegistry.All)
            {
                if (!npc || npc.Kind != kind || npc.VisitingDoor) continue;
                if (visitor == null || (visitor.IsBusy && !npc.IsBusy)) visitor = npc;
            }
            if (!visitor) { Message($"No free {kind} NPC."); return; }

            if (door.BeginVisit(visitor, SandboxKnock.Create()))
                Message($"Forced visit: {visitor.DisplayName} knocks at {door.name}" +
                        (visitor.IsBusy ? " (note: it is busy)" : ""));
            else Message("BeginVisit refused.");
        }

        void SetGodMode(bool on)
        {
            if (on == GodMode) return;
            GodMode = on;
            if (on)
            {
                if (systemHub) Destroy(systemHub);
                systemHub = null;
            }
            else if (hubTemplate)
            {
                systemHub = Instantiate(hubTemplate, hubParent);
                systemHub.name = hubTemplate.name;
            }
            Message(on ? "God mode ON: Caught / SanityLost are only logged." : "God mode OFF: SystemHub restored.");
        }

        // ---------- god mode replacements for SystemHub (only active while the hub is removed) ----------

        void OnPlayerCaught(EnemyNpc e)
        {
            if (!GodMode) return;
            Message($"GOD MODE: caught by {(e ? e.DisplayName : "an enemy")} (ignored, enemy sent back).");
            if (e && e == enemy)
            {
                NavMeshAgent agent = e.GetComponent<NavMeshAgent>();
                if (agent && agent.isOnNavMesh) agent.Warp(enemySpawnPosition);
                e.transform.SetPositionAndRotation(enemySpawnPosition, enemySpawnRotation);
            }
            if (e) e.ResetToIdle();
        }

        void OnSanityDepleted()
        {
            if (GodMode) Message("GOD MODE: sanity reached 0 (SanityLost ignored).");
        }

        void OnDayChanged(int day)
        {
            if (GodMode && lastDay > 0 && day > lastDay && endings) endings.TryEnd(EndingReasons.TimeUp);
        }

        void OnEndingReached(EndingData ending, string reason)
        {
            if (!GodMode) return; // SystemHub does this otherwise
            if (input) input.LockMovement(this);
            if (time) time.Paused = true;
        }

        void Restart()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Works without adding the scene to Build Settings (which lives outside Assets/Nate).
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }

        void Message(string text)
        {
            if (hud) hud.ShowMessage(text);
            else Debug.Log("[Sandbox] " + text);
        }
    }
}
