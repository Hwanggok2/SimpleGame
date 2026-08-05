using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbyControlSettingsView : MonoBehaviour
    {
        private static readonly Color SelectedColor =
            new(0.16f, 0.56f, 0.92f, 0.98f);
        private static readonly Color NormalColor =
            new(0.23f, 0.27f, 0.31f, 0.94f);
        private static readonly Color SwitchOnColor =
            new(0.22f, 0.62f, 0.96f, 1f);
        private static readonly Color SwitchOffColor =
            new(0.34f, 0.36f, 0.39f, 1f);

        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private RectTransform previewArea;
        [SerializeField] private RectTransform joystickPreview;
        [SerializeField] private RectTransform attackPreview;

        private Toggle autoAttackToggle;
        private Image autoAttackTrack;
        private RectTransform autoAttackKnob;
        private Image autoAttackKnobImage;
        private TMP_Text autoAttackValueLabel;
        private Slider joystickSizeSlider;
        private Slider attackSizeSlider;
        private Button modeOneButton;
        private Button modeTwoButton;
        private Button hiddenModeButton;
        private ControlLayoutDragSurface dragSurface;
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
            Transform root = settingsPanel != null
                ? settingsPanel.transform
                : null;
            Transform toggleTransform = root != null
                ? root.Find("AutoAttackToggle")
                : null;
            autoAttackToggle = toggleTransform != null
                ? toggleTransform.GetComponent<Toggle>()
                : null;
            autoAttackTrack = FindImage(toggleTransform, "Track");
            autoAttackKnobImage = FindImage(
                toggleTransform,
                "Track/Knob");
            autoAttackKnob = autoAttackKnobImage != null
                ? autoAttackKnobImage.rectTransform
                : null;
            autoAttackValueLabel = FindText(toggleTransform, "Value");
            joystickSizeSlider = FindSlider(
                root,
                "JoystickSizeSlider");
            attackSizeSlider = FindSlider(root, "AttackSizeSlider");
            modeOneButton = FindButton(
                root,
                "ControlModeButtons/Mode1Button");
            modeTwoButton = FindButton(
                root,
                "ControlModeButtons/Mode2Button");
            hiddenModeButton = FindButton(
                root,
                "ControlModeButtons/HiddenButton");
            dragSurface = root != null
                ? root.Find("ControlDragSurface")
                    ?.GetComponent<ControlLayoutDragSurface>()
                : null;

            if (autoAttackToggle == null ||
                autoAttackTrack == null ||
                autoAttackKnob == null ||
                autoAttackValueLabel == null ||
                joystickSizeSlider == null ||
                attackSizeSlider == null ||
                modeOneButton == null ||
                modeTwoButton == null ||
                hiddenModeButton == null ||
                dragSurface == null ||
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
            if (autoAttackToggle != null)
            {
                autoAttackToggle.onValueChanged.RemoveAllListeners();
                autoAttackToggle.onValueChanged.AddListener(
                    OnAutoAttackChanged);
            }

            ConfigureSlider(
                joystickSizeSlider,
                value =>
                {
                    pendingSettings.joystickScale = value;
                    RefreshPresentation();
                });
            ConfigureSlider(
                attackSizeSlider,
                value =>
                {
                    pendingSettings.attackScale = value;
                    RefreshPresentation();
                });
            BindButton(
                modeOneButton,
                () => SelectMode(
                    MobileControlMode.DirectMoveAutoAim));
            BindButton(
                modeTwoButton,
                () => SelectMode(MobileControlMode.AimCommand));
            BindButton(hiddenModeButton, SelectHiddenMode);
            BindButton(
                FindButton(
                    settingsPanel?.transform,
                    "ControlDefaultsButton"),
                RestoreDefaults);
            BindButton(
                FindButton(
                    settingsPanel?.transform,
                    "ControlApplyButton"),
                Apply);
            dragSurface?.Configure(
                joystickPreview,
                attackPreview,
                OnControlDragged);
        }

        private void Localize()
        {
            Transform root = settingsPanel != null
                ? settingsPanel.transform
                : null;
            SetText(
                root,
                "AutoAttackToggle/Label",
                GameStringIds.UiAutoAttack,
                "자동 공격");
            SetText(
                root,
                "ControlModeLabel",
                GameStringIds.UiControlMode,
                "조작 모드");
            SetText(
                root,
                "ControlModeButtons/Mode1Button/Label",
                GameStringIds.ControlModeOneName,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeOneText
                    : "모드 1");
            SetText(
                root,
                "ControlModeButtons/Mode2Button/Label",
                GameStringIds.ControlModeTwoName,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeTwoText
                    : "모드 2");
            SetText(
                root,
                "ControlModeButtons/HiddenButton/Label",
                GameStringIds.ControlModeHiddenName,
                controlSettingsProfile != null
                    ? controlSettingsProfile.HiddenText
                    : "숨기기");
            SetText(
                root,
                "JoystickSizeSlider/Label",
                GameStringIds.UiJoystickSize,
                "왼쪽 조이스틱 크기");
            SetText(
                root,
                "AttackSizeSlider/Label",
                GameStringIds.UiAttackSize,
                "오른쪽 공격 버튼 크기");
            SetText(
                root,
                "ControlDefaultsButton/Label",
                GameStringIds.UiDefaults,
                "기본값");
            SetText(
                root,
                "ControlApplyButton/Label",
                GameStringIds.UiApply,
                "적용");

            if (controlSettingsProfile != null)
            {
                SetDirectText(
                    root,
                    "ControlModeButtons/Mode1Button/Label",
                    controlSettingsProfile.ModeOneText);
                SetDirectText(
                    root,
                    "ControlModeButtons/Mode2Button/Label",
                    controlSettingsProfile.ModeTwoText);
                SetDirectText(
                    root,
                    "ControlModeButtons/HiddenButton/Label",
                    controlSettingsProfile.HiddenText);
            }
        }

        private void SynchronizeUi()
        {
            pendingSettings = MobileControlSettingsStore.Clamp(
                pendingSettings);
            joystickSizeSlider?.SetValueWithoutNotify(
                pendingSettings.joystickScale);
            attackSizeSlider?.SetValueWithoutNotify(
                pendingSettings.attackScale);
            autoAttackToggle?.SetIsOnWithoutNotify(
                pendingSettings.autoAttackEnabled);
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            pendingSettings = MobileControlSettingsStore.Clamp(
                pendingSettings);
            SetSliderValueLabel(
                joystickSizeSlider,
                pendingSettings.joystickScale);
            SetSliderValueLabel(
                attackSizeSlider,
                pendingSettings.attackScale);
            bool hidden = !pendingSettings.controlsEnabled;
            SetSelected(
                modeOneButton,
                !hidden && pendingSettings.controlMode ==
                    MobileControlMode.DirectMoveAutoAim);
            SetSelected(
                modeTwoButton,
                !hidden && pendingSettings.controlMode ==
                    MobileControlMode.AimCommand);
            SetSelected(hiddenModeButton, hidden);
            RefreshAutoAttackSwitch();
            ApplyPreview();
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
            dragSurface?.SetDragEnabled(visible);
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
            if (autoAttackTrack != null)
            {
                autoAttackTrack.color = enabled
                    ? SwitchOnColor
                    : SwitchOffColor;
            }

            if (autoAttackKnobImage != null)
            {
                autoAttackKnobImage.color = enabled
                    ? Color.white
                    : new Color(0.62f, 0.63f, 0.65f, 1f);
            }

            if (autoAttackKnob != null)
            {
                autoAttackKnob.anchoredPosition = new Vector2(
                    enabled ? 32f : -32f,
                    0f);
            }

            if (autoAttackValueLabel != null)
            {
                autoAttackValueLabel.text = GetText(
                    enabled
                        ? GameStringIds.UiAutoAttackOn
                        : GameStringIds.UiAutoAttackOff,
                    enabled ? "On" : "Off");
            }
        }

        private static void ConfigureSlider(
            Slider slider,
            UnityEngine.Events.UnityAction<float> callback)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = MobileControlSettingsStore.MinimumScale;
            slider.maxValue = MobileControlSettingsStore.MaximumScale;
            slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback);
        }

        private static void BindButton(
            Button button,
            UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        private static void SetSelected(Button button, bool selected)
        {
            Image image = button != null
                ? button.GetComponent<Image>()
                : null;
            if (image != null)
            {
                image.color = selected ? SelectedColor : NormalColor;
            }
        }

        private static void SetSliderValueLabel(
            Slider slider,
            float value)
        {
            TMP_Text label = slider != null
                ? slider.transform.Find("Value")
                    ?.GetComponent<TMP_Text>()
                : null;
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
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

        private static Image FindImage(Transform root, string path)
        {
            return root != null
                ? root.Find(path)?.GetComponent<Image>()
                : null;
        }

        private static TMP_Text FindText(Transform root, string path)
        {
            return root != null
                ? root.Find(path)?.GetComponent<TMP_Text>()
                : null;
        }

        private static Slider FindSlider(Transform root, string path)
        {
            return root != null
                ? root.Find(path)?.GetComponent<Slider>()
                : null;
        }

        private static Button FindButton(Transform root, string path)
        {
            return root != null
                ? root.Find(path)?.GetComponent<Button>()
                : null;
        }

        private void SetText(
            Transform root,
            string path,
            string stringId,
            string fallback)
        {
            TMP_Text label = FindText(root, path);
            if (label != null)
            {
                label.text = GetText(stringId, fallback);
            }
        }

        private static void SetDirectText(
            Transform root,
            string path,
            string value)
        {
            TMP_Text label = FindText(root, path);
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private string GetText(string stringId, string fallback)
        {
            return strings != null
                ? strings.Get(stringId, fallback)
                : fallback;
        }
    }
}
