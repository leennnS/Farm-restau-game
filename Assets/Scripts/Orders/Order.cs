using System;
using UnityEngine;

[Serializable]
public class Order
{
    public string id;
    public RecipeDefinition recipe;
    public float remainingTime;
    public int rewardMoney;
    public int penaltyMoney;

    public bool IsExpired => remainingTime <= 0f;

    public Order(RecipeDefinition requestedRecipe)
    {
        recipe = requestedRecipe;
        id = Guid.NewGuid().ToString("N");
        remainingTime = Mathf.Max(0.1f, requestedRecipe != null ? requestedRecipe.orderPreparationTime : 30f);
        rewardMoney = requestedRecipe != null ? Mathf.Max(0, requestedRecipe.rewardMoney) : 0;
        penaltyMoney = requestedRecipe != null ? Mathf.Max(0, requestedRecipe.GetPenaltyMoney()) : 0;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f || IsExpired)
            return;

        remainingTime -= deltaTime;
        if (remainingTime < 0f)
            remainingTime = 0f;
    }
}
