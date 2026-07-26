using TMPro;
using UnityEngine;

namespace SimpleGame
{
    public static class PrototypeVisualFactory
    {
        private static Sprite squareSprite;

        public static Sprite SquareSprite
        {
            get
            {
                if (squareSprite == null)
                {
                    squareSprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, 1f, 1f),
                        new Vector2(0.5f, 0.5f),
                        1f);
                    squareSprite.name = "PrototypeSquare";
                    squareSprite.hideFlags = HideFlags.DontSave;
                }

                return squareSprite;
            }
        }

        public static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Color color,
            Vector2 size,
            int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        public static TextMeshPro CreateWorldLabel(
            Transform parent,
            string text,
            Vector3 localPosition,
            float fontSize,
            int sortingOrder)
        {
            var child = new GameObject("LevelLabel");
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;

            var label = child.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);

            var meshRenderer = child.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = sortingOrder;
            return label;
        }
    }

}
