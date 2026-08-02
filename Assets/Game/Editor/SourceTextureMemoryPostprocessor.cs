using System;
using UnityEditor;

namespace SimpleGameEditor
{
    public sealed class SourceTextureMemoryPostprocessor :
        AssetPostprocessor
    {
        private const string SourceAssetRoot =
            "Assets/SourceAssets/";
        private const string ImageDataRoot = "Assets/Image/";
        private const uint ImportPolicyVersion = 2;

        private void OnPreprocessTexture()
        {
            bool isSourceAsset = assetPath.StartsWith(
                SourceAssetRoot,
                StringComparison.Ordinal);
            bool isImageDataAsset = assetPath.StartsWith(
                ImageDataRoot,
                StringComparison.Ordinal);
            if ((!isSourceAsset && !isImageDataAsset) ||
                assetImporter is not TextureImporter importer)
            {
                return;
            }

            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            if (isImageDataAsset)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
            }
        }

        public override uint GetVersion()
        {
            return ImportPolicyVersion;
        }
    }
}
