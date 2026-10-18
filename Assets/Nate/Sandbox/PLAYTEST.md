# Nate Sandbox — Playtest checklist

Open `Assets/Nate/Sandbox/NateSandbox.unity` and press **Play**. Click the Game view to capture the mouse.
Press **F1** in game for the controls. You start in **Room 1** (caretaker room), facing the door.

**Map:** a corridor running east (+X), 20 m long. Room 1 is yours (south-west), Room 2 is opposite it (Student),
Room 3 is south-east (Old photo), and Room 4 is north-east (**locked**, altar). The Landlady stands in the corridor.
The Blurred Man (red) waits at the far east end at night, next to the green **Exit** box.
Items sit on small tables: the Diary (Room 1), a Candle (Room 2), the Room key (corridor, east end, south side),
a Candle (corridor, west end) and the Old photo (Room 3).

**Controls:** WASD move · Mouse look · Shift run · E interact · F pick up · Q (hold) peephole · R (hold) ritual · X don't open · Esc cursor
**Debug:** F1 help · F2 next phase · F3 neutral visitor at your door · F4 enemy at your door · F5/F6 sanity −20/+20 ·
F7 god mode · F8 time ×4 · F9 restart

Tip: phases are Day 60 s, Eve 30 s, Night 90 s. Use **F2** to jump phases and **F8** to speed things up.

---

## Movement and stamina
- [ ] Walk with WASD → you move at walking speed; the stamina bar stays full.
- [ ] Hold Shift while moving → you move faster and the stamina bar drains.
- [ ] Hold Shift without moving → stamina does not drain.
- [ ] Run until stamina is empty → **EXHAUSTED** shows and you can't run (normal speed even with Shift).
- [ ] Let go and wait → after about 1 s the bar refills; you can run again only when it is back to about **30%**.

## Look and settings
- [ ] Move the mouse → left/right turns the body, up/down tilts the camera, and you can't look past straight up or down.
- [ ] Esc → the cursor unlocks; Esc again → it locks.
- [ ] (Optional) Change `GameSettings` from a script or the Console (sensitivity / invert Y / FOV) → it applies immediately.

## Sanity levels
- [ ] F5 a few times → the sanity bar drops; the level text goes **Stable → Uneasy (<70) → Panicked (<40) → Breaking (<15)**
      and the bar colour changes.
- [ ] F6 → sanity goes back up and the level text follows.
- [ ] (God mode off) F5 down to 0 → the **"Lost in the past"** ending (see Endings).

## Pickup and inventory
- [ ] Look at the Diary on the table in Room 1 → **"[F] Diary"** under the crosshair.
- [ ] Press F → it disappears, **diary** appears in Inventory, and the "New clue: Diary" popup shows.
- [ ] Pick up both candles → Inventory shows **candle x2**.
- [ ] Look away from an item → the prompt disappears.

## Doors, locked door and key
- [ ] Look at your door → **"[E] Open"** and **"[Q hold] Peephole"**.
- [ ] E → the door swings open; E again → it closes.
- [ ] Try Room 4's door → it stays shut, **"Locked"** shows as the prompt and a "Room 4 Door is locked." message appears.
- [ ] Pick up the **Room key** (corridor, east end) → Room 4's prompt changes to "Open" and it opens.

## Knocking, peephole, open / don't open — neutral visitor
- [ ] Close your door, press **F3** → "Knock knock (1/3)… (3/3) at Room 1 Door", then a countdown
      **"The visitor waits 12s"** and **"[X] Don't open"**.
- [ ] Hold **Q** while looking at the door → a dark frame and **"Outside: Landlady (Neutral)"** (or Student).
      You can't move while looking. Release Q → normal view.
- [ ] Hold Q with nobody outside → **"Outside: Nobody"**.
- [ ] F3 again, then press **X** → message "<name> left Room 1 Door (ignored)"; the visitor hides.
- [ ] F3 again and wait out the 12 s → same "ignored" result.
- [ ] F3 again and press **E** to open → the NPC appears outside the door and says their first line (dialogue box).

