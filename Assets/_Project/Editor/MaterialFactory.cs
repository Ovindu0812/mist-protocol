using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MistProtocol.EditorTools
{
    /// <summary>Creates the URP materials. All opaque materials use URP/Lit with GPU instancing and are SRP-Batcher compatible.</summary>
    public static class MaterialFactory
    {
        public const string Dir = "Assets/_Project/Materials";
        private const string TexDir = TextureFactory.Dir;

        public static Material Palette, Glow, Concrete, Floor, Wall, Corrugated, Brick, Wood, Ground, Hazard,
            LampWarm, LampOrange, LampCyan, Particles, Tracer, Mist, TeaBush, Flask, Keycard, DarkMetal, Hill;

        public static void CreateAll()
        {
            Directory.CreateDirectory(Dir);
            Palette = Lit("M_MistPalette", Tex("Assets/_Project/Art/Textures/MistPalette.png"), null, Color.white, 0.25f, 0.2f);
            Palette.EnableKeyword("_EMISSION");
            Palette.SetTexture("_EmissionMap", Tex("Assets/_Project/Art/Textures/MistPalette_Emission.png"));
            Palette.SetColor("_EmissionColor", Color.white * 1.5f);
            Palette.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            Glow = Lit("M_Glow", null, null, new Color(0.05f, 0.05f, 0.05f), 0.8f, 0f);
            Glow.EnableKeyword("_EMISSION");
            Glow.SetColor("_EmissionColor", new Color(0.25f, 0.9f, 1f) * 2f);

            Concrete = Lit("M_Concrete", Tex($"{TexDir}/T_Concrete.png"), Tex($"{TexDir}/T_Concrete_Normal.png"), Color.white, 0.15f, 0f);
            Floor = Lit("M_FactoryFloor", Tex($"{TexDir}/T_Floor.png"), Tex($"{TexDir}/T_Floor_Normal.png"), Color.white, 0.3f, 0f);
            Wall = Lit("M_LimeWall", Tex($"{TexDir}/T_WallLime.png"), Tex($"{TexDir}/T_WallLime_Normal.png"), Color.white, 0.05f, 0f);
            Corrugated = Lit("M_CorrugatedGreen", Tex($"{TexDir}/T_Corrugated.png"), Tex($"{TexDir}/T_Corrugated_Normal.png"), Color.white, 0.35f, 0.4f);
            Brick = Lit("M_Brick", Tex($"{TexDir}/T_Brick.png"), Tex($"{TexDir}/T_Brick_Normal.png"), Color.white, 0.1f, 0f);
            Wood = Lit("M_Wood", Tex($"{TexDir}/T_Wood.png"), Tex($"{TexDir}/T_Wood_Normal.png"), Color.white, 0.2f, 0f);
            Ground = Lit("M_Ground", Tex($"{TexDir}/T_Ground.png"), Tex($"{TexDir}/T_Ground_Normal.png"), Color.white, 0.1f, 0f);
            Hazard = Lit("M_Hazard", Tex($"{TexDir}/T_Hazard.png"), null, Color.white, 0.3f, 0f);
            DarkMetal = Lit("M_DarkMetal", null, null, new Color(0.12f, 0.13f, 0.14f), 0.45f, 0.7f);
            TeaBush = Lit("M_TeaBush", Tex($"{TexDir}/T_Ground.png"), null, new Color(0.45f, 0.75f, 0.4f), 0.15f, 0f);
            Hill = Lit("M_Hill", null, null, new Color(0.07f, 0.11f, 0.08f), 0.05f, 0f);

            LampWarm = Emissive("M_LampWarm", new Color(1f, 0.72f, 0.4f) * 4f);
            LampOrange = Emissive("M_LampOrange", new Color(1f, 0.4f, 0.12f) * 4f);
            LampCyan = Emissive("M_LampCyan", new Color(0.3f, 0.9f, 1f) * 4f);
            Flask = Emissive("M_TeaFlask", new Color(0.3f, 1f, 0.4f) * 2f);
            Keycard = Emissive("M_Keycard", new Color(0.3f, 0.9f, 1f) * 3f);

            Particles = ParticleMat("M_Particles", Tex($"{TexDir}/T_Mist.png"), additive: true);
            Tracer = ParticleMat("M_Tracer", null, additive: true);
            Mist = ParticleMat("M_Mist", Tex($"{TexDir}/T_Mist.png"), additive: false);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        private static Material LoadOrCreate(string name, string shader)
        {
            string path = $"{Dir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(shader)) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = Shader.Find(shader);
            return m;
        }

        private static Material Lit(string name, Texture2D albedo, Texture2D normal, Color tint, float smooth, float metal)
        {
            var m = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Emissive(string name, Color emission)
        {
            var m = Lit(name, null, null, new Color(0.1f, 0.1f, 0.1f), 0.6f, 0f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            return m;
        }

        private static Material ParticleMat(string name, Texture2D tex, bool additive)
        {
            var m = LoadOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
