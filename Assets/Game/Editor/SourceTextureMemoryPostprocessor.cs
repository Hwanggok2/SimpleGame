using System;
using UnityEditor;

namespace SimpleGameEditor
{
    public sealed class SourceTextureMemoryPostprocessor :
        AssetPostprocessor
    {
        private const string SourceAssetRoot =
            "Assets/SourceAssets/";
        private const uint ImportPolicyVersion = 1;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    SourceAssetRoot,
                    StringComparison.Ordinal) ||
                assetImporter is not TextureImporter importer)
            {
                return;
            }

            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
        }

        public override uint GetVersion()
        {
            return ImportPolicyVersion;
        }
    }
}
