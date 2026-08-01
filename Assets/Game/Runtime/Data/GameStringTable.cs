using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [Serializable]
    public sealed class GameStringEntry
    {
        [SerializeField] private string stringId;
        [SerializeField] private string text;

        public GameStringEntry(string stringId, string text)
        {
            this.stringId = stringId ?? string.Empty;
            this.text = text ?? string.Empty;
        }

        public string StringId => stringId;
        public string Text => text;
    }

    [CreateAssetMenu(
        fileName = "GameStringTable",
        menuName = "SimpleGame/Data/Game String Table")]
    public sealed class GameStringTable : ScriptableObject
    {
        [SerializeField] private List<GameStringEntry> entries = new();

        [NonSerialized]
        private Dictionary<string, string> lookup;

        public IReadOnlyList<GameStringEntry> Entries => entries;

        public void Configure(IEnumerable<GameStringEntry> values)
        {
            entries = values != null
                ? new List<GameStringEntry>(values)
                : new List<GameStringEntry>();
            lookup = null;
        }

        public bool TryGet(string stringId, out string text)
        {
            if (stringId != null &&
                GetLookup().TryGetValue(stringId, out text))
            {
                return true;
            }

            text = null;
            return false;
        }

        public string Get(string stringId, string fallback = null)
        {
            if (TryGet(stringId, out string text))
            {
                return text;
            }

            return fallback ?? $"[{stringId}]";
        }

        public string Format(
            string stringId,
            string fallbackTemplate,
            params object[] arguments)
        {
            bool hasStoredTemplate = TryGet(
                stringId,
                out string storedTemplate);
            string template = hasStoredTemplate
                ? storedTemplate
                : fallbackTemplate ?? $"[{stringId}]";

            if (TryFormat(template, arguments, out string formatted))
            {
                return formatted;
            }

            if (hasStoredTemplate &&
                fallbackTemplate != null &&
                !string.Equals(
                    template,
                    fallbackTemplate,
                    StringComparison.Ordinal) &&
                TryFormat(
                    fallbackTemplate,
                    arguments,
                    out formatted))
            {
                return formatted;
            }

            return template;
        }

        private Dictionary<string, string> GetLookup()
        {
            if (lookup != null)
            {
                return lookup;
            }

            lookup = new Dictionary<string, string>(
                StringComparer.Ordinal);
            if (entries == null)
            {
                return lookup;
            }

            foreach (GameStringEntry entry in entries)
            {
                if (entry == null ||
                    string.IsNullOrEmpty(entry.StringId))
                {
                    continue;
                }

                lookup[entry.StringId] = entry.Text;
            }

            return lookup;
        }

        private static bool TryFormat(
            string template,
            object[] arguments,
            out string formatted)
        {
            try
            {
                formatted = string.Format(
                    template,
                    arguments ?? Array.Empty<object>());
                return true;
            }
            catch (FormatException)
            {
                formatted = null;
                return false;
            }
        }
    }
}
