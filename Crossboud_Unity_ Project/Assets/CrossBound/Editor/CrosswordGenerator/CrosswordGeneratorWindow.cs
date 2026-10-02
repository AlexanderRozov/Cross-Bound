using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrossBound.Editor.CrosswordGenerator
{
    /// <summary>
    /// Editor window: generate crossword JSON either from a curated dictionary
    /// (WordEntryCollection JSON) or directly from the bundled cross.txt base
    /// (155k Russian words with definitions, windows-1251).
    /// Output is validated by the runtime <see cref="CrosswordDataValidator"/> before saving.
    /// </summary>
    public sealed class CrosswordGeneratorWindow : EditorWindow
    {
        private const string AddressableOutputDirectory = "Assets/CrossBound/Content/Crosswords";
        private const string ResourcesOutputDirectory = "Assets/CrossBound/Resources";

        private enum SourceMode { DictionaryJson, CrossTxt }
        private enum OutputMode { AddressableContent, ResourcesPlayable }

        private SourceMode _sourceMode = SourceMode.CrossTxt;
        private OutputMode _outputMode = OutputMode.ResourcesPlayable;

        private TextAsset _dictionary;
        private string _crosswordId = "crossword_002";
        private string _languageId = "ru";
        private CrosswordGeneratorSettings _settings = new CrosswordGeneratorSettings();

        // cross.txt pool settings
        private string _dictionaryPath = CrosswordDictionarySource.DefaultAssetPath;
        private int _minWordLength = 3;
        private int _maxWordLength = 11;
        private int _poolSize = 600;
        private int _poolSeed = 42;
        private CrosswordDictionarySource.LoadResult _loadedBase;
        private string _poolInfo = "Base not loaded yet.";

        [MenuItem("CrossBound/Crossword Generator")]
        private static void Open() => GetWindow<CrosswordGeneratorWindow>("Crossword Generator");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Crossword generation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates validated, playable crossword JSON. Not usable in player builds.", MessageType.Info);

            _sourceMode = (SourceMode)EditorGUILayout.EnumPopup("Source", _sourceMode);
            EditorGUI.indentLevel++;

            if (_sourceMode == SourceMode.DictionaryJson)
            {
                _dictionary = (TextAsset)EditorGUILayout.ObjectField("Dictionary JSON", _dictionary, typeof(TextAsset), false);
                _languageId = EditorGUILayout.Popup("Language", _languageId == "ru" ? 1 : 0, new[] { "en", "ru" }) == 1 ? "ru" : "en";
                _settings.minimumWordLength = EditorGUILayout.IntField("Minimum word length", _settings.minimumWordLength);
            }
            else
            {
                _languageId = "ru";
                EditorGUILayout.LabelField("Dictionary", _dictionaryPath);
                _minWordLength = EditorGUILayout.IntField("Min word length", _minWordLength);
                _maxWordLength = EditorGUILayout.IntField("Max word length", _maxWordLength);
                _poolSize = EditorGUILayout.IntField("Pool size", _poolSize);
                _poolSeed = EditorGUILayout.IntField("Pool seed", _poolSeed);
                if (GUILayout.Button("Load / reload base"))
                    ReloadBase();
                EditorGUILayout.HelpBox(_poolInfo, MessageType.None);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(8);
            _crosswordId = EditorGUILayout.TextField("Crossword ID", _crosswordId);
            _settings.width = EditorGUILayout.IntField("Grid width", _settings.width);
            _settings.height = EditorGUILayout.IntField("Grid height", _settings.height);
            _settings.wordCount = EditorGUILayout.IntField("Word count", _settings.wordCount);
            _settings.maximumAttempts = EditorGUILayout.IntField("Maximum attempts", _settings.maximumAttempts);
            _settings.randomSeed = EditorGUILayout.IntField("Generation seed (0 = random)", _settings.randomSeed);

            _outputMode = (OutputMode)EditorGUILayout.EnumPopup("Output", _outputMode);
            EditorGUILayout.HelpBox(
                _outputMode == OutputMode.ResourcesPlayable
                    ? $"Writes straight to {ResourcesOutputDirectory}/{_crosswordId}.json — the runtime can load it immediately via Resources."
                    : $"Writes to {AddressableOutputDirectory} — mark the file addressable with the id \"{_crosswordId}\" to load it at runtime.",
                MessageType.None);

            bool canGenerate = !string.IsNullOrWhiteSpace(_crosswordId)
                && (_sourceMode == SourceMode.DictionaryJson ? _dictionary != null : _loadedBase != null && _loadedBase.Entries.Count > 0);
            using (new EditorGUI.DisabledScope(!canGenerate))
                if (GUILayout.Button("Generate JSON")) Generate();
        }

        private void ReloadBase()
        {
            _loadedBase = CrosswordDictionarySource.LoadRussian(_dictionaryPath, _minWordLength, _maxWordLength);
            _poolInfo = $"{_loadedBase.Entries.Count:N0} unique words ready " +
                        $"(skipped: {_loadedBase.SkippedLines:N0} malformed, {_loadedBase.SkippedByLength:N0} by length, {_loadedBase.Duplicates:N0} duplicates). " +
                        "Press Generate to sample a pool and build a crossword.";
            if (_loadedBase.Entries.Count == 0)
                Debug.LogError("[CrossBound][Generator] No words loaded from cross.txt. Check the file path and length filters.");
        }

        private List<WordEntry> BuildPool()
        {
            if (_sourceMode == SourceMode.DictionaryJson)
            {
                WordEntryCollection collection = JsonUtility.FromJson<WordEntryCollection>(_dictionary.text);
                return collection?.entries ?? new List<WordEntry>();
            }

            if (_loadedBase == null) ReloadBase();
            if (_loadedBase == null || _loadedBase.Entries.Count == 0) return new List<WordEntry>();
            List<WordEntry> sample = CrosswordDictionarySource.Sample(_loadedBase.Entries, _poolSize, _poolSeed);
            Debug.Log($"[CrossBound][Generator] Sampled {sample.Count} words from {_loadedBase.Entries.Count:N0} (seed {_poolSeed}).");
            return sample;
        }

        private void Generate()
        {
            CrosswordLanguageDefinition language;
            try { language = CrosswordLanguageDefinition.FromId(_languageId); }
            catch (System.Exception exception) { Debug.LogError(exception.Message); return; }

            List<WordEntry> pool = BuildPool();
            if (pool.Count == 0) { Debug.LogError("[CrossBound][Generator] Word pool is empty."); return; }

            var database = new WordDatabase(pool, language, _sourceMode == SourceMode.DictionaryJson ? _settings.minimumWordLength : _minWordLength);
            var ordered = new CrosswordAnalysis().OrderWords(database);
            var crossword = new CrosswordBacktracker(_settings).Generate(_crosswordId, language.Id, ordered);
            if (crossword == null)
            {
                Debug.LogError("[CrossBound][Generator] Generation failed. Lower the word count, raise the attempt limit, or increase the pool size/seed variety.");
                return;
            }

            // Never save a puzzle the runtime validator would reject.
            CrosswordData playable = ConvertToPlayable(crossword);
            List<string> errors = CrosswordDataValidator.Validate(playable);
            if (errors.Count > 0)
            {
                Debug.LogError("[CrossBound][Generator] Generated crossword failed validation and was NOT saved:\n - " + string.Join("\n - ", errors));
                return;
            }

            string directory = _outputMode == OutputMode.ResourcesPlayable ? ResourcesOutputDirectory : AddressableOutputDirectory;
            Directory.CreateDirectory(directory);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, _crosswordId + ".json"));
            File.WriteAllText(assetPath, ToJson(playable), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Debug.Log($"[CrossBound][Generator] Generated {crossword.entries.Count}-word crossword at {assetPath}. Validation passed.");
        }

        private static CrosswordData ConvertToPlayable(GeneratedCrossword generated)
        {
            var data = new CrosswordData { gridWidth = generated.width, gridHeight = generated.height };
            foreach (GeneratedCrosswordEntry entry in generated.entries)
            {
                data.questions.Add(new CrosswordQuestion
                {
                    id = entry.id,
                    number = entry.number,
                    direction = (int)entry.direction,
                    question = entry.question,
                    answer = entry.answer,
                    startX = entry.startX,
                    startY = entry.startY,
                    isHorizontal = entry.direction == CrosswordDirection.Across,
                });
            }
            return data;
        }

        // Hand-rolled JSON so the output contains only meaningful fields
        // (JsonUtility would also emit the empty `entries` compatibility list).
        private static string ToJson(CrosswordData data)
        {
            var builder = new StringBuilder(4096);
            builder.Append("{\n  \"questions\": [\n");
            for (int i = 0; i < data.questions.Count; i++)
            {
                CrosswordQuestion q = data.questions[i];
                builder.Append("    { ")
                    .Append("\"id\": \"").Append(JsonEscape(q.id)).Append("\", ")
                    .Append("\"number\": ").Append(q.number).Append(", ")
                    .Append("\"direction\": ").Append((int)q.direction).Append(", ")
                    .Append("\"question\": \"").Append(JsonEscape(q.question)).Append("\", ")
                    .Append("\"answer\": \"").Append(JsonEscape(q.answer)).Append("\", ")
                    .Append("\"startX\": ").Append(q.startX).Append(", ")
                    .Append("\"startY\": ").Append(q.startY).Append(", ")
                    .Append("\"isHorizontal\": ").Append(q.isHorizontal ? "true" : "false").Append(" }");
                builder.Append(i + 1 < data.questions.Count ? ",\n" : "\n");
            }
            builder.Append("  ],\n  \"gridWidth\": ").Append(data.gridWidth)
                .Append(",\n  \"gridHeight\": ").Append(data.gridHeight).Append("\n}\n");
            return builder.ToString();
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
