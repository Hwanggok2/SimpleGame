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
        private const string LegacyLobbyMusicObjectName = "LobbyBgm";

        private static GameManager instance;

        private AudioSource uiAudioSource;
        private AudioSource backgroundMusicSource;
        private AudioListener managedAudioListener;
        private AudioSettingsProfile audioSettings;

        public static GameManager Instance => EnsureInstance();
        public PrototypeGameSession BattleManager { get; private set; }
        public LobbyView LobbyManager { get; private set; }
        public AudioSource UiAudioSource => EnsureUiAudioSource();
        public AudioSource BackgroundMusicSource =>
            EnsureBackgroundMusicSource();
        public AudioSettingsProfile AudioSettings => audioSettings;

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

        public void ConfigureAudio(
            AudioSettingsProfile configuredAudioSettings)
        {
            if (configuredAudioSettings == null)
            {
                return;
            }

            audioSettings = configuredAudioSettings;
            PlayBackgroundMusic(SceneManager.GetActiveScene());
        }

        public bool PlaySoundEffect(
            string soundEffectId,
            AudioClip fallbackClip = null)
        {
            AudioClip clip = fallbackClip;
            float volume = 1f;
            if (audioSettings != null &&
                audioSettings.TryGetSoundEffect(
                    soundEffectId,
                    out AudioClipDefinition definition))
            {
                clip = definition.Clip;
                volume = definition.Volume;
            }

            if (clip == null || volume <= 0f)
            {
                return false;
            }

            EnsureUiAudioSource().PlayOneShot(clip, volume);
            return true;
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
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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

        private AudioSource EnsureBackgroundMusicSource()
        {
            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();
            }

            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.spatialBlend = 0f;
            return backgroundMusicSource;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener(scene);
            PlayBackgroundMusic(scene);
        }

        private void EnsureAudioListener(Scene scene)
        {
            bool hasSceneListener = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                AudioListener[] listeners =
                    root.GetComponentsInChildren<AudioListener>(true);
                foreach (AudioListener listener in listeners)
                {
                    if (listener != managedAudioListener &&
                        listener.isActiveAndEnabled)
                    {
                        hasSceneListener = true;
                        break;
                    }
                }

                if (hasSceneListener)
                {
                    break;
                }
            }

            if (hasSceneListener)
            {
                if (managedAudioListener != null)
                {
                    managedAudioListener.enabled = false;
                }

                return;
            }

            if (managedAudioListener == null)
            {
                managedAudioListener = gameObject.AddComponent<
                    AudioListener>();
            }

            managedAudioListener.enabled = true;
        }

        private void PlayBackgroundMusic(Scene scene)
        {
            if (audioSettings == null || !scene.IsValid())
            {
                return;
            }

            StopLegacyLobbyMusic(scene);
            if (!audioSettings.TryGetBackgroundMusic(
                    scene.name,
                    out AudioClipDefinition definition) ||
                definition.Clip == null ||
                definition.Volume <= 0f)
            {
                if (backgroundMusicSource != null)
                {
                    backgroundMusicSource.Stop();
                    backgroundMusicSource.clip = null;
                }

                return;
            }

            AudioSource source = EnsureBackgroundMusicSource();
            source.volume = definition.Volume;
            if (source.clip != definition.Clip)
            {
                source.Stop();
                source.clip = definition.Clip;
            }

            if (!source.isPlaying)
            {
                source.Play();
            }
        }

        private void StopLegacyLobbyMusic(Scene scene)
        {
            if (scene.name != LobbySceneName)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform legacyTransform =
                    root.transform.Find(LegacyLobbyMusicObjectName);
                AudioSource legacySource = legacyTransform != null
                    ? legacyTransform.GetComponent<AudioSource>()
                    : null;
                if (legacySource != null &&
                    legacySource != backgroundMusicSource)
                {
                    legacySource.Stop();
                }
            }
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
