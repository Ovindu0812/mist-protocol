using MistProtocol.AI.Navigation;
using UnityEngine;

namespace MistProtocol.AI.HunterDrone
{
    /// <summary>
    /// Coarse decaying heat map of heard sounds (one bucket = factor x factor grid cells).
    /// heat(t) = heat(t0) * 0.5^((t - t0) / halfLife)
    /// </summary>
    public class SuspicionMap
    {
        private readonly NavGrid _grid;
        private readonly int _factor;
        private readonly int _w, _h;
        private readonly float[] _heat;
        private readonly float _halfLife;
        private float _lastDecay;

        public float MaxHeat { get; private set; }

        public SuspicionMap(NavGrid grid, int factor, float halfLife)
        {
            _grid = grid;
            _factor = factor;
            _w = Mathf.CeilToInt(grid.Width / (float)factor);
            _h = Mathf.CeilToInt(grid.Height / (float)factor);
            _heat = new float[_w * _h];
            _halfLife = halfLife;
            _lastDecay = Time.time;
        }

        public void Add(Vector3 pos, float amount)
        {
            if (!_grid.WorldToCell(pos, out int x, out int z)) return;
            int i = (z / _factor) * _w + x / _factor;
            _heat[i] += amount;
        }

        public void Decay()
        {
            float dt = Time.time - _lastDecay;
            if (dt < 0.5f) return;
            _lastDecay = Time.time;
            float k = Mathf.Pow(0.5f, dt / _halfLife);
            float max = 0f;
            for (int i = 0; i < _heat.Length; i++)
            {
                _heat[i] *= k;
                if (_heat[i] > max) max = _heat[i];
            }
            MaxHeat = max;
        }

        public bool TryGetHottest(out Vector3 pos, float minHeat)
        {
            int best = -1; float bestH = minHeat;
            for (int i = 0; i < _heat.Length; i++)
                if (_heat[i] > bestH) { bestH = _heat[i]; best = i; }
            pos = default;
            if (best < 0) return false;
            int bx = best % _w, bz = best / _w;
            int cx = Mathf.Min(_grid.Width - 1, bx * _factor + _factor / 2);
            int cz = Mathf.Min(_grid.Height - 1, bz * _factor + _factor / 2);
            int n = _grid.NearestWalkable(_grid.CellCenter(_grid.Index(cx, cz)), _factor * 2);
            if (n < 0) return false;
            pos = _grid.CellCenter(n);
            // consume part of the heat so the drone doesn't fixate forever on one spot
            _heat[best] *= 0.5f;
            return true;
        }
    }
}
