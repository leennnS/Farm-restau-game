using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MarketUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MarketInventoryBridge inventoryBridge;

    [Header("Items")]
    [SerializeField] private MarketItemEntry[] seedItems;
    [SerializeField] private MarketItemEntry[] toolItems;
    [SerializeField] private MarketItemEntry[] fruitItems;
    [SerializeField] private MarketItemEntry[] produceItems;

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private VisualElement marketRoot;
    private Label marketSubtitle;
    private Label moneyValue;
    private Label interactionHint;

    private Button closeButton;
    private Button seedsTabButton;
    private Button toolsTabButton;
    private Button fruitsTabButton;
    private Button produceTabButton;

    private VisualElement seedsSection;
    private VisualElement toolsSection;
    private VisualElement fruitsSection;
    private VisualElement produceSection;

    private VisualElement seedsGrid;
    private VisualElement toolsGrid;
    private VisualElement fruitsGrid;
    private VisualElement produceGrid;

    private readonly Dictionary<MarketSectionType, VisualElement> sectionLookup = new();
    private readonly Dictionary<MarketSectionType, Button> tabLookup = new();

    private MarketSectionType currentSection = MarketSectionType.Seeds;

    public bool IsOpen => !marketRoot.ClassListContains("hidden");

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;

        marketRoot = root.Q<VisualElement>("market-root");
        marketSubtitle = root.Q<Label>("market-subtitle");
        moneyValue = root.Q<Label>("money-value");
        interactionHint = root.Q<Label>("interaction-hint");

        closeButton = root.Q<Button>("close-button");
        seedsTabButton = root.Q<Button>("tab-seeds");
        toolsTabButton = root.Q<Button>("tab-tools");
        fruitsTabButton = root.Q<Button>("tab-fruits");
        produceTabButton = root.Q<Button>("tab-produce");

        seedsSection = root.Q<VisualElement>("section-seeds");
        toolsSection = root.Q<VisualElement>("section-tools");
        fruitsSection = root.Q<VisualElement>("section-fruits");
        produceSection = root.Q<VisualElement>("section-produce");

        seedsGrid = root.Q<VisualElement>("grid-seeds");
        toolsGrid = root.Q<VisualElement>("grid-tools");
        fruitsGrid = root.Q<VisualElement>("grid-fruits");
        produceGrid = root.Q<VisualElement>("grid-produce");

        sectionLookup[MarketSectionType.Seeds] = seedsSection;
        sectionLookup[MarketSectionType.Tools] = toolsSection;
        sectionLookup[MarketSectionType.Fruits] = fruitsSection;
        sectionLookup[MarketSectionType.Produce] = produceSection;

        tabLookup[MarketSectionType.Seeds] = seedsTabButton;
        tabLookup[MarketSectionType.Tools] = toolsTabButton;
        tabLookup[MarketSectionType.Fruits] = fruitsTabButton;
        tabLookup[MarketSectionType.Produce] = produceTabButton;

        closeButton.clicked += CloseMarket;
        seedsTabButton.clicked += () => OpenSection(MarketSectionType.Seeds);
        toolsTabButton.clicked += () => OpenSection(MarketSectionType.Tools);
        fruitsTabButton.clicked += () => OpenSection(MarketSectionType.Fruits);
        produceTabButton.clicked += () => OpenSection(MarketSectionType.Produce);

        PopulateAllSections();
        RefreshMoney();

        MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;

        CloseMarketInstant();
        SetInteractionHint(string.Empty, false);
    }

    private void OnDestroy()
    {
        if (MoneyManager.HasInstance)
            MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(closeKey))
            CloseMarket();
    }

    private void HandleMoneyChanged(int newAmount)
    {
        RefreshMoney();
        PopulateAllSections();
    }

    public void OpenSection(MarketSectionType section)
    {
        currentSection = section;

        marketRoot.RemoveFromClassList("hidden");
        SetInteractionHint(string.Empty, false);

        foreach (KeyValuePair<MarketSectionType, VisualElement> pair in sectionLookup)
        {
            if (pair.Key == section)
                pair.Value.RemoveFromClassList("hidden");
            else
                pair.Value.AddToClassList("hidden");
        }

        foreach (KeyValuePair<MarketSectionType, Button> pair in tabLookup)
        {
            pair.Value.RemoveFromClassList("active-tab");
        }

        if (tabLookup.TryGetValue(section, out Button activeButton))
            activeButton.AddToClassList("active-tab");

        marketSubtitle.text = GetSubtitle(section);
        RefreshMoney();
    }

    public void CloseMarket()
    {
        marketRoot.AddToClassList("hidden");
    }

    public void CloseMarketInstant()
    {
        marketRoot.AddToClassList("hidden");
    }

    public void SetInteractionHint(string message, bool show)
    {
        interactionHint.text = message;

        if (show)
            interactionHint.RemoveFromClassList("hidden");
        else
            interactionHint.AddToClassList("hidden");
    }

    private string GetSubtitle(MarketSectionType section)
    {
        return section switch
        {
            MarketSectionType.Seeds => "Choose seeds for your next harvest.",
            MarketSectionType.Tools => "Pick the right equipment for the job.",
            MarketSectionType.Fruits => "Fresh fruits ready for sale or cooking.",
            MarketSectionType.Produce => "Fish, meat, and fresh market goods.",
            _ => "Browse the market."
        };
    }

    private void RefreshMoney()
    {
        if (moneyValue == null)
            return;

        int money = MoneyManager.Instance.CurrentMoney;
        moneyValue.text = $"{money}";
    }

    private void PopulateAllSections()
    {
        PopulateSection(seedsGrid, seedItems, MarketSectionType.Seeds);
        PopulateSection(toolsGrid, toolItems, MarketSectionType.Tools);
        PopulateSection(fruitsGrid, fruitItems, MarketSectionType.Fruits);
        PopulateSection(produceGrid, produceItems, MarketSectionType.Produce);
    }

    private void PopulateSection(VisualElement grid, MarketItemEntry[] items, MarketSectionType sectionType)
    {
        grid.Clear();

        if (items == null)
            return;

        foreach (MarketItemEntry item in items)
        {
            if (item == null || !item.available)
                continue;

            grid.Add(CreateItemCard(item, sectionType));
        }
    }

    private VisualElement CreateItemCard(MarketItemEntry item, MarketSectionType sectionType)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("item-card");
        card.AddToClassList(GetCardClass(sectionType));

        VisualElement icon = new VisualElement();
        icon.AddToClassList("item-icon");

        if (item.icon != null)
            icon.style.backgroundImage = new StyleBackground(item.icon);

        Label nameLabel = new Label(item.itemName);
        nameLabel.AddToClassList("item-name");

        Label priceLabel = new Label($"{item.price} G");
        priceLabel.AddToClassList("item-price");

        Button buyButton = new Button(() => TryBuy(item))
        {
            text = "Buy"
        };
        buyButton.AddToClassList("buy-button");

        bool canAfford = MoneyManager.Instance.CanAfford(item.price);
        buyButton.SetEnabled(canAfford);

        card.Add(icon);
        card.Add(nameLabel);
        card.Add(priceLabel);
        card.Add(buyButton);

        return card;
    }

    private string GetCardClass(MarketSectionType sectionType)
    {
        return sectionType switch
        {
            MarketSectionType.Seeds => "seeds-card",
            MarketSectionType.Tools => "tools-card",
            MarketSectionType.Fruits => "fruits-card",
            MarketSectionType.Produce => "produce-card",
            _ => "seeds-card"
        };
    }

    private void TryBuy(MarketItemEntry item)
    {
        if (!MoneyManager.Instance.SpendMoney(item.price))
        {
            marketSubtitle.text = "Not enough coins.";
            return;
        }

        if (inventoryBridge != null)
            inventoryBridge.ReceivePurchase(item, 1);
        else
            Debug.LogWarning("MarketUIController: MarketInventoryBridge is missing.");

        marketSubtitle.text = $"Purchased {item.itemName}.";
        RefreshMoney();
        PopulateAllSections();
    }
}