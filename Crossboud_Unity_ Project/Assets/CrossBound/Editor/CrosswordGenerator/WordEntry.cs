using System;
using System.Collections.Generic;

namespace CrossBound.Editor.CrosswordGenerator
{
    [Serializable]
    public sealed class WordEntry
    {
        public string id;
        public string answer;
        public string question;
        public string questionKey;
        public string language;
    }

    [Serializable]
    public sealed class WordEntryCollection
    {
        public List<WordEntry> entries = new List<WordEntry>();
    }
}
