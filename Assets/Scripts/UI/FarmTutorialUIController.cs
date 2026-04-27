using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight UI presenter for farm tutorial messages with fade transitions.
/// </summary>
public class FarmTutorialUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Runtime Style")]
    [SerializeField] private Color panelColor = new Color(0.08f, 0.06f, 0.04f, 0.58f);
    [SerializeField] private Color textColor = new Color(0.96f, 0.92f, 0.82f, 0.98f);
    [SerializeField] private int runtimeFontSize = 28;

    private Coroutine fadeRoutine;

    public bool IsReady => canvasGroup != null && tutorialText != null;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (tutorialText == null)
            tutorialText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (canvasGroup == null || tutorialText == null)
            CreateRuntimeUI();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void CreateRuntimeUI()
    {
        GameObject canvasGO = new GameObject("FarmTutorialCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<CanvasScaler>();

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        GameObject panelGO = new GameObject("FarmTutorialPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 24f);
        panelRect.sizeDelta = new Vector2(1120f, 190f);

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = panelColor;

        GameObject textGO = new GameObject("FarmTutorialText");
        textGO.transform.SetParent(panelGO.transform, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(34f, 18f);
        rect.offsetMax = new Vector2(-34f, -18f);

        tutorialText = textGO.AddComponent<TextMeshProUGUI>();
        tutorialText.fontSize = runtimeFontSize;
        tutorialText.alignment = TextAlignmentOptions.Center;
        tutorialText.textWrappingMode = TextWrappingModes.Normal;
        tutorialText.overflowMode = TextOverflowModes.Overflow;
        tutorialText.color = textColor;
        tutorialText.outlineWidth = 0.12f;
        tutorialText.outlineColor = new Color(0f, 0f, 0f, 0.55f);
    }

    public void ShowMessage(string message)
    {
        if (!IsReady)
        {
            Debug.LogWarning("[FarmTutorialUI] Missing CanvasGroup or TextMeshProUGUI reference.");
            return;
        }

        tutorialText.text = message;
        StartFade(1f);
    }

    public void HideMessage()
    {
        if (!IsReady)
            return;

        StartFade(0f);
    }

    private void StartFade(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(target));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        canvasGroup.alpha = target;
    }
}
