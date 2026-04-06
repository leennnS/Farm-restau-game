using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections;

public class CameraFollowFix : MonoBehaviour
{
    private static CameraFollowFix instance;
    private CinemachineCamera cam;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        instance = this;
        cam = GetComponent<CinemachineCamera>();
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
        StartCoroutine(AssignPlayer());
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

        cam.Follow = player.transform;


    }
}