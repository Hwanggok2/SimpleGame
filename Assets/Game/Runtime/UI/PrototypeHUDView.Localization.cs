using System;
using TMPro;
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
            if (controlSettingsButtonLabel != null)
            {
                controlSettingsButtonLabel.text = Text(
                    GameStringIds.UiControlButton,
                    "조작");
            }
            ApplyControlModePresentation(controlSettings.controlMode);
        }

        private void ApplyDifficultyStrings()
        {
            if (difficultySelectionPanel == null)
            {
                return;
            }

            difficultySelectionPanel.SetTitle(Text(
                GameStringIds.UiDifficultyTitle,
                "난이도 선택"));
            difficultySelectionPanel.SetDescription(Format(
                GameStringIds.UiDifficultyStageFormat,
                "{0}\n{1}\n난이도는 이번 게임에서만 유지되며 " +
                "적 레벨에 적용됩니다.",
                difficultyStageName,
                difficultyStageDescription));

            string optionFallback = "{0}\n{1}";
            difficultySelectionPanel.SetButtonText(
                GameDifficulty.Easy,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(GameStringIds.DifficultyEasyName, "쉬움"),
                    Text(
                        GameStringIds.DifficultyEasyDescription,
                        "체력 2배 · 자동 공격 1.5배")));
            difficultySelectionPanel.SetButtonText(
                GameDifficulty.Normal,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(GameStringIds.DifficultyNormalName, "보통"),
                    Text(
                        GameStringIds.DifficultyNormalDescription,
                        "기존 쉬움 밸런스")));
            difficultySelectionPanel.SetButtonText(
                GameDifficulty.Hard,
                Format(
                    GameStringIds.UiDifficultyOptionFormat,
                    optionFallback,
                    Text(GameStringIds.DifficultyHardName, "어려움"),
                    Text(
                        GameStringIds.DifficultyHardDescription,
                        "기존 보통 밸런스")));
        }

        private void ApplyPauseStrings()
        {
            if (pauseDetailsPanel == null)
            {
                return;
            }

            pauseDetailsPanel.SetText(
                PauseDetailsTextId.PlayerStatsTitle,
                Text(GameStringIds.PauseStatsTitle, "현재 스탯"));
            pauseDetailsPanel.SetText(
                PauseDetailsTextId.SkillsTitle,
                Text(GameStringIds.PauseSkillsTitle, "획득한 스킬"));
            pauseDetailsPanel.SetText(
                PauseDetailsTextId.RetryButton,
                Text(GameStringIds.UiRetryButton, "다시하기"));
            pauseDetailsPanel.SetText(
                PauseDetailsTextId.ReturnToLobbyButton,
                Text(GameStringIds.UiReturnLobbyButton, "로비로 이동"));

            controlSettingsPanel?.SetText(
                ControlSettingsTextId.Title,
                Text(GameStringIds.UiControlTitle, "조작 패널 설정"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.AutoAttack,
                Text(GameStringIds.UiAutoAttack, "자동 공격"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.ControlMode,
                Text(GameStringIds.UiControlMode, "조작 모드"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.JoystickSize,
                Text(GameStringIds.UiJoystickSize, "왼쪽 조이스틱 크기"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.AttackSize,
                Text(GameStringIds.UiAttackSize, "오른쪽 공격 버튼 크기"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.ModeOne,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeOneText
                    : Text(GameStringIds.ControlModeOneName, "모드 1"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.ModeTwo,
                controlSettingsProfile != null
                    ? controlSettingsProfile.ModeTwoText
                    : Text(GameStringIds.ControlModeTwoName, "모드 2"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.Hidden,
                controlSettingsProfile != null
                    ? controlSettingsProfile.HiddenText
                    : Text(GameStringIds.ControlModeHiddenName, "숨기기"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.Defaults,
                Text(GameStringIds.UiDefaults, "기본값"));
            controlSettingsPanel?.SetText(
                ControlSettingsTextId.Apply,
                Text(GameStringIds.UiApply, "적용"));
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
