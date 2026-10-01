using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrossBound.Editor.CrosswordGenerator
{
    public sealed class CrosswordGeneratorWindow : EditorWindow
    {
        private const string DefaultOutputDirectory = "Assets/CrossBound/Content/Crosswords";
        private TextAsset _dictionary;
        private string _crosswordId = "crossword_001";
        private string _languageId = "en";
        private CrosswordGeneratorSettings _settings = new CrosswordGeneratorSettings();

        [MenuItem("CrossBound/Crossword Generator")]
        private static void Open() => GetWindow<CrosswordGeneratorWindow>("Crossword Generator");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Editor-time crossword generation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("The generator creates JSON content. It does not run in a player build.", MessageType.Info);
            _dictionary = (TextAsset)EditorGUILayout.ObjectField("Dictionary JSON", _dictionary, typeof(TextAsset), false);
            _crosswordId = EditorGUILayout.TextField("Crossword ID", _crosswordId);
            _languageId = EditorGUILayout.Popup("Language", _languageId == "ru" ? 1 : 0, new[] { "en", "ru" }) == 1 ? "ru" : "en";
            _settings.width = EditorGUILayout.IntField("Grid width", _settings.width);
            _settings.height = EditorGUILayout.IntField("Grid height", _settings.height);
            _settings.wordCount = EditorGUILayout.IntField("Word count", _settings.wordCount);
            _settings.minimumWordLength = EditorGUILayout.IntField("Minimum word length", _settings.minimumWordLength);
            _settings.maximumAttempts = EditorGUILayout.IntField("Maximum attempts", _settings.maximumAttempts);
            _settings.randomSeed = EditorGUILayout.IntField("Random seed (0 = random)", _settings.randomSeed);

            using (new EditorGUI.DisabledScope(_dictionary == null || string.IsNullOrWhiteSpace(_crosswordId)))
                if (GUILayout.Button("Generate JSON")) Generate();
        }

        private void Generate()
        {
            CrosswordLanguageDefinition language;
            try { language = CrosswordLanguageDefinition.FromId(_languageId); }
            catch (System.Exception exception) { Debug.LogError(exception.Message); return; }

            WordEntryCollection collection = JsonUtility.FromJson<WordEntryCollection>(_dictionary.text);
            var database = new WordDatabase(collection?.entries ?? new List<WordEntry>(), language, _settings.minimumWordLength);
            var ordered = new CrosswordAnalysis().OrderWords(database);
            var crossword = new CrosswordBacktracker(_settings).Generate(_crosswordId, language.Id, ordered);
            if (crossword == null)
            {
                Debug.LogError("Crossword generation failed. Add intersecting words, reduce the requested word count, or raise the attempt limit.");
                return;
            }

            Directory.CreateDirectory(DefaultOutputDirectory);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(DefaultOutputDirectory, _crosswordId + ".json"));
            File.WriteAllText(assetPath, JsonUtility.ToJson(crossword, true));
            AssetDatabase.ImportAsset(assetPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            Debug.Log($"Generated {crossword.entries.Count}-word crossword at {assetPath}.");
        }
    }
}
