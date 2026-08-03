using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public sealed class SlashTrailEffect : MonoBehaviour
    {
        private static readonly List<SlashTrailEffect> severPool = new();
        private static readonly List<SlashTrailEffect> staticArcPool = new();

        private LineRenderer line;
        private SpriteRenderer spriteRenderer;
        private Material material;
        private Color spriteColor;
        private Color lineColor;
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
        private static void ResetPools()
        {
            severPool.Clear();
            staticArcPool.Clear();
        }

        private static SlashTrailEffect AcquireSever(
            SpriteRenderer template)
        {
            SlashTrailEffect available = FindInactive(severPool);
            if (available != null)
            {
                return available;
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

        private static SlashTrailEffect FindInactive(
            List<SlashTrailEffect> pool)
        {
            for (int index = pool.Count - 1;
                 index >= 0;
                 index--)
            {
                SlashTrailEffect candidate = pool[index];
                if (candidate == null)
                {
                    pool.RemoveAt(index);
                    continue;
                }

                if (!candidate.gameObject.activeSelf)
                {
                    return candidate;
                }
            }

            return null;
        }

        public static void ShowStaticArc(
            Vector2 start,
            Vector2 end)
        {
            SlashTrailEffect effect = AcquireStaticArc();
            effect.gameObject.SetActive(true);
            effect.Configure(
                start,
                end,
                0.035f,
                0.16f,
                0.3f,
                new Color(0.45f, 0.9f, 1f, 0.95f));
        }

        private static SlashTrailEffect AcquireStaticArc()
        {
            SlashTrailEffect available = FindInactive(staticArcPool);
            if (available != null)
            {
                return available;
            }

            var effectObject = new GameObject("StaticArc");
            var effect = effectObject.AddComponent<SlashTrailEffect>();
            effect.EnsureLine();
            staticArcPool.Add(effect);
            return effect;
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
            spriteColor = template.color;
            lineColor = new Color(0.8f, 0.12f, 0.08f, 0.95f);
            ConfigureLine(
                start,
                end,
                0.04f,
                1f,
                lineColor,
                template.sortingOrder - 1);
            PositionSprite(
                start,
                end,
                template.transform.lossyScale.x,
                template.transform.position.z);
            fadeCoroutine = StartCoroutine(FadeRoutine(
                Mathf.Max(0.05f, duration)));
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
            float angle = Mathf.Atan2(direction.y, direction.x) *
                Mathf.Rad2Deg;

            if (spriteRenderer.drawMode != SpriteDrawMode.Simple)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                transform.localScale = Vector3.one;
                spriteRenderer.size = new Vector2(
                    Mathf.Max(0.01f, length),
                    Mathf.Max(0.01f, spriteRenderer.size.y));
                return;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

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
            Color color)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            spriteColor = Color.clear;
            lineColor = color;
            ConfigureLine(
                start,
                end,
                width,
                endWidthMultiplier,
                color,
                25);
            fadeCoroutine = StartCoroutine(FadeRoutine(
                Mathf.Max(0.05f, duration)));
        }

        private void ConfigureLine(
            Vector2 start,
            Vector2 end,
            float width,
            float endWidthMultiplier,
            Color color,
            int sortingOrder)
        {
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
            line.sortingOrder = sortingOrder;
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

        private IEnumerator FadeRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                ApplyFade(CalculateFadeAlpha(elapsed, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }

            ApplyFade(0f);
            fadeCoroutine = null;
            gameObject.SetActive(false);
        }

        private void ApplyFade(float alphaMultiplier)
        {
            if (spriteRenderer != null)
            {
                Color fadedSprite = spriteColor;
                fadedSprite.a *= alphaMultiplier;
                spriteRenderer.color = fadedSprite;
            }

            if (line != null)
            {
                Color fadedLine = lineColor;
                fadedLine.a *= alphaMultiplier;
                line.startColor = fadedLine;
                line.endColor = fadedLine;
            }
        }

        private void OnDestroy()
        {
            severPool.Remove(this);
            staticArcPool.Remove(this);

            if (material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
            }
        }
    }
}
