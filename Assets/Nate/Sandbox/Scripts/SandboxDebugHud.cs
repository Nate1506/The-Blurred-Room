using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BlurredRoom.Sandbox
{
    /// <summary>
    /// IMGUI debug HUD for the sandbox level only (no uGUI needed). Reads the systems through their public API/events.
    /// </summary>
    public class SandboxDebugHud : MonoBehaviour
    {
        [SerializeField] SandboxDebugKeys debugKeys;
        [SerializeField] float dialogueSeconds = 4f;
        [SerializeField] float popupSeconds = 3f;

        const float RefHeight = 720f;

        PlayerStamina stamina;
        PlayerSanity sanity;
        PlayerInventory inventory;
        PlayerInteractor interactor;
        PlayerPickup pickup;
        PlayerRitual ritual;
        PlayerPeephole peephole;
        TimeOfDayManager time;
        ClueManager clues;
        EndingManager endings;
        EnemyNpc enemy;

        readonly List<Door> doors = new List<Door>();
        readonly List<Action> unsubscribe = new List<Action>();
        readonly List<KeyValuePair<string, float>> messages = new List<KeyValuePair<string, float>>();
        readonly StringBuilder sb = new StringBuilder();

        Door knockDoor;
        int knockIndex, knockTotal;
        float waitUntil = -1f;
        string dialogue;
        float dialogueUntil;
        string cluePopup;
        float cluePopupUntil;
        EndingData ending;
        bool showHelp;

        GUIStyle label, center, big, small;

        public void ToggleHelp() => showHelp = !showHelp;

        /// <summary>Short message in the top-left message list (also logged to the Console).</summary>
        public void ShowMessage(string text)
        {
            messages.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + popupSeconds + 1f));
            if (messages.Count > 6) messages.RemoveAt(0);
            Debug.Log("[Sandbox] " + text);
        }

        void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player)
            {
                stamina = player.FindInRoot<PlayerStamina>();
                sanity = player.FindInRoot<PlayerSanity>();
                inventory = player.FindInRoot<PlayerInventory>();
                interactor = player.FindInRoot<PlayerInteractor>();
                pickup = player.FindInRoot<PlayerPickup>();
                ritual = player.FindInRoot<PlayerRitual>();
                peephole = player.FindInRoot<PlayerPeephole>();
            }
            time = TimeOfDayManager.Instance;
            clues = ClueManager.Instance;
            endings = EndingManager.Instance;
            enemy = FindAnyObjectByType<EnemyNpc>();

            doors.AddRange(FindObjectsByType<Door>(FindObjectsSortMode.None));
            doors.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (Door d in doors)
            {
                Door door = d;
                Action<int, int> onKnock = (i, n) => OnKnocked(door, i, n);
                Action<NpcBase, DoorAnswer> onAnswer = (npc, answer) => OnAnswered(door, npc, answer);
                Action onLocked = () => ShowMessage($"{door.name} is locked.");
                door.Knocked += onKnock;
                door.Answered += onAnswer;
                door.LockedAttempt += onLocked;
                unsubscribe.Add(() =>
                {
                    if (!door) return;
                    door.Knocked -= onKnock;
                    door.Answered -= onAnswer;
                    door.LockedAttempt -= onLocked;
                });
            }

            foreach (NeutralNpc n in FindObjectsByType<NeutralNpc>(FindObjectsSortMode.None))
            {
                NeutralNpc npc = n;
                npc.LineSpoken += OnLineSpoken;
                unsubscribe.Add(() => { if (npc) npc.LineSpoken -= OnLineSpoken; });
            }

            if (clues)
            {
                clues.ClueDiscovered += OnClueDiscovered;
                unsubscribe.Add(() => { if (clues) clues.ClueDiscovered -= OnClueDiscovered; });
            }
            if (endings)
            {
                endings.EndingReached += OnEndingReached;
                unsubscribe.Add(() => { if (endings) endings.EndingReached -= OnEndingReached; });
            }
            if (pickup)
            {
                pickup.PickupFailed += OnPickupFailed;
                unsubscribe.Add(() => { if (pickup) pickup.PickupFailed -= OnPickupFailed; });
            }
        }

        void OnDestroy()
        {
            foreach (Action a in unsubscribe) a();
            unsubscribe.Clear();
        }

        // ---------- events ----------

        void OnKnocked(Door door, int index, int total)
        {
            knockDoor = door;
            knockIndex = index;
            knockTotal = total;
            // The answer window starts after the last knock (Door.VisitRoutine). Scaled time, like WaitForSeconds.
            waitUntil = index >= total ? Time.time + SandboxKnock.AnswerWindow : -1f;
        }

        void OnAnswered(Door door, NpcBase npc, DoorAnswer answer)
        {
            if (knockDoor == door) knockDoor = null;
            string who = npc ? npc.DisplayName : "someone";
            ShowMessage(answer == DoorAnswer.Opened ? $"You opened {door.name} to {who}." : $"{who} left {door.name} (ignored).");
        }

        void OnLineSpoken(NeutralNpc npc, string line)
        {
            dialogue = $"{npc.DisplayName}: \"{line}\"";
            dialogueUntil = Time.unscaledTime + dialogueSeconds;
        }

        void OnClueDiscovered(ClueData clue)
        {
            cluePopup = "New clue: " + (string.IsNullOrEmpty(clue.Title) ? clue.Id : clue.Title);
            cluePopupUntil = Time.unscaledTime + popupSeconds;
        }

        void OnEndingReached(EndingData e, string reason)
        {
            ending = e;
            PlayerLook.SetCursorLocked(false);
        }

        void OnPickupFailed(IPickupable item) => ShowMessage($"Can't pick up {item.DisplayName}: inventory full.");

        // ---------- drawing ----------

        void OnGUI()
        {
            EnsureStyles();
            float scale = Screen.height / RefHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = RefHeight;

            if (ending)
            {
                DrawEnding(w, h);
                return;
            }

            bool peeping = peephole && peephole.IsViewing;
            if (peeping) DrawPeephole(w, h);
            else DrawCrosshairAndPrompts(w, h);

            DrawStatus();
            DrawRightPanel(w);
            DrawKnocking(w);
            DrawDialogue(w, h);
            DrawRitual(w, h);
            DrawPopups(w, h);
            if (showHelp) DrawHelp(w, h);
            else GUI.Label(new Rect(w - 210, h - 26, 200, 22), "F1: help / debug keys", small);
        }

        void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = true };
            label.normal.textColor = Color.white;
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(center) { fontSize = 34, fontStyle = FontStyle.Bold };
            small = new GUIStyle(label) { fontSize = 12, alignment = TextAnchor.MiddleRight };
        }

        void DrawCrosshairAndPrompts(float w, float h)
        {
            GUI.Label(new Rect(w / 2 - 10, h / 2 - 12, 20, 24), "+", center);

            sb.Clear();
            if (interactor && interactor.Focus != null)
            {
                sb.Append("[E] ").Append(interactor.Focus.Prompt).Append('\n');
                if (interactor.Focus is Door d && d.HasPeephole && !d.IsOpen) sb.Append("[Q hold] Peephole\n");
            }
            if (pickup && pickup.Focus != null) sb.Append("[F] ").Append(pickup.Focus.DisplayName).Append('\n');
            if (ritual && ritual.Focus != null) sb.Append("[R hold] ").Append(ritual.Focus.Prompt).Append('\n');
            if (sb.Length > 0) Shadowed(new Rect(w / 2 - 200, h / 2 + 14, 400, 80), sb.ToString().TrimEnd(), center);
        }

        void DrawStatus()
        {
            GUI.Box(new Rect(8, 8, 290, 168), GUIContent.none);
            float y = 14;
            if (stamina)
            {
                Bar(new Rect(16, y, 274, 18), stamina.Normalized, new Color(0.3f, 0.8f, 0.3f), $"Stamina {stamina.Current:0}/{stamina.Max:0}");
                if (stamina.IsExhausted) GUI.Label(new Rect(16, y + 18, 274, 20), "<color=#ff5050><b>EXHAUSTED</b></color>", label);
            }
            y += 40;
            if (sanity)
            {
                Bar(new Rect(16, y, 274, 18), sanity.Normalized, SanityColor(sanity.Level), $"Sanity {sanity.Current:0}/{sanity.Max:0}");
                GUI.Label(new Rect(16, y + 18, 274, 20), "Level: " + sanity.Level, label);
            }
            y += 42;
            if (time)
            {
                GUI.Label(new Rect(16, y, 274, 20), $"Day {time.DayNumber} - {time.Phase}{(time.Paused ? " (paused)" : "")}", label);
                Bar(new Rect(16, y + 22, 274, 12), time.PhaseProgress, new Color(0.5f, 0.6f, 0.9f), "");
            }
            y += 38;
            string flags = "";
            if (debugKeys && debugKeys.GodMode) flags += "<color=#ffd040>GOD MODE</color>  ";
            if (!Mathf.Approximately(Time.timeScale, 1f)) flags += $"<color=#ffd040>TIME x{Time.timeScale:0}</color>";
            GUI.Label(new Rect(16, y, 274, 20), flags, label);

            float my = 182;
            for (int i = messages.Count - 1; i >= 0; i--)
                if (messages[i].Value < Time.unscaledTime) messages.RemoveAt(i);
            foreach (KeyValuePair<string, float> m in messages)
            {
                Shadowed(new Rect(12, my, 420, 22), m.Key, label);
                my += 20;
            }
        }

        void DrawRightPanel(float w)
        {
            sb.Clear();
            sb.Append("<b>Inventory</b>\n");
            if (inventory && inventory.Items.Count > 0)
            {
                var counted = new List<string>();
                foreach (string id in inventory.Items)
                {
                    if (counted.Contains(id)) continue;
                    counted.Add(id);
                    int n = inventory.Count(id);
                    sb.Append("  ").Append(id).Append(n > 1 ? $" x{n}" : "").Append('\n');
                }
            }
            else sb.Append("  (empty)\n");

            sb.Append("<b>Clues</b>\n");
            if (clues && clues.ClueCount > 0)
                foreach (ClueData c in clues.Discovered) sb.Append("  ").Append(string.IsNullOrEmpty(c.Title) ? c.Id : c.Title).Append('\n');
            else sb.Append("  (none)\n");
            if (clues && clues.HasFlag("ritual_done")) sb.Append("  <color=#c080ff>flag: ritual_done</color>\n");

            if (enemy)
            {
                sb.Append("<b>Enemy (debug)</b>\n  ").Append(enemy.DisplayName).Append('\n');
                sb.Append("  ").Append(enemy.IsActive ? "State: " + enemy.State : "Inactive (not its phase)").Append('\n');
                sb.Append("  Player sees it: ").Append(enemy.IsSeenByPlayer ? "<color=#ff5050>YES</color>" : "no").Append('\n');
            }

            GUI.Box(new Rect(w - 248, 8, 240, 300), GUIContent.none);
            GUI.Label(new Rect(w - 240, 12, 226, 292), sb.ToString(), label);
        }

        void DrawKnocking(float w)
        {
            if (knockDoor && !knockDoor.HasPendingVisitor) knockDoor = null;

            float y = 12;
            if (knockDoor)
            {
                string text = $"<b>Knock knock ({knockIndex}/{knockTotal}) at {knockDoor.name}</b>";
                if (waitUntil > 0f) text += $"\nThe visitor waits {Mathf.Max(0f, waitUntil - Time.time):0}s";
                Shadowed(new Rect(w / 2 - 220, y, 440, 44), text, center);
                y += 46;
            }

            foreach (Door d in doors)
            {
                if (!d || !d.HasPendingVisitor) continue;
                Shadowed(new Rect(w / 2 - 220, y, 440, 22), $"[X] Don't open ({d.name})", center);
                break;
            }
        }

        void DrawPeephole(float w, float h)
        {
            float size = h * 0.62f;
            float x0 = (w - size) / 2f, y0 = (h - size) / 2f;
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, w, y0), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, y0 + size, w, h - y0 - size), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, y0, x0, size), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x0 + size, y0, w - x0 - size, size), Texture2D.whiteTexture);
            GUI.color = old;

            Door door = peephole.Current;
            NpcBase visitor = door ? door.Visitor : null;
            string text = visitor ? $"Outside: <b>{visitor.DisplayName}</b> ({visitor.Kind})" : "Outside: Nobody";
            Shadowed(new Rect(w / 2 - 250, y0 + size + 8, 500, 26), text, center);
            Shadowed(new Rect(w / 2 - 250, y0 - 30, 500, 24), "Peephole - release Q to stop", center);
        }

        void DrawDialogue(float w, float h)
        {
            if (string.IsNullOrEmpty(dialogue) || Time.unscaledTime > dialogueUntil) return;
            GUI.Box(new Rect(w / 2 - 320, h - 110, 640, 60), GUIContent.none);
            GUI.Label(new Rect(w / 2 - 310, h - 106, 620, 52), dialogue, center);
        }

        void DrawRitual(float w, float h)
        {
            if (!ritual || !ritual.IsPerforming) return;
            Bar(new Rect(w / 2 - 150, h / 2 + 70, 300, 20), ritual.Progress, new Color(0.7f, 0.4f, 1f),
                $"Ritual {ritual.Progress * 100f:0}%");
        }

        void DrawPopups(float w, float h)
        {
            if (string.IsNullOrEmpty(cluePopup) || Time.unscaledTime > cluePopupUntil) return;
            GUI.Box(new Rect(w / 2 - 180, h * 0.25f, 360, 34), GUIContent.none);
            GUI.Label(new Rect(w / 2 - 180, h * 0.25f, 360, 34), "<b>" + cluePopup + "</b>", center);
        }

        void DrawEnding(float w, float h)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.9f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;
            string title = string.IsNullOrEmpty(ending.Title) ? ending.Id : ending.Title;
            GUI.Label(new Rect(0, h * 0.28f, w, 50), title, big);
            GUI.Label(new Rect(w / 2 - 300, h * 0.28f + 60, 600, 160), ending.Text, center);
            GUI.Label(new Rect(0, h * 0.75f, w, 30), "Press F9 to restart", center);
        }

        void DrawHelp(float w, float h)
        {
            const string text =
                "<b>Controls</b>\n" +
                "WASD move, Mouse look, Shift run\n" +
                "E interact (doors, talk)   F pick up\n" +
                "Q (hold) peephole   R (hold) ritual\n" +
                "X  don't open (decline the visitor)\n" +
                "Esc  lock / unlock cursor\n\n" +
                "<b>Debug keys</b>\n" +
                "F1  this help\n" +
                "F2  advance time phase\n" +
                "F3  force a neutral visitor at your door\n" +
                "F4  force the enemy at your door\n" +
                "F5 / F6  sanity -20 / +20\n" +
                "F7  god mode (ignore Caught / SanityLost)\n" +
                "F8  time x4 on / off\n" +
                "F9  restart";
            GUI.Box(new Rect(w / 2 - 210, h / 2 - 190, 420, 380), GUIContent.none);
            GUI.Label(new Rect(w / 2 - 195, h / 2 - 180, 390, 360), text, label);
        }

        // ---------- helpers ----------

        void Bar(Rect r, float t, Color fill, string text)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(t), r.height - 4), Texture2D.whiteTexture);
            GUI.color = old;
            if (!string.IsNullOrEmpty(text)) GUI.Label(r, text, center);
        }

        void Shadowed(Rect r, string text, GUIStyle style)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), text, style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
        }

        static Color SanityColor(SanityLevel level)
        {
            switch (level)
            {
                case SanityLevel.Stable: return new Color(0.3f, 0.6f, 1f);
                case SanityLevel.Uneasy: return new Color(0.9f, 0.8f, 0.3f);
                case SanityLevel.Panicked: return new Color(1f, 0.5f, 0.2f);
                default: return new Color(0.9f, 0.15f, 0.15f);
            }
        }
    }
}
