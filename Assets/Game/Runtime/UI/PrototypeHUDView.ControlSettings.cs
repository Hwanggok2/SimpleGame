using TMPro;
using UnityEngine;

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

        private void OpenControlSettingsPage()
        {
            editingControlSettings = true;
            SetControlSettingsPageVisible(true);
            SynchronizeControlSettingsUi();
            ApplyControlLayout(pendingControlSettings);
            ApplyControlModePresentation(
                pendingControlSettings.controlMode);
            ApplyCommandControlsVisibility(
                pendingControlSettings.controlsEnabled);
        }

        private void CloseControlSettingsPage()
        {
            editingControlSettings = false;
            RestoreAppliedControlPresentation();
            SetControlSettingsPageVisible(false);
        }

        private void RestoreDefaultControlSettings()
        {
            pendingControlSettings = controlSettingsProfile != null
                ? controlSettingsProfile.CreateDefaultSettings()
                : MobileControlSettings.Default;
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
            pauseDetailsPanel?.SetControlSettingsVisible(visible);
            controlSettingsPanel?.SetDragEnabled(
                visible && pendingControlSettings.controlsEnabled);
        }

        private void SynchronizeControlSettingsUi()
        {
            controlSettingsPanel?.SetValues(pendingControlSettings);
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
            controlSettingsPanel?.SetDragEnabled(
                editingControlSettings &&
                pendingControlSettings.controlsEnabled);
        }

        private void RefreshControlSettingLabels()
        {
            controlSettingsPanel?.SetValues(pendingControlSettings);
            RefreshAutoAttackSwitch();
        }

        private void RefreshAutoAttackSwitch()
        {
            if (controlSettingsPanel == null)
            {
                return;
            }

            bool enabled = pendingControlSettings.autoAttackEnabled;
            controlSettingsPanel.SetAutoAttackPresentation(enabled);
            controlSettingsPanel.SetAutoAttackValueText(enabled
                ? Text(GameStringIds.UiAutoAttackOn, "On")
                : Text(GameStringIds.UiAutoAttackOff, "Off"));
        }

        private void OnControlSettingsActionRequested(
            ControlSettingsAction action)
        {
            switch (action)
            {
                case ControlSettingsAction.RestoreDefaults:
                    RestoreDefaultControlSettings();
                    break;
                case ControlSettingsAction.Apply:
                    ApplyPendingControlSettings();
                    break;
                case ControlSettingsAction.ModeOne:
                    SelectControlMode(
                        MobileControlMode.DirectMoveAutoAim);
                    break;
                case ControlSettingsAction.ModeTwo:
                    SelectControlMode(MobileControlMode.AimCommand);
                    break;
                case ControlSettingsAction.Hidden:
                    SelectHiddenControlMode();
                    break;
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
