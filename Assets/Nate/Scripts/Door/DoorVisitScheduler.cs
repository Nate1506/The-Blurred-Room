using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>
    /// Optional: every so often (during chosen phases) picks a door and an NPC and starts a knock visit.
    /// Remove it if visits are scripted by the story instead.
    /// </summary>
    public class DoorVisitScheduler : MonoBehaviour
    {
        [SerializeField] Door[] doors;
        [SerializeField] DayPhaseMask activePhases = DayPhaseMask.Eve | DayPhaseMask.Night;
        [SerializeField] Vector2 secondsBetweenVisits = new Vector2(40f, 90f);
        [SerializeField, Range(0f, 1f)] float enemyChance = 0.4f;

        float timer;
        readonly List<NpcBase> candidates = new List<NpcBase>();
        readonly List<Door> freeDoors = new List<Door>();

        void Start() => ResetTimer();

        void Update()
        {
            TimeOfDayManager time = TimeOfDayManager.Instance;
            if (time && !activePhases.Includes(time.Phase)) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            ResetTimer();
            TryStartVisit();
        }

        public bool TryStartVisit()
        {
            Door door = PickDoor();
            if (!door) return false;

            NpcKind kind = Random.value < enemyChance ? NpcKind.Enemy : NpcKind.Neutral;
            NpcBase npc = PickVisitor(kind) ?? PickVisitor(kind == NpcKind.Enemy ? NpcKind.Neutral : NpcKind.Enemy);
            return npc && door.BeginVisit(npc);
        }

        Door PickDoor()
        {
            freeDoors.Clear();
            if (doors != null)
                foreach (Door d in doors)
                    if (d && !d.IsOpen && !d.HasPendingVisitor) freeDoors.Add(d);
            return freeDoors.Count > 0 ? freeDoors[Random.Range(0, freeDoors.Count)] : null;
        }

        NpcBase PickVisitor(NpcKind kind)
        {
            candidates.Clear();
            foreach (NpcBase npc in NpcRegistry.All)
                if (npc && npc.Kind == kind && npc.CanBeVisitor && !npc.IsBusy && !npc.VisitingDoor) candidates.Add(npc);
            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        void ResetTimer()
        {
            timer = Random.Range(secondsBetweenVisits.x, secondsBetweenVisits.y);
        }
    }
}
