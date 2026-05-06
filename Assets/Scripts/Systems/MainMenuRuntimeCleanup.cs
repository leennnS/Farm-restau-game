using UnityEngine;

public static class MainMenuRuntimeCleanup
{
    private const string FromIntroKey = "FromIntroScene";
    private const string ForceShedDoorSpawnOnceKey = "ForceShedDoorSpawnOnce";
    private const string ReturnToFarmFromKey = "ReturnToFarmFrom";
    private const string SkipSpawnManagerOnceKey = "SkipSpawnManagerOnce";
    private const string PendingFarmTutorialKey = "PendingFarmTutorial";
    private const string FarmTutorialStartedKey = "FarmTutorialStarted";
    private const string FarmTutorialCompletedKey = "FarmTutorialCompleted";

    public static void PrepareForMainMenu(bool destroyGameManager)
    {
        Time.timeScale = 1f;
        ClearSceneTransitionState();

        DestroyPersistentPlayerObjects();
        DestroyPersistentObjects<InventoryController>();
        DestroyPersistentObjects<HotBarController>();
        DestroyPersistentObjects<HotBarHUDController>();
        DestroyPersistentObjects<MoneyManager>();
        DestroyPersistentObjects<DayNightCycleNice2D>();
        DestroyPersistentObjects<GlobalMoneyHUD>();
        DestroyPersistentObjects<GlobalClockHUD>();
        DestroyPersistentObjects<GlobalNextDayButtonHUD>();
        DestroyPersistentObjects<ClockHUDController>();
        DestroyPersistentObjects<OrderListHUD>();
        DestroyPersistentObjects<LanternController>();
        DestroyPersistentObjects<ImprovedLanternController>();
        DestroyPersistentObjects<CameraFollowFix>();

        if (destroyGameManager)
            DestroyPersistentObjects<GameManager>();
    }

    public static void ClearSceneTransitionState()
    {
        MarketReturnContext.PendingReturnToFarm = false;
        RestaurantReturnContext.PendingReturnToFarm = false;
        HouseExitTrigger.PendingReturnToFarm = false;

        PlayerPrefs.DeleteKey(FromIntroKey);
        PlayerPrefs.DeleteKey(ForceShedDoorSpawnOnceKey);
        PlayerPrefs.DeleteKey(ReturnToFarmFromKey);
        PlayerPrefs.DeleteKey(SkipSpawnManagerOnceKey);
        PlayerPrefs.DeleteKey(PendingFarmTutorialKey);
        PlayerPrefs.DeleteKey(FarmTutorialStartedKey);
        PlayerPrefs.DeleteKey(FarmTutorialCompletedKey);
        PlayerPrefs.Save();
    }

    private static void DestroyPersistentPlayerObjects()
    {
        DestroyPersistentObjects<DontDestroyOnLoad>();

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            GameObject player = taggedPlayers[i];
            if (player != null)
                Object.Destroy(player);
        }
    }

    private static void DestroyPersistentObjects<T>() where T : MonoBehaviour
    {
        T[] instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (instances == null)
            return;

        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance == null)
                continue;

            GameObject target = instance.gameObject;
            if (target == null)
                continue;

            target.SetActive(false);
            Object.Destroy(target);
        }
    }
}
