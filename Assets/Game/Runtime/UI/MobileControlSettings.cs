using System;
using UnityEngine;

namespace SimpleGame
{
    public enum MobileControlMode
    {
        DirectMoveAutoAim = 1,
        AimCommand = 2
    }

    [Serializable]
    public struct MobileControlSettings
    {
        public int version;
        public bool controlsEnabled;
        public bool autoAttackEnabled;
        public MobileControlMode controlMode;
        public float joystickScale;
        public Vector2 joystickPosition;
        public float attackScale;
        public Vector2 attackPosition;

        public static MobileControlSettings Default => new()
        {
            version = MobileControlSettingsStore.CurrentVersion,
            controlsEnabled = true,
            autoAttackEnabled = true,
            controlMode = MobileControlMode.DirectMoveAutoAim,
            joystickScale = 1f,
            joystickPosition = new Vector2(0.05f, 0.11f),
            attackScale = 1f,
            attackPosition = new Vector2(0.95f, 0.12f)
        };
    }

    public static class MobileControlSettingsStore
    {
        public const int CurrentVersion = 2;
        public const float MinimumScale = 0.7f;
        public const float MaximumScale = 1.5f;

        private const string PreferencesKey =
            "SimpleGame.MobileControls.v1";

        public static MobileControlSettings Load(
            ControlSettingsProfile profile = null)
        {
            MobileControlSettings defaultSettings = profile != null
                ? profile.CreateDefaultSettings()
                : MobileControlSettings.Default;
            if (!PlayerPrefs.HasKey(PreferencesKey))
            {
                return defaultSettings;
            }

            try
            {
                MobileControlSettings settings =
                    JsonUtility.FromJson<MobileControlSettings>(
                        PlayerPrefs.GetString(PreferencesKey));
                if (settings.version == 1)
                {
                    settings.controlMode =
                        MobileControlMode.AimCommand;
                    return Clamp(settings);
                }

                return settings.version == CurrentVersion
                    ? Clamp(settings)
                    : defaultSettings;
            }
            catch (ArgumentException)
            {
                return defaultSettings;
            }
        }

