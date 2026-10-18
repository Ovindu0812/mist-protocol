using UnityEngine;

namespace MistProtocol.Core
{
    public static class MistUtil
    {
        public static Transform FindDeep(this Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var t = root.GetChild(i).FindDeep(name);
                if (t != null) return t;
            }
            return null;
        }

        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Line of sight test that ignores the colliders of <paramref name="self"/>.</summary>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, Transform self, Transform target)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.01f) return true;
            var hits = Physics.RaycastAll(from, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Transform bestT = null;
            foreach (var h in hits)
            {
                if (self != null && h.transform.IsChildOf(self)) continue;
                if (h.distance < best) { best = h.distance; bestT = h.transform; }
            }
            return bestT == null || (target != null && bestT.IsChildOf(target));
        }
    }
}
