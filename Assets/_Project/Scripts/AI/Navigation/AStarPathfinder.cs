using System.Collections.Generic;
using UnityEngine;

namespace MistProtocol.AI.Navigation
{
    /// <summary>
    /// Weighted A* on the 8-connected NavGrid.
    ///   f(n) = g(n) + w * h(n),  h = octile distance (admissible and consistent
    ///   for 8-way movement with unit/√2 step costs, so w = 1 gives optimal paths).
    /// An optional per-cell cost modifier turns it into a *tactical* search
    /// (e.g. Sentinels add a penalty to cells the player can see, which makes
    /// flanking routes emerge from the search instead of being scripted).
    /// </summary>
    public static class AStarPathfinder
    {
        public delegate float CellCost(int cell);

        private static float[] _g;
        private static int[] _parent;
        private static int[] _closedStamp;
        private static int[] _openStamp;
        private static int _stamp;
        private static readonly MinHeap Open = new();

        public static int LastExpanded { get; private set; }

        private static void EnsureBuffers(int n)
        {
            if (_g != null && _g.Length == n) return;
            _g = new float[n];
            _parent = new int[n];
            _closedStamp = new int[n];
            _openStamp = new int[n];
            _stamp = 0;
        }

        public static float Octile(int dx, int dz)
        {
            dx = Mathf.Abs(dx); dz = Mathf.Abs(dz);
            return (dx + dz) + (1.4142f - 2f) * Mathf.Min(dx, dz);
        }

        /// <returns>True if a path was found. <paramref name="result"/> receives world-space cell centres.</returns>
        public static bool FindPath(NavGrid grid, Vector3 start, Vector3 goal, List<Vector3> result,
            CellCost extraCost = null, float heuristicWeight = 1f, int maxExpansions = 12000)
        {
            result.Clear();
            if (grid == null) return false;
            EnsureBuffers(grid.Count);

            int s = grid.NearestWalkable(start);
            int t = grid.NearestWalkable(goal);
            if (s < 0 || t < 0) return false;
            if (s == t) { result.Add(grid.CellCenter(t)); return true; }

            _stamp++;
            Open.Clear();
            grid.Coords(t, out int tx, out int tz);
            _g[s] = 0f;
            _parent[s] = -1;
            _openStamp[s] = _stamp;
            grid.Coords(s, out int sx, out int sz);
            Open.Push(s, heuristicWeight * Octile(sx - tx, sz - tz));

            int expanded = 0;
            int found = -1;
            while (Open.Count > 0 && expanded < maxExpansions)
            {
                int cur = Open.Pop(out _);
                if (_closedStamp[cur] == _stamp) continue; // stale heap entry
                _closedStamp[cur] = _stamp;
                expanded++;
                if (cur == t) { found = cur; break; }

                grid.Coords(cur, out int cx, out int cz);
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + NavGrid.DX[d], nz = cz + NavGrid.DZ[d];
                    if (!grid.InBounds(nx, nz)) continue;
                    int n = grid.Index(nx, nz);
                    if (!grid.IsWalkable(n) || _closedStamp[n] == _stamp) continue;
                    // no corner cutting: both orthogonal neighbours must be free for a diagonal step
                    if (d >= 4 && (!grid.IsWalkable(grid.Index(cx + NavGrid.DX[d], cz)) ||
                                   !grid.IsWalkable(grid.Index(cx, cz + NavGrid.DZ[d])))) continue;

                    float step = NavGrid.StepCost[d];
                    if (extraCost != null) step += extraCost(n);
                    float g = _g[cur] + step;
                    if (_openStamp[n] == _stamp && g >= _g[n]) continue;
                    _openStamp[n] = _stamp;
                    _g[n] = g;
                    _parent[n] = cur;
                    Open.Push(n, g + heuristicWeight * Octile(nx - tx, nz - tz));
                }
            }
            LastExpanded = expanded;
            if (found < 0) return false;

            for (int c = found; c >= 0; c = _parent[c]) result.Add(grid.CellCenter(c));
            result.Reverse();
            // end exactly on the requested point when it is itself walkable
            var exactGoal = new Vector3(goal.x, result[result.Count - 1].y, goal.z);
            if (grid.IsWalkableWorld(exactGoal)) result[result.Count - 1] = exactGoal;
            return true;
        }

        /// <summary>Path length in metres of a found path (used by the director to score spawn points).</summary>
        public static float PathLength(List<Vector3> path)
        {
            float len = 0f;
            for (int i = 1; i < path.Count; i++) len += Vector3.Distance(path[i - 1], path[i]);
            return len;
        }
    }
}
