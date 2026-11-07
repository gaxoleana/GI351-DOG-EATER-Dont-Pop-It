using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        guide.guideCanvas = root.GetComponent<Canvas>();
        guide.guideCanvas.overrideSorting = true;
        guide.guideCanvas.sortingOrder = 30000;
        guide.raycaster = root.GetComponent<GraphicRaycaster>();
        // Keep the menu font while giving small copy a solid, high-contrast face.
        guide.readableTextMaterial = new Material(menuFontTemplate.fontSharedMaterial);
        guide.readableTextMaterial.name = "Panic Guide Readable Text";
        guide.readableTextMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        guide.readableTextMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);

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
        return true;
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
    }

    private GameObject CreatePanel(bool red, TextMeshProUGUI fontTemplate, Sprite panelSprite,
        Sprite buttonSprite, out Button primaryButton)
    {
        Image panel = CreateImage(red ? "Red Panic Page" : "Blue Panic Page", transform,
            panelSprite, new Color(0.8784314f, 1f, 1f));
        Place(panel.rectTransform, Vector2.zero, new Vector2(900f, 1020f));
        AddOutline(panel.gameObject, 3f);
        CreateText("Page Number", panel.transform, fontTemplate, red ? "PANIC EVENTS   1 / 2" : "PANIC EVENTS   2 / 2",
            36f, new Vector2(0f, 465f), new Vector2(790f, 50f), true);

        Color accent = red ? new Color(0.86f, 0.19f, 0.25f) : new Color(0.12f, 0.49f, 0.83f);
        Image heading = CreateImage("Event Color", panel.transform, buttonSprite, accent);
        Place(heading.rectTransform, new Vector2(0f, 370f), new Vector2(770f, 120f));
        heading.raycastTarget = false;
        AddOutline(heading.gameObject, 3f);
        CreateText("Event Name", heading.transform, fontTemplate, red ? "RED PANIC" : "BLUE PANIC",
            66f, Vector2.zero, new Vector2(730f, 110f));

        CreateText("Action", panel.transform, fontTemplate, red ? "CLICK FAST!" : "HANDS OFF!",
            82f, new Vector2(0f, 225f), new Vector2(800f, 120f));
        CreateText("Instructions", panel.transform, fontTemplate,
            red
                ? "Click / tap fast,\nor tap Space repeatedly.\nReach the target in time."
                : "Release mouse / touch\nand Space.\nWait for the timer to end.",
            60f, new Vector2(0f, 25f), new Vector2(790f, 250f), true);
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
