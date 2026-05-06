using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages contextual UI prompts for animal interactions.
/// Shows "Press E to milk the cow" when near a cow ready for milking.
/// Shows "Press G to feed the animal (any seeds)" when near a hungry animal.
/// 
/// This is purely a UI layer - no gameplay logic is modified.
/// </summary>
public class AnimalInteractionPromptManager : MonoBehaviour
{
    private static AnimalInteractionPromptManager _instance;

    [Header("Detection")]
    [SerializeField] private float cowPromptDistance = 2.6f;  // Same as CowInteraction.interactionDistance
    [SerializeField] private float animalHungerPromptDistance = 3f;

    [Header("References")]
    private PickupToastUIToolkit _toastUI;
    private Transform _playerTransform;
    private InventoryController _inventory;

    private CowInteraction _nearestCow;
    private AnimalPersonalityController _nearestHungryAnimal;
    private string _currentPromptType; // "cow", "hungry", or null
    private float _updateTimer;

    private const float UPDATE_INTERVAL = 0.2f;  // Update prompts every 200ms for efficiency

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("AnimalInteractionPromptManager");
        _instance = go.AddComponent<AnimalInteractionPromptManager>();
        Object.DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!IsFarmScene())
            return;

        _updateTimer -= Time.deltaTime;
        if (_updateTimer > 0f)
            return;

        _updateTimer = UPDATE_INTERVAL;
        UpdatePrompts();
    }

    private void UpdatePrompts()
    {
        ResolveReferences();

        if (_playerTransform == null || _toastUI == null)
        {
            HidePrompt();
            return;
        }

        // Check for cow interaction first (priority)
        _nearestCow = FindNearestReadyCow();
        if (_nearestCow != null)
        {
            ShowCowPrompt();
            return;
        }

        // Check for hungry animal
        _nearestHungryAnimal = FindNearestHungryAnimal();
        if (_nearestHungryAnimal != null && HasFoodInHotbar())
        {
            ShowHungryAnimalPrompt();
            return;
        }

        // No valid interaction, hide prompt
        HidePrompt();
    }

    private void ShowCowPrompt()
    {
        if (_currentPromptType == "cow")
            return; // Already showing this prompt

        _currentPromptType = "cow";
        _toastUI.ShowPersistent("Press E to milk the cow", fontSize: 24);
    }

    private void ShowHungryAnimalPrompt()
    {
        if (_currentPromptType == "hungry")
            return; // Already showing this prompt

        _currentPromptType = "hungry";
        _toastUI.ShowPersistent("Press G to feed the animal (any seeds)", fontSize: 24);
    }

    private void HidePrompt()
    {
        if (_currentPromptType == null)
            return; // Already hidden

        _currentPromptType = null;
        _toastUI.Hide();
    }

    private CowInteraction FindNearestReadyCow()
    {
        CowInteraction[] cows = FindObjectsByType<CowInteraction>(FindObjectsSortMode.None);
        CowInteraction nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var cow in cows)
        {
            if (cow == null || cow.IsBeingMilked)
                continue;

            if (!cow.CanStartMilking())
                continue;

            float distance = GetDistanceToCow(cow);
            if (distance <= cowPromptDistance && distance < nearestDistance)
            {
                nearest = cow;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private AnimalPersonalityController FindNearestHungryAnimal()
    {
        AnimalPersonalityController[] animals = FindObjectsByType<AnimalPersonalityController>(FindObjectsSortMode.None);
        AnimalPersonalityController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var animal in animals)
        {
            if (animal == null)
                continue;

            if (!animal.IsHungry())
                continue;

            float distance = Vector2.Distance(_playerTransform.position, animal.transform.position);
            if (distance <= animalHungerPromptDistance && distance < nearestDistance)
            {
                nearest = animal;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private float GetDistanceToCow(CowInteraction cow)
    {
        if (cow == null || _playerTransform == null)
            return float.MaxValue;

        // Try to get more accurate distance using cow's collider
        Collider2D cowCollider = cow.GetComponent<Collider2D>();
        if (cowCollider != null)
        {
            Vector2 closestPoint = cowCollider.ClosestPoint(_playerTransform.position);
            return Vector2.Distance(_playerTransform.position, closestPoint);
        }

        // Fallback to simple distance
        return Vector2.Distance(_playerTransform.position, cow.transform.position);
    }

    private bool HasFoodInHotbar()
    {
        if (_inventory == null)
            return false;

        for (int i = 0; i < InventoryController.HotbarSize; i++)
        {
            ItemDefinition item = _inventory.GetHotbarItem(i);
            if (IsFoodItem(item))
                return true;
        }

        return false;
    }

    private bool IsFoodItem(ItemDefinition item)
    {
        if (item == null)
            return false;

        string name = $"{item.displayName} {item.name}".ToLowerInvariant();
        string[] foodKeywords = { "seed", "wheat", "corn", "carrot", "lettuce", "apple", "berry" };

        foreach (string keyword in foodKeywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword) && name.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        if (_toastUI == null)
            _toastUI = FindFirstObjectByType<PickupToastUIToolkit>();

        if (_inventory == null)
            _inventory = InventoryController.Instance != null ? InventoryController.Instance : FindFirstObjectByType<InventoryController>();
    }

    private static bool IsFarmScene()
    {
        string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
        return sceneName.IndexOf("farm", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
