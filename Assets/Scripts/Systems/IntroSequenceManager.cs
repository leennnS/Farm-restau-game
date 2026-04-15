using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the intro sequence state machine.
/// Orchestrates narrative, lantern activation, movement unlock, and note discovery.
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    [SerializeField]
    private CharacterController2D playerController;

    [SerializeField]
    private ImprovedLanternController lantern;

    [SerializeField]
    private NoteInteraction note;

    [SerializeField]
    private IntroNarrativeController narrativeController;

    // Intro sequence state
    public enum IntroState
    {
        Starting,
        OpeningNarrative,
        LanternActivation,
        MovementUnlocked,
        SearchingForNote,
        NoteDiscovered,
        LetterReading,
        Complete
    }

    private IntroState currentState = IntroState.Starting;

    private void Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<CharacterController2D>();

        if (lantern == null)
            lantern = FindFirstObjectByType<ImprovedLanternController>();

        if (note == null)
            note = FindFirstObjectByType<NoteInteraction>();

        if (narrativeController == null)
            narrativeController = FindFirstObjectByType<IntroNarrativeController>();

        // Mark intro entry
        PlayerPrefs.SetInt("FromIntroScene", 1);
        PlayerPrefs.Save();

        StartCoroutine(RunIntroSequence());
    }

    private IEnumerator RunIntroSequence()
    {
        yield return StartCoroutine(StateOpening());
        yield return StartCoroutine(StateLanternActivation());
        yield return StartCoroutine(StateMovementUnlock());
        yield return StartCoroutine(StateSearching());
        yield return StartCoroutine(StateLetterReading());
        yield return StartCoroutine(StateComplete());
    }

    private IEnumerator StateOpening()
    {
        currentState = IntroState.OpeningNarrative;
        Debug.Log("[IntroSequence] Starting opening narrative...");

        // Freeze player
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Start narrative
        if (narrativeController != null)
        {
            yield return StartCoroutine(narrativeController.PlayOpening());
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator StateLanternActivation()
    {
        currentState = IntroState.LanternActivation;
        Debug.Log("[IntroSequence] Lantern activation...");

        if (lantern != null)
        {
            yield return StartCoroutine(lantern.PlayActivationSequence());
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator StateMovementUnlock()
    {
        currentState = IntroState.MovementUnlocked;
        Debug.Log("[IntroSequence] Unlocking player movement...");

        // Unfreeze player
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Enable note discovery
        if (note != null)
        {
            note.EnableDiscovery();
        }

        // Show hint
        if (narrativeController != null)
        {
            narrativeController.ShowHint("Pick up the lantern.");
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator StateSearching()
    {
        currentState = IntroState.SearchingForNote;
        Debug.Log("[IntroSequence] Waiting for player to discover note...");

        // Wait for player to get close to note
        if (note != null)
        {
            yield return new WaitUntil(() => note.IsPlayerNearby);
        }

        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator StateLetterReading()
    {
        currentState = IntroState.LetterReading;
        Debug.Log("[IntroSequence] Letter reading...");

        if (narrativeController != null)
        {
            narrativeController.ShowHint("Read the note. Click Go To Farm when you're ready.");
        }

        // Wait for note to be opened at least once.
        if (note != null)
        {
            yield return new WaitUntil(() => note.NoteHasBeenRead);

            // Wait until player explicitly presses Go To Farm on the letter UI.
            yield return new WaitUntil(() =>
                note.LetterPanelRef != null &&
                note.LetterPanelRef.GoToFarmRequested);
        }

        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator StateComplete()
    {
        currentState = IntroState.Complete;
        Debug.Log("[IntroSequence] Intro complete! Transitioning to gameplay...");

        // Short fade or transition here if desired
        yield return new WaitForSeconds(0.5f);

        // Destroy intro lantern before loading FarmScene.
        if (lantern != null)
        {
            Destroy(lantern.gameObject);
            lantern = null;
        }

        // Keep the intro flag for FarmScene SpawnManager to consume.
        // SpawnManager clears it after placing the player at ShedDoorSpawnPoint.
        PlayerPrefs.SetInt("FromIntroScene", 1);
        PlayerPrefs.SetInt("ForceShedDoorSpawnOnce", 1);
        PlayerPrefs.Save();

        // Load farm scene
        SceneManager.LoadScene("FarmScene");
    }

    public IntroState GetCurrentState() => currentState;

    public bool IsPlayerFrozen => playerController != null && !playerController.enabled;
}
