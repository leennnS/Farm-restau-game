using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HotBarHUDController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement[] _slots;

    private const int HotbarSize = 12;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("HotBarHUDController: UIDocument component not found!");
            return;
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("HotBarHUDController: Root visual element is null!");
            return;
        }

        CacheSlots();

        // Ensure hotbar is visible at startup
        SetVisible(true);
    }

    private void CacheSlots()
    {
        _slots = new VisualElement[HotbarSize];

        for (int i = 0; i < HotbarSize; i++)
        {
            string slotName = $"hotbarSlot{(i + 1):00}";
            _slots[i] = _root.Q<VisualElement>(slotName);

            if (_slots[i] != null)
                _slots[i].style.position = Position.Relative;
        }
    }

    public void SetSlot(int index, Sprite icon, int amount)
    {
        if (_slots == null || index < 0 || index >= _slots.Length)
            return;

        var slot = _slots[index];
        if (slot == null)
            return;

        bool hasItem = icon != null && amount > 0;

        if (!hasItem)
        {
            slot.style.backgroundImage = StyleKeyword.None;
            SetSlotCount(slot, "");
            return;
        }

        slot.style.backgroundImage = new StyleBackground(icon);
        SetSlotCount(slot, amount > 1 ? amount.ToString() : "");
    }

    private void SetSlotCount(VisualElement slot, string text)
    {
        var countLabel = slot.Q<Label>("countLabel");
        if (countLabel == null)
        {
            countLabel = new Label { name = "countLabel" };
            countLabel.style.position = Position.Absolute;
            countLabel.style.right = 2;
            countLabel.style.bottom = 0;
            countLabel.style.fontSize = 11;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            countLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
            slot.Add(countLabel);
        }

        countLabel.text = text;
    }

    public void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

