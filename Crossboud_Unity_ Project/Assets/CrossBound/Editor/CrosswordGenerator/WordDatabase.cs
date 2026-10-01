using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossBound.Editor.CrosswordGenerator
{
    public sealed class WordDatabase
    {
        private readonly Dictionary<int, List<WordEntry>> _byLength = new Dictionary<int, List<WordEntry>>();
        private readonly Dictionary<string, List<WordEntry>> _byPositionAndLetter = new Dictionary<string, List<WordEntry>>();

        public IReadOnlyCollection<WordEntry> Entries { get; }

        public WordDatabase(IEnumerable<WordEntry> source, CrosswordLanguageDefinition language, int minimumLength)
        {
            var uniqueAnswers = new HashSet<string>(StringComparer.Ordinal);
            var accepted = new List<WordEntry>();
            foreach (WordEntry entry in source ?? Enumerable.Empty<WordEntry>())
            {
                if (entry == null) continue;
                if (!string.IsNullOrEmpty(entry.language) && !string.Equals(entry.language, language.Id, StringComparison.OrdinalIgnoreCase)) continue;
                entry.answer = language.Normalize(entry.answer);
                if (entry.answer.Length < minimumLength || !language.IsValidAnswer(entry.answer) || !uniqueAnswers.Add(entry.answer)) continue;
                accepted.Add(entry);
                Add(_byLength, entry.answer.Length, entry);
                for (int position = 0; position < entry.answer.Length; position++)
                    Add(_byPositionAndLetter, Key(entry.answer.Length, position, entry.answer[position]), entry);
            }
            Entries = accepted;
        }

        public IReadOnlyList<WordEntry> Find(CrosswordPattern pattern, ISet<string> excludedAnswers = null)
        {
            if (!_byLength.TryGetValue(pattern.Length, out List<WordEntry> sameLength)) return Array.Empty<WordEntry>();
            List<WordEntry> best = sameLength;
            for (int index = 0; index < pattern.Length; index++)
            {
                char letter = pattern.Value[index];
                if (letter == CrosswordPattern.Wildcard) continue;
                if (!_byPositionAndLetter.TryGetValue(Key(pattern.Length, index, letter), out List<WordEntry> indexed)) return Array.Empty<WordEntry>();
                if (indexed.Count < best.Count) best = indexed;
            }
            return best.Where(entry => (excludedAnswers == null || !excludedAnswers.Contains(entry.answer)) && pattern.Matches(entry.answer)).ToList();
        }

        private static string Key(int length, int position, char letter) => length + ":" + position + ":" + letter;
        private static void Add<TKey>(IDictionary<TKey, List<WordEntry>> index, TKey key, WordEntry entry)
        {
            if (!index.TryGetValue(key, out List<WordEntry> values)) index[key] = values = new List<WordEntry>();
            values.Add(entry);
        }
    }
}
