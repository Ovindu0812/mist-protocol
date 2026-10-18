using System.Collections.Generic;

namespace MistProtocol.AI.Navigation
{
    /// <summary>
    /// Binary min-heap keyed on float priority. O(log n) push/pop.
    /// Decrease-key is handled lazily: a node may be pushed more than once and
    /// stale entries are skipped by the caller via its closed set.
    /// </summary>
    public sealed class MinHeap
    {
        private readonly List<(int item, float key)> _data = new(1024);

        public int Count => _data.Count;
        public void Clear() => _data.Clear();

        public void Push(int item, float key)
        {
            _data.Add((item, key));
            int i = _data.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_data[p].key <= _data[i].key) break;
                (_data[p], _data[i]) = (_data[i], _data[p]);
                i = p;
            }
        }

        public int Pop(out float key)
        {
            var top = _data[0];
            int last = _data.Count - 1;
            _data[0] = _data[last];
            _data.RemoveAt(last);
            int i = 0;
            int n = _data.Count;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < n && _data[l].key < _data[m].key) m = l;
                if (r < n && _data[r].key < _data[m].key) m = r;
                if (m == i) break;
                (_data[m], _data[i]) = (_data[i], _data[m]);
                i = m;
            }
            key = top.key;
            return top.item;
        }
    }
}
