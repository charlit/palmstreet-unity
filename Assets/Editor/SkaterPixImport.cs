using UnityEditor;

namespace PalmStreet.Editor
{
    /// <summary>Planches du skateur PixelLab : lisibles (repeintes selon la tenue), sans compression ni lissage (les masques codent des numéros de partie).</summary>
    sealed class SkaterPixImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/SkaterPix/")) return;
            var t = (TextureImporter)assetImporter;
            t.isReadable = true;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false;
            t.filterMode = UnityEngine.FilterMode.Point;
            t.npotScale = TextureImporterNPOTScale.None;
            if (assetPath.EndsWith("_Mask.png")) t.sRGBTexture = false;
        }
    }
}
