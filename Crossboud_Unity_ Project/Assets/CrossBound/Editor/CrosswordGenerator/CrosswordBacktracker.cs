using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossBound.Editor.CrosswordGenerator
{
    public enum CrosswordDirection { Across, Down }

    public sealed class GeneratedCrosswordEntry
    {
        public string id;
        public int number;
        public CrosswordDirection direction;
        public int startX;
        public int startY;
        public string answer;
        public string question;
        public string questionKey;
    }

    public sealed class GeneratedCrossword
    {
        public string id;
        public string language;
        public int width;
        public int height;
        public List<GeneratedCrosswordEntry> entries = new List<GeneratedCrosswordEntry>();
    }

    internal readonly struct Placement
    {
        public readonly WordEntry Word;
        public readonly int X;
        public readonly int Y;
        public readonly CrosswordDirection Direction;
        public Placement(WordEntry word, int x, int y, CrosswordDirection direction) { Word = word; X = x; Y = y; Direction = direction; }
    }

    public sealed class CrosswordBacktracker
    {
        private readonly CrosswordGeneratorSettings _settings;
        private readonly Random _random;
        private readonly List<Placement> _placements = new List<Placement>();
        private readonly Dictionary<(int x, int y), char> _cells = new Dictionary<(int x, int y), char>();
        private int _attempts;

        public CrosswordBacktracker(CrosswordGeneratorSettings settings)
        {
            _settings = settings;
            _settings.Validate();
            _random = new Random(settings.randomSeed == 0 ? Environment.TickCount : settings.randomSeed);
        }

        public GeneratedCrossword Generate(string id, string language, IReadOnlyList<WordEntry> orderedWords)
        {
            if (orderedWords == null || orderedWords.Count == 0) return null;
            // Trying every word as the seed multiplies the cost by the pool size;
            // the analysis ordering already puts the most promising words first.
            foreach (WordEntry seed in orderedWords.Where(word => word.answer.Length <= _settings.width).Take(25))
            {
                Clear();
                int x = (_settings.width - seed.answer.Length) / 2;
                if (TryPlace(new Placement(seed, x, _settings.height / 2, CrosswordDirection.Across)) && Search(orderedWords))
                    return ToResult(id, language);
            }
            return null;
        }

        private bool Search(IReadOnlyList<WordEntry> words)
        {
            if (_placements.Count >= _settings.wordCount) return true;
            if (_attempts++ >= _settings.maximumAttempts) return false;

            var used = new HashSet<string>(_placements.Select(placement => placement.Word.answer), StringComparer.Ordinal);
            List<Placement> candidates = BuildCandidates(words, used);
            Shuffle(candidates);
            foreach (Placement candidate in candidates)
            {
                if (!TryPlace(candidate)) continue;
                if (Search(words)) return true;
                Remove(candidate);
            }
            return false;
        }

        private List<Placement> BuildCandidates(IReadOnlyList<WordEntry> words, ISet<string> used)
        {
            var candidates = new List<Placement>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Placement placed in _placements)
            {
                for (int placedIndex = 0; placedIndex < placed.Word.answer.Length; placedIndex++)
                {
                    char intersectionLetter = placed.Word.answer[placedIndex];
                    int cellX = placed.X + (placed.Direction == CrosswordDirection.Across ? placedIndex : 0);
                    int cellY = placed.Y + (placed.Direction == CrosswordDirection.Down ? placedIndex : 0);
                    foreach (WordEntry word in words)
                    {
                        if (used.Contains(word.answer)) continue;
                        for (int index = 0; index < word.answer.Length; index++)
                        {
                            if (word.answer[index] != intersectionLetter) continue;
                            CrosswordDirection direction = placed.Direction == CrosswordDirection.Across ? CrosswordDirection.Down : CrosswordDirection.Across;
                            int x = cellX - (direction == CrosswordDirection.Across ? index : 0);
                            int y = cellY - (direction == CrosswordDirection.Down ? index : 0);
                            string key = word.answer + ":" + x + ":" + y + ":" + direction;
                            if (seen.Add(key)) candidates.Add(new Placement(word, x, y, direction));
                        }
                    }
                }
            }
            return candidates;
        }

        private bool TryPlace(Placement placement)
        {
            if (!IsValid(placement)) return false;
            _placements.Add(placement);
            for (int index = 0; index < placement.Word.answer.Length; index++)
                _cells[(placement.X + (placement.Direction == CrosswordDirection.Across ? index : 0), placement.Y + (placement.Direction == CrosswordDirection.Down ? index : 0))] = placement.Word.answer[index];
            return true;
        }

        private bool IsValid(Placement placement)
        {
            int dx = placement.Direction == CrosswordDirection.Across ? 1 : 0;
            int dy = placement.Direction == CrosswordDirection.Down ? 1 : 0;
            if (placement.X < 0 || placement.Y < 0 || placement.X + dx * (placement.Word.answer.Length - 1) >= _settings.width || placement.Y + dy * (placement.Word.answer.Length - 1) >= _settings.height) return false;
            if (_cells.ContainsKey((placement.X - dx, placement.Y - dy)) || _cells.ContainsKey((placement.X + dx * placement.Word.answer.Length, placement.Y + dy * placement.Word.answer.Length))) return false;

            int intersections = 0;
            for (int index = 0; index < placement.Word.answer.Length; index++)
            {
                int x = placement.X + dx * index;
                int y = placement.Y + dy * index;
                if (_cells.TryGetValue((x, y), out char existing))
                {
                    if (existing != placement.Word.answer[index]) return false;
                    intersections++;
                    continue;
                }
                if (placement.Direction == CrosswordDirection.Across && (_cells.ContainsKey((x, y - 1)) || _cells.ContainsKey((x, y + 1)))) return false;
                if (placement.Direction == CrosswordDirection.Down && (_cells.ContainsKey((x - 1, y)) || _cells.ContainsKey((x + 1, y)))) return false;
            }
            return _placements.Count == 0 || intersections > 0;
        }

        private void Remove(Placement placement)
        {
            _placements.RemoveAt(_placements.Count - 1);
            _cells.Clear();
            foreach (Placement existing in _placements)
                for (int index = 0; index < existing.Word.answer.Length; index++)
                    _cells[(existing.X + (existing.Direction == CrosswordDirection.Across ? index : 0), existing.Y + (existing.Direction == CrosswordDirection.Down ? index : 0))] = existing.Word.answer[index];
        }

        private GeneratedCrossword ToResult(string id, string language)
        {
            var result = new GeneratedCrossword { id = id, language = language, width = _settings.width, height = _settings.height };
            var ordered = _placements.OrderBy(item => item.Y).ThenBy(item => item.X).ThenBy(item => item.Direction).ToList();

            // Standard crossword numbering: words sharing a start cell share a number.
            var numbers = new Dictionary<(int x, int y), int>();
            int next = 1;
            foreach (Placement placement in ordered)
            {
                var key = (placement.X, placement.Y);
                if (!numbers.ContainsKey(key)) numbers[key] = next++;
            }

            foreach (Placement placement in ordered)
                result.entries.Add(new GeneratedCrosswordEntry { id = placement.Word.id, number = numbers[(placement.X, placement.Y)], direction = placement.Direction, startX = placement.X, startY = placement.Y, answer = placement.Word.answer, question = placement.Word.question, questionKey = placement.Word.questionKey });
            return result;
        }

        private void Clear() { _placements.Clear(); _cells.Clear(); _attempts = 0; }
        private void Shuffle<T>(IList<T> values) { for (int index = values.Count - 1; index > 0; index--) { int swap = _random.Next(index + 1); (values[index], values[swap]) = (values[swap], values[index]); } }
    }
}
