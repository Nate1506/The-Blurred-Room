# The Blurred Room — Systems (Nate)

Code only. No scenes, prefabs or UI. Every system works alone and talks through C# events,
so it can be combined with other people's work later. Namespace: `BlurredRoom`.
Assembly: `BlurredRoom.Nate` (auto-referenced, so any script in the project can use these classes).

## Player
| Script | Does | Key events |
|---|---|---|
| `PlayerInputSource` | Abstract input; every system reads from it. `LockMovement(owner)` / `UnlockMovement(owner)` | – |
| `InputSystemPlayerInput` | New Input System (compiles only when the package is installed). WASD, mouse, Shift run, E interact, F pickup, Q peephole, R ritual | – |
| `LegacyPlayerInput` | Old Input Manager (compiles only if legacy input is enabled) | – |
| `PlayerMovement` | Walk / run (CharacterController) | `IsMoving`, `IsRunning` |
| `PlayerLook` | First-person camera (yaw body, pitch camera) | – |
| `PlayerStamina` | Drain while running, regen after delay, exhausted state | `Changed`, `Exhausted`, `Recovered` |
| `PlayerSanity` | `Drain`, `Restore`, `SetDrain(source, perSec)`, levels | `Changed`, `LevelChanged`, `Depleted` |
| `GameSettings` (static) | Sensitivity, invert Y, volume, FOV, saved in PlayerPrefs | `Changed` |
| `PlayerSight` | One raycast from the camera, shared by the scripts below | – |
| `PlayerInteractor` | Interact with `IInteractable` (doors, NPCs) | `FocusChanged`, `Interacted` |
| `PlayerPickup` + `PlayerInventory` | Pick up `IPickupable` into inventory | `FocusChanged`, `PickedUp`, `ItemAdded` |
| `PlayerRitual` | Hold to perform on `IRitualTarget` | `Started`, `ProgressChanged`, `Completed`, `Cancelled` |
| `PlayerPeephole` | Hold to look through a door's peephole | `Started(door)`, `Ended(door)` |

## Time
`TimeOfDayManager`: Day → Eve → Night → next day. `AdvancePhase()`, `SetPhase()`, `Paused`, `AutoAdvance`.
Events: `PhaseChanged`, `DayChanged`.

## Door
`Door`: open/close/lock, peephole, knocking visitors.
`BeginVisit(npc)` → knocks → player peeks (`door.Visitor`) → **Open** (`Answered: Opened`) or
**Don't open** (`DeclineVisitor()` or wait out the timer → `Answered: Ignored`).
`DoorVisitScheduler` (optional) sends random visitors during chosen phases.

## NPCs
- `NpcBase`: active only in chosen `DayPhaseMask` phases, registry, door callbacks.
- `EnemyNpc`: NavMeshAgent. Idle → sees player → Chase → Caught. Drains sanity while the player is looking at it.
- `NeutralNpc`: talk (Interact) through lines, gives clues at the end.
- `NpcRegistry` (static): all NPCs, `PlayerCaught` event.

## Clue + Ending
- `ClueData` / `EndingData`: ScriptableObjects (Create > Blurred Room > Clue / Ending).
- `ClueManager`: discovered clues + story flags.
- `EndingManager.TryEnd(reason)`: picks the highest-priority matching ending → `EndingReached`.
- Reasons: `SanityLost`, `Caught`, `TimeUp`, `Solved` (or any custom string).

## SystemHub (the "System" node)
Optional glue: sanity 0 / caught / last day → `EndingManager.TryEnd`, and locks the player when an ending plays.

## Map team hooks
Doors, `PickupItem`, `RitualPoint` and NPCs are plain components; put them on objects in your rooms.
NPC visitors appear at `Door.VisitorStandPoint`. Enemies need a baked NavMesh (AI Navigation package).
