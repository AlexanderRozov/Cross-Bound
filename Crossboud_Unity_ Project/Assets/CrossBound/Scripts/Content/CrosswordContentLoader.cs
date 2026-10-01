using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Loads one crossword at a time. Addressables are preferred; Resources is a safe editor/offline fallback.</summary>
public sealed class CrosswordContentLoader
{
    public async UniTask<CrosswordData> LoadAsync(string address, string resourcesPath)
    {
        if (!string.IsNullOrWhiteSpace(address))
        {
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                CrosswordData addressableData = JsonUtility.FromJson<CrosswordData>(handle.Result.text);
                Addressables.Release(handle);
                if (addressableData != null)
                {
                    addressableData.NormalizeGeneratedLayout();
                    return addressableData;
                }
            }
            else
            {
                Addressables.Release(handle);
            }
        }

        TextAsset fallback = Resources.Load<TextAsset>(resourcesPath);
        if (fallback == null)
        {
            Debug.LogError($"CrossBound: crossword was not found. Address: '{address}', Resources: '{resourcesPath}'.");
            return new CrosswordData();
        }

        CrosswordData result = JsonUtility.FromJson<CrosswordData>(fallback.text) ?? new CrosswordData();
        result.NormalizeGeneratedLayout();
        return result;
    }
}
