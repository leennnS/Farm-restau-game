using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Rustic parchment letter UI panel.
/// Displays inherited farm letter to player.
/// Uses UIToolkit for styling and layout.
/// </summary>
public class LetterPanel : MonoBehaviour
{
    [SerializeField]
    private UIDocument uiDocument;

    [SerializeField]
    private string letterTitle = "An Unexpected Inheritance";

    [SerializeField]
    private string letterContent =
        "My dear,\n\n" +
        "I know this must be unexpected. For years, I've watched over this old farm, hoping one day it might truly thrive again.\n\n" +
        "Now, I am leaving it to you.\n\n" +
        "This land has stories to tell—of failed harvests and forgotten dreams, but also of potential and possibility. " +
        "The soil is rich, the water runs clear, and the place has a peculiar magic about it, if you know how to listen.\n\n" +
        "I believe you can bring it back to life.\n\n" +
        "Take care of it. Listen to it. And perhaps, in tending to this farm, you'll find what you've been searching for.\n\n" +
        "With faith and love,\n" +
        "Your Grandmother";

    private VisualElement letterPanel;
    private VisualElement letterContent_UI;
    private Button closeButton;
    private Button goToFarmButton;
    private VisualElement backdrop;

    private bool isOpen = false;
    private bool goToFarmRequested = false;

