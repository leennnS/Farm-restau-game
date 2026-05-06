using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnimalPersonalityBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToSceneAnimals();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachAfterInitialSceneLoad()
    {
        AttachToSceneAnimals();
    }

    private static void AttachToSceneAnimals()
    {
        CowInteraction[] cows = Object.FindObjectsByType<CowInteraction>(FindObjectsSortMode.None);
        for (int i = 0; i < cows.Length; i++)
        {
            if (cows[i] == null)
                continue;

            AnimalPersonalityController personality = cows[i].GetComponent<AnimalPersonalityController>();
            if (personality == null)
                personality = cows[i].gameObject.AddComponent<AnimalPersonalityController>();

            personality.Configure(AnimalPersonalityKind.Cow);
        }

        ChickenController[] chickens = Object.FindObjectsByType<ChickenController>(FindObjectsSortMode.None);
        for (int i = 0; i < chickens.Length; i++)
        {
            if (chickens[i] == null)
                continue;

            AnimalPersonalityController personality = chickens[i].GetComponent<AnimalPersonalityController>();
            if (personality == null)
                personality = chickens[i].gameObject.AddComponent<AnimalPersonalityController>();

            personality.Configure(AnimalPersonalityKind.Chicken);
        }
    }
}
