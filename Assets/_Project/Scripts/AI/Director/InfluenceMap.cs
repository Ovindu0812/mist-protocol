using MistProtocol.AI.Navigation;
using UnityEngine;

namespace MistProtocol.AI.Director
{
    /// <summary>
    /// Coarse influence map of where the player has spent time. Each tick the
    /// player's bucket gains influence, which then diffuses to neighbours and
    /// decays: I' = decay * ((1 - s) * I + s * avg(neighbours)).
    /// High values = "cleared territory"; the director avoids spawning there so
    /// enemies arrive from ahead or from unexplored flanks, not from behind walls
    /// the player has just checked.
    /// </summary>
    public class InfluenceMap
    {
        private readonly NavGrid _grid;
        private readonly int _f, _w, _h;
        private float[] _a, _b;
        private const float Spread = 0.25f;
        private const float Decay = 0.995f;

        public InfluenceMap(NavGrid grid, int factor)
        {
            _grid = grid; _f = factor;
            _w = Mathf.CeilToInt(grid.Width / (float)factor);
            _h = Mathf.CeilToInt(grid.Height / (float)factor);
            _a = new float[_w * _h];
            _b = new float[_w * _h];
        }

        public void Stamp(Vector3 pos, float amount)
        {
            if (!_grid.WorldToCell(pos, out int x, out int z)) return;
            int i = (z / _f) * _w + x / _f;
            _a[i] = Mathf.Min(1f, _a[i] + amount);
        }

        public void Propagate()
        {
            for (int z = 0; z < _h; z++)
            for (int x = 0; x < _w; x++)
            {
                float sum = 0f; int n = 0;
                if (x > 0) { sum += _a[z * _w + x - 1]; n++; }
                if (x < _w - 1) { sum += _a[z * _w + x + 1]; n++; }
                if (z > 0) { sum += _a[(z - 1) * _w + x]; n++; }
                if (z < _h - 1) { sum += _a[(z + 1) * _w + x]; n++; }
                float self = _a[z * _w + x];
                _b[z * _w + x] = Decay * ((1f - Spread) * self + Spread * (n > 0 ? sum / n : 0f));
            }
            (_a, _b) = (_b, _a);
        }

        public float Sample(Vector3 pos)
        {
            if (!_grid.WorldToCell(pos, out int x, out int z)) return 0f;
            return _a[(z / _f) * _w + x / _f];
        }
    }
}
