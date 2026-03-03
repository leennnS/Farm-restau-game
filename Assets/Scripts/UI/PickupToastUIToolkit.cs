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
            Debug.LogError("[PickupToastUIToolkit] No UIDocument found!");
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
        toast.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
        toast.style.paddingLeft = 12;
        toast.style.paddingRight = 12;
        toast.style.paddingTop = 8;
        toast.style.paddingBottom = 8;
        toast.style.borderTopLeftRadius = 10;
        toast.style.borderTopRightRadius = 10;
        toast.style.borderBottomLeftRadius = 10;
        toast.style.borderBottomRightRadius = 10;
        toast.style.opacity = 0f;
        toast.style.display = DisplayStyle.None;

        toastLabel = new Label();
        toastLabel.style.color = Color.white;
        toastLabel.style.fontSize = 18;
        toastLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        toastLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        toast.Add(toastLabel);
        root.Add(toast);
    }

    public void Show(string message)
    {
        if (toast == null || toastLabel == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        toastLabel.text = message;
        currentRoutine = StartCoroutine(AnimateToast());
    }

    private IEnumerator AnimateToast()
    {
        toast.style.display = DisplayStyle.Flex;
        toast.style.opacity = 1f;
        toast.style.bottom = startBottom;

        float timer = 0f;

        while (timer < showTime)
        {
            timer += Time.deltaTime;
            float t = timer / showTime;

            toast.style.opacity = Mathf.Lerp(1f, 0f, t);
            toast.style.bottom = Mathf.Lerp(startBottom, endBottom, t);

            yield return null;
        }

        toast.style.opacity = 0f;
        toast.style.display = DisplayStyle.None;
        toast.style.bottom = startBottom;
    }
}