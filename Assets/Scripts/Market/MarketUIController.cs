using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MarketUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MarketInventoryBridge inventoryBridge;

    [Header("Items")]
    [FormerlySerializedAs("seedItems")]
    [SerializeField] private MarketItemEntry[] seedsEntries;
    [FormerlySerializedAs("toolItems")]
    [SerializeField] private MarketItemEntry[] toolsEntries;
    [FormerlySerializedAs("fruitVegetableItems")]
    [SerializeField] private MarketItemEntry[] fruitsVegetablesEntries;
    [FormerlySerializedAs("fishSeafoodItems")]
    [SerializeField] private MarketItemEntry[] fishSeafoodEntries;
    [FormerlySerializedAs("meatPoultryItems")]
    [SerializeField] private MarketItemEntry[] meatPoultryEntries;
    [FormerlySerializedAs("drinkItems")]
    [SerializeField] private MarketItemEntry[] drinksEntries;
    [FormerlySerializedAs("bakingDairyItems")]
    [SerializeField] private MarketItemEntry[] breadDairySweetenersEntries;

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private VisualElement marketRoot;
    private Label marketSubtitle;
    private Label moneyValue;
    private Label interactionHint;

    private Button closeButton;
    private Button seedsTabButton;
    private Button toolsTabButton;
    private Button fruitsVegetablesTabButton;
    private Button fishSeafoodTabButton;
    private Button meatPoultryTabButton;
    private Button drinksTabButton;
    private Button breadDairySweetenersTabButton;

    private VisualElement seedsSection;
    private VisualElement toolsSection;
    private VisualElement fruitsVegetablesSection;
    private VisualElement fishSeafoodSection;
    private VisualElement meatPoultrySection;
    private VisualElement drinksSection;
    private VisualElement breadDairySweetenersSection;

    private VisualElement seedsGrid;
    private VisualElement toolsGrid;
    private VisualElement fruitsVegetablesGrid;
    private VisualElement fishSeafoodGrid;
    private VisualElement meatPoultryGrid;
    private VisualElement drinksGrid;
    private VisualElement breadDairySweetenersGrid;

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
        fruitsVegetablesTabButton = root.Q<Button>("tab-fruits-vegetables");
        fishSeafoodTabButton = root.Q<Button>("tab-fish-seafood");
        meatPoultryTabButton = root.Q<Button>("tab-meat-poultry");
        drinksTabButton = root.Q<Button>("tab-drinks");
        breadDairySweetenersTabButton = root.Q<Button>("tab-baking-dairy");

        seedsSection = root.Q<VisualElement>("section-seeds");
        toolsSection = root.Q<VisualElement>("section-tools");
        fruitsVegetablesSection = root.Q<VisualElement>("section-fruits-vegetables");
        fishSeafoodSection = root.Q<VisualElement>("section-fish-seafood");
        meatPoultrySection = root.Q<VisualElement>("section-meat-poultry");
        drinksSection = root.Q<VisualElement>("section-drinks");
        breadDairySweetenersSection = root.Q<VisualElement>("section-baking-dairy");

        seedsGrid = root.Q<VisualElement>("grid-seeds");
        toolsGrid = root.Q<VisualElement>("grid-tools");
        fruitsVegetablesGrid = root.Q<VisualElement>("grid-fruits-vegetables");
        fishSeafoodGrid = root.Q<VisualElement>("grid-fish-seafood");
        meatPoultryGrid = root.Q<VisualElement>("grid-meat-poultry");
        drinksGrid = root.Q<VisualElement>("grid-drinks");
        breadDairySweetenersGrid = root.Q<VisualElement>("grid-baking-dairy");

        sectionLookup[MarketSectionType.Seeds] = seedsSection;
        sectionLookup[MarketSectionType.Tools] = toolsSection;
        sectionLookup[MarketSectionType.FruitsAndVegetables] = fruitsVegetablesSection;
        sectionLookup[MarketSectionType.FishAndSeafood] = fishSeafoodSection;
        sectionLookup[MarketSectionType.MeatAndPoultry] = meatPoultrySection;
        sectionLookup[MarketSectionType.Drinks] = drinksSection;
        sectionLookup[MarketSectionType.BreadDairySweeteners] = breadDairySweetenersSection;

        tabLookup[MarketSectionType.Seeds] = seedsTabButton;
        tabLookup[MarketSectionType.Tools] = toolsTabButton;
        tabLookup[MarketSectionType.FruitsAndVegetables] = fruitsVegetablesTabButton;
        tabLookup[MarketSectionType.FishAndSeafood] = fishSeafoodTabButton;
        tabLookup[MarketSectionType.MeatAndPoultry] = meatPoultryTabButton;
        tabLookup[MarketSectionType.Drinks] = drinksTabButton;
        tabLookup[MarketSectionType.BreadDairySweeteners] = breadDairySweetenersTabButton;

        closeButton.clicked += CloseMarket;
        seedsTabButton.clicked += () => OpenSection(MarketSectionType.Seeds);
        toolsTabButton.clicked += () => OpenSection(MarketSectionType.Tools);
        fruitsVegetablesTabButton.clicked += () => OpenSection(MarketSectionType.FruitsAndVegetables);
        fishSeafoodTabButton.clicked += () => OpenSection(MarketSectionType.FishAndSeafood);
        meatPoultryTabButton.clicked += () => OpenSection(MarketSectionType.MeatAndPoultry);
        drinksTabButton.clicked += () => OpenSection(MarketSectionType.Drinks);
        breadDairySweetenersTabButton.clicked += () => OpenSection(MarketSectionType.BreadDairySweeteners);

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
        currentSection = NormalizeSection(section);

        marketRoot.RemoveFromClassList("hidden");
        SetInteractionHint(string.Empty, false);

        foreach (KeyValuePair<MarketSectionType, VisualElement> pair in sectionLookup)
        {
            if (pair.Key == currentSection)
                pair.Value.RemoveFromClassList("hidden");
            else
                pair.Value.AddToClassList("hidden");
        }

        foreach (KeyValuePair<MarketSectionType, Button> pair in tabLookup)
        {
            pair.Value.RemoveFromClassList("active-tab");
        }

        if (tabLookup.TryGetValue(currentSection, out Button activeButton))
            activeButton.AddToClassList("active-tab");

        marketSubtitle.text = GetSubtitle(currentSection);
        RefreshMoney();
    }

    private static MarketSectionType NormalizeSection(MarketSectionType section)
    {
        // Map legacy serialized enum values that may still exist in scene objects.
        return (int)section switch
        {
            2 => MarketSectionType.FruitsAndVegetables,
            3 => MarketSectionType.FishAndSeafood,
            _ => section
        };
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
            MarketSectionType.FruitsAndVegetables => "Fresh fruits and vegetables for daily cooking.",
            MarketSectionType.FishAndSeafood => "Fresh fish and seafood from the market.",
            MarketSectionType.MeatAndPoultry => "Quality meat and poultry cuts.",
            MarketSectionType.Drinks => "Refreshing drinks and juices.",
            MarketSectionType.BreadDairySweeteners => "Bread, dairy, and sweeteners for your pantry.",
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
        PopulateSection(seedsGrid, seedsEntries, MarketSectionType.Seeds);
        PopulateSection(toolsGrid, toolsEntries, MarketSectionType.Tools);
        PopulateSection(fruitsVegetablesGrid, ResolveItemsForSection(MarketSectionType.FruitsAndVegetables), MarketSectionType.FruitsAndVegetables);
        PopulateSection(fishSeafoodGrid, ResolveItemsForSection(MarketSectionType.FishAndSeafood), MarketSectionType.FishAndSeafood);
        PopulateSection(meatPoultryGrid, ResolveItemsForSection(MarketSectionType.MeatAndPoultry), MarketSectionType.MeatAndPoultry);
        PopulateSection(drinksGrid, ResolveItemsForSection(MarketSectionType.Drinks), MarketSectionType.Drinks);
        PopulateSection(breadDairySweetenersGrid, ResolveItemsForSection(MarketSectionType.BreadDairySweeteners), MarketSectionType.BreadDairySweeteners);
    }

    private MarketItemEntry[] ResolveItemsForSection(MarketSectionType section)
    {
        return section switch
        {
            MarketSectionType.FruitsAndVegetables => fruitsVegetablesEntries,
            MarketSectionType.FishAndSeafood => fishSeafoodEntries,
            MarketSectionType.MeatAndPoultry => meatPoultryEntries,
            MarketSectionType.Drinks => drinksEntries,
            MarketSectionType.BreadDairySweeteners => breadDairySweetenersEntries,
            _ => null
        };
    }

    private static bool HasEntries(MarketItemEntry[] items)
    {
        return items != null && items.Length > 0;
    }

    private void PopulateSection(VisualElement grid, MarketItemEntry[] items, MarketSectionType sectionType)
    {
        if (grid == null)
            return;

        grid.Clear();

        if (items == null || items.Length == 0)
        {
            grid.Add(CreateEmptySectionCard(sectionType));
            return;
        }

        int addedCount = 0;

        foreach (MarketItemEntry item in items)
        {
            if (item == null || !item.available)
                continue;

            grid.Add(CreateItemCard(item, sectionType));
            addedCount++;
        }

        if (addedCount == 0)
            grid.Add(CreateEmptySectionCard(sectionType));
    }

    private VisualElement CreateEmptySectionCard(MarketSectionType sectionType)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("item-card");
        card.AddToClassList(GetCardClass(sectionType));
        card.AddToClassList("empty-item-card");

        Label title = new Label("No items yet");
        title.AddToClassList("item-name");

        Label detail = new Label("This category is ready and can be filled later.");
        detail.AddToClassList("empty-item-detail");

        card.Add(title);
        card.Add(detail);
        return card;
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
            MarketSectionType.FruitsAndVegetables => "fruits-vegetables-card",
            MarketSectionType.FishAndSeafood => "fish-seafood-card",
            MarketSectionType.MeatAndPoultry => "meat-poultry-card",
            MarketSectionType.Drinks => "drinks-card",
            MarketSectionType.BreadDairySweeteners => "baking-dairy-card",
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