## Knocking — enemy visitor
- [ ] Close the door, press **F4** → knocking; the peephole shows **"Outside: The Blurred Man (Enemy)"**.
- [ ] X → it is ignored and nothing happens.
- [ ] F4 again and **open** the door → the red capsule appears right outside, **sanity drops by 15**, and the enemy debug panel shows **Chase**.
- [ ] (Eve/Night only) Random visits also happen every 25–45 s (DoorVisitScheduler).

## Enemy: see, chase, lose, caught
- [ ] F2 to **Night** → the red capsule appears at the east end ("State: Idle").
- [ ] Look at it → "Player sees it: **YES**" and sanity slowly drains; look away → the drain stops.
- [ ] Walk into its view (corridor, in front of it) → **Chase**; it walks to you.
- [ ] Run back into a room and stay out of sight → after about 6 s it goes back to **Idle**.
- [ ] Let it reach you → the **"Taken"** ending (god mode off).
- [ ] With **F7 god mode on**, being caught only logs a message and the enemy is sent back to its spot.

## Ritual
- [ ] Open Room 4 (key) and have at least one candle. Look at the purple altar → **"[R hold] Perform ritual (needs a candle)"**.
- [ ] Without a candle → no prompt.
- [ ] Hold R → a progress bar fills over **4 s** and you can't move while it fills.
- [ ] Release early → the bar resets and you can move again.
- [ ] Hold until done → one candle is used, **sanity −10**, and **flag: ritual_done** shows in the Clues panel.
      The altar can't be used again.

## Time phases
- [ ] Watch the time line: **Day 1 – Day** with a progress bar. Day lasts 60 s, Eve 30 s, Night 90 s.
- [ ] F2 → Day → Eve → Night → **Day 2**.
- [ ] F8 → "TIME x4" shows and everything runs 4× faster; F8 again → normal.

## NPC day / night appearance
- [ ] Day: Landlady (corridor) and Student (Room 2) are visible; the Blurred Man is not.
- [ ] Eve: Landlady and Student are visible.
- [ ] Night: the Landlady disappears, the Student stays, the Blurred Man appears.

## Clues
- [ ] Talk to the Landlady with E three times → three lines, then "New clue: Landlady's story".
- [ ] Talk to the Student three times → "New clue: Student's rumour".
- [ ] Old photo (Room 3) and Diary (Room 1) → their clues. All four appear in the Clues list.

## Endings (F9 restarts after each)
- [ ] **Lost in the past**: F5 until sanity hits 0.
- [ ] **Taken**: get caught by the Blurred Man at night.
- [ ] **Time's up**: survive past day 3 (F2 nine times, or F8 and wait). The ending shows when Day 4 starts.
- [ ] **Ran away**: walk into the green Exit box without doing the ritual.
- [ ] **The truth**: collect at least 3 clues, do the ritual, then walk into the Exit.
- [ ] On every ending: the ending screen shows the title + text + "Press F9 to restart", you can't move, and time stops.
- [ ] God mode (F7) → sanity 0 and being caught don't end the game (only messages). Time's up / exit still work.

## Restart
- [ ] F9 → the scene reloads: full stamina/sanity, Day 1, empty inventory, doors closed, Room 4 locked, god mode off, time ×1.

---

### Known limits of the sandbox
- The "visitor waits Ns" countdown uses the sandbox knock pattern (3 knocks, 0.8 s, 12 s). The builder writes the same
  values into every door, but `Door` doesn't expose the remaining time, so a different `KnockAgain` pattern would show a wrong countdown.
- Visitors don't walk to the door; they knock from wherever they are and appear at the stand point when you open.
- The player's CharacterController uses `Min Move Distance = 0`. With the default 0.001, movement is skipped at very
  high frame rates.
- Rebuilding the scene (`SandboxSceneBuilder.Build`) overwrites the scene, materials, data and NavMesh in `Assets/Nate/Sandbox`.
