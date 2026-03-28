using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Handles planting trees via 2-step process:
/// 1. Use hands tool to dig a hole
/// 2. Click hole with seed to plant tree
/// </summary>
public class TreePlanter : MonoBehaviour
{
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject holeMarkerPrefab;
    [SerializeField] private Sprite holeSprite;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PickupToastUIToolkit toastUI;
    [SerializeField, Tooltip("Search radius to find nearby holes when planting")]
    private float holeSearchRadius = 0.5f;

    private int selectedHotbarSlot = 0;
    private Dictionary<Vector3, PlantingHole> _holes = new Dictionary<Vector3, PlantingHole>(); // Key is rounded position

    private Vector3 RoundPosition(Vector3 pos) => new Vector3(Mathf.Round(pos.x * 10f) / 10f, Mathf.Round(pos.y * 10f) / 10f, pos.z);

    private void OnEnable()
    {
        FarmingInputHandler input = FindFirstObjectByType<FarmingInputHandler>();
        if (input != null)
            input.RegisterTreePlanter(this);
    }

    private void OnDisable()
    {
        FarmingInputHandler input = FindFirstObjectByType<FarmingInputHandler>();
        if (input != null)
            input.UnregisterTreePlanter(this);
    }

    private void Awake()
    {
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    public void SetSelectedHotbarSlot(int slot)
    {
        selectedHotbarSlot = slot;
    }

    /// <summary>
    /// Called when player uses hands tool to dig a hole in grass.
    /// </summary>
    public bool TryDigHole(Vector3 worldPosition)
    {
        if (holeSprite == null)
        {
            Debug.LogError("[TreePlanter] holeSprite is NOT assigned in inspector!");
            return false;
        }

        Vector3 roundedPos = RoundPosition(worldPosition);
        roundedPos.z = 0f;

        // Check if hole already exists
        if (_holes.ContainsKey(roundedPos))
        {
            if (toastUI != null)
                toastUI.Show("Hole already exists here!");
            return false;
        }

        // Create hole marker - simple empty GameObject (no prefab needed)
        GameObject holeMarker = new GameObject("Hole_Marker");
        holeMarker.transform.position = roundedPos;

        // Add SpriteRenderer directly
        SpriteRenderer sr = holeMarker.AddComponent<SpriteRenderer>();
        sr.sprite = holeSprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;  // Visible in front (changed from -5)

        // Add PlantingHole component
        PlantingHole hole = holeMarker.AddComponent<PlantingHole>();
        hole.Initialize(roundedPos);

        _holes[roundedPos] = hole;

        if (toastUI != null)
            toastUI.Show("Hole dug! Plant a seed here.");
        Debug.Log($"[TreePlanter] Hole dug at {roundedPos}. Check Hierarchy!");

        return true;
    }

    /// <summary>
    /// Called when player clicks on ground with seed selected.
    /// Checks if there's a hole nearby to plant into.
    /// </summary>
    public bool TryPlantTree(Vector3 worldPosition)
    {
        if (treePrefab == null || inventoryController == null)
            return false;

        // Get selected item
        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);
        if (selectedItem == null)
            return false;

        // Check if it's a seed by looking for a tree that uses this seed
        FruitTreeInteraction referenceTree = treePrefab.GetComponent<FruitTreeInteraction>();
        if (referenceTree == null)
            return false;

        ItemDefinition treesSeedItem = referenceTree.GetSeedItem();
        if (treesSeedItem == null || selectedItem != treesSeedItem)
            return false;

        // Check if there's a hole nearby
        Vector3 roundedPos = RoundPosition(worldPosition);
        PlantingHole nearestHole = null;
        float nearestDist = holeSearchRadius;

        foreach (var hole in _holes.Values)
        {
            float dist = Vector3.Distance(worldPosition, hole.GetPosition());
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestHole = hole;
            }
        }

        if (nearestHole == null)
        {
            if (toastUI != null)
                toastUI.Show("Need to dig a hole first!");
            return false;
        }

        if (nearestHole.HasSeed())
        {
            if (toastUI != null)
                toastUI.Show("Hole already has a seed!");
            return false;
        }

        // Plant the tree in the hole
        Vector3 holePos = nearestHole.GetPosition();
        GameObject treeInstance = Instantiate(treePrefab, holePos, Quaternion.identity);
        FruitTreeInteraction treeScript = treeInstance.GetComponent<FruitTreeInteraction>();
        if (treeScript != null)
        {
            treeScript.InitializeAsNewSapling();
            Debug.Log($"[TreePlanter] Tree planted in hole at {holePos}");
        }

        // Mark hole as having seed and hide hole marker
        nearestHole.PlantSeed();
        if (nearestHole.TryGetComponent<SpriteRenderer>(out var holeSR))
            holeSR.enabled = false; // Hide hole sprite once planted

        // Consume seed from inventory
        inventoryController.TryRemoveItem(selectedItem, 1);
        if (toastUI != null)
            toastUI.Show("Tree seed planted! It will grow over time.");

        return true;
    }
}
