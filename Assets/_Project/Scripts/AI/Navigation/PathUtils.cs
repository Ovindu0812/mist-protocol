using System.Collections.Generic;
using UnityEngine;

namespace MistProtocol.AI.Navigation
{
    public static class PathUtils
    {
        /// <summary>
        /// String-pulling: removes grid waypoints that can be skipped with a straight,
        /// fully walkable line. Turns the staircase of grid cells into a few corners.
        /// </summary>
        public static void Simplify(NavGrid grid, List<Vector3> path)
        {
            if (path.Count < 3) return;
            var outPts = new List<Vector3>(path.Count) { path[0] };
            int anchor = 0;
            for (int i = 2; i < path.Count; i++)
            {
                if (!grid.WalkableLine(path[anchor], path[i]))
                {
                    outPts.Add(path[i - 1]);
                    anchor = i - 1;
                }
            }
            outPts.Add(path[path.Count - 1]);
            path.Clear();
            path.AddRange(outPts);
        }

        /// <summary>
        /// Centripetal-style Catmull-Rom resampling of corner points into a smooth curve.
        /// Samples are only kept if they stay on walkable cells, so the curve can't clip walls.
        /// </summary>
        public static void Smooth(NavGrid grid, List<Vector3> corners, List<Vector3> output, float spacing = 0.5f)
        {
            output.Clear();
            if (corners.Count < 3) { output.AddRange(corners); return; }
            output.Add(corners[0]);
            for (int i = 0; i < corners.Count - 1; i++)
            {
                Vector3 p0 = corners[Mathf.Max(0, i - 1)];
                Vector3 p1 = corners[i];
                Vector3 p2 = corners[i + 1];
                Vector3 p3 = corners[Mathf.Min(corners.Count - 1, i + 2)];
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / spacing));
                for (int s = 1; s <= samples; s++)
                {
                    float t = s / (float)samples;
                    Vector3 p = CatmullRom(p0, p1, p2, p3, t);
                    if (grid != null && !grid.IsWalkableWorld(p)) p = Vector3.Lerp(p1, p2, t);
                    output.Add(p);
                }
            }
        }

        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
