using UnityEngine;

namespace SimpleGame
{
    public enum InitialControlPresentation
    {
        ModeOne,
        ModeTwo,
        Hidden
    }

    [CreateAssetMenu(
        fileName = "ControlSettingsProfile",
        menuName = "SimpleGame/Data/Control Settings Profile")]
    public sealed class ControlSettingsProfile : ScriptableObject
    {
        [SerializeField]
        private InitialControlPresentation initialPresentation =
            InitialControlPresentation.ModeOne;
        [SerializeField] private string modeOneText = "모드 1";
        [SerializeField] private string modeTwoText = "모드 2";
        [SerializeField] private string hiddenText = "숨기기";

        public InitialControlPresentation InitialPresentation =>
            initialPresentation;
        public string ModeOneText => modeOneText;
        public string ModeTwoText => modeTwoText;
        public string HiddenText => hiddenText;

        public MobileControlSettings CreateDefaultSettings()
        {
            MobileControlSettings settings = MobileControlSettings.Default;
            settings.controlsEnabled =
                initialPresentation != InitialControlPresentation.Hidden;
            settings.controlMode = initialPresentation ==
                    InitialControlPresentation.ModeTwo
                ? MobileControlMode.AimCommand
                : MobileControlMode.DirectMoveAutoAim;
            return settings;
        }
    }
}
