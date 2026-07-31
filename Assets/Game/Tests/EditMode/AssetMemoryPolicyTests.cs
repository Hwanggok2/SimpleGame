using NUnit.Framework;
using UnityEditor;

namespace SimpleGame.Tests
{
    public sealed class AssetMemoryPolicyTests
    {
        private const string SourceAssetRoot =
            "Assets/SourceAssets";

        private static readonly string[] SpriteSourceFolders =
        {
            SourceAssetRoot + "/Bandits - Pixel Art/Sprites",
            SourceAssetRoot +
                "/Monsters Creatures Fantasy/Sprites",
            SourceAssetRoot + "/PNG",
            SourceAssetRoot + "/Effects"
        };

        [Test]
        public void SourceAssets_AreOutsideRootResourcesFolder()
        {
            Assert.That(
                AssetDatabase.IsValidFolder(SourceAssetRoot),
                Is.True);
            Assert.That(
                AssetDatabase.IsValidFolder("Assets/Resources"),
                Is.False,
                "Source art under a root Resources folder is " +
                "included in every player build, even when unused.");
        }

        [Test]
        public void SpriteSources_DoNotKeepCpuCopiesOrMipmaps()
        {
            string[] textureGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                SpriteSourceFolders);
            Assert.That(textureGuids, Is.Not.Empty);

            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(
                    importer.isReadable,
                    Is.False,
                    $"Read/Write duplicates texture memory: {path}");
                Assert.That(
                    importer.mipmapEnabled,
                    Is.False,
                    $"Pixel-art source does not need mipmaps: {path}");
            }
        }
    }
}
