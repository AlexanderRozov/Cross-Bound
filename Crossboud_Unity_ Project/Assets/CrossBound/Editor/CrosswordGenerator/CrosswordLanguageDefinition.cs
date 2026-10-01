using System;
using System.Collections.Generic;

namespace CrossBound.Editor.CrosswordGenerator
{
    public sealed class CrosswordLanguageDefinition
    {
        public static readonly CrosswordLanguageDefinition English = new CrosswordLanguageDefinition("en", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        public static readonly CrosswordLanguageDefinition Russian = new CrosswordLanguageDefinition("ru", "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ");

        private readonly HashSet<char> _alphabet;

        public string Id { get; }
        public string Alphabet { get; }

        public CrosswordLanguageDefinition(string id, string alphabet)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Alphabet = alphabet ?? throw new ArgumentNullException(nameof(alphabet));
            _alphabet = new HashSet<char>(alphabet);
        }

        public bool IsValidAnswer(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            foreach (char letter in value)
                if (!_alphabet.Contains(letter)) return false;
            return true;
        }

        public string Normalize(string value) => value == null ? string.Empty : value.Trim().ToUpperInvariant();

        public static CrosswordLanguageDefinition FromId(string id)
        {
            if (string.Equals(id, Russian.Id, StringComparison.OrdinalIgnoreCase)) return Russian;
            if (string.Equals(id, English.Id, StringComparison.OrdinalIgnoreCase)) return English;
            throw new ArgumentOutOfRangeException(nameof(id), id, "Only en and ru dictionaries are supported.");
        }
    }
}
