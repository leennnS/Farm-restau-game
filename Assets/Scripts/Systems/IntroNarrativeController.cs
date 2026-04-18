using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Handles all narrative text display during intro with typewriter effect.
/// Also manages objective hints.
/// </summary>
public class IntroNarrativeController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI narrativeText;

    [SerializeField]
    private TextMeshProUGUI hintText;

    [SerializeField]
    private float typewriterSpeed = 0.05f;

    [SerializeField]
    private float fadeInDuration = 0.5f;

    [SerializeField]
    private CanvasGroup narrativeCanvasGroup;

    private Coroutine typewriterCoroutine;
    private string[] openingLines = new string[]
    {
         "Cold wood presses against your back.",
        "You wake up in the old shed, alone.",
        "For a moment, you do not remember how you got here.",
        "Someone must have carried you inside during the night.",
        "Your head feels heavy. The room is dark. Too dark.",
        "You need light.",
        "Take the lantern and search the room."
    };

    private void Start()
    {
        if (narrativeText == null)
        {
            Debug.LogWarning("[IntroNarrative] Narrative text not assigned!");
        }

        if (narrativeCanvasGroup == null && narrativeText != null)
        {
            narrativeCanvasGroup = narrativeText.GetComponentInParent<CanvasGroup>();
        }

        // Fade in from transparent
        if (narrativeCanvasGroup != null)
        {
            narrativeCanvasGroup.alpha = 0f;
        }
    }

    public IEnumerator PlayOpening()
    {
        if (narrativeText == null)
            yield break;

        // Fade in canvas
        if (narrativeCanvasGroup != null)
        {
            yield return FadeCanvasGroup(narrativeCanvasGroup, 0f, 1f, fadeInDuration);
        }

        yield return new WaitForSeconds(0.3f);

        // Play each opening line with typewriter effect
        foreach (string line in openingLines)
        {
            yield return StartCoroutine(TypewriteLine(line));
            yield return new WaitForSeconds(1.2f);
        }

        // Fade out
        if (narrativeCanvasGroup != null)
        {
            yield return FadeCanvasGroup(narrativeCanvasGroup, 1f, 0f, fadeInDuration);
        }

        narrativeText.text = "";
    }

    public void ShowHint(string hintText)
    {
        if (this.hintText != null)
        {
            StopAllCoroutines();
            StartCoroutine(DisplayHintWithFade(hintText));
        }
    }

    private IEnumerator DisplayHintWithFade(string text)
    {
        CanvasGroup hintCanvasGroup = hintText.GetComponentInParent<CanvasGroup>();

        if (hintCanvasGroup != null)
        {
            hintCanvasGroup.alpha = 0f;
        }

        hintText.text = text;

        if (hintCanvasGroup != null)
        {
            yield return FadeCanvasGroup(hintCanvasGroup, 0f, 1f, 0.5f);
            yield return new WaitForSeconds(3f);
            yield return FadeCanvasGroup(hintCanvasGroup, 1f, 0f, 0.5f);
        }

        hintText.text = "";
    }

    private IEnumerator TypewriteLine(string text)
    {
        narrativeText.text = "";

        foreach (char character in text)
        {
            narrativeText.text += character;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }

    public void ClearText()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        narrativeText.text = "";
    }
}
