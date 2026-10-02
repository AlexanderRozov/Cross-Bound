using System;
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
            Debug.Log($"[CrossBound][Content] Trying Addressables with address '{address}'...");
            CrosswordData addressableData = await TryLoadFromAddressables(address);
            if (addressableData != null)
            {
                Debug.Log("[CrossBound][Content] Crossword loaded from Addressables.");
                return addressableData;
            }
        }

        Debug.Log($"[CrossBound][Content] Falling back to Resources.Load('{resourcesPath}').");
        TextAsset fallback = Resources.Load<TextAsset>(resourcesPath);
        if (fallback == null)
        {
            Debug.LogError($"CrossBound: crossword was not found. Address: '{address}', Resources: '{resourcesPath}'.");
            return new CrosswordData();
        }

        CrosswordData result = JsonUtility.FromJson<CrosswordData>(fallback.text) ?? new CrosswordData();
        result.NormalizeGeneratedLayout();
        Debug.Log($"[CrossBound][Content] Crossword parsed from Resources: {result.Questions.Count} questions, {result.gridWidth}x{result.gridHeight}. JSON size: {fallback.text.Length} chars.");
        return result;
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
            Debug.LogWarning($"[CrossBound][Content] Addressables load for '{address}' failed ({exception.GetType().Name}: {exception.Message}). Falling back to Resources.");
        }
        finally
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        return null;
    }
}
