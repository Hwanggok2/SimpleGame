using System.Globalization;
using TMPro;
using UnityEngine;

namespace SimpleGame
{
    public enum DamagePopupStyle
    {
        Dealt,
        Critical,
        Received
    }

    public sealed class DamagePopupView : MonoBehaviour
    {
        private const float DefaultLifetime = 0.82f;
        private const float DefaultRiseDistance = 0.9f;
        private const float FadeStartProgress = 0.52f;
        private const float StartScale = 0.78f;
        private const float PeakScale = 1.14f;
        private const float PeakScaleProgress = 0.16f;
        private const int MinimumSortingOrder = 220;

        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.05f)] private float lifetime =
            DefaultLifetime;
        [SerializeField, Min(0f)] private float riseDistance =
            DefaultRiseDistance;
        [SerializeField] private Color dealtColor = Color.white;
        [SerializeField] private Color criticalColor =
            new(1f, 0.82f, 0.18f, 1f);
        [SerializeField] private Color receivedColor =
            new(1f, 0.28f, 0.24f, 1f);
        [SerializeField, Min(0.1f)] private float dealtFontSize = 3.1f;
        [SerializeField, Min(0.1f)] private float criticalFontSize = 3.8f;
        [SerializeField, Min(0.1f)] private float receivedFontSize = 3.35f;

        private Vector3 startPosition;
        private Vector3 restingScale = Vector3.one;
        private Renderer labelRenderer;
        private float elapsed;
        private bool presentationCached;

        public bool IsPlaying => gameObject.activeSelf;
        public bool HasConfiguredLabel => label != null;

        public void Configure(TMP_Text configuredLabel)
        {
            label = configuredLabel;
            CachePresentation();
        }

        public static string FormatDamage(float amount)
        {
            return Mathf.Max(0f, amount).ToString(
                "0.##",
                CultureInfo.InvariantCulture);
        }

        public void Play(
            Vector3 worldPosition,
            float amount,
            DamagePopupStyle style)
        {
            if (label == null)
            {
                Debug.LogError(
                    "Damage popup requires a preconfigured TMP label.",
                    this);
                gameObject.SetActive(false);
                return;
            }

            CachePresentation();
            startPosition = worldPosition;
            transform.position = startPosition;
            transform.localScale = restingScale * StartScale;
            elapsed = 0f;
            label.text = FormatDamage(amount);
            ApplyStyle(style);
            label.alpha = 1f;
            gameObject.SetActive(true);
        }

        public void Stop()
        {
            transform.localScale = restingScale;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            CachePresentation();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.05f, lifetime);
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.position = startPosition +
                Vector3.up * (riseDistance * progress);
            if (label != null)
            {
                float fadeProgress = Mathf.InverseLerp(
                    FadeStartProgress,
                    1f,
                    progress);
                label.alpha = 1f - fadeProgress;
            }

            float scale = progress < PeakScaleProgress
                ? Mathf.Lerp(
                    StartScale,
                    PeakScale,
                    progress / PeakScaleProgress)
                : Mathf.Lerp(
                    PeakScale,
                    1f,
                    Mathf.InverseLerp(
                        PeakScaleProgress,
                        1f,
                        progress));
            transform.localScale = restingScale * scale;

            if (progress >= 1f)
            {
                Stop();
            }
        }

        private void ApplyStyle(DamagePopupStyle style)
        {
            switch (style)
            {
                case DamagePopupStyle.Critical:
                    label.color = criticalColor;
                    label.fontSize = criticalFontSize;
                    break;
                case DamagePopupStyle.Received:
                    label.color = receivedColor;
                    label.fontSize = receivedFontSize;
                    break;
                default:
                    label.color = dealtColor;
                    label.fontSize = dealtFontSize;
                    break;
            }
        }

        private void CachePresentation()
        {
            if (!presentationCached)
            {
                restingScale = transform.localScale;
                presentationCached = true;
            }

            if (label == null)
            {
                return;
            }

            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            labelRenderer ??= label.GetComponent<Renderer>();
            if (labelRenderer != null &&
                labelRenderer.sortingOrder < MinimumSortingOrder)
            {
                labelRenderer.sortingOrder = MinimumSortingOrder;
            }
        }
    }
}
