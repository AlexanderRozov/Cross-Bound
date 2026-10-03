using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Loads one crossword at a time. Resources is tried first — it is synchronous,
/// always available and does not spam errors when the project has no Addressables
/// groups built. Addressables is only attempted when Resources has nothing.
/// </summary>
public sealed class CrosswordContentLoader
{
    public async UniTask<CrosswordData> LoadAsync(string resourcesPath, string address)
    {
        Debug.Log($"[CrossBound][Content] Trying Resources.Load('{resourcesPath}').");
        TextAsset resource = Resources.Load<TextAsset>(resourcesPath);
        if (resource != null)
        {
            CrosswordData result = JsonUtility.FromJson<CrosswordData>(resource.text) ?? new CrosswordData();
            result.NormalizeGeneratedLayout();
            Debug.Log($"[CrossBound][Content] Crossword parsed from Resources: {result.Questions.Count} questions, {result.gridWidth}x{result.gridHeight}. JSON size: {resource.text.Length} chars.");
            return result;
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            Debug.Log($"[CrossBound][Content] Resources is empty — falling back to Addressables with address '{address}'...");
            CrosswordData addressableData = await TryLoadFromAddressables(address);
            if (addressableData != null)
            {
                Debug.Log("[CrossBound][Content] Crossword loaded from Addressables.");
                return addressableData;
            }
        }

        Debug.LogError($"CrossBound: crossword was not found. Resources: '{resourcesPath}', Address: '{address}'.");
        return new CrosswordData();
    }

    // A missing address or an unloaded Addressables group used to throw straight out of
    // InitializeAsync and left the game on a blank screen. The attempt is fully guarded now.
    private static async UniTask<CrosswordData> TryLoadFromAddressables(string address)
    {
        AsyncOperationHandle<TextAsset> handle = default;
        try
        {
            handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                CrosswordData data = JsonUtility.FromJson<CrosswordData>(handle.Result.text);
                if (data != null)
                {
                    data.NormalizeGeneratedLayout();
                    return data;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[CrossBound][Content] Addressables load for '{address}' failed ({exception.GetType().Name}: {exception.Message}). Falling back to an empty puzzle.");
        }
        finally
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        return null;
    }
}
