using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds static TMP font assets from the bundled PT Serif TTFs so the game renders
/// newspaper text from a pre-generated atlas instead of generating it at runtime.
/// Not strictly required — <see cref="NewspaperFonts"/> creates dynamic assets on
/// the fly when the static ones are missing — but static assets are the preferred
/// shipping path (faster startup, deterministic rendering on WebGL).
/// </summary>
public static class CrossBoundFontSetup
{
    private const string FontDir = "Assets/CrossBound/Fonts";
    private const string OutDir = "Assets/CrossBound/Resources/Fonts";

    [MenuItem("CrossBound/Create Font Assets")]
    public static void CreateFontAssets()
    {
        Create("PTSerif-Regular");
        Create("PTSerif-Bold");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Create(string name)
    {
        string ttfPath = $"{FontDir}/{name}.ttf";
        string assetPath = $"{OutDir}/{name}.asset";

        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogError($"[CrossBound][Fonts] TTF not found at '{ttfPath}'.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
        {
            Debug.Log($"[CrossBound][Fonts] '{assetPath}' already exists — skipped.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/CrossBound/Resources", "Fonts");

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font);
        asset.name = name;
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        AssetDatabase.CreateAsset(asset, assetPath);
        if (asset.material != null)
        {
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.ImportAsset(assetPath);
        Debug.Log($"[CrossBound][Fonts] Created '{assetPath}' from '{ttfPath}'.");
    }
}
