using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossBound.Editor.CrosswordGenerator
{
    public sealed class CrosswordAnalysis
    {
        public IReadOnlyList<WordEntry> OrderWords(WordDatabase database)
        {
            return database.Entries
                .OrderByDescending(entry => entry.answer.Length * CountPossibleIntersections(entry, database))
                .ThenByDescending(entry => entry.answer.Length)
                .ThenBy(entry => entry.answer, StringComparer.Ordinal)
                .ToList();
        }

        private static int CountPossibleIntersections(WordEntry entry, WordDatabase database)
        {
            int count = 0;
            foreach (char letter in entry.answer)
                count += database.Entries.Count(other => !ReferenceEquals(other, entry) && other.answer.IndexOf(letter) >= 0);
            return count;
        }
    }
}
