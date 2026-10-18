using UnityEditor;
using UnityEngine;

namespace MistProtocol.EditorTools
{
    /// <summary>
    /// Enforces import settings for every custom model and texture so nobody on
    /// the team has to remember them (and the viva can point to one place).
    /// </summary>
    public class MistImportSettings : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("_Project/Art/Models")) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;          // Blender Z-up -> Unity Y-up baked into vertices, identity transforms
            mi.materialImportMode = ModelImporterMaterialImportMode.None; // we assign the shared palette material
            mi.importAnimation = false;            // animation is procedural (rigid parts on joints)
            mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.generateSecondaryUV = true;         // lightmap UVs for static props
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.isReadable = false;                 // halves mesh memory
            mi.optimizeMeshPolygons = true;
            mi.optimizeMeshVertices = true;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("_Project/Art/Textures")) return;
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("MistPalette"))
            {
                // flat colour swatches: point filtering, no mips (no colour bleeding between swatches)
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.maxTextureSize = 256;
                return;
            }
            if (assetPath.Contains("_Normal"))
            {
                ti.textureType = TextureImporterType.NormalMap;
            }
            if (assetPath.Contains("T_Mist") || assetPath.Contains("T_Soft"))
            {
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
            }
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.maxTextureSize = 512;
        }
    }
}
