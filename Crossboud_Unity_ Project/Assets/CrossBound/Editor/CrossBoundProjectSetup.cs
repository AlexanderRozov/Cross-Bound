using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// One-click project setup: creates the PanelSettings asset the runtime bootstrap
/// expects (with a proper theme and screen-size scaling) and runs the smoke tests.
/// </summary>
public static class CrossBoundProjectSetup
{
    private const string PanelSettingsPath = "Assets/CrossBound/Resources/UI/CrossBoundPanelSettings.asset";
    private const string DefaultThemePath = "Packages/com.unity.ui/PackageResources/StyleSheets/UnityDefaultRuntimeTheme.uss";

    [MenuItem("CrossBound/Setup Project")]
    public static void SetupProject()
    {
        CreatePanelSettings();
        CrossBoundSmokeTests.RunAll();
    }

    [MenuItem("CrossBound/Create Panel Settings")]
    public static void CreatePanelSettings()
    {
        PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
        }

        ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(DefaultThemePath);
        if (theme != null)
            settings.themeStyleSheet = theme;
        else
            Debug.LogWarning($"[CrossBound][Setup] Default UI Toolkit theme not found at '{DefaultThemePath}'. Assign it manually in the PanelSettings inspector.");

        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1920, 1080);
        settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = 0.5f;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CrossBound][Setup] PanelSettings saved to {PanelSettingsPath} (theme: {(theme != null ? "assigned" : "MISSING")}).");
    }

    /// <summary>Entry point for CI/batchmode: Unity.exe -batchmode -executeMethod CrossBoundProjectSetup.BatchSetup -quit</summary>
    public static void BatchSetup()
    {
        CreatePanelSettings();
        CrossBoundSmokeTests.RunAll();
    }
}
