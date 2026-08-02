using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed partial class PrototypeHUDView
    {
        private void ApplyPersistentStrings()
        {
            SetButtonText(
                settingsButton,
                GameStringIds.UiSettingsButton,
                "설정");
            ApplyControlModePresentation(controlSettings.controlMode);
        }

        private void ApplyDifficultyStrings()
        {
            if (difficultySelectionPanel == null)
            {
                return;
            }

            Transform root = difficultySelectionPanel.transform;
            SetTextAtPath(
                root,
                "DifficultyTitle",
                GameStringIds.UiDifficultyTitle,
                "난이도 선택");

            Transform descriptionTransform = root.Find(
                "DifficultyDescription");
            TMP_Text description = descriptionTransform != null
                ? descriptionTransform.GetComponent<TMP_Text>()
                : null;
            if (description != null)
            {
                description.text = Format(
                    GameStringIds.UiDifficultyStageFormat,
                    "{0}\n{1}\n난이도는 이번 게임의 적 수와 " +
                    "적 레벨에 적용됩니다.",
                    difficultyStageName,
                    difficultyStageDescription);
            }

            string optionFallback = "{0}\n{1}";
            SetButtonText(
                difficultyEasyButton,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(
                        GameStringIds.DifficultyEasyName,
                        "쉬움"),
                    Text(
                        GameStringIds.DifficultyEasyDescription,
                        "적 수 75% · 적 레벨 80%")));
            SetButtonText(
                difficultyNormalButton,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(
                        GameStringIds.DifficultyNormalName,
                        "보통"),
                    Text(
                        GameStringIds.DifficultyNormalDescription,
                        "현재 밸런스")));
        }

        private void ApplyPauseStrings()
        {
            if (pauseDetailsPanel == null)
            {
                return;
            }

            Transform root = pauseDetailsPanel.transform;
            SetTextAtPath(
                root,
                "SettingsPage/PlayerStatsTitle",
                GameStringIds.PauseStatsTitle,
                "현재 스탯");
            SetTextAtPath(
                root,
                "SettingsPage/SkillsTitle",
                GameStringIds.PauseSkillsTitle,
                "획득한 스킬");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/AutoAttackToggle/Label",
                GameStringIds.UiAutoAttack,
                "자동 공격");
            SetTextAtPath(
                root,
                "ControlSettingsButton/Label",
                GameStringIds.UiControlButton,
                "조작");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlSettingsTitle",
                GameStringIds.UiControlTitle,
                "조작 패널 설정");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/JoystickSizeSlider/Label",
                GameStringIds.UiJoystickSize,
                "왼쪽 조이스틱 크기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/AttackSizeSlider/Label",
                GameStringIds.UiAttackSize,
                "오른쪽 공격 버튼 크기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeLabel",
                GameStringIds.UiControlMode,
                "조작 모드");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/Mode1Button/Label",
                GameStringIds.ControlModeOneName,
                "모드 1");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/Mode2Button/Label",
                GameStringIds.ControlModeTwoName,
                "모드 2");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlModeButtons/HiddenButton/Label",
                GameStringIds.ControlModeHiddenName,
                "숨기기");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlDefaultsButton/Label",
                GameStringIds.UiDefaults,
                "기본값");
            SetTextAtPath(
                root,
                "ControlSettingsPanel/ControlApplyButton/Label",
                GameStringIds.UiApply,
                "적용");
            RefreshControlSettingLabels();
        }

        private string Text(string stringId, string fallback)
        {
            return gameStrings != null
                ? gameStrings.Get(stringId, fallback)
                : fallback;
        }

        private string Format(
            string stringId,
            string fallbackTemplate,
            params object[] arguments)
        {
            if (gameStrings != null)
            {
                return gameStrings.Format(
                    stringId,
                    fallbackTemplate,
                    arguments);
            }

            try
            {
                return string.Format(
                    fallbackTemplate,
                    arguments ?? Array.Empty<object>());
            }
            catch (FormatException)
            {
                return fallbackTemplate;
            }
        }

        private void SetTextAtPath(
            Transform root,
            string path,
            string stringId,
            string fallback)
        {
            Transform child = root != null ? root.Find(path) : null;
            TMP_Text label = child != null
                ? child.GetComponent<TMP_Text>()
                : null;
            if (label != null)
            {
                label.text = Text(stringId, fallback);
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text label = button != null
                ? button.GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null)
            {
                label.text = value;
            }
        }

        private void SetButtonText(
            Button button,
            string stringId,
            string fallback)
        {
            SetButtonText(button, Text(stringId, fallback));
        }
    }
}
