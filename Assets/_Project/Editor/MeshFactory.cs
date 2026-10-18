using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// Box meshes with *world-scaled* UVs: every face maps 1 texture tile per
    /// <c>tile</c> metres, so texel density is identical on a 1 m crate and a
    /// 60 m wall (no stretching), and UV2 is generated for lightmapping.
    /// Meshes are cached by size so identical walls share one asset.
    /// </summary>
    public static class MeshFactory
    {
        public const string Dir = "Assets/_Project/Art/Meshes/Generated";
        private static readonly Dictionary<string, Mesh> Cache = new();

        public static void ClearCache() => Cache.Clear();

        public static Mesh Box(Vector3 size, float tile)
        {
            string key = $"Box_{size.x:0.##}x{size.y:0.##}x{size.z:0.##}_t{tile:0.##}";
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            string path = $"{Dir}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { Cache[key] = existing; return existing; }

            Directory.CreateDirectory(Dir);
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3 h = size * 0.5f;

            void Face(Vector3 n, Vector3 u, Vector3 v, float uLen, float vLen)
            {
                int b = verts.Count;
                Vector3 c = Vector3.Scale(n, h);
                Vector3 hu = u * (uLen * 0.5f), hv = v * (vLen * 0.5f);
                verts.Add(c - hu - hv); verts.Add(c + hu - hv); verts.Add(c + hu + hv); verts.Add(c - hu + hv);
                for (int i = 0; i < 4; i++) norms.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(uLen / tile, 0));
                uvs.Add(new Vector2(uLen / tile, vLen / tile)); uvs.Add(new Vector2(0, vLen / tile));
                tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }

            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z);

            var m = new Mesh { name = key };
            m.SetVertices(verts);
            m.SetNormals(norms);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(m);
            AssetDatabase.CreateAsset(m, path);
            Cache[key] = m;
            return m;
        }
    }
}
