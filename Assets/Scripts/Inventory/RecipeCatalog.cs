using UnityEngine;

[CreateAssetMenu(menuName = "Farming Game/Recipe Catalog")]
public class RecipeCatalog : ScriptableObject
{
    [SerializeField] private RecipeDefinition[] recipes;

    public RecipeDefinition[] GetAllRecipes()
    {
        if (recipes == null || recipes.Length == 0)
            return System.Array.Empty<RecipeDefinition>();

        int validCount = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return System.Array.Empty<RecipeDefinition>();

        RecipeDefinition[] output = new RecipeDefinition[validCount];
        int outputIndex = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null)
                output[outputIndex++] = recipes[i];
        }

        return output;
    }

    public RecipeDefinition[] GetRecipesByCategory(RecipeCategory category)
    {
        if (recipes == null || recipes.Length == 0)
            return System.Array.Empty<RecipeDefinition>();

        int count = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && recipes[i].category == category)
                count++;
        }

        if (count == 0)
            return System.Array.Empty<RecipeDefinition>();

        RecipeDefinition[] output = new RecipeDefinition[count];
        int outputIndex = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && recipes[i].category == category)
                output[outputIndex++] = recipes[i];
        }

        return output;
    }

    public RecipeDefinition GetRandomRecipe()
    {
        RecipeDefinition[] all = GetAllRecipes();
        if (all.Length == 0)
            return null;

        return all[Random.Range(0, all.Length)];
    }

    public RecipeDefinition GetRandomRecipeByCategory(RecipeCategory category)
    {
        RecipeDefinition[] filtered = GetRecipesByCategory(category);
        if (filtered.Length == 0)
            return null;

        return filtered[Random.Range(0, filtered.Length)];
    }
}
