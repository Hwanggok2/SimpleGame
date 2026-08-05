using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public enum ControlSettingsAction
    {
        RestoreDefaults,
        Apply,
        ModeOne,
        ModeTwo,
        Hidden
    }

    public enum ControlSettingsTextId
    {
        Title,
        AutoAttack,
        ControlMode,
        JoystickSize,
        AttackSize,
        ModeOne,
        ModeTwo,
        Hidden,
        Defaults,
        Apply
    }

    [DisallowMultipleComponent]
    public sealed class ControlSettingsPanelView : MonoBehaviour
    {
        private static readonly Color SelectedColor =
            new(0.16f, 0.56f, 0.92f, 0.98f);
        private static readonly Color NormalColor =
            new(0.23f, 0.27f, 0.31f, 0.94f);
        private static readonly Color SwitchOnColor =
            new(0.16f, 0.56f, 0.92f, 1f);
        private static readonly Color SwitchOffColor =
            new(0.34f, 0.36f, 0.39f, 1f);

        [Header("Text")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text autoAttackLabel;
        [SerializeField] private TMP_Text controlModeLabel;
        [SerializeField] private TMP_Text joystickSizeLabel;
        [SerializeField] private TMP_Text attackSizeLabel;
        [SerializeField] private TMP_Text modeOneLabel;
        [SerializeField] private TMP_Text modeTwoLabel;
        [SerializeField] private TMP_Text hiddenLabel;
        [SerializeField] private TMP_Text defaultsLabel;
        [SerializeField] private TMP_Text applyLabel;
        [SerializeField] private TMP_Text autoAttackValueLabel;
        [SerializeField] private TMP_Text joystickSizeValueLabel;
        [SerializeField] private TMP_Text attackSizeValueLabel;

        [Header("Controls")]
        [SerializeField] private Toggle autoAttackToggle;
        [SerializeField] private Image autoAttackTrack;
        [SerializeField] private RectTransform autoAttackKnob;
        [SerializeField] private Image autoAttackKnobImage;
        [SerializeField] private Slider joystickSizeSlider;
        [SerializeField] private Slider attackSizeSlider;
        [SerializeField] private Button modeOneButton;
        [SerializeField] private Button modeTwoButton;
        [SerializeField] private Button hiddenButton;
        [SerializeField] private Button defaultsButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private ControlLayoutDragSurface dragSurface;

        public event Action<ControlSettingsAction> ActionRequested;
        public event Action<bool> AutoAttackChanged;
        public event Action<float> JoystickSizeChanged;
        public event Action<float> AttackSizeChanged;

        public bool IsConfigured =>
            autoAttackToggle != null &&
            autoAttackTrack != null &&
            autoAttackKnob != null &&
            autoAttackKnobImage != null &&
            autoAttackValueLabel != null &&
            joystickSizeValueLabel != null &&
            attackSizeValueLabel != null &&
            joystickSizeSlider != null &&
            attackSizeSlider != null &&
            modeOneButton != null &&
            modeTwoButton != null &&
            hiddenButton != null &&
            defaultsButton != null &&
            applyButton != null &&
            dragSurface != null;

        public void ConfigureReferences(
            TMP_Text configuredTitleLabel,
            TMP_Text configuredAutoAttackLabel,
            TMP_Text configuredControlModeLabel,
            TMP_Text configuredJoystickSizeLabel,
            TMP_Text configuredAttackSizeLabel,
            TMP_Text configuredModeOneLabel,
            TMP_Text configuredModeTwoLabel,
            TMP_Text configuredHiddenLabel,
            TMP_Text configuredDefaultsLabel,
            TMP_Text configuredApplyLabel,
            TMP_Text configuredAutoAttackValueLabel,
            TMP_Text configuredJoystickSizeValueLabel,
            TMP_Text configuredAttackSizeValueLabel,
            Toggle configuredAutoAttackToggle,
            Image configuredAutoAttackTrack,
            RectTransform configuredAutoAttackKnob,
            Image configuredAutoAttackKnobImage,
            Slider configuredJoystickSizeSlider,
            Slider configuredAttackSizeSlider,
            Button configuredModeOneButton,
            Button configuredModeTwoButton,
            Button configuredHiddenButton,
            Button configuredDefaultsButton,
            Button configuredApplyButton,
            ControlLayoutDragSurface configuredDragSurface)
        {
            titleLabel = configuredTitleLabel;
            autoAttackLabel = configuredAutoAttackLabel;
            controlModeLabel = configuredControlModeLabel;
            joystickSizeLabel = configuredJoystickSizeLabel;
            attackSizeLabel = configuredAttackSizeLabel;
            modeOneLabel = configuredModeOneLabel;
            modeTwoLabel = configuredModeTwoLabel;
            hiddenLabel = configuredHiddenLabel;
            defaultsLabel = configuredDefaultsLabel;
            applyLabel = configuredApplyLabel;
            autoAttackValueLabel = configuredAutoAttackValueLabel;
            joystickSizeValueLabel = configuredJoystickSizeValueLabel;
            attackSizeValueLabel = configuredAttackSizeValueLabel;
            autoAttackToggle = configuredAutoAttackToggle;
            autoAttackTrack = configuredAutoAttackTrack;
            autoAttackKnob = configuredAutoAttackKnob;
            autoAttackKnobImage = configuredAutoAttackKnobImage;
            joystickSizeSlider = configuredJoystickSizeSlider;
            attackSizeSlider = configuredAttackSizeSlider;
            modeOneButton = configuredModeOneButton;
            modeTwoButton = configuredModeTwoButton;
            hiddenButton = configuredHiddenButton;
            defaultsButton = configuredDefaultsButton;
            applyButton = configuredApplyButton;
            dragSurface = configuredDragSurface;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            BindControls();
        }

        public void SetText(ControlSettingsTextId id, string value)
        {
            TMP_Text label = id switch
            {
                ControlSettingsTextId.Title => titleLabel,
                ControlSettingsTextId.AutoAttack => autoAttackLabel,
                ControlSettingsTextId.ControlMode => controlModeLabel,
                ControlSettingsTextId.JoystickSize => joystickSizeLabel,
                ControlSettingsTextId.AttackSize => attackSizeLabel,
                ControlSettingsTextId.ModeOne => modeOneLabel,
                ControlSettingsTextId.ModeTwo => modeTwoLabel,
                ControlSettingsTextId.Hidden => hiddenLabel,
                ControlSettingsTextId.Defaults => defaultsLabel,
                ControlSettingsTextId.Apply => applyLabel,
                _ => null
            };
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        public void SetValues(MobileControlSettings settings)
        {
            joystickSizeSlider?.SetValueWithoutNotify(
                settings.joystickScale);
            attackSizeSlider?.SetValueWithoutNotify(
                settings.attackScale);
            autoAttackToggle?.SetIsOnWithoutNotify(
                settings.autoAttackEnabled);
            SetScaleLabel(
                joystickSizeValueLabel,
                settings.joystickScale);
            SetScaleLabel(
                attackSizeValueLabel,
                settings.attackScale);
            SetModeSelection(
                settings.controlsEnabled,
                settings.controlMode);
            SetAutoAttackPresentation(settings.autoAttackEnabled);
        }

        public void SetModeSelection(
            bool controlsEnabled,
            MobileControlMode mode)
        {
            bool hidden = !controlsEnabled;
            SetSelected(
                modeOneButton,
                !hidden && mode == MobileControlMode.DirectMoveAutoAim);
            SetSelected(
                modeTwoButton,
                !hidden && mode == MobileControlMode.AimCommand);
            SetSelected(hiddenButton, hidden);
        }

        public void SetAutoAttackPresentation(bool enabled)
        {
            autoAttackToggle?.SetIsOnWithoutNotify(enabled);
            if (autoAttackTrack != null)
            {
                autoAttackTrack.color = enabled
                    ? SwitchOnColor
                    : SwitchOffColor;
            }

            if (autoAttackKnob != null)
            {
                Vector2 position = autoAttackKnob.anchoredPosition;
                position.x = enabled ? 32f : -32f;
                autoAttackKnob.anchoredPosition = position;
            }

            if (autoAttackKnobImage != null)
            {
                autoAttackKnobImage.color = enabled
                    ? new Color(0.52f, 0.78f, 1f, 1f)
                    : new Color(0.62f, 0.63f, 0.65f, 1f);
            }
        }

        public void SetAutoAttackValueText(string value)
        {
            if (autoAttackValueLabel != null)
            {
                autoAttackValueLabel.text = value ?? string.Empty;
            }
        }

        public void SetDragEnabled(bool enabled)
        {
            dragSurface?.SetDragEnabled(enabled);
        }

        public void ConfigureDragSurface(
            RectTransform joystick,
            RectTransform attack,
            Action<ControlLayoutDragTarget, Vector2, Camera> moved)
        {
            dragSurface?.Configure(joystick, attack, moved);
        }

        private void BindControls()
        {
            Bind(modeOneButton, ControlSettingsAction.ModeOne);
            Bind(modeTwoButton, ControlSettingsAction.ModeTwo);
            Bind(hiddenButton, ControlSettingsAction.Hidden);
            Bind(defaultsButton, ControlSettingsAction.RestoreDefaults);
            Bind(applyButton, ControlSettingsAction.Apply);
            if (autoAttackToggle != null)
            {
                autoAttackToggle.onValueChanged.RemoveAllListeners();
                autoAttackToggle.onValueChanged.AddListener(
                    value => AutoAttackChanged?.Invoke(value));
            }

            ConfigureSlider(
                joystickSizeSlider,
                value => JoystickSizeChanged?.Invoke(value));
            ConfigureSlider(
                attackSizeSlider,
                value => AttackSizeChanged?.Invoke(value));
        }

        private void Bind(Button button, ControlSettingsAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => ActionRequested?.Invoke(action));
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

        private static void SetScaleLabel(TMP_Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }
    }
}
