using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleGame
{
    public sealed class LobbySettingsView : MonoBehaviour
    {
        [SerializeField] private Button outsideButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button controlSettingsButton;
        [SerializeField] private TMP_Text settingsTitleLabel;
        [SerializeField] private TMP_Text controlSettingsLabel;
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private GameObject controlSettingsPanel;
        [SerializeField]
        private LobbyControlSettingsView controlSettingsView;

        private bool initialized;
        private bool editingControlSettings;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsEditingControlSettings => editingControlSettings;

        public void Configure(
            Button configuredOutsideButton,
            Button configuredCloseButton,
            Button configuredControlSettingsButton,
            TMP_Text configuredSettingsTitleLabel,
            TMP_Text configuredControlSettingsLabel,
            GameObject configuredSettingsPage,
            GameObject configuredControlSettingsPanel,
            LobbyControlSettingsView configuredControlSettingsView)
        {
            outsideButton = configuredOutsideButton;
            closeButton = configuredCloseButton;
            controlSettingsButton = configuredControlSettingsButton;
            settingsTitleLabel = configuredSettingsTitleLabel;
            controlSettingsLabel = configuredControlSettingsLabel;
            settingsPage = configuredSettingsPage;
            controlSettingsPanel = configuredControlSettingsPanel;
            controlSettingsView = configuredControlSettingsView;
        }

        public void Initialize(GameStringTable strings)
        {
            Initialize(strings, null);
        }

        public void Initialize(
            GameStringTable strings,
            ControlSettingsProfile controlSettingsProfile)
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Bind(outsideButton, Close);
            Bind(closeButton, Close);
            Bind(controlSettingsButton, ToggleControlSettingsPage);
            if (settingsTitleLabel != null)
            {
                settingsTitleLabel.text = GetText(
                    strings,
                    GameStringIds.UiSettingsButton,
                    "설정");
            }

            if (controlSettingsLabel != null)
            {
                controlSettingsLabel.text = GetText(
                    strings,
                    GameStringIds.UiControlButton,
                    "조작");
            }

            controlSettingsView?.Initialize(
                strings,
                controlSettingsProfile);
            if (controlSettingsView != null)
            {
                controlSettingsView.Applied += CloseControlSettingsPage;
            }

            SetControlSettingsPageVisible(false);
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            controlSettingsView?.BeginEditing();
            SetControlSettingsPageVisible(false);
        }

        public void Close()
        {
            controlSettingsView?.DiscardDraft();
            SetControlSettingsPageVisible(false);
            gameObject.SetActive(false);
        }

        private void ToggleControlSettingsPage()
        {
            SetControlSettingsPageVisible(!editingControlSettings);
        }

        private void CloseControlSettingsPage()
        {
            SetControlSettingsPageVisible(false);
        }

        private void SetControlSettingsPageVisible(bool visible)
        {
            editingControlSettings = visible;
            // SetActive(settingsPage, !visible);
            SetActive(controlSettingsPanel, visible);
        }

        private static void Bind(
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

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static string GetText(
            GameStringTable strings,
            string stringId,
            string fallback)
        {
            return strings != null
                ? strings.Get(stringId, fallback)
                : fallback;
        }
    }
}
