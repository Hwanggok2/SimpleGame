using System.Collections;
using UnityEngine;

namespace SimpleGame
{
    public sealed class SlashTrailEffect : MonoBehaviour
    {
        private LineRenderer line;
        private Material material;
        private Color effectColor;

        public static void Show(
            Vector2 start,
            Vector2 end,
            float width,
            float duration)
        {
            var effectObject = new GameObject("SeverTrail");
            var effect = effectObject.AddComponent<SlashTrailEffect>();
            effect.Configure(
                start,
                end,
                width,
                duration,
                new Color(0.45f, 0.95f, 1f, 0.85f));
        }

        public static void ShowStaticArc(
            Vector2 start,
            Vector2 end)
        {
            var effectObject = new GameObject("StaticArc");
            var effect = effectObject.AddComponent<SlashTrailEffect>();
            effect.Configure(
                start,
                end,
                0.035f,
                0.16f,
                new Color(0.45f, 0.9f, 1f, 0.95f));
        }

        private void Configure(
            Vector2 start,
            Vector2 end,
            float width,
            float duration,
            Color color)
        {
            effectColor = color;
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width * 0.3f;
            line.numCapVertices = 2;
            line.sortingOrder = 25;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                line.material = material;
            }

            StartCoroutine(FadeRoutine(Mathf.Max(0.05f, duration)));
        }

        private IEnumerator FadeRoutine(float duration)
        {
            Color color = effectColor;
            float initialAlpha = color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = initialAlpha *
                    (1f - Mathf.Clamp01(elapsed / duration));
                line.startColor = color;
                line.endColor = color;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
