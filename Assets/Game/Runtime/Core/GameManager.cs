using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleGame
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public const string RootName = "Manager";
        public const string LobbySceneName = "Lobby";
        public const string BattleSceneName = "Battle";

        private static GameManager instance;

        private AudioSource uiAudioSource;

        public static GameManager Instance => EnsureInstance();
        public PrototypeGameSession BattleManager { get; private set; }
        public LobbyView LobbyManager { get; private set; }
        public AudioSource UiAudioSource => EnsureUiAudioSource();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void RegisterBattle(
            PrototypeGameSession battleManager)
        {
            if (Application.isPlaying && battleManager != null)
            {
                Instance.BattleManager = battleManager;
            }
        }

        public static void UnregisterBattle(
            PrototypeGameSession battleManager)
        {
            if (instance != null &&
                instance.BattleManager == battleManager)
            {
                instance.BattleManager = null;
            }
        }

        public static void RegisterLobby(LobbyView lobbyManager)
        {
            if (Application.isPlaying && lobbyManager != null)
            {
                Instance.LobbyManager = lobbyManager;
            }
        }

        public static void UnregisterLobby(LobbyView lobbyManager)
        {
            if (instance != null && instance.LobbyManager == lobbyManager)
            {
                instance.LobbyManager = null;
            }
        }

        public void LoadBattle()
        {
            LoadScene(BattleSceneName);
        }

        public void RestartBattle()
        {
            string sceneName = BattleManager != null &&
                               BattleManager.gameObject.scene.IsValid()
                ? BattleManager.gameObject.scene.name
                : SceneManager.GetActiveScene().name;
            LoadScene(sceneName);
        }

        public void LoadLobby()
        {
            LoadScene(LobbySceneName);
        }

        private static GameManager EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            var root = new GameObject(RootName);
            instance = root.AddComponent<GameManager>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameObject.name = RootName;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureUiAudioSource();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private AudioSource EnsureUiAudioSource()
        {
            uiAudioSource = uiAudioSource != null
                ? uiAudioSource
                : GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }

            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;
            return uiAudioSource;
        }

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
