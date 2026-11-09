using TMPro;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore;
using UnityEngine.UI;

/// <summary>A two-page introduction to both panic variants, shown once per run.</summary>
public sealed class PanicEventGuide : MonoBehaviour
{
    private Canvas guideCanvas;
    private GraphicRaycaster raycaster;
    private GameObject redPanel;
    private GameObject bluePanel;
    private Button nextButton;
    private Button backButton;
    private Button closeButton;
    private bool showingBluePage;
    private bool navigationReady;
    private bool hasReadGuide;
    private int openedFrame;
    private Material readableTextMaterial;
    private PanicGuideAssets artwork;
    private bool thai;
    private TMP_FontAsset menuFont;
    private Material menuMaterial;
    private Material thaiOutlinedMaterial;
    private Material thaiReadableMaterial;
    private RawImage blurredBackdrop;
    private RenderTexture backdropTexture;
    private Material blurMaterial;
    private Coroutine captureRoutine;

    public bool IsOpen { get; private set; }

    public static PanicEventGuide Create(TextMeshProUGUI menuFontTemplate, Sprite panelSprite, Sprite buttonSprite)
    {
        if (menuFontTemplate == null || menuFontTemplate.canvas == null) return null;

        var root = new GameObject("Panic Event Guide", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(PanicEventGuide));
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(menuFontTemplate.canvas.rootCanvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        var guide = root.GetComponent<PanicEventGuide>();
        guide.artwork = Resources.Load<PanicGuideAssets>("PanicGuideAssets");
        guide.menuFont = menuFontTemplate.font;
        guide.menuMaterial = menuFontTemplate.fontSharedMaterial;
        guide.guideCanvas = root.GetComponent<Canvas>();
        guide.guideCanvas.overrideSorting = true;
        guide.guideCanvas.sortingOrder = 30000;
        guide.raycaster = root.GetComponent<GraphicRaycaster>();
        var blurObject = new GameObject("Blurred Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        blurObject.transform.SetParent(root.transform, false);
        blurObject.layer = root.layer;
        guide.blurredBackdrop = blurObject.GetComponent<RawImage>();
        guide.blurredBackdrop.raycastTarget = false;
        Stretch(guide.blurredBackdrop.rectTransform);
        // Keep the menu font while giving small copy a solid, high-contrast face.
        guide.readableTextMaterial = new Material(menuFontTemplate.fontSharedMaterial);
        guide.readableTextMaterial.name = "Panic Guide Readable Text";
        guide.readableTextMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        guide.readableTextMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
        if (guide.artwork != null && guide.artwork.thaiFont != null)
        {
            guide.thaiOutlinedMaterial = guide.CreateThaiMaterial(menuFontTemplate.font.material);
            guide.thaiOutlinedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.32f);
            guide.thaiOutlinedMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            guide.thaiOutlinedMaterial.SetFloat("_OutlineSoftness", 0f);
            guide.thaiOutlinedMaterial.SetColor("_OutlineColor", Color.black);
            guide.thaiReadableMaterial = guide.CreateThaiMaterial(guide.readableTextMaterial);
        }

        Image backdrop = CreateImage("Input Blocker", root.transform, null, new Color(0.04f, 0.09f, 0.14f, 0.78f));
        Stretch(backdrop.rectTransform);
        // The backdrop absorbs pointers, but deliberately has no dismiss handler.
        backdrop.raycastTarget = true;

        // The scene supplies the same panel/button sprite references as MainMenuV.1.
        guide.redPanel = guide.CreatePanel(true, menuFontTemplate, panelSprite, buttonSprite, out guide.nextButton);
        guide.bluePanel = guide.CreatePanel(false, menuFontTemplate, panelSprite, buttonSprite, out guide.closeButton);
        guide.nextButton.onClick.AddListener(guide.NextPage);
        guide.backButton.onClick.AddListener(guide.PreviousPage);
        guide.closeButton.onClick.AddListener(guide.CloseFromButton);
        guide.Hide();
        return guide;
    }

    public bool TryShow()
    {
        if (IsOpen || hasReadGuide) return false;

        IsOpen = true;
        ShowPage(false);
        guideCanvas.enabled = true;
        raycaster.enabled = true;
        captureRoutine = StartCoroutine(CaptureBlurredBackdrop());
        return true;
    }

    private System.Collections.IEnumerator CaptureBlurredBackdrop()
    {
        // Capture the complete scene and HUD without the guide itself.
        guideCanvas.enabled = false;
        yield return new WaitForEndOfFrame();
        if (!IsOpen) yield break;
        var shader = Resources.Load<Shader>("PanicGuideBackdropBlur");
        if (shader != null)
        {
            if (blurMaterial == null) blurMaterial = new Material(shader);
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            int width = Mathf.Max(1, Screen.width / 4);
            int height = Mathf.Max(1, Screen.height / 4);
            if (backdropTexture != null) { backdropTexture.Release(); Destroy(backdropTexture); }
            backdropTexture = new RenderTexture(width, height, 0) { filterMode = FilterMode.Bilinear };
            var temporary = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(screenshot, backdropTexture);
            for (int i = 0; i < 3; i++)
            {
                blurMaterial.SetVector("_Direction", new Vector4(2f / width, 0f, 0f, 0f));
                Graphics.Blit(backdropTexture, temporary, blurMaterial);
                blurMaterial.SetVector("_Direction", new Vector4(0f, 2f / height, 0f, 0f));
                Graphics.Blit(temporary, backdropTexture, blurMaterial);
            }
            RenderTexture.ReleaseTemporary(temporary);
            Destroy(screenshot);
            blurredBackdrop.texture = backdropTexture;
        }
        guideCanvas.enabled = true;
        captureRoutine = null;
    }

    private void Update()
    {
        if (IsOpen && !navigationReady && Time.frameCount > openedFrame && !IsEventInputHeld())
        {
            navigationReady = true;
            SetNavigationInteractable(true);
        }
    }

    private void ShowPage(bool blue)
    {
        showingBluePage = blue;
        redPanel.SetActive(!blue);
        bluePanel.SetActive(blue);
        openedFrame = Time.frameCount;
        navigationReady = false;
        // Require a fresh press after opening or switching pages.
        SetNavigationInteractable(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void SetNavigationInteractable(bool ready)
    {
        nextButton.interactable = ready;
        backButton.interactable = ready;
        closeButton.interactable = ready;
    }

    private void NextPage()
    {
        if (IsOpen && navigationReady && !showingBluePage) ShowPage(true);
    }

    private void PreviousPage()
    {
        if (IsOpen && navigationReady && showingBluePage) ShowPage(false);
    }

    public static bool IsEventInputHeld()
    {
        return Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.touchCount > 0;
    }

    private void CloseFromButton()
    {
        if (!IsOpen || !navigationReady || !showingBluePage) return;
        hasReadGuide = true;
        Hide();
    }

    public void Hide()
    {
        if (captureRoutine != null) { StopCoroutine(captureRoutine); captureRoutine = null; }
        IsOpen = false;
        navigationReady = false;
        if (redPanel != null) redPanel.SetActive(false);
        if (bluePanel != null) bluePanel.SetActive(false);
        if (guideCanvas != null) guideCanvas.enabled = false;
        if (raycaster != null) raycaster.enabled = false;
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (readableTextMaterial != null) Destroy(readableTextMaterial);
        if (thaiOutlinedMaterial != null) Destroy(thaiOutlinedMaterial);
        if (thaiReadableMaterial != null) Destroy(thaiReadableMaterial);
        if (blurMaterial != null) Destroy(blurMaterial);
        if (backdropTexture != null) { backdropTexture.Release(); Destroy(backdropTexture); }
    }

    private Material CreateThaiMaterial(Material style)
    {
        // Keep the Thai atlas and SDF metrics, while matching the game's face and outline.
        var material = new Material(artwork.thaiFont.material);
        material.shader = style.shader;
        material.shaderKeywords = style.shaderKeywords;
        foreach (string property in new[] { "_FaceDilate", "_OutlineWidth", "_OutlineSoftness" })
            if (style.HasProperty(property)) material.SetFloat(property, style.GetFloat(property));
        foreach (string property in new[] { "_FaceColor", "_OutlineColor" })
            if (style.HasProperty(property)) material.SetColor(property, style.GetColor(property));
        return material;
    }

    private GameObject CreatePanel(bool red, TextMeshProUGUI fontTemplate, Sprite panelSprite,
        Sprite buttonSprite, out Button primaryButton)
    {
        Image panel = CreateImage(red ? "Red Panic Page" : "Blue Panic Page", transform,
            panelSprite, new Color(0.8784314f, 1f, 1f));
        Place(panel.rectTransform, Vector2.zero, new Vector2(900f, 1020f));
        AddOutline(panel.gameObject, 3f);
        CreateText("Page Number", panel.transform, fontTemplate, red ? "PANIC EVENTS   1 / 2" : "PANIC EVENTS   2 / 2",
            32f, new Vector2(-150f, 465f), new Vector2(480f, 50f), true);

        Button language = CreateButton("Language", panel.transform, fontTemplate, buttonSprite, new Vector2(275f, 465f));
        Place(language.GetComponent<RectTransform>(), new Vector2(275f, 465f), new Vector2(260f, 64f));
        var languageOutline = language.GetComponent<Outline>();
        languageOutline.effectColor = Color.white;
        var languageLabel = language.GetComponentInChildren<TextMeshProUGUI>();
        Place(languageLabel.rectTransform, Vector2.zero, new Vector2(240f, 60f));
        languageLabel.fontSize = 36f;
        languageLabel.color = new Color(0.07f, 0.13f, 0.17f);
        if (artwork != null && artwork.thaiFont != null)
        {
            languageLabel.font = artwork.thaiFont;
            languageLabel.fontSharedMaterial = thaiReadableMaterial;
            EnableThaiMarkPositioning(languageLabel);
        }
        languageLabel.text = "[ภาษาไทย]";
        languageLabel.color = new Color(0.07f, 0.13f, 0.17f);
        language.onClick.AddListener(ToggleLanguage);
        Image heading = CreateImage("Event Expression", panel.transform,
            artwork != null ? (red ? artwork.shock : artwork.relax) : null, Color.white);
        heading.type = Image.Type.Simple;
        heading.preserveAspect = true;
        Place(heading.rectTransform, new Vector2(0f, 325f), new Vector2(500f, 200f));
        heading.raycastTarget = false;
        CreateText("Action", panel.transform, fontTemplate, red ? "CLICK FAST!" : "HANDS OFF!",
            72f, new Vector2(0f, 160f), new Vector2(800f, 110f));
        panel.transform.Find("Action").GetComponent<TextMeshProUGUI>().color = ActionColor(red);
        CreateText("Instructions", panel.transform, fontTemplate,
            red
                ? "Click / tap fast,\nor tap Space repeatedly.\nReach the target in time."
                : "Release mouse / touch\nand Space.\nWait for the timer to end.",
            56f, new Vector2(0f, -15f), new Vector2(790f, 230f), true);
        CreateText("Tip", panel.transform, fontTemplate,
            red ? "Your target is shown on the bar." : "Pressing early pops your gum!",
            46f, new Vector2(0f, -160f), new Vector2(790f, 100f), true);

        primaryButton = CreateButton(red ? "Next" : "Close", panel.transform, fontTemplate,
            buttonSprite, new Vector2(red ? 0f : 205f, -300f));
        if (!red)
            backButton = CreateButton("Back", panel.transform, fontTemplate, buttonSprite, new Vector2(-205f, -300f));
        CreateText("Paused", panel.transform, fontTemplate, "Game paused. Take your time.",
            40f, new Vector2(0f, -425f), new Vector2(790f, 70f), true);
        return panel.gameObject;
    }

    private Button CreateButton(string label, Transform parent, TextMeshProUGUI fontTemplate,
        Sprite sprite, Vector2 position)
    {
        Image image = CreateImage(label + " Button", parent, sprite, Color.white);
        Place(image.rectTransform, position, new Vector2(360f, 112f));
        AddOutline(image.gameObject, 3f);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        CreateText("Label", image.transform, fontTemplate, label, 64f, Vector2.zero, new Vector2(330f, 104f));
        return button;
    }

    private void ToggleLanguage()
    {
        if (!IsOpen || !navigationReady || artwork == null || artwork.thaiFont == null) return;
        thai = !thai;
        LocalizePage(redPanel, true);
        LocalizePage(bluePanel, false);
    }

    private void LocalizePage(GameObject page, bool red)
    {
        foreach (var label in page.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool language = label.transform.parent.name == "Language Button";
            string english = null;
            string translated = null;
            switch (label.name)
            {
                case "Action":
                    english = red ? "CLICK FAST!" : "HANDS OFF!";
                    translated = red ? "กดให้เร็ว!" : "ห้ามกด!";
                    break;
                case "Instructions":
                    english = red ? "Click / tap fast,\nor tap Space repeatedly.\nReach the target in time." : "Release mouse / touch\nand Space.\nWait for the timer to end.";
                    translated = red ? "คลิก / แตะรัว ๆ\nหรือกด Space ซ้ำ ๆ\nให้ถึงเป้าหมายก่อนหมดเวลา" : "ปล่อยเมาส์ / หยุดแตะจอ\nและปล่อยปุ่ม Space\nรอจนหมดเวลา";
                    break;
                case "Tip":
                    english = red ? "Your target is shown on the bar." : "Pressing early pops your gum!";
                    translated = red ? "ดูเป้าหมายได้บนแถบ" : "กดก่อนหมดเวลา หมากฝรั่งจะแตก!";
                    break;
                case "Paused":
                    english = "Game paused. Take your time.";
                    translated = "เกมหยุดอยู่ อ่านได้ตามสบาย";
                    break;
                case "Page Number":
                    english = red ? "PANIC EVENTS   1 / 2" : "PANIC EVENTS   2 / 2";
                    translated = red ? "เหตุการณ์ตกใจ   1 / 2" : "เหตุการณ์ตกใจ   2 / 2";
                    break;
                case "Label":
                    if (language) { english = "[ภาษาไทย]"; translated = "[English]"; }
                    else if (label.transform.parent.name == "Next Button") { english = "Next"; translated = "ถัดไป"; }
                    else if (label.transform.parent.name == "Back Button") { english = "Back"; translated = "ย้อนกลับ"; }
                    else { english = "Close"; translated = "ปิด"; }
                    break;
            }
            if (english == null) continue;
            label.text = thai ? PrepareThaiText(translated) : english;
            label.enableAutoSizing = thai;
            label.fontSizeMax = label.name == "Action" ? 72f : label.name == "Instructions" ? 56f : label.name == "Tip" ? 46f : label.name == "Paused" ? 40f : label.name == "Page Number" ? 32f : language ? 36f : 64f;
            label.fontSizeMin = label.fontSizeMax * 0.65f;
            label.fontSize = label.fontSizeMax;
            bool readable = label.name == "Instructions" || label.name == "Tip" || label.name == "Paused" || label.name == "Page Number";
            label.color = readable || language ? new Color(0.07f, 0.13f, 0.17f) : Color.white;
            if (label.name == "Action") label.color = ActionColor(red);
            // Latin text retains the menu font; Thai uses the bundled glyph fallback.
            if (thai || language)
            {
                label.font = artwork.thaiFont;
                label.fontSharedMaterial = readable || language ? thaiReadableMaterial : thaiOutlinedMaterial;
                EnableThaiMarkPositioning(label);
            }
            else
            {
                label.font = menuFont;
                label.fontSharedMaterial = readable ? readableTextMaterial : menuMaterial;
            }
        }
    }

    private static void EnableThaiMarkPositioning(TextMeshProUGUI label)
    {
        var features = new List<OTL_FeatureTag>(label.fontFeatures);
        if (!features.Contains(OTL_FeatureTag.mark)) features.Add(OTL_FeatureTag.mark);
        if (!features.Contains(OTL_FeatureTag.mkmk)) features.Add(OTL_FeatureTag.mkmk);
        label.fontFeatures = features;
    }

    private static string PrepareThaiText(string text)
    {
        // TMP positions combining marks but does not shape Sara Am. Split it into
        // Nikhahit + Sara Aa, placing Nikhahit before a preceding tone mark (ซ้ำ).
        var result = new StringBuilder(text.Length);
        foreach (char character in text)
        {
            if (character != '\u0E33')
            {
                result.Append(character);
                continue;
            }

            int insertion = result.Length;
            while (insertion > 0 && result[insertion - 1] >= '\u0E48' && result[insertion - 1] <= '\u0E4B')
                insertion--;
            result.Insert(insertion, '\u0E4D');
            result.Append('\u0E32');
        }
        return result.ToString();
    }

    private static Color ActionColor(bool red)
    {
        return red ? new Color32(235, 45, 60, 255) : new Color32(35, 150, 245, 255);
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        return image;
    }

    private void CreateText(string name, Transform parent, TextMeshProUGUI template, string value,
        float size, Vector2 position, Vector2 dimensions, bool readable = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = template.font;
        label.fontSharedMaterial = readable ? readableTextMaterial : template.fontSharedMaterial;
        label.text = value;
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.color = readable ? new Color(0.07f, 0.13f, 0.17f) : Color.white;
        label.raycastTarget = false;
        Place(label.rectTransform, position, dimensions);
    }

    private static void AddOutline(GameObject go, float width)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(width, width);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
