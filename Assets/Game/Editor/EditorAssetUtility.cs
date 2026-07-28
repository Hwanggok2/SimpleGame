using System;
using UnityEditor;

namespace SimpleGameEditor
{
    internal static class EditorAssetUtility
    {
        public static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Asset folder path cannot be empty.",
                    nameof(path));
            }

            string normalizedPath =
                path.Replace('\\', '/').TrimEnd('/');
            if (!string.Equals(
                    normalizedPath,
                    "Assets",
                    StringComparison.Ordinal) &&
                !normalizedPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Asset folder path must be under Assets: {path}",
                    nameof(path));
            }

            string current = "Assets";
            string[] parts = normalizedPath.Split('/');
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }
    }
}
