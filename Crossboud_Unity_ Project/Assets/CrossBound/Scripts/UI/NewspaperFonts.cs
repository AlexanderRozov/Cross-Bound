using TMPro;
using UnityEngine;

/// <summary>
/// Shared PT Serif font assets (regular + bold) for the newspaper-styled UI.
/// Prefers the static TMP assets built by CrossBound/Create Font Assets; when those
/// have not been generated yet, builds dynamic font assets from the bundled TTFs at
/// runtime so the game never falls back to a non-serif look silently.
/// </summary>
public static class NewspaperFonts
{
    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;

    public static TMP_FontAsset Regular => _regular != null ? _regular : _regular = Load("PTSerif-Regular");
    public static TMP_FontAsset Bold => _bold != null ? _bold : _bold = Load("PTSerif-Bold");

    private static TMP_FontAsset Load(string name)
    {
        TMP_FontAsset asset = Resources.Load<TMP_FontAsset>("Fonts/" + name);
        if (asset != null) return asset;

        Font ttf = Resources.Load<Font>("Fonts/" + name);
        if (ttf != null)
        {
            asset = TMP_FontAsset.CreateFontAsset(ttf);
            asset.name = name + " (Runtime)";
            return asset;
        }

        Debug.LogWarning($"[CrossBound][Fonts] '{name}' not found in Resources/Fonts — falling back to the TMP default font.");
        return TMP_Settings.defaultFontAsset;
    }
}
