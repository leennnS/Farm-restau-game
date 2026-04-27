using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HotBarHUDController : MonoBehaviour
{
    private static HotBarHUDController _instance;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement[] _slots;

    private const int HotbarSize = 12;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {

            return;
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null)
        {

            return;
        }

        CacheSlots();

        // Ensure hotbar is visible at startup
        SetVisible(true);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void CacheSlots()
    {
        _slots = new VisualElement[HotbarSize];

        for (int i = 0; i < HotbarSize; i++)
        {
            int slotIndex = i;
            string slotName = $"hotbarSlot{(i + 1):00}";
            _slots[i] = _root.Q<VisualElement>(slotName);

            if (_slots[i] != null)
            {
                _slots[i].style.position = Position.Relative;
                _slots[i].pickingMode = PickingMode.Position;
                _slots[i].RegisterCallback<ClickEvent>(_ => SelectHotbarSlot(slotIndex));
            }
        }
    }

    private void SelectHotbarSlot(int index)
    {
        FarmingInputHandler input = FindFirstObjectByType<FarmingInputHandler>();
        if (input != null)
            input.SetSelectedHotbarSlot(index);
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
            countLabel.pickingMode = PickingMode.Ignore;
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

