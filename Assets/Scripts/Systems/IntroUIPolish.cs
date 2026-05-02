using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Polishes the intro scene UI with centered narrative text and phase-aware hints.
/// Hides the intro UI once the character starts moving.
/// </summary>
public class IntroUIPolish : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI narrativeText;

    [SerializeField]
    private Transform playerCharacter;

    [SerializeField]
    private TextMeshProUGUI hintText;

    private GameObject narrativeTextObject;
    private GameObject hintContainer;
    private Vector3 lastPlayerPos;
    private bool uiHidden = false;

    private void Awake()
    {
        if (narrativeText == null)
        {
            // Try to find narrative text in children
            TextMeshProUGUI[] textComps = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var tc in textComps)
            {
                if (!tc.name.Contains("Hint") && !tc.name.Contains("Continue"))
                {
                    narrativeText = tc;
                    break;
                }
            }
        }

        // Find hint text
        if (hintText == null)
        {
            TextMeshProUGUI[] textComps = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var tc in textComps)
            {
                if (tc.name.Contains("Hint") || tc.name.Contains("Continue"))
                {
                    hintText = tc;
                    break;
                }
            }
        }

        // Find player character if not assigned
        if (playerCharacter == null)
        {
            CharacterController2D charController = FindFirstObjectByType<CharacterController2D>();
            if (charController != null)
                playerCharacter = charController.transform;
        }

        if (playerCharacter != null)
            lastPlayerPos = playerCharacter.position;

        if (narrativeText == null)
        {
            Debug.LogError("[IntroUIPolish] Could not find narrative text! Please assign manually.");
            return;
        }

        SetupDialoguePanel();
        if (hintText != null)
            SetupContinuePrompt();
    }

    private void Update()
    {
        // Check if player has moved
        if (!uiHidden && playerCharacter != null)
        {
            if (Vector3.Distance(playerCharacter.position, lastPlayerPos) > 0.1f)
            {
                HideUI();
            }
        }
    }

    private void HideUI()
    {
        if (narrativeTextObject != null)
            narrativeTextObject.SetActive(false);
        if (hintContainer != null)
            hintContainer.SetActive(false);
        else if (hintText != null)
            hintText.gameObject.SetActive(false);
        uiHidden = true;
    }

    private void SetupDialoguePanel()
    {
        // Just center the narrative text without a panel
        RectTransform textRect = narrativeText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(-85f, 285f);
        textRect.sizeDelta = new Vector2(920f, 140f);

        // Style the narrative text
        narrativeText.color = new Color(0.95f, 0.92f, 0.85f, 1f);
        narrativeText.fontSize = 30;
        narrativeText.enableAutoSizing = true;
        narrativeText.fontSizeMin = 18f;
        narrativeText.fontSizeMax = 30f;
        narrativeText.alignment = TextAlignmentOptions.Center;
        narrativeText.textWrappingMode = TextWrappingModes.Normal;
        narrativeText.raycastTarget = false;

        narrativeTextObject = narrativeText.gameObject;
    }

    private void SetupContinuePrompt()
    {
        // Set the hint text content - only show space prompt initially
        hintText.text = "Press Space to continue";

        // Create a container for the continue prompt with decorative elements
        GameObject containerGO = new GameObject("ContinuePromptContainer");
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.SetParent(hintText.transform.parent);
        containerRect.localScale = Vector3.one;

        // Position at bottom center
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 28f);
        containerRect.sizeDelta = new Vector2(560f, 72f);

        // Add subtle background panel for the bottom hint only.
        Image bgImage = containerGO.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.045f, 0.04f, 0.68f);

        // Add layout group
        VerticalLayoutGroup layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 8f;
        layoutGroup.padding = new RectOffset(20, 20, 15, 15);

        // Add layout element for sizing
        LayoutElement layoutElem = containerGO.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = 560f;
        layoutElem.preferredHeight = 72f;

        // Create decorative separators above text
        CreateDecorator(containerGO, "SeparatorTop");

        // Move hint text into container
        hintText.transform.SetParent(containerRect);
        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = Vector2.one;
        hintRect.sizeDelta = Vector2.zero;

        // Style the hint text
        hintText.color = new Color(0.85f, 0.8f, 0.7f, 0.9f);
        hintText.fontSize = 22;
        hintText.enableAutoSizing = true;
        hintText.fontSizeMin = 14f;
        hintText.fontSizeMax = 22f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.textWrappingMode = TextWrappingModes.NoWrap;
        hintText.raycastTarget = false;

        // Add decorative separator below text
        CreateDecorator(containerGO, "SeparatorBottom");

        hintContainer = containerGO;
    }

    private void CreateDecorator(GameObject parent, string name)
    {
        GameObject decorGO = new GameObject(name);
        RectTransform decorRect = decorGO.AddComponent<RectTransform>();
        decorRect.SetParent(parent.transform);
        decorRect.localPosition = Vector3.zero;

        Image decorImage = decorGO.AddComponent<Image>();
        decorImage.color = new Color(0.5f, 0.38f, 0.22f, 0.4f);

        LayoutElement layoutElem = decorGO.AddComponent<LayoutElement>();
        layoutElem.preferredHeight = 1f;
    }
}
