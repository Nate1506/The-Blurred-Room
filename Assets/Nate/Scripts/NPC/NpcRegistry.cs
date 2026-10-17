using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlurredRoom
{
    /// <summary>All NPCs currently in the scene, plus NPC-wide events.</summary>
    public static class NpcRegistry
    {
        static readonly List<NpcBase> all = new List<NpcBase>();

        public static IReadOnlyList<NpcBase> All => all;

        /// <summary>An enemy reached the player.</summary>
        public static event Action<EnemyNpc> PlayerCaught;

        public static void Register(NpcBase npc)
        {
            if (npc && !all.Contains(npc)) all.Add(npc);
        }

        public static void Unregister(NpcBase npc) => all.Remove(npc);

        public static void GetAll(NpcKind kind, List<NpcBase> results)
        {
            results.Clear();
            foreach (NpcBase npc in all)
                if (npc && npc.Kind == kind) results.Add(npc);
        }

        public static NpcBase Find(string npcId)
        {
            foreach (NpcBase npc in all)
                if (npc && npc.NpcId == npcId) return npc;
            return null;
        }

        internal static void RaisePlayerCaught(EnemyNpc enemy) => PlayerCaught?.Invoke(enemy);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            PlayerCaught = null;
        }
    }
}
