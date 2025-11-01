using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Runtime helper that injects a Japanese-capable fallback font into every TMP font asset that gets used.
/// </summary>
public static class TMPJapaneseFallback
{
    private const string ResourcePath = "Fonts/NotoSansJP-Regular";

    private static TMP_FontAsset runtimeFallback;
    private static readonly HashSet<TMP_FontAsset> patchedFonts = new HashSet<TMP_FontAsset>();
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        ConfigureFallback();
    }

    private static void ConfigureFallback()
    {
        var sourceFont = Resources.Load<Font>(ResourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"[TMPJapaneseFallback] Font resource '{ResourcePath}' was not found. Japanese text might still render as missing glyphs.");
            return;
        }

        runtimeFallback = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);

        runtimeFallback.name = "NotoSansJP TMP Dynamic Fallback";
        runtimeFallback.hideFlags = HideFlags.HideAndDontSave;
        runtimeFallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        runtimeFallback.isMultiAtlasTexturesEnabled = true;

        EnsureFallback(runtimeFallback, TMP_Settings.defaultFontAsset);

        var fallbackList = TMP_Settings.fallbackFontAssets;
        if (fallbackList == null)
        {
            fallbackList = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = fallbackList;
        }

        if (!fallbackList.Contains(runtimeFallback))
        {
            fallbackList.Add(runtimeFallback);
        }

        PatchAllFonts();
        PatchLoadedTexts();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PatchAllFonts();
        PatchLoadedTexts();
    }

    private static void PatchAllFonts()
    {
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            ApplyFallback(font);
        }
    }

    private static void PatchLoadedTexts()
    {
        var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (var text in texts)
        {
            ApplyFallback(text.font);
        }
    }

    private static void ApplyFallback(TMP_FontAsset font)
    {
        if (font == null || runtimeFallback == null || patchedFonts.Contains(font))
        {
            return;
        }

        patchedFonts.Add(font);
        EnsureFallback(runtimeFallback, font);
    }

    private static void EnsureFallback(TMP_FontAsset fallback, TMP_FontAsset target)
    {
        if (fallback == null || target == null)
        {
            return;
        }

        var table = target.fallbackFontAssetTable;
        if (table == null)
        {
            table = new List<TMP_FontAsset>();
            target.fallbackFontAssetTable = table;
        }

        if (!table.Contains(fallback))
        {
            table.Add(fallback);
        }
    }
}
