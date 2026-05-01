using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections;

public class CameraFollowFix : MonoBehaviour
{
    private static CameraFollowFix instance;
    private CinemachineCamera cam;
    [SerializeField] private string marketSceneName = "MarketScene";
    [SerializeField] private float marketOrthographicSize = 10f;
    [SerializeField] private string houseSceneName = "HouseInteriorLITEDEMO";

    private float originalOrthographicSize;
    private bool originalOrthographicSizeCaptured;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        instance = this;
        cam = GetComponent<CinemachineCamera>();

        if (cam != null)
        {
            var lens = cam.Lens;
            originalOrthographicSize = lens.OrthographicSize;
            originalOrthographicSizeCaptured = true;
        }

        DontDestroyOnLoad(transform.root.gameObject); // persist whole camera rig
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneLensOverride(scene.name);
        StartCoroutine(AssignPlayer());
    }

    private void ApplySceneLensOverride(string sceneName)
    {
        float targetOrthographicSize = originalOrthographicSize;
        if (sceneName == marketSceneName)
            targetOrthographicSize = marketOrthographicSize;
        else if (sceneName == houseSceneName)
            targetOrthographicSize = 5f;

        if (cam != null)
        {
            var lens = cam.Lens;

            if (!originalOrthographicSizeCaptured)
            {
                originalOrthographicSize = lens.OrthographicSize;
                originalOrthographicSizeCaptured = true;
            }

            lens.OrthographicSize = targetOrthographicSize;
            cam.Lens = lens;
        }
    }

    IEnumerator AssignPlayer()
    {
        GameObject player = null;

        // KEEP trying until player exists (this is the fix)
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null; // wait next frame
        }

        AssignTargetNow(player.transform);
    }

    public void AssignTargetNow(Transform target)
    {
        if (cam == null || target == null)
            return;

        // Project primarily uses TrackingTarget. Keep Follow for compatibility.
        cam.Target.TrackingTarget = target;
        cam.Follow = target;
    }
}