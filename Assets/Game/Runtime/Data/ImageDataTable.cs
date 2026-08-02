using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [Serializable]
    public sealed class ImageDataDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string fileName;
        [SerializeField] private Sprite sprite;

        public ImageDataDefinition(
            string id,
            string fileName,
            Sprite sprite)
        {
            this.id = id ?? string.Empty;
            this.fileName = fileName ?? string.Empty;
            this.sprite = sprite;
        }

        public string Id => id;
        public string FileName => fileName;
        public Sprite Sprite => sprite;
    }

    [CreateAssetMenu(
        fileName = "ImageDataTable",
        menuName = "SimpleGame/Data/Image Data Table")]
    public sealed class ImageDataTable : ScriptableObject
    {
        [SerializeField] private List<ImageDataDefinition> definitions = new();

        public IReadOnlyList<ImageDataDefinition> Definitions => definitions;

        public void Configure(IEnumerable<ImageDataDefinition> values)
        {
            definitions = values != null
                ? new List<ImageDataDefinition>(values)
                : new List<ImageDataDefinition>();
        }

        public bool TryGet(string id, out Sprite sprite)
        {
            foreach (ImageDataDefinition definition in definitions)
            {
                if (string.Equals(
                        definition.Id,
                        id,
                        StringComparison.Ordinal))
                {
                    sprite = definition.Sprite;
                    return sprite != null;
                }
            }

            sprite = null;
            return false;
        }
    }
}
