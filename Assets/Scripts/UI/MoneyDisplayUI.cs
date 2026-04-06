using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Displays global money on a UI Toolkit label.
/// Attach this to a GameObject with UIDocument.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MoneyDisplayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string labelName = "money-value";
    [SerializeField] private string suffix = " G";

    private Label _moneyLabel;

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {

            return;
        }

        _moneyLabel = uiDocument.rootVisualElement.Q<Label>(labelName);

        if (_moneyLabel == null)
            return;
    }

    private void OnEnable()
    {
        MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (MoneyManager.HasInstance)
            MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_moneyLabel == null)
            return;

        _moneyLabel.text = $"{MoneyManager.Instance.CurrentMoney}{suffix}";
    }
}