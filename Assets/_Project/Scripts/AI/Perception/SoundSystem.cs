using System.Collections.Generic;
using MistProtocol.AI.Navigation;
using MistProtocol.Interaction;
using UnityEngine;

namespace MistProtocol.AI.Perception
{
    public enum SoundType { Footstep, Gunshot, EnemyGunshot, Impact, Explosion, Door, Alarm }

    public struct SoundStimulus
    {
        public Vector3 Origin;
        /// <summary>Where the listener *thinks* the sound came from (quieter = less accurate).</summary>
        public Vector3 EstimatedPosition;
        public float Intensity;
        public float Loudness;
        public SoundType Type;
        public bool FromPlayer;
        public float Time;
    }

    public interface ISoundListener
    {
        Vector3 ListenPosition { get; }
        float HearingThreshold { get; }
        void OnHeard(in SoundStimulus stimulus);
    }

    /// <summary>
    /// Acoustic propagation over the NavGrid using Dijkstra's algorithm.
    /// Sound loses <see cref="falloffPerMetre"/> per metre travelled through open
    /// cells, bleeds through walls at a much higher cost, and closed doors add a
    /// fixed attenuation. A gunshot in the next room is therefore heard as coming
    /// from the doorway it leaks through, exactly like real sound, rather than
    /// from a straight-line radius check.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class SoundSystem : MonoBehaviour
    {
        public static SoundSystem Instance { get; private set; }

        [SerializeField] private float falloffPerMetre = 1f;
        [SerializeField] private float wallCostPerMetre = 7f;
        [SerializeField] private float closedDoorAttenuation = 10f;

        private readonly List<ISoundListener> _listeners = new();
        private readonly MinHeap _open = new();
        private float[] _cost;
        private int[] _stamp;
        private int _currentStamp;

        public struct DebugRing { public Vector3 Pos; public float Radius; public float Time; public bool Player; }
        public readonly List<DebugRing> Recent = new();

        public static void Register(ISoundListener l) { if (Instance != null && !Instance._listeners.Contains(l)) Instance._listeners.Add(l); }
        public static void Unregister(ISoundListener l) { if (Instance != null) Instance._listeners.Remove(l); }

        private void Awake() => Instance = this;

        /// <summary>Static helper so emitters don't need to null-check the system.</summary>
        public static void Emit(Vector3 position, float loudness, SoundType type, bool fromPlayer)
        {
            if (Instance != null) Instance.Propagate(position, loudness, type, fromPlayer);
        }

        private void Propagate(Vector3 position, float loudness, SoundType type, bool fromPlayer)
        {
            var grid = NavGrid.Instance;
            if (grid == null) return;
            if (_cost == null || _cost.Length != grid.Count)
            {
                _cost = new float[grid.Count];
                _stamp = new int[grid.Count];
            }
            int src = grid.WorldToIndex(position);
            if (src < 0) return;

            _currentStamp++;
            _open.Clear();
            _cost[src] = 0f;
            _stamp[src] = _currentStamp;
            _open.Push(src, 0f);
            float cs = grid.CellSize;

            // Dijkstra bounded by the loudness budget
            while (_open.Count > 0)
            {
                int cur = _open.Pop(out float c);
                if (c > _cost[cur]) continue;
                grid.Coords(cur, out int cx, out int cz);
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + NavGrid.DX[d], nz = cz + NavGrid.DZ[d];
                    if (!grid.InBounds(nx, nz)) continue;
                    int n = grid.Index(nx, nz);
                    float perMetre = grid.IsStaticWalkable(n) ? falloffPerMetre : wallCostPerMetre;
                    float nc = c + NavGrid.StepCost[d] * cs * perMetre;
                    if (PhysicsDoor.IsClosedDoorCell(n) && !PhysicsDoor.IsClosedDoorCell(cur)) nc += closedDoorAttenuation;
                    if (nc >= loudness) continue;
                    if (_stamp[n] == _currentStamp && nc >= _cost[n]) continue;
                    _stamp[n] = _currentStamp;
                    _cost[n] = nc;
                    _open.Push(n, nc);
                }
            }

            Recent.Add(new DebugRing { Pos = position, Radius = loudness / falloffPerMetre, Time = Time.time, Player = fromPlayer });
            if (Recent.Count > 24) Recent.RemoveAt(0);

            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                var l = _listeners[i];
                int li = grid.WorldToIndex(l.ListenPosition);
                if (li < 0 || _stamp[li] != _currentStamp) continue;
                float intensity = loudness - _cost[li];
                if (intensity < l.HearingThreshold) continue;

                // localisation error shrinks as the perceived sound gets louder
                float clarity = Mathf.Clamp01(intensity / loudness);
                float error = Mathf.Lerp(5f, 0.4f, clarity);
                Vector2 r = Random.insideUnitCircle * error;
                var s = new SoundStimulus
                {
                    Origin = position,
                    EstimatedPosition = position + new Vector3(r.x, 0f, r.y),
                    Intensity = intensity,
                    Loudness = loudness,
                    Type = type,
                    FromPlayer = fromPlayer,
                    Time = Time.time,
                };
                l.OnHeard(s);
            }
        }
    }
}
