using UnityEngine;
using System.Collections;

/// <summary>
/// Interactable note object in the world.
/// Shows proximity hint and responds to mouse click.
/// Only becomes discoverable after player is unfrozen.
/// </summary>
public class NoteInteraction : MonoBehaviour
{
    [SerializeField]
    private float discoveryDistance = 3f;

    [SerializeField]
    private SpriteRenderer noteSprite;

    [SerializeField]
    private Collider2D noteCollider;

    [SerializeField]
    private LetterPanel letterPanel;

    private bool isPlayerNearby = false;
    private bool noteHasBeenRead = false;
    private bool isDiscoverable = false;

    private CharacterController2D player;
    private Canvas proximityHintCanvas;

    public bool IsPlayerNearby => isPlayerNearby;
    public bool NoteHasBeenRead => noteHasBeenRead;
    public LetterPanel LetterPanelRef => letterPanel;

    private void Start()
    {
        if (noteSprite == null)
            noteSprite = GetComponent<SpriteRenderer>();

        if (noteCollider == null)
            noteCollider = GetComponent<Collider2D>();

        // Start dimmed until discoverable
        if (noteSprite != null)
        {
            noteSprite.color = new Color(1f, 1f, 1f, 0.3f);
        }

        // Create proximity hint
        CreateProximityHint();
    }

    private void Update()
    {
        player = FindFirstObjectByType<CharacterController2D>();

        if (player != null && isDiscoverable)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            bool wasNearby = isPlayerNearby;
            isPlayerNearby = distance < discoveryDistance;

            if (isPlayerNearby && !wasNearby)
            {
                OnPlayerNearby();
            }
            else if (!isPlayerNearby && wasNearby)
            {
                OnPlayerLeaveNearby();
            }

            // Check for mouse click on note
            if (isPlayerNearby && Input.GetMouseButtonDown(0))
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;

                if (noteCollider != null && noteCollider.OverlapPoint(mouseWorldPos))
                {
                    ReadNote();
                }
            }
        }
    }

    private void OnPlayerNearby()
    {
        Debug.Log("[Note] Player nearby - showing hint");

        // Brighten sprite
        if (noteSprite != null)
        {
            StartCoroutine(PulseSprite());
        }

        // Show interaction prompt
        ShowProximityHint();
    }

    private void OnPlayerLeaveNearby()
    {
        Debug.Log("[Note] Player left proximity");

        // Dim sprite
        if (noteSprite != null)
        {
            noteSprite.color = new Color(1f, 1f, 1f, 0.5f);
        }

        HideProximityHint();
    }

    private void ReadNote()
    {
        Debug.Log("[Note] Note opened!");

        if (letterPanel != null)
        {
            letterPanel.ShowLetter();
        }

        noteHasBeenRead = true;

        // Brighten note permanently
        if (noteSprite != null)
        {
            noteSprite.color = Color.white;
        }
    }

    private IEnumerator PulseSprite()
    {
        for (int i = 0; i < 2; i++)
        {
            if (noteSprite == null)
                yield break;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.5f, 1f, elapsed / duration);
                noteSprite.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            noteSprite.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.2f);

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0.5f, elapsed / duration);
                noteSprite.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            noteSprite.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void CreateProximityHint()
    {
        // Create a simple UI hint (will be controlled separately)
        Debug.Log("[Note] Proximity hint system ready");
    }

    private void ShowProximityHint()
    {
        // Show hint UI
        Debug.Log("[Note] Showing proximity hint: Click to read the note");
    }

    private void HideProximityHint()
    {
        // Hide hint UI
        Debug.Log("[Note] Hiding proximity hint");
    }

    public void EnableDiscovery()
    {
        isDiscoverable = true;
        Debug.Log("[Note] Note is now discoverable by player");
    }

    public void DisableDiscovery()
    {
        isDiscoverable = false;
    }
}
