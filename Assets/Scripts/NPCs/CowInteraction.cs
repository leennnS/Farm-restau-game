using UnityEngine;

public class CowInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float milkCooldown = 10f;

    [Header("Reward")]
    [SerializeField] private ItemDefinition milkItemDefinition;
    [SerializeField] private int milkQuantity = 1;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryController playerInventory;
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("UI")]
    [SerializeField] private Sprite cowPortraitSprite;

    [Header("Effects")]
    [SerializeField] private string milkFlyEffectResourcesPath = "Prefabs/Items/MilkFlyEffect";

    private float timeSinceLastMilk;
    private bool isBeingMilked;
    private GameObject milkFlyEffectPrefab;
    private CowMilkingMinigameUI milkingUI;

    public Sprite CowPortraitSprite => cowPortraitSprite;
    public bool IsBeingMilked => isBeingMilked;

    private void Start()
    {
        timeSinceLastMilk = milkCooldown;

        milkFlyEffectPrefab = Resources.Load<GameObject>(milkFlyEffectResourcesPath);

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<InventoryController>();

        milkingUI = FindFirstObjectByType<CowMilkingMinigameUI>();
    }

    private void Update()
    {
        timeSinceLastMilk += Time.deltaTime;

        if (playerTransform == null)
            TryResolvePlayer();

        if (playerTransform == null || milkingUI == null)
            return;

        if (isBeingMilked)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionDistance && Input.GetKeyDown(interactionKey))
        {
            if (CanStartMilking())
            {
                milkingUI.OpenMilkingGame(this);
            }
        }
    }

    private void TryResolvePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    public bool CanStartMilking()
    {
        return !isBeingMilked && timeSinceLastMilk >= milkCooldown;
    }

    public void BeginMilking()
    {
        isBeingMilked = true;
    }

    public void CancelMilking()
    {
        isBeingMilked = false;
    }

    public void CompleteMilking(bool success)
    {
        isBeingMilked = false;

        if (!success)
        {

            return;
        }

        if (playerInventory == null || milkItemDefinition == null)
        {

            return;
        }

        if (playerInventory.TryAdd(milkItemDefinition, milkQuantity))
        {
            timeSinceLastMilk = 0f;



            if (pickupToast != null)
                pickupToast.Show($"+{milkQuantity} {milkItemDefinition.displayName}");

            SpawnMilkFlyEffect();
        }
        else
        {

        }
    }

    private void SpawnMilkFlyEffect()
    {
        if (milkFlyEffectPrefab == null || playerTransform == null)
            return;

        GameObject fx = Instantiate(
            milkFlyEffectPrefab,
            transform.position + Vector3.up * 0.5f,
            Quaternion.identity
        );

        ItemFlyToPlayer fly = fx.GetComponent<ItemFlyToPlayer>();
        if (fly != null)
            fly.Initialize(playerTransform);
    }
}