using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CrossBound.Editor.CrosswordGenerator
{
    /// <summary>
    /// Loads the bundled cross.txt word base (windows-1251, "СЛОВО - определение" per line)
    /// and turns it into generator-ready word entries. Decoding is done with a built-in
    /// cp1251 table so no encoding provider registration is required.
    /// </summary>
    public static class CrosswordDictionarySource
    {
        public const string DefaultAssetPath = "Assets/CrossBound/words/cross.txt";

        public sealed class LoadResult
        {
            public List<WordEntry> Entries = new List<WordEntry>();
            public int TotalLines;
            public int SkippedLines;
            public int SkippedByLength;
            public int Duplicates;
        }

        public static LoadResult LoadRussian(string assetPath, int minimumLength, int maximumLength)
        {
            var result = new LoadResult();
            string fsPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fsPath))
            {
                UnityEngine.Debug.LogError($"[CrossBound][Generator] Dictionary file not found: {assetPath}");
                return result;
            }

            // cp1251 without Encoding.GetEncoding: bytes 0x00-0x7F are ASCII,
            // Cyrillic lives in 0xC0-0xFF (direct А-я), plus Ё/ё at 0xA8/0xB8.
            string text = DecodeCp1251(File.ReadAllBytes(fsPath));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int id = 0;

            foreach (string rawLine in text.Split('\n'))
            {
                result.TotalLines++;
                string line = rawLine.TrimEnd('\r');
                int separator = line.IndexOf(" - ", StringComparison.Ordinal);
                if (separator <= 0) { result.SkippedLines++; continue; }

                string word = line.Substring(0, separator).Trim().ToUpperInvariant();
                string definition = line.Substring(separator + 3).Trim();
                if (word.Length == 0 || definition.Length == 0) { result.SkippedLines++; continue; }
                if (word.Length < minimumLength || word.Length > maximumLength) { result.SkippedByLength++; continue; }
                if (!CrosswordLanguageDefinition.Russian.IsValidAnswer(word)) { result.SkippedLines++; continue; }
                if (!seen.Add(word)) { result.Duplicates++; continue; }

                result.Entries.Add(new WordEntry
                {
                    id = "w" + id++,
                    answer = word,
                    question = definition,
                    questionKey = word,
                    language = CrosswordLanguageDefinition.Russian.Id,
                });
            }
            return result;
        }

        /// <summary>Takes a deterministic random sample of the pool (Fisher-Yates with a fixed seed).</summary>
        public static List<WordEntry> Sample(IReadOnlyList<WordEntry> entries, int count, int randomSeed)
        {
            var pool = new List<WordEntry>(entries);
            var random = new Random(randomSeed == 0 ? Environment.TickCount : randomSeed);
            int take = Math.Min(count, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int swap = random.Next(i, pool.Count);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }
            pool.RemoveRange(take, pool.Count - take);
            return pool;
        }

        public static string DecodeCp1251(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length);
            foreach (byte b in bytes)
            {
                if (b < 0x80) { builder.Append((char)b); continue; }
                if (b >= 0xC0) { builder.Append((char)(0x0410 + (b - 0xC0))); continue; }
                if (b == 0xA8) { builder.Append('Ё'); continue; }
                if (b == 0xB8) { builder.Append('ё'); continue; }
                switch (b) // common punctuation in definitions
                {
                    case 0x96: case 0x97: builder.Append('—'); break; // en/em dash
                    case 0x91: case 0x92: builder.Append('\''); break;
                    case 0x93: case 0x94: builder.Append('"'); break;
                    case 0x85: builder.Append('…'); break;
                    default: builder.Append(' '); break;
                }
            }
            return builder.ToString();
        }
    }
}
