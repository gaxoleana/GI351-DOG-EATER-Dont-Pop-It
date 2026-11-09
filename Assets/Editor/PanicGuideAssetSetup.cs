using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// Generate once in the editor, keeping runtime loads independent of AssetDatabase.
[InitializeOnLoad]
public static class PanicGuideAssetSetup
{
    static PanicGuideAssetSetup() { EditorApplication.delayCall += EnsureAssets; }

    [MenuItem("Tools/Don't Pop It/Prepare Panic Guide Artwork")]
    public static void EnsureAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var existing = AssetDatabase.LoadAssetAtPath<PanicGuideAssets>("Assets/Resources/PanicGuideAssets.asset");
        if (existing != null && existing.thaiFont != null && existing.thaiFont.name == "Panic Guide Mali Bold SDF")
        {
            EnsureThaiFontFeatures(existing.thaiFont);
            return;
        }
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mali/Mali-Bold.ttf");
        if (source == null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Panic Guide Mali Bold SDF";
        string characters = string.Concat(Enumerable.Range(0x0E01, 0x5B).Select(c => ((char)c).ToString())) + "[English] Space0123456789/! .";
        font.TryAddCharacters(characters, out string missing, includeFontFeatures: true);
        AssetDatabase.CreateAsset(font, "Assets/Fonts/Mali/Panic Guide Mali Bold SDF.asset");
        foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        AssetDatabase.AddObjectToAsset(font.material, font);
        EnsureThaiFontFeatures(font);
        var assets = existing != null ? existing : ScriptableObject.CreateInstance<PanicGuideAssets>();
        assets.shock = AssetDatabase.LoadAllAssetsAtPath("Assets/V.1/Assets/Player/Expressions/shock.png").OfType<Sprite>().First();
        assets.relax = AssetDatabase.LoadAllAssetsAtPath("Assets/V.1/Assets/Player/Expressions/relax.png").OfType<Sprite>().First();
        assets.thaiFont = font;
        if (existing == null) AssetDatabase.CreateAsset(assets, "Assets/Resources/PanicGuideAssets.asset");
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureThaiFontFeatures(TMP_FontAsset font)
    {
        // Builds can clear dynamic glyph caches. Retain the baked anchors until
        // glyphs are regenerated instead of pruning against an empty atlas.
        if (font.glyphTable.Count == 0) return;

        bool imported = (font.fontFeatureTable.MarkToBaseAdjustmentRecords?.Count ?? 0) == 0 ||
            (font.fontFeatureTable.MarkToMarkAdjustmentRecords?.Count ?? 0) == 0;
        if (imported)
        {
            // Import OpenType anchors into the existing asset without changing
            // its GUID or repacking its atlas. Builds need these tables too.
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ShouldReimportFontFeatures").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            font.ReadFontAssetDefinition();
        }

        // Native import includes the whole typeface. Keep positioning records
        // only for glyphs in this guide's atlas, avoiding unrelated font data.
        var glyphs = new HashSet<uint>(font.glyphTable.Select(glyph => glyph.index));
        var features = font.fontFeatureTable;
        int removed = features.MarkToBaseAdjustmentRecords.RemoveAll(record =>
            !glyphs.Contains(record.baseGlyphID) || !glyphs.Contains(record.markGlyphID));
        removed += features.MarkToMarkAdjustmentRecords.RemoveAll(record =>
            !glyphs.Contains(record.baseMarkGlyphID) || !glyphs.Contains(record.combiningMarkGlyphID));
        removed += features.glyphPairAdjustmentRecords.RemoveAll(record =>
            !glyphs.Contains(record.firstAdjustmentRecord.glyphIndex) || !glyphs.Contains(record.secondAdjustmentRecord.glyphIndex));
        if (!imported && removed == 0) return;

        font.ReadFontAssetDefinition();
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
    }
}

