using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class LobbyControlSettingsView : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private RectTransform previewArea;
        [SerializeField] private RectTransform joystickPreview;
        [SerializeField] private RectTransform attackPreview;

        private ControlSettingsPanelView panelView;
        private GameStringTable strings;
        private ControlSettingsProfile controlSettingsProfile;
        private MobileControlSettings pendingSettings;
        private Vector2 joystickBaseSize;
        private Vector2 attackBaseSize;
        private bool initialized;

        public MobileControlSettings PendingSettings => pendingSettings;
        public event Action Applied;

        public void Configure(
            GameObject configuredSettingsPanel,
            RectTransform configuredPreviewArea,
            RectTransform configuredJoystickPreview,
            RectTransform configuredAttackPreview)
        {
            settingsPanel = configuredSettingsPanel;
            previewArea = configuredPreviewArea;
            joystickPreview = configuredJoystickPreview;
            attackPreview = configuredAttackPreview;
        }

        public void Initialize(GameStringTable configuredStrings)
        {
            Initialize(configuredStrings, null);
        }

        public void Initialize(
            GameStringTable configuredStrings,
            ControlSettingsProfile configuredControlSettingsProfile)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            strings = configuredStrings;
            controlSettingsProfile = configuredControlSettingsProfile;
            ResolveReferences();
            Localize();
            BindControls();
            joystickBaseSize = GetBaseSize(joystickPreview);
            attackBaseSize = GetBaseSize(attackPreview);
            BeginEditing();
        }

        public void BeginEditing()
        {
            pendingSettings = MobileControlSettingsStore.Load(
                controlSettingsProfile);
            SynchronizeUi();
        }

        public void DiscardDraft()
        {
            pendingSettings = MobileControlSettingsStore.Load(
                controlSettingsProfile);
            SynchronizeUi();
        }

        private void ResolveReferences()
        {
            panelView = settingsPanel != null
                ? settingsPanel.GetComponent<ControlSettingsPanelView>()
                : null;
            if (panelView == null ||
                !panelView.IsConfigured ||
                previewArea == null ||
                joystickPreview == null ||
                attackPreview == null)
            {
                Debug.LogError(
                    "Lobby control settings prefab references are " +
                    "incomplete.",
                    this);
            }
        }

        private void BindControls()
        {
            if (panelView == null)
            {
                return;
            }

            panelView.ActionRequested += OnActionRequested;
            panelView.AutoAttackChanged += OnAutoAttackChanged;
            panelView.JoystickSizeChanged += OnJoystickSizeChanged;
            panelView.AttackSizeChanged += OnAttackSizeChanged;
            panelView.ConfigureDragSurface(
                joystickPreview,
                attackPreview,
                OnControlDragged);
        }

        private void Localize()
        {
            if (panelView == null)
            {
                return;
            }

            panelView.SetText(
                ControlSettingsTextId.AutoAttack,
                GetText(GameStringIds.UiAutoAttack, "자동 공격"));
            panelView.SetText(
                ControlSettingsTextId.ControlMode,
                GetText(GameStringIds.UiControlMode, "조작 모드"));
            panelView.SetText(
                ControlSettingsTextId.ModeOne,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeOneText
                    : GetText(GameStringIds.ControlModeOneName, "모드 1"));
            panelView.SetText(
                ControlSettingsTextId.ModeTwo,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeTwoText
                    : GetText(GameStringIds.ControlModeTwoName, "모드 2"));
            panelView.SetText(
                ControlSettingsTextId.Hidden,
                controlSettingsProfile != null
                    ? controlSettingsProfile.HiddenText
                    : GetText(GameStringIds.ControlModeHiddenName, "숨기기"));
            panelView.SetText(
                ControlSettingsTextId.JoystickSize,
                GetText(GameStringIds.UiJoystickSize, "왼쪽 조이스틱 크기"));
            panelView.SetText(
                ControlSettingsTextId.AttackSize,
                GetText(GameStringIds.UiAttackSize, "오른쪽 공격 버튼 크기"));
            panelView.SetText(
                ControlSettingsTextId.Defaults,
                GetText(GameStringIds.UiDefaults, "기본값"));
            panelView.SetText(
                ControlSettingsTextId.Apply,
                GetText(GameStringIds.UiApply, "적용"));
        }

        private void SynchronizeUi()
        {
            pendingSettings = MobileControlSettingsStore.Clamp(
                pendingSettings);
            panelView?.SetValues(pendingSettings);
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            pendingSettings = MobileControlSettingsStore.Clamp(
                pendingSettings);
            panelView?.SetValues(pendingSettings);
            RefreshAutoAttackSwitch();
            ApplyPreview();
        }

        private void OnActionRequested(ControlSettingsAction action)
        {
            switch (action)
            {
                case ControlSettingsAction.RestoreDefaults:
                    RestoreDefaults();
                    break;
                case ControlSettingsAction.Apply:
                    Apply();
                    break;
                case ControlSettingsAction.ModeOne:
                    SelectMode(MobileControlMode.DirectMoveAutoAim);
                    break;
                case ControlSettingsAction.ModeTwo:
                    SelectMode(MobileControlMode.AimCommand);
                    break;
                case ControlSettingsAction.Hidden:
                    SelectHiddenMode();
                    break;
            }
        }

        private void OnJoystickSizeChanged(float value)
        {
            pendingSettings.joystickScale = value;
            RefreshPresentation();
        }

        private void OnAttackSizeChanged(float value)
        {
            pendingSettings.attackScale = value;
            RefreshPresentation();
        }

        private void OnAutoAttackChanged(bool enabled)
        {
            pendingSettings.autoAttackEnabled = enabled;
            RefreshAutoAttackSwitch();
        }

        private void SelectMode(MobileControlMode mode)
        {
            pendingSettings.controlsEnabled = true;
            pendingSettings.controlMode = mode;
            RefreshPresentation();
        }

        private void SelectHiddenMode()
        {
            pendingSettings.controlsEnabled = false;
            RefreshPresentation();
        }

        private void RestoreDefaults()
        {
            pendingSettings = controlSettingsProfile != null
                ? controlSettingsProfile.CreateDefaultSettings()
                : MobileControlSettings.Default;
            SynchronizeUi();
        }

        private void Apply()
        {
            pendingSettings = MobileControlSettingsStore.Clamp(
                pendingSettings);
            MobileControlSettingsStore.Save(pendingSettings);
            SynchronizeUi();
            Applied?.Invoke();
        }

        private void OnControlDragged(
            ControlLayoutDragTarget target,
            Vector2 screenCenter,
            Camera eventCamera)
        {
            if (previewArea == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    previewArea,
                    screenCenter,
                    eventCamera,
                    out Vector2 localCenter))
            {
                return;
            }

            Vector2 normalized =
                MobileControlSettingsStore.CalculateNormalizedPosition(
                    previewArea.rect,
                    target == ControlLayoutDragTarget.Joystick
                        ? joystickBaseSize
                        : attackBaseSize,
                    target == ControlLayoutDragTarget.Joystick
                        ? pendingSettings.joystickScale
                        : pendingSettings.attackScale,
                    localCenter);
            if (target == ControlLayoutDragTarget.Joystick)
            {
                pendingSettings.joystickPosition = normalized;
            }
            else
            {
                pendingSettings.attackPosition = normalized;
            }

            RefreshPresentation();
        }

        private void ApplyPreview()
        {
            bool visible = pendingSettings.controlsEnabled;
            SetActive(joystickPreview, visible);
            SetActive(attackPreview, visible);
            panelView?.SetDragEnabled(visible);
            if (!visible || previewArea == null)
            {
                return;
            }

            ApplyPreviewControl(
                joystickPreview,
                joystickBaseSize,
                pendingSettings.joystickScale,
                pendingSettings.joystickPosition);
            ApplyPreviewControl(
                attackPreview,
                attackBaseSize,
                pendingSettings.attackScale,
                pendingSettings.attackPosition);
        }

        private void ApplyPreviewControl(
            RectTransform control,
            Vector2 baseSize,
            float scale,
            Vector2 normalizedPosition)
        {
            if (control == null || previewArea == null)
            {
                return;
            }

            control.anchorMin = new Vector2(0.5f, 0.5f);
            control.anchorMax = new Vector2(0.5f, 0.5f);
            control.pivot = new Vector2(0.5f, 0.5f);
            control.anchoredPosition =
                MobileControlSettingsStore.CalculateControlCenter(
                    previewArea.rect,
                    baseSize,
                    scale,
                    normalizedPosition);
            control.localScale = new Vector3(scale, scale, 1f);
        }

        private void RefreshAutoAttackSwitch()
        {
            bool enabled = pendingSettings.autoAttackEnabled;
            panelView?.SetAutoAttackPresentation(enabled);
            panelView?.SetAutoAttackValueText(GetText(
                enabled
                    ? GameStringIds.UiAutoAttackOn
                    : GameStringIds.UiAutoAttackOff,
                enabled ? "On" : "Off"));
        }

        private static void SetActive(
            Component component,
            bool active)
        {
            if (component != null &&
                component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }

        private static Vector2 GetBaseSize(RectTransform rect)
        {
            if (rect == null)
            {
                return Vector2.zero;
            }

            Vector2 size = rect.rect.size;
            return size.x > 0f && size.y > 0f
                ? size
                : rect.sizeDelta;
        }

        private string GetText(string stringId, string fallback)
        {
            return strings != null
                ? strings.Get(stringId, fallback)
                : fallback;
        }
    }
}
