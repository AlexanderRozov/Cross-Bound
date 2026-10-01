using UnityEngine;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "CrosswordData", menuName = "Crossword/Crossword Data")]
public class CrosswordDataSO : ScriptableObject
{
    public TextAsset jsonFile;
    [Tooltip("Addressables key used when jsonFile is not assigned.")]
    public string addressableKey = "CrosswordQuestions";
    public string resourcesPath = "CrosswordQuestions";
    
    public async UniTask<CrosswordData> LoadAsync()
    {
        if (jsonFile != null)
        {
            CrosswordData data = JsonUtility.FromJson<CrosswordData>(jsonFile.text) ?? new CrosswordData();
            data.NormalizeGeneratedLayout();
            return data;
        }
        
        return await new CrosswordContentLoader().LoadAsync(addressableKey, resourcesPath);
    }
}
