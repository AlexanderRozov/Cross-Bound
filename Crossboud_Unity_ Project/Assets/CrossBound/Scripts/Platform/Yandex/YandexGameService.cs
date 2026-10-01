using System;
using YG;

/// <summary>
/// Small platform boundary for the PluginYG2 lifecycle API.
/// Game code should use this class rather than call YG2 directly.
/// </summary>
public sealed class YandexGameService : IDisposable
{
    private bool _isDisposed;
    private bool _gameReadyRequested;
    private bool _gameplayRequested;

    public bool IsInitialized => YG2.isSDKEnabled;
    public bool IsGameplayActive => _gameplayRequested;

    public event Action Initialized;

    public void Initialize()
    {
        if (IsInitialized)
        {
            Initialized?.Invoke();
            return;
        }

        YG2.onGetSDKData += OnSdkInitialized;
    }

    public void MarkGameReady()
    {
        _gameReadyRequested = true;
        TryReportReadyState();
    }

    public void StartGameplay()
    {
        _gameplayRequested = true;

        if (IsInitialized)
            YG2.GameplayStart();
    }

    public void StopGameplay()
    {
        _gameplayRequested = false;

        if (IsInitialized)
            YG2.GameplayStop();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        YG2.onGetSDKData -= OnSdkInitialized;
        StopGameplay();
    }

    private void OnSdkInitialized()
    {
        if (_isDisposed)
            return;

        YG2.onGetSDKData -= OnSdkInitialized;
        TryReportReadyState();

        if (_gameplayRequested)
            YG2.GameplayStart();

        Initialized?.Invoke();
    }

    private void TryReportReadyState()
    {
        if (_gameReadyRequested && IsInitialized)
            YG2.GameReadyAPI();
    }
}
