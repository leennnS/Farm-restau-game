using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loot chest that opens, spawns visual flying rewards, and can be closed again.
/// </summary>
public class StarterToolsChest : MonoBehaviour
{
    private const string LegacyDefaultChestId = "starter_tools_chest";

    [Serializable]
    public class RewardEntry
    {
        public ItemDefinition item;
        [Min(1)] public int amount = 1;
        public Sprite flySpriteOverride;
        [Min(0.01f)] public float flyScale = 1f;
    }

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool useTriggerRange = true;

    [Header("Chest Animation")]
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string openedBoolName = "IsOpen";
    [SerializeField] private float openToFirstRewardDelay = 0.2f;

    [Header("Chest Visual Fallback (No Animator)")]
    [Tooltip("Optional: if no Animator is assigned, this renderer can switch between closed/open sprites.")]
    [SerializeField] private SpriteRenderer chestSpriteRenderer;
    [SerializeField] private Sprite closedChestSprite;
    [SerializeField] private Sprite openedChestSprite;

    [Header("Reward Sequence")]
    [SerializeField] private List<RewardEntry> rewards = new List<RewardEntry>(3);
    [SerializeField] private float delayBetweenRewards = 0.08f;

    [Header("References")]
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("Flying Visual")]
    [Tooltip("Optional. If null, a visual object is created automatically with SpriteRenderer + FlyingRewardVisual.")]
    [SerializeField] private GameObject flyingRewardPrefab;

    [Header("Loot Persistence")]
    [Tooltip("If enabled, loot is permanently consumed using PlayerPrefs. If disabled, chest resets each game run.")]
    [SerializeField] private bool rememberOpenedState = false;
    [Tooltip("Leave empty to auto-generate from scene hierarchy. Set manually for stable custom IDs.")]
    [SerializeField] private string chestId = "";
    [SerializeField] private string emptyMessage = "The chest is empty";
    [SerializeField] private string closeMessage = "Chest closed";

    private bool _playerInRange;
    private bool _isOpening;
    private bool _isOpen;
    private bool _isLooted;

    private void Awake()
    {
        AutoResolveReferences();

        if (rememberOpenedState)
        {
            _isLooted = PlayerPrefs.GetInt(GetSaveKey(), 0) == 1;
            _isOpen = _isLooted;
        }
        else
        {
            // Non-persistent mode: chest always starts fresh and closed each run.
            _isLooted = false;
            _isOpen = false;
        }

        ApplyAnimatorState(_isOpen);
    }

    private void Update()
    {
        if (_isOpening)
            return;

        bool canInteract = useTriggerRange ? _playerInRange : IsPlayerCloseNoTrigger();
        if (!canInteract)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (_isOpen)
            {
                CloseChest();
                return;
            }

            if (_isLooted)
            {
                pickupToast?.Show(emptyMessage);
                return;
            }

            StartCoroutine(OpenAndRewardRoutine());
        }
    }

    private IEnumerator OpenAndRewardRoutine()
    {
        _isOpening = true;
        _isOpen = true;

        ApplyAnimatorState(true);
        yield return new WaitForSeconds(openToFirstRewardDelay);

        Vector3 spawnPos = rewardSpawnPoint != null ? rewardSpawnPoint.position : transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position : spawnPos;
        int validRewardCount = 0;
        int grantedRewardCount = 0;

        for (int i = 0; i < rewards.Count; i++)
        {
            RewardEntry reward = rewards[i];
            if (reward == null || reward.item == null || reward.amount <= 0)
                continue;

            validRewardCount++;

            targetPos = playerTransform != null ? playerTransform.position : targetPos;

            bool arrived = false;
            Action onReached = () =>
            {
                if (TryGrantReward(reward))
                    grantedRewardCount++;

                arrived = true;
            };

            SpawnFlyingVisual(reward, spawnPos, targetPos, onReached);

            yield return new WaitUntil(() => arrived);
            yield return new WaitForSeconds(delayBetweenRewards);
        }

        if (validRewardCount == 0)
        {
            pickupToast?.Show("Chest has no rewards configured");
        }
        else if (grantedRewardCount == validRewardCount)
        {
            if (rememberOpenedState)
            {
                MarkLooted();
            }
        }
        else
        {
            pickupToast?.Show("Could not collect all rewards");
        }

        _isOpening = false;
    }

    private bool TryGrantReward(RewardEntry reward)
    {
        if (inventoryController == null)
            return false;

        bool added = inventoryController.TryAdd(reward.item, reward.amount);
        if (added)
        {
            pickupToast?.Show($"+{reward.amount} {reward.item.displayName}");
            return true;
        }

        pickupToast?.Show($"Inventory full: {reward.item.displayName}");
        return false;
    }

    private void SpawnFlyingVisual(RewardEntry reward, Vector3 spawnPos, Vector3 targetPos, Action onReached)
    {
        GameObject visualGo;

        if (flyingRewardPrefab != null)
        {
            visualGo = Instantiate(flyingRewardPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            visualGo = new GameObject($"FlyingReward_{reward.item.displayName}");
            visualGo.transform.position = spawnPos;
        }

        FlyingRewardVisual visual = visualGo.GetComponent<FlyingRewardVisual>();
        if (visual == null)
        {
            visual = visualGo.AddComponent<FlyingRewardVisual>();
        }

        Sprite icon = reward.flySpriteOverride != null ? reward.flySpriteOverride : reward.item.icon;
        visual.Play(spawnPos, targetPos, icon, reward.flyScale, onReached);
    }

    private void MarkLooted()
    {
        _isLooted = true;

        if (rememberOpenedState)
        {
            PlayerPrefs.SetInt(GetSaveKey(), 1);
            PlayerPrefs.Save();
        }
    }

    private void CloseChest()
    {
        // One-time persisted chests remain open once looted.
        if (rememberOpenedState && _isLooted)
        {
            pickupToast?.Show(emptyMessage);
            return;
        }

        _isOpen = false;
        ApplyAnimatorState(false);
        pickupToast?.Show(closeMessage);
    }

    private void ApplyAnimatorState(bool opened)
    {
        if (chestAnimator != null)
        {
            if (!string.IsNullOrWhiteSpace(openedBoolName))
            {
                chestAnimator.SetBool(openedBoolName, opened);
            }

            if (opened && !string.IsNullOrWhiteSpace(openTriggerName))
            {
                chestAnimator.SetTrigger(openTriggerName);
            }
        }

        ApplySpriteVisualState(opened);
    }

    private void ApplySpriteVisualState(bool opened)
    {
        if (chestSpriteRenderer == null)
            return;

        if (opened)
        {
            if (openedChestSprite != null)
                chestSpriteRenderer.sprite = openedChestSprite;
        }
        else
        {
            if (closedChestSprite != null)
                chestSpriteRenderer.sprite = closedChestSprite;
        }
    }

    private string GetSaveKey()
    {
        string id = GetResolvedChestId();
        string scene = SceneManager.GetActiveScene().name;
        return $"starter_chest_opened_{scene}_{id}";
    }

    private string GetResolvedChestId()
    {
        if (!string.IsNullOrWhiteSpace(chestId) && chestId != LegacyDefaultChestId)
            return chestId;

        return BuildHierarchyPathId();
    }

    private string BuildHierarchyPathId()
    {
        Transform current = transform;
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path.Replace(" ", "_");
    }

    private bool IsPlayerCloseNoTrigger()
    {
        if (playerTransform == null)
            return false;

        float sqrDistance = (playerTransform.position - transform.position).sqrMagnitude;
        return sqrDistance <= 4f;
    }

    private void AutoResolveReferences()
    {
        if (rewardSpawnPoint == null)
            rewardSpawnPoint = transform;

        if (chestAnimator == null)
            chestAnimator = GetComponent<Animator>();

        if (chestSpriteRenderer == null)
            chestSpriteRenderer = GetComponent<SpriteRenderer>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
        }

        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerRange)
            return;

        if (other.CompareTag(playerTag))
            _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTriggerRange)
            return;

        if (other.CompareTag(playerTag))
            _playerInRange = false;
    }

    [ContextMenu("Reset Chest Save State")]
    public void ResetSavedState()
    {
        PlayerPrefs.DeleteKey(GetSaveKey());
        PlayerPrefs.Save();
        _isLooted = false;
        _isOpen = false;
        _isOpening = false;
        ApplyAnimatorState(false);
    }

    [ContextMenu("Reset All Chest Saves In Scene")]
    public void ResetAllChestSavesInScene()
    {
        StarterToolsChest[] chests = FindObjectsByType<StarterToolsChest>(FindObjectsSortMode.None);
        for (int i = 0; i < chests.Length; i++)
        {
            PlayerPrefs.DeleteKey(chests[i].GetSaveKey());
            chests[i]._isLooted = false;
            chests[i]._isOpen = false;
            chests[i]._isOpening = false;
            chests[i].ApplyAnimatorState(false);
        }

        PlayerPrefs.Save();
    }

    [ContextMenu("Log Chest Save Debug")]
    public void LogChestSaveDebug()
    {
        string key = GetSaveKey();
        int value = PlayerPrefs.GetInt(key, 0);
        Debug.Log($"[StarterToolsChest] key={key}, value={value}, resolvedChestId={GetResolvedChestId()}, rememberOpenedState={rememberOpenedState}");
    }
}
