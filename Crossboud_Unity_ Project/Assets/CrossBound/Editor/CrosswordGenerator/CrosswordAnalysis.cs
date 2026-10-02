using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossBound.Editor.CrosswordGenerator
{
    public sealed class CrosswordAnalysis
    {
        /// <summary>
        /// Orders words so the generator tries the most "connectable" words first:
        /// long words whose letters appear frequently in the whole pool.
        /// Letter frequencies are indexed once (O(total letters)) instead of the
        /// original O(pool²) pairwise scan, which was unusable with the 155k-word base.
        /// </summary>
        public IReadOnlyList<WordEntry> OrderWords(WordDatabase database)
        {
            var letterFrequency = new Dictionary<char, int>();
            foreach (WordEntry entry in database.Entries)
            {
                foreach (char letter in entry.answer.Distinct())
                {
                    letterFrequency.TryGetValue(letter, out int count);
                    letterFrequency[letter] = count + 1;
                }
            }

            return database.Entries
                .OrderByDescending(entry => entry.answer.Length * Connectivity(entry, letterFrequency))
                .ThenByDescending(entry => entry.answer.Length)
                .ThenBy(entry => entry.answer, StringComparer.Ordinal)
                .ToList();
        }

        private static int Connectivity(WordEntry entry, IReadOnlyDictionary<char, int> letterFrequency)
        {
            int sum = 0;
            foreach (char letter in entry.answer.Distinct())
                if (letterFrequency.TryGetValue(letter, out int count))
                    sum += count;
            return sum;
        }
    }
}
