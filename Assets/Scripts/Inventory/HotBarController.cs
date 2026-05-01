using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HotBarController : MonoBehaviour
{
    private static HotBarController _instance;

    private const int HotbarSize = 12;

    private UIDocument _doc;
    private VisualElement _root;          // rootVisualElement
    private VisualElement _hotbarRoot;    // optional: "hotbarRoot" in your UXML
    private VisualElement[] _slots;
    private bool _requestedVisible = true;

    private const string FarmSceneName = "FarmScene";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        // If you named the top element "hotbarRoot" (you did), use it for show/hide
        _hotbarRoot = _root.Q<VisualElement>("hotbarRoot") ?? _root;

        CacheSlots();
        ClearAll();
        ApplySceneVisibility();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySceneVisibility();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneVisibility();
    }

    private void CacheSlots()
    {
        _slots = new VisualElement[HotbarSize];

        for (int i = 0; i < HotbarSize; i++)
        {
            int slotIndex = i;
            string name = $"hotbarSlot{(i + 1):00}";
            _slots[i] = _root.Q<VisualElement>(name);

            if (_slots[i] == null)
                Debug.LogWarning($"[HotBarHUD] Missing slot in HUD UXML: {name}");
            else
            {
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

    public void SetVisible(bool visible)
    {
        _requestedVisible = visible;
        ApplySceneVisibility();
    }

    private void ApplySceneVisibility()
    {
        if (_hotbarRoot == null) return;
        bool visibleInScene = string.Equals(SceneManager.GetActiveScene().name, FarmSceneName, System.StringComparison.Ordinal);
        _hotbarRoot.style.display = _requestedVisible && visibleInScene ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetSlot(int index, Sprite icon, int amount)
    {
        if (_slots == null || index < 0 || index >= HotbarSize) return;

        var slot = _slots[index];
        if (slot == null) return;

        if (icon == null || amount <= 0)
        {
            slot.style.backgroundImage = StyleKeyword.None;
            SetCount(slot, "");
            return;
        }

        slot.style.backgroundImage = new StyleBackground(icon);
        SetCount(slot, amount > 1 ? amount.ToString() : "");
    }

    public void ClearAll()
    {
        for (int i = 0; i < HotbarSize; i++)
            SetSlot(i, null, 0);
    }

    private void SetCount(VisualElement slot, string text)
    {
        var label = slot.Q<Label>("countLabel");
        if (label == null)
        {
            label = new Label { name = "countLabel" };
            label.style.position = Position.Absolute;
            label.style.right = 2;
            label.style.bottom = 0;
            label.style.fontSize = 11;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new Color(0.23f, 0.16f, 0.09f);
            label.pickingMode = PickingMode.Ignore;  // Allow mouse events to pass through to parent slot
            slot.Add(label);
        }
        label.text = text;
    }
}
