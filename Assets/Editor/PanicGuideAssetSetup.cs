using System.Linq;
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
        if (existing != null && existing.thaiFont != null && existing.thaiFont.name == "Panic Guide Mali Bold SDF") return;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Mali/Mali-Bold.ttf");
        if (source == null) return;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var font = TMP_FontAsset.CreateFontAsset(source);
        font.name = "Panic Guide Mali Bold SDF";
        string characters = string.Concat(Enumerable.Range(0x0E01, 0x5B).Select(c => ((char)c).ToString())) + "[English] Space0123456789/! .";
        font.TryAddCharacters(characters, out string missing);
        AssetDatabase.CreateAsset(font, "Assets/Fonts/Mali/Panic Guide Mali Bold SDF.asset");
        foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        AssetDatabase.AddObjectToAsset(font.material, font);
        var assets = existing != null ? existing : ScriptableObject.CreateInstance<PanicGuideAssets>();
        assets.shock = AssetDatabase.LoadAllAssetsAtPath("Assets/V.1/Assets/Player/Expressions/shock.png").OfType<Sprite>().First();
        assets.relax = AssetDatabase.LoadAllAssetsAtPath("Assets/V.1/Assets/Player/Expressions/relax.png").OfType<Sprite>().First();
        assets.thaiFont = font;
        if (existing == null) AssetDatabase.CreateAsset(assets, "Assets/Resources/PanicGuideAssets.asset");
        EditorUtility.SetDirty(assets);
        AssetDatabase.SaveAssets();
    }
}

