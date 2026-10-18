using System.Collections.Generic;
using MistProtocol.Core;
using MistProtocol.Interaction;
using UnityEngine;
using UnityEngine.AI;

namespace MistProtocol.AI.Navigation
{
    /// <summary>
    /// Discrete search space for every agent. The grid is sampled from the baked
    /// Unity NavMesh (World Builder's bake is the single source of truth) and then
    /// overlaid with dynamic obstacles (barricades the player pushes around).
    /// All IS-module searches (A*, sound propagation, influence maps) run on it.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class NavGrid : MonoBehaviour
    {
        public static NavGrid Instance { get; private set; }

        [SerializeField] private Vector3 origin = new Vector3(-32f, 0f, -8f);
        [SerializeField] private int width = 128;
        [SerializeField] private int height = 92;
        [SerializeField] private float cellSize = 0.5f;
        [SerializeField] private bool drawGizmos;

        private bool[] _staticWalkable;
        private bool[] _walkable;
        private readonly List<int> _coverCells = new();

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int Count => width * height;
        /// <summary>Bumped whenever walkability changes so agents know to re-plan.</summary>
        public int Version { get; private set; }
        public IReadOnlyList<int> CoverCells => _coverCells;

        public static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        public static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };
        public static readonly float[] StepCost = { 1f, 1f, 1f, 1f, 1.4142f, 1.4142f, 1.4142f, 1.4142f };

        public void Configure(Vector3 gridOrigin, int w, int h, float size)
        {
            origin = gridOrigin; width = w; height = h; cellSize = size;
        }

        private void Awake() => Instance = this;

        // Start, not Awake: NavMeshSurface registers its baked data in OnEnable, which
        // runs after this object's Awake. Execution order -200 keeps this Start first.
        private void Start() => Build();

        public void Build()
        {
            _staticWalkable = new bool[Count];
            _walkable = new bool[Count];
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int i = Index(x, z);
                Vector3 c = CellCenter(i);
                bool onMesh = NavMesh.SamplePosition(c + Vector3.up * 0.1f, out var hit, cellSize * 0.45f, NavMesh.AllAreas)
                              && Mathf.Abs(hit.position.y - origin.y) < 0.6f;
                _staticWalkable[i] = onMesh;
            }
            for (int i = 0; i < Count; i++) _walkable[i] = _staticWalkable[i] && !DynamicBlocked(CellCenter(i));
            BuildCover();
            Version++;
        }

        /// <summary>Re-evaluates dynamic obstacles inside a world-space area (called by barricades when they settle).</summary>
        public void RefreshArea(Bounds area)
        {
            area.Expand(cellSize * 2f);
            WorldToCell(area.min, out int x0, out int z0);
            WorldToCell(area.max, out int x1, out int z1);
            for (int z = Mathf.Max(0, z0); z <= Mathf.Min(height - 1, z1); z++)
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++)
            {
                int i = Index(x, z);
                _walkable[i] = _staticWalkable[i] && !DynamicBlocked(CellCenter(i));
            }
            Version++;
        }

        private readonly Collider[] _overlap = new Collider[16];

        private bool DynamicBlocked(Vector3 c)
        {
            int n = Physics.OverlapBoxNonAlloc(c + Vector3.up * 0.5f, new Vector3(cellSize * 0.5f, 0.4f, cellSize * 0.5f),
                _overlap, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
            {
                var b = _overlap[k].GetComponentInParent<Barricade>();
                if (b != null && !b.IsCarried) return true;
                var d = _overlap[k].GetComponentInParent<PhysicsDoor>();
                if (d != null && d.Locked) return true;
            }
            return false;
        }

        private void BuildCover()
        {
            _coverCells.Clear();
            for (int z = 1; z < height - 1; z++)
            for (int x = 1; x < width - 1; x++)
            {
                int i = Index(x, z);
                if (!_walkable[i]) continue;
                int blockedNeighbours = 0;
                for (int d = 0; d < 4; d++)
                    if (!_walkable[Index(x + DX[d], z + DZ[d])]) blockedNeighbours++;
                if (blockedNeighbours > 0) _coverCells.Add(i);
            }
        }

        // ------------------------------------------------------------------ queries
        public int Index(int x, int z) => z * width + x;
        public void Coords(int i, out int x, out int z) { x = i % width; z = i / width; }
        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < height;
        public bool IsWalkable(int i) => i >= 0 && i < Count && _walkable[i];
        public bool IsStaticWalkable(int i) => i >= 0 && i < Count && _staticWalkable[i];

        public Vector3 CellCenter(int i)
        {
            Coords(i, out int x, out int z);
            return origin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
        }

        public bool WorldToCell(Vector3 p, out int x, out int z)
        {
            x = Mathf.FloorToInt((p.x - origin.x) / cellSize);
            z = Mathf.FloorToInt((p.z - origin.z) / cellSize);
            return InBounds(x, z);
        }

        public int WorldToIndex(Vector3 p) => WorldToCell(p, out int x, out int z) ? Index(x, z) : -1;

        public bool IsWalkableWorld(Vector3 p) => IsWalkable(WorldToIndex(p));

        /// <summary>Breadth-first search outwards for the closest walkable cell.</summary>
        public int NearestWalkable(Vector3 p, int maxRadius = 12)
        {
            WorldToCell(p, out int cx, out int cz);
            cx = Mathf.Clamp(cx, 0, width - 1);
            cz = Mathf.Clamp(cz, 0, height - 1);
            if (_walkable[Index(cx, cz)]) return Index(cx, cz);
            for (int r = 1; r <= maxRadius; r++)
            {
                int best = -1; float bestD = float.MaxValue;
                for (int dz = -r; dz <= r; dz++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                    int x = cx + dx, z = cz + dz;
                    if (!InBounds(x, z)) continue;
                    int i = Index(x, z);
                    if (!_walkable[i]) continue;
                    float d = (CellCenter(i) - p).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = i; }
                }
                if (best >= 0) return best;
            }
            return -1;
        }

        /// <summary>True if a straight walk between a and b stays on walkable cells.</summary>
        public bool WalkableLine(Vector3 a, Vector3 b)
        {
            Vector3 d = (b - a).Flat();
            float len = d.magnitude;
            int steps = Mathf.CeilToInt(len / (cellSize * 0.35f));
            for (int s = 0; s <= steps; s++)
            {
                Vector3 p = a + d * (s / (float)Mathf.Max(1, steps));
                if (!IsWalkableWorld(p)) return false;
            }
            return true;
        }

        public Vector3 RandomWalkablePoint(Vector3 around, float radius)
        {
            for (int k = 0; k < 30; k++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                Vector3 p = around + new Vector3(r.x, 0f, r.y);
                if (IsWalkableWorld(p)) return CellCenter(WorldToIndex(p));
            }
            int n = NearestWalkable(around);
            return n >= 0 ? CellCenter(n) : around;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || _walkable == null) return;
            for (int i = 0; i < Count; i++)
            {
                Gizmos.color = _walkable[i] ? new Color(0, 1, 0, 0.15f) : new Color(1, 0, 0, 0.15f);
                Gizmos.DrawCube(CellCenter(i), new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
            }
        }
    }
}
