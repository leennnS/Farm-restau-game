using UnityEngine;


/// <summary>
/// Sets up atmospheric lighting for the intro scene with two point lights.
/// One overhead glow above player, one from window area with cool tone.
/// </summary>
public class IntroLighting : MonoBehaviour
{
    [SerializeField]
    private Transform playerCharacter;

    [SerializeField]
    private float lightIntensity = 0.5f;

    private void Awake()
    {
        // Set a dark ambient light with cool blue tone to simulate window mood
        RenderSettings.ambientLight = new Color(0.2f, 0.25f, 0.35f, 1f);

        SetupCharacterLight();
    }

    private void SetupCharacterLight()
    {
        // Create a warm point light centered above the player (the lantern glow)
        GameObject charLightGO = new GameObject("CharacterLightGlow");
        charLightGO.transform.SetParent(transform);

        if (playerCharacter != null)
        {
            charLightGO.transform.position = playerCharacter.position + new Vector3(8f, 4f, -3f);
        }
        else
        {
            charLightGO.transform.localPosition = new Vector3(8f, 4f, -3f);
        }

        UnityEngine.Rendering.Universal.Light2D charLight = charLightGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        charLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        charLight.color = new Color(0.85f, 0.8f, 0.75f, 1f); // Warm off-white
        charLight.intensity = lightIntensity;
        charLight.pointLightOuterRadius = 10f;
    }
}
