using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Beautiful parchment letter UI panel.
/// Displays rustic inherited farm letter to player.
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

        // Backdrop (click to close)
        backdrop = new VisualElement();
        backdrop.style.position = Position.Absolute;
        backdrop.style.left = 0;
        backdrop.style.top = 0;
        backdrop.style.right = 0;
        backdrop.style.bottom = 0;
        backdrop.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
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

        // Parchment background
        letterPanel.style.backgroundColor = new Color(0.95f, 0.93f, 0.85f, 1f); // Beige
        letterPanel.style.borderTopLeftRadius = 3;
        letterPanel.style.borderTopRightRadius = 3;
        letterPanel.style.borderBottomLeftRadius = 3;
        letterPanel.style.borderBottomRightRadius = 3;

        // Add slight shadow / border effect for parchment
        letterPanel.style.borderLeftWidth = 1;
        letterPanel.style.borderRightWidth = 1;
        letterPanel.style.borderTopWidth = 1;
        letterPanel.style.borderBottomWidth = 1;
        letterPanel.style.borderLeftColor = new Color(0.7f, 0.65f, 0.5f, 0.5f);
        letterPanel.style.borderRightColor = new Color(0.7f, 0.65f, 0.5f, 0.5f);
        letterPanel.style.borderTopColor = new Color(0.7f, 0.65f, 0.5f, 0.5f);
        letterPanel.style.borderBottomColor = new Color(0.7f, 0.65f, 0.5f, 0.5f);

        // Burnt edges effect - add darker corners
        AddBurntEdgeOverlay(letterPanel);

        // Title
        Label titleLabel = new Label(letterTitle);
        titleLabel.style.fontSize = 28;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.color = new Color(0.3f, 0.25f, 0.15f, 1f);
        titleLabel.style.marginBottom = 20;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        letterPanel.Add(titleLabel);

        // Divider
        VisualElement divider = new VisualElement();
        divider.style.height = 1;
        divider.style.backgroundColor = new Color(0.7f, 0.65f, 0.5f, 0.4f);
        divider.style.marginBottom = 25;
        letterPanel.Add(divider);

        // Letter content
        letterContent_UI = new VisualElement();
        letterContent_UI.style.overflow = Overflow.Hidden;

        Label contentLabel = new Label(letterContent);
        contentLabel.style.fontSize = 14;
        contentLabel.style.color = new Color(0.3f, 0.25f, 0.15f, 1f);
        contentLabel.style.whiteSpace = WhiteSpace.Normal;
        contentLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        contentLabel.style.marginBottom = 8;
        letterContent_UI.Add(contentLabel);

        letterPanel.Add(letterContent_UI);

        // Close button
        closeButton = new Button(() => CloseLetter());
        closeButton.text = "Close";
        closeButton.style.marginTop = 30;
        closeButton.style.paddingLeft = 20;
        closeButton.style.paddingRight = 20;
        closeButton.style.paddingTop = 10;
        closeButton.style.paddingBottom = 10;
        closeButton.style.backgroundColor = new Color(0.7f, 0.65f, 0.5f, 0.8f);
        closeButton.style.borderLeftWidth = 0;
        closeButton.style.borderRightWidth = 0;
        closeButton.style.borderTopWidth = 0;
        closeButton.style.borderBottomWidth = 0;
        closeButton.style.borderTopLeftRadius = 3;
        closeButton.style.borderTopRightRadius = 3;
        closeButton.style.borderBottomLeftRadius = 3;
        closeButton.style.borderBottomRightRadius = 3;
        closeButton.style.color = Color.white;
        letterPanel.Add(closeButton);

        // Continue button - player manually decides when to leave intro.
        goToFarmButton = new Button(RequestGoToFarm);
        goToFarmButton.text = "Go To Farm";
        goToFarmButton.style.marginTop = 10;
        goToFarmButton.style.paddingLeft = 20;
        goToFarmButton.style.paddingRight = 20;
        goToFarmButton.style.paddingTop = 10;
        goToFarmButton.style.paddingBottom = 10;
        goToFarmButton.style.backgroundColor = new Color(0.45f, 0.35f, 0.2f, 0.95f);
        goToFarmButton.style.borderLeftWidth = 0;
        goToFarmButton.style.borderRightWidth = 0;
        goToFarmButton.style.borderTopWidth = 0;
        goToFarmButton.style.borderBottomWidth = 0;
        goToFarmButton.style.borderTopLeftRadius = 3;
        goToFarmButton.style.borderTopRightRadius = 3;
        goToFarmButton.style.borderBottomLeftRadius = 3;
        goToFarmButton.style.borderBottomRightRadius = 3;
        goToFarmButton.style.color = Color.white;
        letterPanel.Add(goToFarmButton);

        root.Add(letterPanel);
    }

    private void AddBurntEdgeOverlay(VisualElement panel)
    {
        // Create dark corners for burnt effect
        VisualElement burnOverlay = new VisualElement();
        burnOverlay.style.position = Position.Absolute;
        burnOverlay.style.left = 0;
        burnOverlay.style.top = 0;
        burnOverlay.style.right = 0;
        burnOverlay.style.bottom = 0;
        burnOverlay.pickingMode = PickingMode.Ignore;

        // This is a simplified burnt edge - could be enhanced with actual sprites
        burnOverlay.style.backgroundColor = new Color(0, 0, 0, 0);
        panel.Insert(0, burnOverlay);
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

        // Disable player interaction while letter is open
        CharacterController2D player = FindFirstObjectByType<CharacterController2D>();
        if (player != null)
        {
            player.enabled = false;
        }

        // Optional: Add animation
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

        // Scale up from small
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);
            letterPanel.style.scale = new Scale(new Vector2(scale, scale));
            yield return null;
        }

        letterPanel.style.scale = new Scale(Vector2.one);
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
            yield return null;
        }

        letterPanel.style.display = DisplayStyle.None;
        backdrop.style.display = DisplayStyle.None;

        // Re-enable player
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

        // Recreate UI if already created
        if (uiDocument != null && uiDocument.rootVisualElement.childCount > 0)
        {
            uiDocument.rootVisualElement.Clear();
            CreateLetterUI();
        }
    }
}