    public bool GoToFarmRequested => goToFarmRequested;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("[LetterPanel] No UIDocument assigned!");
            return;
        }

        CreateLetterUI();
    }

    private void CreateLetterUI()
    {
        VisualElement root = uiDocument.rootVisualElement;

        // Backdrop
        backdrop = new VisualElement();
        backdrop.style.position = Position.Absolute;
        backdrop.style.left = 0;
        backdrop.style.top = 0;
        backdrop.style.right = 0;
        backdrop.style.bottom = 0;
        backdrop.style.backgroundColor = new Color(0.03f, 0.02f, 0.01f, 0.72f);
        backdrop.style.display = DisplayStyle.None;
        backdrop.RegisterCallback<ClickEvent>(_ => CloseLetter());
        root.Add(backdrop);

        // Main letter panel
        letterPanel = new VisualElement();
        letterPanel.style.position = Position.Absolute;
        letterPanel.style.left = Length.Percent(50);
        letterPanel.style.top = Length.Percent(50);
        letterPanel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        letterPanel.style.width = 600;
        letterPanel.style.maxWidth = Length.Percent(90);
        letterPanel.style.paddingLeft = 40;
        letterPanel.style.paddingRight = 40;
        letterPanel.style.paddingTop = 40;
        letterPanel.style.paddingBottom = 40;
        letterPanel.style.display = DisplayStyle.None;
        letterPanel.style.flexDirection = FlexDirection.Column;
        letterPanel.style.overflow = Overflow.Hidden;

        // Main paper look
        letterPanel.style.backgroundColor = new Color(0.89f, 0.83f, 0.67f, 1f);
        letterPanel.style.borderTopLeftRadius = 6;
        letterPanel.style.borderTopRightRadius = 4;
        letterPanel.style.borderBottomLeftRadius = 5;
        letterPanel.style.borderBottomRightRadius = 7;

        letterPanel.style.borderLeftWidth = 2;
        letterPanel.style.borderRightWidth = 2;
        letterPanel.style.borderTopWidth = 2;
        letterPanel.style.borderBottomWidth = 2;
        letterPanel.style.borderLeftColor = new Color(0.46f, 0.35f, 0.22f, 0.50f);
        letterPanel.style.borderRightColor = new Color(0.46f, 0.35f, 0.22f, 0.50f);
        letterPanel.style.borderTopColor = new Color(0.56f, 0.44f, 0.28f, 0.40f);
        letterPanel.style.borderBottomColor = new Color(0.33f, 0.24f, 0.14f, 0.60f);

        // Slight paper tilt
        letterPanel.style.rotate = new Rotate(new Angle(-0.8f, AngleUnit.Degree));

        AddPaperShadow(root);
        AddPaperWear(letterPanel);
        AddBurntEdgeOverlay(letterPanel);
        AddCornerTape(letterPanel);

        // Title
        Label titleLabel = new Label(letterTitle);
        titleLabel.style.fontSize = 28;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.color = new Color(0.24f, 0.16f, 0.08f, 1f);
        titleLabel.style.marginBottom = 12;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.letterSpacing = 1.1f;
        letterPanel.Add(titleLabel);

        // Divider
        VisualElement divider = new VisualElement();
        divider.style.height = 2;
        divider.style.backgroundColor = new Color(0.44f, 0.30f, 0.18f, 0.22f);
        divider.style.marginBottom = 18;
        divider.style.borderTopLeftRadius = 2;
        divider.style.borderTopRightRadius = 2;
        divider.style.borderBottomLeftRadius = 2;
        divider.style.borderBottomRightRadius = 2;
        letterPanel.Add(divider);

        // Intro stain / faded mark
        VisualElement stampFade = new VisualElement();
        stampFade.style.position = Position.Absolute;
        stampFade.style.left = 18;
        stampFade.style.top = 58;
        stampFade.style.width = 100;
        stampFade.style.height = 68;
        stampFade.style.backgroundColor = new Color(0.68f, 0.52f, 0.28f, 0.08f);
        stampFade.style.borderTopLeftRadius = 40;
        stampFade.style.borderTopRightRadius = 40;
        stampFade.style.borderBottomLeftRadius = 40;
        stampFade.style.borderBottomRightRadius = 40;
        stampFade.pickingMode = PickingMode.Ignore;
        letterPanel.Add(stampFade);

        // Letter content container
        letterContent_UI = new VisualElement();
        letterContent_UI.style.overflow = Overflow.Hidden;
        letterContent_UI.style.flexGrow = 1;

        Label contentLabel = new Label(letterContent);
        contentLabel.style.fontSize = 15;
        contentLabel.style.color = new Color(0.23f, 0.17f, 0.10f, 0.97f);
        contentLabel.style.whiteSpace = WhiteSpace.Normal;
        contentLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        contentLabel.style.marginBottom = 12;
        contentLabel.style.unityParagraphSpacing = 7;
        letterContent_UI.Add(contentLabel);

        letterPanel.Add(letterContent_UI);

        // Signature flourish
        Label signatureLabel = new Label("— Your Grandmother");
        signatureLabel.style.fontSize = 18;
        signatureLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        signatureLabel.style.color = new Color(0.31f, 0.19f, 0.11f, 0.84f);
        signatureLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        signatureLabel.style.marginTop = 10;
        signatureLabel.style.marginBottom = 20;
        letterPanel.Add(signatureLabel);

        // Button row
        VisualElement buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.Row;
        buttonRow.style.justifyContent = Justify.Center;
        buttonRow.style.alignItems = Align.Center;
        buttonRow.style.marginTop = 10;
        letterPanel.Add(buttonRow);

        // Close button
        closeButton = new Button(() => CloseLetter());
        closeButton.text = "Close";
        StyleButton(closeButton,
            new Color(0.60f, 0.50f, 0.34f, 0.92f),
            new Color(0.36f, 0.25f, 0.14f, 0.95f),
            new Color(0.95f, 0.91f, 0.82f, 1f));
        closeButton.style.marginRight = 12;
        buttonRow.Add(closeButton);

        // Go To Farm button
        goToFarmButton = new Button(RequestGoToFarm);
        goToFarmButton.text = "Go To Farm";
        StyleButton(goToFarmButton,
            new Color(0.43f, 0.28f, 0.14f, 0.96f),
            new Color(0.22f, 0.13f, 0.07f, 1f),
            new Color(0.98f, 0.93f, 0.84f, 1f));
        buttonRow.Add(goToFarmButton);

        root.Add(letterPanel);
    }

    private void StyleButton(Button button, Color fill, Color border, Color textColor)
    {
        button.style.minWidth = 135;
        button.style.height = 42;
        button.style.paddingLeft = 18;
        button.style.paddingRight = 18;
        button.style.paddingTop = 8;
        button.style.paddingBottom = 8;
        button.style.backgroundColor = fill;
        button.style.color = textColor;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.letterSpacing = 0.7f;

        button.style.borderLeftWidth = 2;
        button.style.borderRightWidth = 2;
        button.style.borderTopWidth = 2;
        button.style.borderBottomWidth = 2;

        button.style.borderLeftColor = border;
        button.style.borderRightColor = border;
        button.style.borderTopColor = new Color(
            Mathf.Clamp01(border.r + 0.08f),
            Mathf.Clamp01(border.g + 0.08f),
            Mathf.Clamp01(border.b + 0.08f),
            border.a);
        button.style.borderBottomColor = border;

        button.style.borderTopLeftRadius = 6;
        button.style.borderTopRightRadius = 6;
        button.style.borderBottomLeftRadius = 6;
        button.style.borderBottomRightRadius = 6;
    }

    private void AddPaperShadow(VisualElement root)
    {
        VisualElement shadow = new VisualElement();
        shadow.style.position = Position.Absolute;
        shadow.style.left = Length.Percent(50);
        shadow.style.top = Length.Percent(50);
        shadow.style.translate = new Translate(new Length(-49, LengthUnit.Percent), new Length(-48, LengthUnit.Percent));
        shadow.style.width = 600;
        shadow.style.maxWidth = Length.Percent(90);
        shadow.style.height = 100;
        shadow.style.backgroundColor = new Color(0.06f, 0.03f, 0.01f, 0.12f);
        shadow.style.borderTopLeftRadius = 14;
        shadow.style.borderTopRightRadius = 14;
        shadow.style.borderBottomLeftRadius = 14;
        shadow.style.borderBottomRightRadius = 14;
        shadow.style.rotate = new Rotate(new Angle(-0.8f, AngleUnit.Degree));
        shadow.style.display = DisplayStyle.None;
        shadow.name = "letter-shadow";
        root.Add(shadow);
    }

    private void AddPaperWear(VisualElement panel)
    {
        VisualElement grainOverlay = new VisualElement();
        grainOverlay.style.position = Position.Absolute;
        grainOverlay.style.left = 0;
        grainOverlay.style.top = 0;
        grainOverlay.style.right = 0;
        grainOverlay.style.bottom = 0;
        grainOverlay.style.backgroundColor = new Color(0.72f, 0.58f, 0.32f, 0.05f);
        grainOverlay.pickingMode = PickingMode.Ignore;
        panel.Insert(0, grainOverlay);

        VisualElement foldLine1 = new VisualElement();
        foldLine1.style.position = Position.Absolute;
        foldLine1.style.left = 22;
        foldLine1.style.right = 22;
        foldLine1.style.top = Length.Percent(34);
        foldLine1.style.height = 1;
        foldLine1.style.backgroundColor = new Color(0.30f, 0.22f, 0.13f, 0.07f);
        foldLine1.style.rotate = new Rotate(new Angle(-1.2f, AngleUnit.Degree));
        foldLine1.pickingMode = PickingMode.Ignore;
        panel.Add(foldLine1);

        VisualElement foldLine2 = new VisualElement();
        foldLine2.style.position = Position.Absolute;
        foldLine2.style.left = 16;
        foldLine2.style.right = 16;
        foldLine2.style.top = Length.Percent(58);
        foldLine2.style.height = 1;
        foldLine2.style.backgroundColor = new Color(0.26f, 0.18f, 0.10f, 0.08f);
        foldLine2.style.rotate = new Rotate(new Angle(0.9f, AngleUnit.Degree));
        foldLine2.pickingMode = PickingMode.Ignore;
        panel.Add(foldLine2);
    }

    private void AddCornerTape(VisualElement panel)
    {
        VisualElement tapeTL = new VisualElement();
        tapeTL.style.position = Position.Absolute;
        tapeTL.style.left = 18;
        tapeTL.style.top = 12;
        tapeTL.style.width = 62;
        tapeTL.style.height = 18;
        tapeTL.style.backgroundColor = new Color(0.87f, 0.78f, 0.58f, 0.18f);
        tapeTL.style.rotate = new Rotate(new Angle(-18f, AngleUnit.Degree));
        tapeTL.pickingMode = PickingMode.Ignore;
        panel.Add(tapeTL);

        VisualElement tapeTR = new VisualElement();
        tapeTR.style.position = Position.Absolute;
        tapeTR.style.right = 18;
        tapeTR.style.top = 10;
        tapeTR.style.width = 56;
        tapeTR.style.height = 18;
        tapeTR.style.backgroundColor = new Color(0.87f, 0.78f, 0.58f, 0.14f);
        tapeTR.style.rotate = new Rotate(new Angle(16f, AngleUnit.Degree));
        tapeTR.pickingMode = PickingMode.Ignore;
        panel.Add(tapeTR);
    }

    private void AddBurntEdgeOverlay(VisualElement panel)
    {
        VisualElement topEdge = new VisualElement();
        topEdge.style.position = Position.Absolute;
        topEdge.style.left = 0;
        topEdge.style.right = 0;
        topEdge.style.top = 0;
        topEdge.style.height = 10;
        topEdge.style.backgroundColor = new Color(0.24f, 0.15f, 0.08f, 0.08f);
        topEdge.pickingMode = PickingMode.Ignore;
        panel.Insert(0, topEdge);

        VisualElement bottomEdge = new VisualElement();
        bottomEdge.style.position = Position.Absolute;
        bottomEdge.style.left = 0;
        bottomEdge.style.right = 0;
        bottomEdge.style.bottom = 0;
        bottomEdge.style.height = 12;
        bottomEdge.style.backgroundColor = new Color(0.18f, 0.10f, 0.05f, 0.11f);
        bottomEdge.pickingMode = PickingMode.Ignore;
        panel.Insert(0, bottomEdge);

        VisualElement leftEdge = new VisualElement();
        leftEdge.style.position = Position.Absolute;
        leftEdge.style.left = 0;
        leftEdge.style.top = 0;
        leftEdge.style.bottom = 0;
        leftEdge.style.width = 10;
        leftEdge.style.backgroundColor = new Color(0.21f, 0.13f, 0.07f, 0.05f);
        leftEdge.pickingMode = PickingMode.Ignore;
        panel.Insert(0, leftEdge);

        VisualElement rightEdge = new VisualElement();
        rightEdge.style.position = Position.Absolute;
        rightEdge.style.right = 0;
        rightEdge.style.top = 0;
        rightEdge.style.bottom = 0;
        rightEdge.style.width = 10;
        rightEdge.style.backgroundColor = new Color(0.21f, 0.13f, 0.07f, 0.04f);
        rightEdge.pickingMode = PickingMode.Ignore;
        panel.Insert(0, rightEdge);

        VisualElement cornerTL = new VisualElement();
        cornerTL.style.position = Position.Absolute;
        cornerTL.style.left = -8;
        cornerTL.style.top = -8;
        cornerTL.style.width = 34;
        cornerTL.style.height = 34;
        cornerTL.style.backgroundColor = new Color(0.20f, 0.12f, 0.06f, 0.11f);
        cornerTL.style.borderTopLeftRadius = 28;
        cornerTL.style.borderTopRightRadius = 24;
        cornerTL.style.borderBottomLeftRadius = 24;
        cornerTL.style.borderBottomRightRadius = 16;
        cornerTL.pickingMode = PickingMode.Ignore;
        panel.Add(cornerTL);

        VisualElement cornerBR = new VisualElement();
        cornerBR.style.position = Position.Absolute;
        cornerBR.style.right = -10;
        cornerBR.style.bottom = -10;
        cornerBR.style.width = 40;
        cornerBR.style.height = 38;
        cornerBR.style.backgroundColor = new Color(0.16f, 0.09f, 0.04f, 0.13f);
        cornerBR.style.borderTopLeftRadius = 24;
        cornerBR.style.borderTopRightRadius = 28;
        cornerBR.style.borderBottomLeftRadius = 30;
        cornerBR.style.borderBottomRightRadius = 34;
        cornerBR.pickingMode = PickingMode.Ignore;
        panel.Add(cornerBR);
    }

    public void ShowLetter()
    {
        if (isOpen)
            return;

        isOpen = true;
        Debug.Log("[LetterPanel] Showing letter...");

        if (letterPanel != null)
        {
            letterPanel.style.display = DisplayStyle.Flex;
        }

        if (backdrop != null)
        {
            backdrop.style.display = DisplayStyle.Flex;
        }

        VisualElement shadow = uiDocument.rootVisualElement.Q<VisualElement>("letter-shadow");
        if (shadow != null)
        {
            shadow.style.display = DisplayStyle.Flex;
        }

        CharacterController2D player = FindFirstObjectByType<CharacterController2D>();
        if (player != null)
        {
            player.enabled = false;
        }

        StartCoroutine(AnimateLetterIn());
    }

    private void RequestGoToFarm()
    {
        goToFarmRequested = true;
        Debug.Log("[LetterPanel] Go To Farm requested by player.");
        CloseLetter();
    }

    public void CloseLetter()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[LetterPanel] Closing letter...");

        StartCoroutine(AnimateLetterOut());
    }

    private IEnumerator AnimateLetterIn()
    {
        if (letterPanel == null)
            yield break;

        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);
            letterPanel.style.scale = new Scale(new Vector2(scale, scale));

            VisualElement shadow = uiDocument.rootVisualElement.Q<VisualElement>("letter-shadow");
            if (shadow != null)
            {
                shadow.style.scale = new Scale(new Vector2(scale * 0.985f, scale * 0.985f));
            }

            yield return null;
        }

        letterPanel.style.scale = new Scale(Vector2.one);

        VisualElement finalShadow = uiDocument.rootVisualElement.Q<VisualElement>("letter-shadow");
        if (finalShadow != null)
        {
            finalShadow.style.scale = new Scale(Vector2.one);
        }
    }

    private IEnumerator AnimateLetterOut()
    {
        if (letterPanel == null)
            yield break;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0.8f, elapsed / duration);
            letterPanel.style.scale = new Scale(new Vector2(scale, scale));

            VisualElement shadow = uiDocument.rootVisualElement.Q<VisualElement>("letter-shadow");
            if (shadow != null)
            {
                shadow.style.scale = new Scale(new Vector2(scale * 0.985f, scale * 0.985f));
            }

            yield return null;
        }

        letterPanel.style.display = DisplayStyle.None;
        backdrop.style.display = DisplayStyle.None;

        VisualElement finalShadow = uiDocument.rootVisualElement.Q<VisualElement>("letter-shadow");
        if (finalShadow != null)
        {
            finalShadow.style.display = DisplayStyle.None;
        }

        CharacterController2D player = FindFirstObjectByType<CharacterController2D>();
        if (player != null)
        {
            player.enabled = true;
        }
    }

    public void SetLetterContent(string title, string content)
    {
        letterTitle = title;
        letterContent = content;

        if (uiDocument != null && uiDocument.rootVisualElement.childCount > 0)
        {
            uiDocument.rootVisualElement.Clear();
            CreateLetterUI();
        }
    }
}