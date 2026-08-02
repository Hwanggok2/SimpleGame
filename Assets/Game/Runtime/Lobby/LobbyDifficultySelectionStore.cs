using System;
using UnityEngine;

namespace SimpleGame
{
    public static class LobbyDifficultySelectionStore
    {
        public const string PreferencesKey =
            "SimpleGame.Lobby.LastDifficulty.v1";

        public static bool TryLoad(out LobbyDifficultyId difficultyId)
        {
            difficultyId = default;
            if (!PlayerPrefs.HasKey(PreferencesKey))
            {
                return false;
            }

            string savedValue = PlayerPrefs.GetString(
                PreferencesKey,
                string.Empty);
            return Enum.TryParse(savedValue, false, out difficultyId) &&
                Enum.IsDefined(typeof(LobbyDifficultyId), difficultyId) &&
                difficultyId != LobbyDifficultyId.Hard;
        }

        public static void Save(LobbyDifficultyId difficultyId)
        {
            if (difficultyId == LobbyDifficultyId.Hard)
            {
                return;
            }

            PlayerPrefs.SetString(
                PreferencesKey,
                difficultyId.ToString());
            PlayerPrefs.Save();
        }
    }
}
