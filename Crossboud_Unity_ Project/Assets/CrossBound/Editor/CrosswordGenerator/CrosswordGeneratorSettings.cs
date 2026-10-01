using System;
using UnityEngine;

namespace CrossBound.Editor.CrosswordGenerator
{
    [Serializable]
    public sealed class CrosswordGeneratorSettings
    {
        [Min(5)] public int width = 15;
        [Min(5)] public int height = 15;
        [Min(2)] public int wordCount = 10;
        [Min(1)] public int minimumWordLength = 3;
        [Min(1)] public int maximumAttempts = 15000;
        public int randomSeed;

        public void Validate()
        {
            width = Mathf.Max(5, width);
            height = Mathf.Max(5, height);
            wordCount = Mathf.Max(2, wordCount);
            minimumWordLength = Mathf.Max(1, minimumWordLength);
            maximumAttempts = Mathf.Max(1, maximumAttempts);
        }
    }
}
