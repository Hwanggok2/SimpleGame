using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    public static class GameAudioIds
    {
        public const string UiTouch = "UiTouch";
        public const string PlayerAttack = "PlayerAttack";
        public const string EnemyDeathPrefix = "EnemyDeath/";

        public static string EnemyDeath(string enemyId)
        {
            return EnemyDeathPrefix + enemyId;
        }
    }

    [Serializable]
    public sealed class AudioClipDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        public string Id => id;
        public AudioClip Clip => clip;
        public float Volume => volume;

        public AudioClipDefinition(
            string configuredId,
            AudioClip configuredClip,
            float configuredVolume = 1f)
        {
            id = configuredId ?? string.Empty;
            clip = configuredClip;
            volume = Mathf.Clamp01(configuredVolume);
        }
    }

    [CreateAssetMenu(
        fileName = "AudioSettingsProfile",
        menuName = "SimpleGame/Audio/Audio Settings Profile")]
    public sealed class AudioSettingsProfile : ScriptableObject
    {
        [Tooltip("ID must match the Unity scene name that uses the music.")]
        [SerializeField]
        private List<AudioClipDefinition> backgroundMusic = new();
        [Tooltip("Audio clip and volume for each sound-effect ID.")]
        [SerializeField]
        private List<AudioClipDefinition> soundEffects = new();

        public IReadOnlyList<AudioClipDefinition> BackgroundMusic =>
            backgroundMusic;
        public IReadOnlyList<AudioClipDefinition> SoundEffects =>
            soundEffects;

        public bool TryGetBackgroundMusic(
            string sceneName,
            out AudioClipDefinition definition)
        {
            return TryGet(backgroundMusic, sceneName, out definition);
        }

        public bool TryGetSoundEffect(
            string soundEffectId,
            out AudioClipDefinition definition)
        {
            return TryGet(soundEffects, soundEffectId, out definition);
        }

        public void Configure(
            IEnumerable<AudioClipDefinition> configuredBackgroundMusic,
            IEnumerable<AudioClipDefinition> configuredSoundEffects)
        {
            backgroundMusic.Clear();
            soundEffects.Clear();
            if (configuredBackgroundMusic != null)
            {
                backgroundMusic.AddRange(configuredBackgroundMusic);
            }

            if (configuredSoundEffects != null)
            {
                soundEffects.AddRange(configuredSoundEffects);
            }
        }

        private static bool TryGet(
            IReadOnlyList<AudioClipDefinition> definitions,
            string id,
            out AudioClipDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                for (int index = 0; index < definitions.Count; index++)
                {
                    AudioClipDefinition candidate = definitions[index];
                    if (candidate != null &&
                        string.Equals(
                            candidate.Id,
                            id,
                            StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }
    }
}
