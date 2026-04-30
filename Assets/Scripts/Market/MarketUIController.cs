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
    [SerializeField] private PickupToastUIToolkit toastUI;
    [SerializeField] private string zeroMoneyLoanHintMessage = "💸 No money? Press L to take a loan.";
    [SerializeField] private float zeroMoneyLoanHintDuration = 6.0f;

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
    [SerializeField] private MarketItemEntry[] treeSeedsEntries;

    [Header("Input")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    [Header("Audio")]
    [SerializeField] private AudioClip openMarketSound;
    [SerializeField] private AudioClip closeMarketSound;
    [SerializeField] private AudioClip errorSound;

    private AudioSource _audioSource;

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
    private Button treeSeedsTabButton;

    private VisualElement seedsSection;
    private VisualElement toolsSection;
    private VisualElement fruitsVegetablesSection;
    private VisualElement fishSeafoodSection;
    private VisualElement meatPoultrySection;
    private VisualElement drinksSection;
    private VisualElement breadDairySweetenersSection;
    private VisualElement treeSeedsSection;

    private VisualElement seedsGrid;
    private VisualElement toolsGrid;
    private VisualElement fruitsVegetablesGrid;
    private VisualElement fishSeafoodGrid;
    private VisualElement meatPoultryGrid;
    private VisualElement drinksGrid;
    private VisualElement breadDairySweetenersGrid;
    private VisualElement treeSeedsGrid;

    private readonly Dictionary<MarketSectionType, VisualElement> sectionLookup = new();
    private readonly Dictionary<MarketSectionType, Button> tabLookup = new();

    private MarketSectionType currentSection = MarketSectionType.Seeds;
    private bool isSectionLocked;
    private MarketSectionType lockedSection = MarketSectionType.Seeds;
    private bool hasShownDebtHintThisOpen;
    private Coroutine debtHintFallbackRoutine;

    public bool IsOpen => !marketRoot.ClassListContains("hidden");

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();

        EnsureAudioSource();

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
        treeSeedsTabButton = root.Q<Button>("tab-tree-seeds");

        seedsSection = root.Q<VisualElement>("section-seeds");
        toolsSection = root.Q<VisualElement>("section-tools");
        fruitsVegetablesSection = root.Q<VisualElement>("section-fruits-vegetables");
        fishSeafoodSection = root.Q<VisualElement>("section-fish-seafood");
        meatPoultrySection = root.Q<VisualElement>("section-meat-poultry");
        drinksSection = root.Q<VisualElement>("section-drinks");
        breadDairySweetenersSection = root.Q<VisualElement>("section-baking-dairy");
        treeSeedsSection = root.Q<VisualElement>("section-tree-seeds");

        seedsGrid = root.Q<VisualElement>("grid-seeds");
        toolsGrid = root.Q<VisualElement>("grid-tools");
        fruitsVegetablesGrid = root.Q<VisualElement>("grid-fruits-vegetables");
        fishSeafoodGrid = root.Q<VisualElement>("grid-fish-seafood");
        meatPoultryGrid = root.Q<VisualElement>("grid-meat-poultry");
        drinksGrid = root.Q<VisualElement>("grid-drinks");
        breadDairySweetenersGrid = root.Q<VisualElement>("grid-baking-dairy");
        treeSeedsGrid = root.Q<VisualElement>("grid-tree-seeds");

        sectionLookup[MarketSectionType.Seeds] = seedsSection;
        sectionLookup[MarketSectionType.Tools] = toolsSection;
        sectionLookup[MarketSectionType.FruitsAndVegetables] = fruitsVegetablesSection;
        sectionLookup[MarketSectionType.FishAndSeafood] = fishSeafoodSection;
        sectionLookup[MarketSectionType.MeatAndPoultry] = meatPoultrySection;
        sectionLookup[MarketSectionType.Drinks] = drinksSection;
        sectionLookup[MarketSectionType.BreadDairySweeteners] = breadDairySweetenersSection;
        sectionLookup[MarketSectionType.TreeSeeds] = treeSeedsSection;

        tabLookup[MarketSectionType.Seeds] = seedsTabButton;
        tabLookup[MarketSectionType.Tools] = toolsTabButton;
        tabLookup[MarketSectionType.FruitsAndVegetables] = fruitsVegetablesTabButton;
        tabLookup[MarketSectionType.FishAndSeafood] = fishSeafoodTabButton;
        tabLookup[MarketSectionType.MeatAndPoultry] = meatPoultryTabButton;
        tabLookup[MarketSectionType.Drinks] = drinksTabButton;
        tabLookup[MarketSectionType.BreadDairySweeteners] = breadDairySweetenersTabButton;
        tabLookup[MarketSectionType.TreeSeeds] = treeSeedsTabButton;

        closeButton.clicked += CloseMarket;
        seedsTabButton.clicked += () => TrySwitchTab(MarketSectionType.Seeds);
        toolsTabButton.clicked += () => TrySwitchTab(MarketSectionType.Tools);
        fruitsVegetablesTabButton.clicked += () => TrySwitchTab(MarketSectionType.FruitsAndVegetables);
        fishSeafoodTabButton.clicked += () => TrySwitchTab(MarketSectionType.FishAndSeafood);
        meatPoultryTabButton.clicked += () => TrySwitchTab(MarketSectionType.MeatAndPoultry);
        drinksTabButton.clicked += () => TrySwitchTab(MarketSectionType.Drinks);
        breadDairySweetenersTabButton.clicked += () => TrySwitchTab(MarketSectionType.BreadDairySweeteners);
        treeSeedsTabButton.clicked += () => TrySwitchTab(MarketSectionType.TreeSeeds);

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

        if (newAmount > 0)
            hasShownDebtHintThisOpen = false;

        TryShowDebtHintToast();
    }

    public void OpenSection(MarketSectionType section)
    {
        OpenSection(section, false);
    }

    public void OpenSection(MarketSectionType section, bool lockToSingleSection)
    {
        bool wasOpen = IsOpen;
        currentSection = NormalizeSection(section);

        if (lockToSingleSection)
        {
            isSectionLocked = true;
            lockedSection = currentSection;
        }

        marketRoot.RemoveFromClassList("hidden");

        if (!wasOpen)
            PlayOpenSound();
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

        RefreshTabVisibility();

        marketSubtitle.text = GetSubtitle(currentSection);
        RefreshMoney();
        TryShowDebtHintToast();
    }

    private void TrySwitchTab(MarketSectionType section)
    {
        if (isSectionLocked && section != lockedSection)
            return;

        OpenSection(section, false);
    }

    private void RefreshTabVisibility()
    {
        foreach (KeyValuePair<MarketSectionType, Button> pair in tabLookup)
        {
            bool shouldShow = !isSectionLocked || pair.Key == lockedSection;
            pair.Value.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }
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
        PlayCloseSound();
        marketRoot.AddToClassList("hidden");
        isSectionLocked = false;
        hasShownDebtHintThisOpen = false;
        StopDebtHintFallback();
        RefreshTabVisibility();
    }

    public void CloseMarketInstant()
    {
        marketRoot.AddToClassList("hidden");
        isSectionLocked = false;
        hasShownDebtHintThisOpen = false;
        StopDebtHintFallback();
        RefreshTabVisibility();
    }

    private void TryShowDebtHintToast()
    {
        if (!IsOpen || hasShownDebtHintThisOpen)
            return;

        if (MoneyManager.Instance.CurrentMoney > 0)
            return;

        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();

        if (toastUI != null)
        {
            toastUI.Show(zeroMoneyLoanHintMessage, zeroMoneyLoanHintDuration);
        }
        else
        {
            ShowDebtHintFallback();
        }

        hasShownDebtHintThisOpen = true;
    }

    private void ShowDebtHintFallback()
    {
        StopDebtHintFallback();
        SetInteractionHint(zeroMoneyLoanHintMessage, true);
        debtHintFallbackRoutine = StartCoroutine(HideDebtHintFallbackAfterDelay());
    }

    private System.Collections.IEnumerator HideDebtHintFallbackAfterDelay()
    {
        yield return new WaitForSeconds(zeroMoneyLoanHintDuration);

        if (IsOpen)
            SetInteractionHint(string.Empty, false);

        debtHintFallbackRoutine = null;
    }

    private void StopDebtHintFallback()
    {
        if (debtHintFallbackRoutine == null)
            return;

        StopCoroutine(debtHintFallbackRoutine);
        debtHintFallbackRoutine = null;
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void PlayOpenSound()
    {
        if (_audioSource == null || openMarketSound == null)
            return;

        _audioSource.PlayOneShot(openMarketSound);
    }

    private void PlayErrorSound()
    {
        if (_audioSource == null || errorSound == null)
            return;

        _audioSource.PlayOneShot(errorSound);
    }

    private void PlayCloseSound()
    {
        if (_audioSource == null || closeMarketSound == null)
            return;

        _audioSource.PlayOneShot(closeMarketSound);
    }

    public void SetInteractionHint(string message, bool show)
    {
        if (interactionHint == null)
            return;

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
            MarketSectionType.TreeSeeds => "Plant trees to grow over time.",
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
        PopulateSection(treeSeedsGrid, ResolveItemsForSection(MarketSectionType.TreeSeeds), MarketSectionType.TreeSeeds);
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
            MarketSectionType.TreeSeeds => treeSeedsEntries,
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

        // price is already the total for the configured bundle quantity
        bool canAfford = MoneyManager.Instance.CanAfford(item.price);
        if (!canAfford)
            buyButton.AddToClassList("buy-button-unaffordable");

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
            MarketSectionType.TreeSeeds => "tree-seeds-card",
            _ => "seeds-card"
        };
    }

    private void TryBuy(MarketItemEntry item)
    {
        int quantity = GetPurchaseQuantity(item);
        // price is already the total for the configured bundle quantity
        int totalPrice = item.price;

        if (!MoneyManager.Instance.SpendMoney(totalPrice))
        {
            marketSubtitle.text = "Not enough coins.";
            PlayErrorSound();
            // Prompt the player to take a loan (use MoneyManager to show the loan hint immediately)
            if (MoneyManager.HasInstance)
                MoneyManager.Instance.ShowZeroMoneyLoanHintImmediate();
            return;
        }

        if (inventoryBridge == null)
        {
            MoneyManager.Instance.AddMoney(totalPrice);
            marketSubtitle.text = "Shop inventory bridge is missing.";
            RefreshMoney();
            PopulateAllSections();
            return;
        }

        bool success = inventoryBridge.TryReceivePurchase(item, quantity, out string message);
        if (!success)
        {
            // Keep money and inventory transaction in sync.
            MoneyManager.Instance.AddMoney(totalPrice);
            marketSubtitle.text = string.IsNullOrEmpty(message) ? "Purchase failed." : message;
            PlayErrorSound();
            RefreshMoney();
            PopulateAllSections();
            return;
        }

        marketSubtitle.text = string.IsNullOrEmpty(message) ? $"Purchased {item.itemName} x{quantity}." : message;
        RefreshMoney();
        PopulateAllSections();
    }

    private int GetPurchaseQuantity(MarketItemEntry item)
    {
        if (item == null)
            return 1;

        return Mathf.Max(1, item.quantity);
    }
}