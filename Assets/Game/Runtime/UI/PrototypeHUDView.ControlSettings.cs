using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed partial class PrototypeHUDView
    {
        private void ApplyCommandControlsVisibility(bool enabled)
        {
            if (aimJoystick != null &&
                aimJoystick.gameObject.activeSelf != enabled)
            {
                aimJoystick.gameObject.SetActive(enabled);
            }

            if (attackButton != null &&
                attackButton.gameObject.activeSelf != enabled)
            {
                attackButton.gameObject.SetActive(enabled);
            }

        }

        private void BindControlSettingSliders()
        {
            ConfigureControlSlider(
                joystickSizeSlider,
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                OnJoystickSizeChanged);
            ConfigureControlSlider(
                attackSizeSlider,
                MobileControlSettingsStore.MinimumScale,
                MobileControlSettingsStore.MaximumScale,
                OnAttackSizeChanged);
        }

        private static void ConfigureControlSlider(
            Slider slider,
            float minimum,
            float maximum,
            UnityEngine.Events.UnityAction<float> callback)
        {
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = false;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback);
        }

        private void ToggleControlSettingsPage()
        {
            if (editingControlSettings)
            {
                CloseControlSettingsPage();
            }
            else
            {
                OpenControlSettingsPage();
            }
        }

        private void OpenControlSettingsPage()
        {
            editingControlSettings = true;
            SynchronizeControlSettingsUi();
            ApplyControlLayout(pendingControlSettings);
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
            ApplyCommandControlsVisibility(
                pendingControlSettings.controlsEnabled);
            SetControlSettingsPageVisible(true);
        }

        private void CloseControlSettingsPage()
        {
            editingControlSettings = false;
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void RestoreDefaultControlSettings()
        {
            pendingControlSettings = MobileControlSettings.Default;
            SynchronizeControlSettingsUi();
            PreviewPendingControlSettings();
        }

        private void ApplyPendingControlSettings()
        {
            MobileControlMode previousMode =
                controlSettings.controlMode;
            controlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            commandControlsEnabled = controlSettings.controlsEnabled;
            pendingControlSettings = controlSettings;
            MobileControlSettingsStore.Save(controlSettings);
            editingControlSettings = false;
            if (previousMode != controlSettings.controlMode ||
                !commandControlsEnabled)
            {
                aimJoystick?.CancelInput();
            }

            aimControlsPlayer?.SetControlMode(
                controlSettings.controlMode);
            aimControlsPlayer?.SetAutoAttackEnabled(
                controlSettings.autoAttackEnabled);
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void DiscardControlSettingsDraft()
        {
            pendingControlSettings = controlSettings;
            editingControlSettings = false;
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void RestoreAppliedControlPresentation()
        {
            ApplyControlModePresentation(
                controlSettings.controlMode);
            ApplyControlLayout(controlSettings);
            ApplyCommandControlsVisibility(
                controlSettings.controlsEnabled);
        }

        private void SetControlSettingsPageVisible(bool visible)
        {
            settingsPage?.SetActive(!visible);
            controlSettingsPanel?.SetActive(visible);
            if (settingsButton != null)
            {
                settingsButton.interactable = !visible;
            }

            if (settingsButtonGroup != null)
            {
                settingsButtonGroup.alpha = visible ? 0.35f : 1f;
                settingsButtonGroup.interactable = !visible;
                settingsButtonGroup.blocksRaycasts = !visible;
            }

            controlDragSurface?.SetDragEnabled(
                visible && pendingControlSettings.controlsEnabled);
        }

        private void SynchronizeControlSettingsUi()
        {
            joystickSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.joystickScale);
            attackSizeSlider.SetValueWithoutNotify(
                pendingControlSettings.attackScale);
            autoAttackToggle.SetIsOnWithoutNotify(
                pendingControlSettings.autoAttackEnabled);
            RefreshControlSettingLabels();
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
        }

        private void OnJoystickSizeChanged(float value)
        {
            pendingControlSettings.joystickScale = value;
            PreviewPendingControlSettings();
        }

        private void OnAutoAttackDraftChanged(bool enabled)
        {
            pendingControlSettings.autoAttackEnabled = enabled;
            RefreshAutoAttackSwitch();
        }

        private void SelectControlMode(MobileControlMode mode)
        {
            pendingControlSettings.controlsEnabled = true;
            pendingControlSettings.controlMode = mode;
            PreviewPendingControlSettings();
        }

        private void SelectHiddenControlMode()
        {
            pendingControlSettings.controlsEnabled = false;
            PreviewPendingControlSettings();
        }

        private void OnAttackSizeChanged(float value)
        {
            pendingControlSettings.attackScale = value;
            PreviewPendingControlSettings();
        }

        private void PreviewPendingControlSettings()
        {
            pendingControlSettings = MobileControlSettingsStore.Clamp(
                pendingControlSettings);
            RefreshControlSettingLabels();
            ApplyControlLayout(pendingControlSettings);
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
            ApplyCommandControlsVisibility(
                pendingControlSettings.controlsEnabled);
            controlDragSurface?.SetDragEnabled(
                editingControlSettings &&
                pendingControlSettings.controlsEnabled);
        }

        private void RefreshControlSettingLabels()
        {
            SetControlSettingLabel(
                joystickSizeSlider,
                pendingControlSettings.joystickScale);
            SetControlSettingLabel(
                attackSizeSlider,
                pendingControlSettings.attackScale);
            RefreshControlModeButtons();
            RefreshAutoAttackSwitch();
        }

        private static void SetControlSettingLabel(
            Slider slider,
            float value)
        {
            Transform valueTransform = slider.transform.Find("Value");
            TMP_Text label = valueTransform != null
                ? valueTransform.GetComponent<TMP_Text>()
                : null;
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void RefreshControlModeButtons()
        {
            bool hidden = !pendingControlSettings.controlsEnabled;
            SetControlModeButtonSelected(
                modeOneButton,
                !hidden && pendingControlSettings.controlMode ==
                    MobileControlMode.DirectMoveAutoAim);
            SetControlModeButtonSelected(
                modeTwoButton,
                !hidden && pendingControlSettings.controlMode ==
                    MobileControlMode.AimCommand);
            SetControlModeButtonSelected(hiddenModeButton, hidden);
        }

        private static void SetControlModeButtonSelected(
            Button button,
            bool selected)
        {
            Image image = button != null
                ? button.GetComponent<Image>()
                : null;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.16f, 0.56f, 0.92f, 0.98f)
                    : new Color(0.23f, 0.27f, 0.31f, 0.94f);
            }
        }

        private void RefreshAutoAttackSwitch()
        {
            if (autoAttackToggle == null)
            {
                return;
            }

            bool enabled = pendingControlSettings.autoAttackEnabled;
            autoAttackToggle.SetIsOnWithoutNotify(enabled);
            if (autoAttackTrack != null)
            {
                autoAttackTrack.color = enabled
                    ? new Color(0.16f, 0.56f, 0.92f, 1f)
                    : new Color(0.34f, 0.36f, 0.39f, 1f);
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

            if (autoAttackValueLabel != null)
            {
                autoAttackValueLabel.text = enabled
                    ? Text(GameStringIds.UiAutoAttackOn, "On")
                    : Text(GameStringIds.UiAutoAttackOff, "Off");
            }
        }

        private void OnControlDragged(
            ControlLayoutDragTarget target,
            Vector2 screenPoint,
            Camera eventCamera)
        {
            RectTransform control = target ==
                ControlLayoutDragTarget.Joystick
                    ? aimJoystick?.TouchArea
                    : attackButton != null
                        ? attackButton.GetComponent<RectTransform>()
                        : null;
            RectTransform parent = control != null
                ? control.parent as RectTransform
                : null;
            if (parent == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    screenPoint,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Vector2 baseSize = target == ControlLayoutDragTarget.Joystick
                ? joystickBaseSize
                : attackBaseSize;
            float scale = target == ControlLayoutDragTarget.Joystick
                ? pendingControlSettings.joystickScale
                : pendingControlSettings.attackScale;
            Rect safeArea = MobileControlSettingsStore
                .CalculateSafeAreaInParent(
                    parent.rect,
                    Screen.safeArea,
                    new Vector2(Screen.width, Screen.height));
            Vector2 normalized = MobileControlSettingsStore
                .CalculateNormalizedPosition(
                    safeArea,
                    baseSize,
                    scale,
                    localPoint);
            if (target == ControlLayoutDragTarget.Joystick)
            {
                pendingControlSettings.joystickPosition = normalized;
            }
            else
            {
                pendingControlSettings.attackPosition = normalized;
            }

            PreviewPendingControlSettings();
        }

        private void ApplyControlModePresentation(
            MobileControlMode mode)
        {
            TMP_Text label = attackButton != null
                ? attackButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null)
            {
                label.text = mode ==
                    MobileControlMode.DirectMoveAutoAim
                        ? Text(
                            GameStringIds.UiAutoAimButton,
                            "자동 조준")
                        : Text(
                            GameStringIds.UiAttackButton,
                            "공격");
            }
        }

        private void CaptureControlBaseSizes()
        {
            RectTransform joystickRect = aimJoystick != null
                ? aimJoystick.TouchArea
                : null;
            RectTransform attackRect = attackButton != null
                ? attackButton.GetComponent<RectTransform>()
                : null;
            joystickBaseSize = GetControlBaseSize(joystickRect);
            attackBaseSize = GetControlBaseSize(attackRect);
        }

        private static Vector2 GetControlBaseSize(RectTransform control)
        {
            if (control == null)
            {
                return Vector2.zero;
            }

            Vector2 size = control.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = control.sizeDelta;
            }

            return new Vector2(
                Mathf.Abs(size.x),
                Mathf.Abs(size.y));
        }

        private void ApplyControlLayout(MobileControlSettings settings)
        {
            RectTransform joystickRect = aimJoystick != null
                ? aimJoystick.TouchArea
                : null;
            RectTransform attackRect = attackButton != null
                ? attackButton.GetComponent<RectTransform>()
                : null;
            ApplyControlLayout(
                joystickRect,
                joystickBaseSize,
                settings.joystickScale,
                settings.joystickPosition);
            ApplyControlLayout(
                attackRect,
                attackBaseSize,
                settings.attackScale,
                settings.attackPosition);
        }

        private static void ApplyControlLayout(
            RectTransform control,
            Vector2 baseSize,
            float scale,
            Vector2 normalizedPosition)
        {
            RectTransform parent = control != null
                ? control.parent as RectTransform
                : null;
            if (parent == null || baseSize.x <= 0f || baseSize.y <= 0f)
            {
                return;
            }

            MobileControlSettingsStore.Apply(
                control,
                baseSize,
                scale,
                normalizedPosition,
                parent.rect,
                Screen.safeArea,
                new Vector2(Screen.width, Screen.height));
        }

        private void OnRectTransformDimensionsChange()
        {
            if (joystickBaseSize.x <= 0f || attackBaseSize.x <= 0f)
            {
                return;
            }

            ApplyControlLayout(
                editingControlSettings
                    ? pendingControlSettings
                    : controlSettings);
        }
    }
}