        public static void Save(MobileControlSettings settings)
        {
            settings = Clamp(settings);
            settings.version = CurrentVersion;
            PlayerPrefs.SetString(
                PreferencesKey,
                JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }

        public static MobileControlSettings Clamp(
            MobileControlSettings settings)
        {
            settings.version = CurrentVersion;
            if (settings.controlMode !=
                    MobileControlMode.DirectMoveAutoAim &&
                settings.controlMode !=
                    MobileControlMode.AimCommand)
            {
                settings.controlMode =
                    MobileControlMode.DirectMoveAutoAim;
            }

            settings.joystickScale = Mathf.Clamp(
                settings.joystickScale,
                MinimumScale,
                MaximumScale);
            settings.joystickPosition = ClampPosition(
                settings.joystickPosition);
            settings.attackScale = Mathf.Clamp(
                settings.attackScale,
                MinimumScale,
                MaximumScale);
            settings.attackPosition = ClampPosition(
                settings.attackPosition);
            return settings;
        }

        public static Rect CalculateSafeAreaInParent(
            Rect parentRect,
            Rect safeAreaPixels,
            Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return parentRect;
            }

            float minimumX = Mathf.Clamp01(
                safeAreaPixels.xMin / screenSize.x);
            float maximumX = Mathf.Clamp01(
                safeAreaPixels.xMax / screenSize.x);
            float minimumY = Mathf.Clamp01(
                safeAreaPixels.yMin / screenSize.y);
            float maximumY = Mathf.Clamp01(
                safeAreaPixels.yMax / screenSize.y);
            if (maximumX <= minimumX || maximumY <= minimumY)
            {
                return parentRect;
            }

            return Rect.MinMaxRect(
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, minimumX),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, minimumY),
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, maximumX),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, maximumY));
        }

        public static Vector2 CalculateControlCenter(
            Rect safeAreaInParent,
            Vector2 baseSize,
            float scale,
            Vector2 normalizedPosition)
        {
            scale = Mathf.Clamp(scale, MinimumScale, MaximumScale);
            normalizedPosition = ClampPosition(normalizedPosition);
            Vector2 halfSize = baseSize * scale * 0.5f;
            return new Vector2(
                InterpolateCenter(
                    safeAreaInParent.xMin,
                    safeAreaInParent.xMax,
                    halfSize.x,
                    normalizedPosition.x),
                InterpolateCenter(
                    safeAreaInParent.yMin,
                    safeAreaInParent.yMax,
                    halfSize.y,
                    normalizedPosition.y));
        }

        public static Vector2 CalculateNormalizedPosition(
            Rect safeAreaInParent,
            Vector2 baseSize,
            float scale,
            Vector2 center)
        {
            scale = Mathf.Clamp(scale, MinimumScale, MaximumScale);
            Vector2 halfSize = baseSize * scale * 0.5f;
            return new Vector2(
                InverseCenter(
                    safeAreaInParent.xMin,
                    safeAreaInParent.xMax,
                    halfSize.x,
                    center.x),
                InverseCenter(
                    safeAreaInParent.yMin,
                    safeAreaInParent.yMax,
                    halfSize.y,
                    center.y));
        }

        public static bool UsesTwoColumnSettingsLayout(
            Vector2 panelSize)
        {
            return panelSize.x > panelSize.y ||
                panelSize.y < 1500f;
        }

        public static float CalculateSettingsSliderWidth(
            Vector2 panelSize)
        {
            return UsesTwoColumnSettingsLayout(panelSize)
                ? Mathf.Clamp(panelSize.x * 0.4f, 280f, 720f)
                : Mathf.Clamp(panelSize.x - 160f, 280f, 760f);
        }

        public static Vector2 CalculateSettingsSliderPosition(
            Vector2 panelSize,
            bool attackControl,
            int row)
        {
            row = Mathf.Clamp(row, 0, 2);
            if (UsesTwoColumnSettingsLayout(panelSize))
            {
                float horizontalOffset = panelSize.x * 0.24f;
                return new Vector2(
                    attackControl
                        ? horizontalOffset
                        : -horizontalOffset,
                    -210f - row * 190f);
            }

            return new Vector2(
                0f,
                (attackControl ? -895f : -245f) - row * 200f);
        }

        public static void Apply(
            RectTransform control,
            Vector2 baseSize,
            float scale,
            Vector2 normalizedPosition,
            Rect parentRect,
            Rect safeAreaPixels,
            Vector2 screenSize)
        {
            if (control == null)
            {
                return;
            }

            scale = Mathf.Clamp(scale, MinimumScale, MaximumScale);
            Rect safeAreaInParent = CalculateSafeAreaInParent(
                parentRect,
                safeAreaPixels,
                screenSize);
            Vector2 center = CalculateControlCenter(
                safeAreaInParent,
                baseSize,
                scale,
                normalizedPosition);

            control.anchorMin = new Vector2(0.5f, 0.5f);
            control.anchorMax = new Vector2(0.5f, 0.5f);
            control.pivot = new Vector2(0.5f, 0.5f);
            control.anchoredPosition = center - parentRect.center;
            control.localScale = new Vector3(scale, scale, 1f);
        }

        private static Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp01(position.x),
                Mathf.Clamp01(position.y));
        }

        private static float InterpolateCenter(
            float minimum,
            float maximum,
            float halfSize,
            float normalizedPosition)
        {
            float availableMinimum = minimum + Mathf.Max(0f, halfSize);
            float availableMaximum = maximum - Mathf.Max(0f, halfSize);
            return availableMinimum <= availableMaximum
                ? Mathf.Lerp(
                    availableMinimum,
                    availableMaximum,
                    normalizedPosition)
                : (minimum + maximum) * 0.5f;
        }

        private static float InverseCenter(
            float minimum,
            float maximum,
            float halfSize,
            float center)
        {
            float availableMinimum = minimum + Mathf.Max(0f, halfSize);
            float availableMaximum = maximum - Mathf.Max(0f, halfSize);
            return availableMinimum < availableMaximum
                ? Mathf.InverseLerp(
                    availableMinimum,
                    availableMaximum,
                    center)
                : 0.5f;
        }
    }
}
