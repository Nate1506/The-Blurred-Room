using UnityEngine;

namespace BlurredRoom
{
    public static class SightUtil
    {
        static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>
        /// True if nothing blocks the line from <paramref name="from"/> to <paramref name="to"/>,
        /// ignoring colliders under <paramref name="ignoreRoot"/>. Hitting <paramref name="targetRoot"/> counts as visible.
        /// </summary>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, Transform ignoreRoot, Transform targetRoot,
                                          int layerMask = ~0)
        {
            Vector3 delta = to - from;
            float dist = delta.magnitude;
            if (dist < 0.01f) return true;

            int count = Physics.RaycastNonAlloc(from, delta / dist, Hits, dist, layerMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            bool nearestIsTarget = false;
            for (int i = 0; i < count; i++)
            {
                Transform t = Hits[i].transform;
                if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue;
                if (Hits[i].distance < nearest)
                {
                    nearest = Hits[i].distance;
                    nearestIsTarget = targetRoot != null && t.IsChildOf(targetRoot);
                }
            }
            return nearest == float.MaxValue || nearestIsTarget;
        }
    }
}
