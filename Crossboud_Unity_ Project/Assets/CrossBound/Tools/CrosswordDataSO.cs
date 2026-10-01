using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

[CreateAssetMenu(fileName = "CrosswordData", menuName = "Crossword/Crossword Data")]
public class CrosswordDataSO : ScriptableObject
{
    public TextAsset jsonFile;
    
    public async UniTask<CrosswordData> LoadAsync()
    {
        if (jsonFile != null)
            return JsonUtility.FromJson<CrosswordData>(jsonFile.text);
        
        var handle = Addressables.LoadAssetAsync<TextAsset>("CrosswordQuestions");
        await handle.Task;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
            return JsonUtility.FromJson<CrosswordData>(handle.Result.text);
        
        return new CrosswordData();
    }
}