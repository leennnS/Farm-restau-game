using System;
using UnityEngine;

[Serializable]
public struct ItemStack
{
    public ItemDefinition item;
    public int amount;
}

public enum RecipeCategory
{
    Breakfast,
    MainDishes,
    BakeryDesserts,
    DrinksSmoothies
}

/// <summary>
/// Defines a crafting recipe with ingredients and result
/// </summary>
[CreateAssetMenu(menuName = "Farming Game/Recipe")]
public class RecipeDefinition : ScriptableObject
{
    [Serializable]
    public struct Ingredient
    {
        public ItemDefinition item;
        [Min(1)] public int amount;
    }

    public RecipeCategory category;
    public string recipeName;
    public ItemDefinition result;
    [Min(1)] public int resultAmount = 1;
    public Sprite recipeIcon;
    public Ingredient[] ingredients;

    [Header("Order Data")]
    [Min(0)] public int rewardMoney = 0;
    [Min(0f)] public float orderPreparationTime = 30f;
    public bool usePenalty = false;
    [Min(0)] public int penaltyMoney = 0;

    public int GetPenaltyMoney()
    {
        return usePenalty ? penaltyMoney : 0;
    }

    public bool IsValidForOrder()
    {
        if (ingredients == null || ingredients.Length == 0)
            return false;

        for (int i = 0; i < ingredients.Length; i++)
        {
            if (ingredients[i].item == null)
                return false;

            if (ingredients[i].amount <= 0)
                return false;
        }

        return true;
    }

    public bool CanCraft(ItemStack[] inventory)
    {
        if (ingredients == null || ingredients.Length == 0)
            return false;

        // Check if we have all required ingredients
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null)
                continue;

            int totalRequired = ingredient.amount;
            int totalFound = 0;

            // Count how many of this item we have in inventory
            foreach (var slot in inventory)
            {
                if (slot.item == ingredient.item)
                    totalFound += slot.amount;
            }

            if (totalFound < totalRequired)
                return false;
        }

        return true;
    }

    public void Craft(ref ItemStack[] inventory)
    {
        if (!CanCraft(inventory))
            return;

        // Consume ingredients
        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null)
                continue;

            int toConsume = ingredient.amount;
            for (int i = 0; i < inventory.Length && toConsume > 0; i++)
            {
                if (inventory[i].item == ingredient.item)
                {
                    int consumed = Mathf.Min(toConsume, inventory[i].amount);
                    inventory[i].amount -= consumed;
                    toConsume -= consumed;

                    if (inventory[i].amount <= 0)
                        inventory[i] = new ItemStack { item = null, amount = 0 };
                }
            }
        }

        // Add result to inventory
        // Try to stack first
        bool stacked = false;
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].item == result && inventory[i].amount > 0)
            {
                inventory[i].amount += resultAmount;
                stacked = true;
                break;
            }
        }

        // If not stacked, find empty slot
        if (!stacked)
        {
            for (int i = 0; i < inventory.Length; i++)
            {
                if (inventory[i].item == null || inventory[i].amount <= 0)
                {
                    inventory[i] = new ItemStack { item = result, amount = resultAmount };
                    break;
                }
            }
        }
    }
}
