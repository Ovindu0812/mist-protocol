using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// World Builder: procedurally authored, tileable environment textures
    /// (albedo + normal from a height field). Everything is seamless because
    /// noise is sampled on a torus-wrapped domain.
    /// </summary>
    public static class TextureFactory
    {
        public const string Dir = "Assets/_Project/Art/Textures/Generated";

        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);
            Make("T_Concrete", 512, Concrete, 2.5f);
            Make("T_Floor", 512, FactoryFloor, 3f);
            Make("T_WallLime", 512, LimeWall, 1.5f);
            Make("T_Corrugated", 256, Corrugated, 6f);
            Make("T_Brick", 512, Brick, 4f);
            Make("T_Wood", 256, Wood, 2f);
            Make("T_Ground", 256, Ground, 2f);
            Make("T_Hazard", 128, Hazard, 0f);
            MakeSoft("T_Mist", 128);
            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------ framework
        private delegate (Color albedo, float height) Sampler(float u, float v);

        private static void Make(string name, int size, Sampler s, float normalStrength)
        {
            var albedo = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var h = new float[size * size];
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var (c, ht) = s((float)x / size, (float)y / size);
                px[y * size + x] = c;
                h[y * size + x] = ht;
            }
            albedo.SetPixels(px);
            File.WriteAllBytes($"{Dir}/{name}.png", albedo.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(albedo);
            if (normalStrength <= 0f) return;

            var normal = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float l = h[y * size + (x - 1 + size) % size], r = h[y * size + (x + 1) % size];
                float d = h[((y - 1 + size) % size) * size + x], u = h[((y + 1) % size) * size + x];
                var n = new Vector3((l - r) * normalStrength, (d - u) * normalStrength, 1f).normalized;
                px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            normal.SetPixels(px);
            File.WriteAllBytes($"{Dir}/{name}_Normal.png", normal.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(normal);
        }

        private static void MakeSoft(string name, int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (3f - 2f * a);
                a *= 0.75f + 0.25f * Fbm(x / (float)size, y / (float)size, 4, 3);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            File.WriteAllBytes($"{Dir}/{name}.png", t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }

        /// <summary>Tileable fractal noise: Perlin sampled with wrap-around blending.</summary>
        private static float Fbm(float u, float v, float freq, int octaves, float seed = 0f)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float f = freq * (1 << o);
                sum += amp * Tile(u, v, f, seed + o * 17.3f);
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        private static float Tile(float u, float v, float f, float seed)
        {
            float a = Mathf.PerlinNoise(u * f + seed, v * f + seed);
            float b = Mathf.PerlinNoise((u - 1f) * f + seed, v * f + seed);
            float c = Mathf.PerlinNoise(u * f + seed, (v - 1f) * f + seed);
            float d = Mathf.PerlinNoise((u - 1f) * f + seed, (v - 1f) * f + seed);
            float ab = Mathf.Lerp(a, b, u), cd = Mathf.Lerp(c, d, u);
            return Mathf.Lerp(ab, cd, v);
        }

        private static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, Mathf.Clamp01(t));

        // ------------------------------------------------------------------ materials
        private static (Color, float) Concrete(float u, float v)
        {
            float n = Fbm(u, v, 6, 5, 1f);
            float stain = Mathf.Clamp01((Fbm(u, v, 2, 3, 9f) - 0.5f) * 3f);
            Color c = Mix(new Color(0.52f, 0.52f, 0.5f), new Color(0.38f, 0.37f, 0.35f), n);
            c = Mix(c, new Color(0.28f, 0.3f, 0.24f), stain * 0.6f);
            return (c, n);
        }

        private static (Color, float) FactoryFloor(float u, float v)
        {
            // 2x2 concrete slabs with grooves and worn green paint (tea factory floors)
            float gx = Mathf.Abs((u * 2f) % 1f - 0.5f), gy = Mathf.Abs((v * 2f) % 1f - 0.5f);
            float groove = Mathf.Max(Mathf.SmoothStep(0.485f, 0.5f, gx), Mathf.SmoothStep(0.485f, 0.5f, gy));
            float n = Fbm(u, v, 8, 5, 3f);
            float wear = Mathf.Clamp01((Fbm(u, v, 3, 4, 5f) - 0.42f) * 4f);
            Color paint = new Color(0.22f, 0.32f, 0.24f);
            Color bare = Mix(new Color(0.46f, 0.45f, 0.42f), new Color(0.34f, 0.33f, 0.31f), n);
            Color c = Mix(paint * (0.85f + 0.3f * n), bare, wear);
            c = Mix(c, new Color(0.1f, 0.1f, 0.1f), groove * 0.8f);
            return (c, n * 0.3f - groove);
        }

        private static (Color, float) LimeWall(float u, float v)
        {
            // whitewashed lime plaster with grime creeping up from the floor
            float n = Fbm(u, v, 5, 5, 11f);
            float grime = Mathf.Clamp01((1f - v) * 1.4f - 0.6f + (Fbm(u, v, 4, 3, 13f) - 0.5f));
            Color c = Mix(new Color(0.86f, 0.84f, 0.76f), new Color(0.74f, 0.72f, 0.64f), n);
            c = Mix(c, new Color(0.35f, 0.33f, 0.25f), grime);
            return (c, n);
        }

        private static (Color, float) Corrugated(float u, float v)
        {
            float wave = Mathf.Sin(u * Mathf.PI * 2f * 16f) * 0.5f + 0.5f;
            float rust = Mathf.Clamp01((Fbm(u, v, 4, 4, 21f) - 0.55f) * 4f);
            Color paint = new Color(0.16f, 0.36f, 0.25f) * (0.8f + 0.25f * wave);
            Color c = Mix(paint, new Color(0.45f, 0.22f, 0.1f), rust);
            return (c, wave);
        }

        private static (Color, float) Brick(float u, float v)
        {
            const int rows = 16, cols = 8;
            float ry = v * rows;
            int row = Mathf.FloorToInt(ry);
            float rx = u * cols + (row % 2) * 0.5f;
            float fx = rx - Mathf.Floor(rx), fy = ry - row;
            float mortar = (fx < 0.05f || fy < 0.08f) ? 1f : 0f;
            float id = Mathf.PerlinNoise(Mathf.Floor(rx) * 3.1f, row * 7.7f);
            float n = Fbm(u, v, 10, 4, 31f);
            Color brick = Mix(new Color(0.55f, 0.24f, 0.16f), new Color(0.42f, 0.17f, 0.11f), id) * (0.85f + 0.3f * n);
            Color c = Mix(brick, new Color(0.62f, 0.6f, 0.55f), mortar);
            return (c, mortar > 0 ? 0f : 0.6f + n * 0.4f);
        }

        private static (Color, float) Wood(float u, float v)
        {
            float plank = Mathf.Floor(u * 4f);
            float grain = Mathf.Sin((v * 30f + Fbm(u, v, 6, 3, plank) * 8f)) * 0.5f + 0.5f;
            float edge = Mathf.Abs((u * 4f) % 1f - 0.5f) > 0.47f ? 1f : 0f;
            Color c = Mix(new Color(0.48f, 0.31f, 0.17f), new Color(0.33f, 0.2f, 0.1f), grain * 0.7f);
            c *= 0.9f + 0.2f * Mathf.PerlinNoise(plank * 5f, 1f);
            c = Mix(c, new Color(0.12f, 0.07f, 0.04f), edge);
            return (c, grain * 0.5f - edge);
        }

        private static (Color, float) Ground(float u, float v)
        {
            float n = Fbm(u, v, 6, 5, 41f);
            float gravel = Mathf.Clamp01((Fbm(u, v, 3, 3, 43f) - 0.45f) * 4f);
            Color grass = Mix(new Color(0.12f, 0.2f, 0.09f), new Color(0.2f, 0.29f, 0.12f), n);
            Color c = Mix(grass, new Color(0.36f, 0.33f, 0.29f) * (0.8f + 0.4f * n), gravel);
            return (c, n);
        }

        private static (Color, float) Hazard(float u, float v)
        {
            float s = ((u + v) * 4f) % 1f;
            return (s < 0.5f ? new Color(0.95f, 0.7f, 0.08f) : new Color(0.06f, 0.06f, 0.06f), 0f);
        }
    }
}
