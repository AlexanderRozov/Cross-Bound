using System;

namespace CrossBound.Editor.CrosswordGenerator
{
    public readonly struct CrosswordPattern
    {
        public const char Wildcard = '_';
        public string Value { get; }
        public int Length => Value.Length;

        public CrosswordPattern(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("A pattern cannot be empty.", nameof(value));
            Value = value;
        }

        public bool Matches(string word)
        {
            if (word == null || word.Length != Length) return false;
            for (int index = 0; index < Length; index++)
                if (Value[index] != Wildcard && Value[index] != word[index]) return false;
            return true;
        }
    }
}
