using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporarily enforces a desired localScale on the GameObject for a short duration
/// to guard against race conditions where other scripts change scale after scene load.
/// Only applies in scenes that need scale adjustments (HouseInteriorLITEDEMO, RestaurantScene).
/// Destroys itself when done or when entering a scene that doesn't need it.
/// </summary>
public class PlayerScaleFixer : MonoBehaviour
{
    public Vector3 targetScale = new Vector3(0.5f, 0.5f, 1f);
    public float duration = 0.5f; // seconds

    float elapsed = 0f;
    string[] scenesThatNeedScale = { "HouseInteriorLITEDEMO", "RestaurantScene" };

    void Start()
    {
        if (targetScale == Vector3.zero) targetScale = new Vector3(0.5f, 0.5f, 1f);
        transform.localScale = targetScale;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If entering a scene that doesn't need scale fixing, destroy this component
        if (System.Array.IndexOf(scenesThatNeedScale, scene.name) == -1)
        {
            Destroy(this);
        }
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.localScale = targetScale;
        if (elapsed >= duration)
            Destroy(this);
    }
}
