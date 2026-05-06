using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PickupToastUIToolkit : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float showTime = 1.2f;
    [SerializeField] private float startBottom = 90f;
    [SerializeField] private float endBottom = 125f;

    private VisualElement toast;
    private Label toastLabel;
    private Coroutine currentRoutine;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {

            return;
        }

        CreateToast();
    }

    private void CreateToast()
    {
        VisualElement root = uiDocument.rootVisualElement;

        toast = new VisualElement();
        toast.style.position = Position.Absolute;
        toast.style.left = Length.Percent(50);
        toast.style.bottom = startBottom;
        toast.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
        toast.style.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 0.92f);
        toast.style.paddingLeft = 18;
        toast.style.paddingRight = 18;
        toast.style.paddingTop = 12;
        toast.style.paddingBottom = 12;
        toast.style.borderTopLeftRadius = 10;
        toast.style.borderTopRightRadius = 10;
        toast.style.borderBottomLeftRadius = 10;
        toast.style.borderBottomRightRadius = 10;
        toast.style.opacity = 0f;
        toast.style.display = DisplayStyle.None;
        toast.style.maxWidth = 920f;
        toast.style.minWidth = 420f;

        toastLabel = new Label();
        toastLabel.style.color = Color.white;
        toastLabel.style.fontSize = 24;
        toastLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        toast.Add(toastLabel);
        root.Add(toast);
    }

    public void Show(string message)
    {
        Show(message, showTime, 18);
    }

    public void Show(string message, float duration)
    {
        Show(message, duration, 18);
    }

    public void Show(string message, float duration, int fontSize)
    {
        if (toast == null || toastLabel == null)
            return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        toastLabel.text = message;
        toastLabel.style.fontSize = fontSize;
        currentRoutine = StartCoroutine(AnimateToast(duration));
    }

    public void ShowPersistent(string message, int fontSize = 28)
    {
        if (toast == null || toastLabel == null)
            return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        toastLabel.text = message;
        toastLabel.style.fontSize = fontSize;
        toast.style.display = DisplayStyle.Flex;
        toast.style.opacity = 1f;
        toast.style.bottom = endBottom;
    }

    public void Hide()
    {
        if (toast == null)
            return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        toast.style.opacity = 0f;
        toast.style.display = DisplayStyle.None;
        toast.style.bottom = startBottom;
    }

    private IEnumerator AnimateToast(float duration)
    {
        toast.style.display = DisplayStyle.Flex;
        toast.style.opacity = 1f;
        toast.style.bottom = startBottom;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            toast.style.opacity = Mathf.Lerp(1f, 0f, t);
            toast.style.bottom = Mathf.Lerp(startBottom, endBottom, t);

            yield return null;
        }

        toast.style.opacity = 0f;
        toast.style.display = DisplayStyle.None;
        toast.style.bottom = startBottom;
        currentRoutine = null;
    }
}