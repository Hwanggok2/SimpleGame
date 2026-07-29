using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class SlashTrailEffect : MonoBehaviour
    {
        private static readonly List<SlashTrailEffect> severPool = new();

        private LineRenderer line;
        private SpriteRenderer spriteRenderer;
        private Material material;
        private Color effectColor;
        private Coroutine fadeCoroutine;

        public static void Show(
            SpriteRenderer template,
            Vector2 start,
            Vector2 end,
            float duration)
        {
            if (template == null)
            {
                return;
            }

            SlashTrailEffect effect = AcquireSever(template);
            effect.gameObject.SetActive(true);
            effect.ConfigureSprite(
                start,
                end,
                duration,
                template);
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSeverPool()
        {
            severPool.Clear();
        }

        private static SlashTrailEffect AcquireSever(
            SpriteRenderer template)
        {
            for (int index = severPool.Count - 1;
                 index >= 0;
                 index--)
            {
                SlashTrailEffect candidate = severPool[index];
                if (candidate == null)
                {
                    severPool.RemoveAt(index);
                    continue;
                }

                if (!candidate.gameObject.activeSelf)
                {
                    return candidate;
                }
            }

            SpriteRenderer clonedRenderer = Instantiate(template);
            clonedRenderer.gameObject.name = "SeverTrail";
            clonedRenderer.transform.SetParent(null, true);
            SlashTrailEffect effect =
                clonedRenderer.gameObject
                    .GetComponent<SlashTrailEffect>();
            if (effect == null)
            {
                effect =
                    clonedRenderer.gameObject
                        .AddComponent<SlashTrailEffect>();
            }

            effect.spriteRenderer = clonedRenderer;
            severPool.Add(effect);
            return effect;
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
                0.3f,
                new Color(0.45f, 0.9f, 1f, 0.95f),
                true);
        }

        private void ConfigureSprite(
            Vector2 start,
            Vector2 end,
            float duration,
            SpriteRenderer template)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            ApplySpriteTemplate(template);
            effectColor = template.color;
            PositionSprite(
                start,
                end,
                template.transform.lossyScale.x,
                template.transform.position.z);
            fadeCoroutine = StartCoroutine(FadeRoutine(
                Mathf.Max(0.05f, duration),
                false));
        }

        private void ApplySpriteTemplate(SpriteRenderer template)
        {
            spriteRenderer.sprite = template.sprite;
            spriteRenderer.sharedMaterial = template.sharedMaterial;
            spriteRenderer.color = template.color;
            spriteRenderer.sortingLayerID = template.sortingLayerID;
            spriteRenderer.sortingOrder = template.sortingOrder;
            spriteRenderer.flipX = template.flipX;
            spriteRenderer.flipY = template.flipY;
            spriteRenderer.maskInteraction = template.maskInteraction;
            spriteRenderer.spriteSortPoint = template.spriteSortPoint;
            spriteRenderer.drawMode = template.drawMode;
            spriteRenderer.size = template.size;
            spriteRenderer.tileMode = template.tileMode;
            spriteRenderer.adaptiveModeThreshold =
                template.adaptiveModeThreshold;
            spriteRenderer.enabled = template.enabled;
        }

        private void PositionSprite(
            Vector2 start,
            Vector2 end,
            float width,
            float worldZ)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            Vector2 midpoint = (start + end) * 0.5f;
            transform.position =
                new Vector3(midpoint.x, midpoint.y, worldZ);
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) *
                    Mathf.Rad2Deg - 90f);

            Vector2 spriteSize = spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.size
                : Vector2.one;
            transform.localScale = new Vector3(
                width / Mathf.Max(0.0001f, spriteSize.x),
                length / Mathf.Max(0.0001f, spriteSize.y),
                1f);
        }

        private void Configure(
            Vector2 start,
            Vector2 end,
            float width,
            float duration,
            float endWidthMultiplier,
            Color color,
            bool destroyWhenFinished)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            effectColor = color;
            EnsureLine();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width *
                Mathf.Max(0f, endWidthMultiplier);
            line.startColor = color;
            line.endColor = color;
            fadeCoroutine = StartCoroutine(FadeRoutine(
                Mathf.Max(0.05f, duration),
                destroyWhenFinished));
        }

        public static float CalculateFadeAlpha(
            float elapsed,
            float duration)
        {
            return 1f - Mathf.Clamp01(
                elapsed / Mathf.Max(0.0001f, duration));
        }

        private void EnsureLine()
        {
            if (line != null)
            {
                return;
            }

            line = gameObject.AddComponent<LineRenderer>();
            line.numCapVertices = 2;
            line.sortingOrder = 25;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                line.material = material;
            }
        }

        private IEnumerator FadeRoutine(
            float duration,
            bool destroyWhenFinished)
        {
            Color color = effectColor;
            float initialAlpha = color.a;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                color.a = initialAlpha *
                    CalculateFadeAlpha(elapsed, duration);
                ApplyColor(color);
                yield return null;
                elapsed += Time.deltaTime;
            }

            color.a = 0f;
            ApplyColor(color);
            fadeCoroutine = null;
            if (destroyWhenFinished)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }

            if (line != null)
            {
                line.startColor = color;
                line.endColor = color;
            }
        }

        private void OnDestroy()
        {
            severPool.Remove(this);

            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
