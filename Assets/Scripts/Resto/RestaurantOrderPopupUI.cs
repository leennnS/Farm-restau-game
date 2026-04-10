using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestaurantOrderPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Content")]
    [SerializeField] private Image recipeImage;
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button serveButton;
    [SerializeField] private Sprite fallbackRecipeSprite;

    public event System.Action OnServePressed;

    private void OnEnable()
    {
        if (serveButton != null)
            serveButton.onClick.AddListener(HandleServeClicked);
    }

    private void OnDisable()
    {
        if (serveButton != null)
            serveButton.onClick.RemoveListener(HandleServeClicked);
    }

    private void Awake()
    {
        Hide();
    }

    public void ShowOrder(RecipeDefinition recipe, float remainingSeconds = -1f, bool canServeNow = false)
    {
        if (popupRoot == null)
        {
            Debug.LogWarning("[RestaurantOrderPopupUI] popupRoot is not assigned. Cannot show order popup.");
            return;
        }

        popupRoot.SetActive(true);

        if (recipeNameText != null)
            recipeNameText.text = recipe != null ? recipe.recipeName : "Unknown Order";

        if (statusText != null)
        {
            if (remainingSeconds >= 0f)
                statusText.text = $"Cook and deliver this order ({Mathf.CeilToInt(remainingSeconds)}s)";
            else
                statusText.text = "Cook and deliver this order";
        }

        if (recipeImage != null)
        {
            if (recipe != null && recipe.recipeIcon != null)
                recipeImage.sprite = recipe.recipeIcon;
            else
                recipeImage.sprite = fallbackRecipeSprite;

            recipeImage.enabled = recipeImage.sprite != null;
        }

        if (serveButton != null)
            serveButton.interactable = canServeNow;
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void ShowServedMessage(string message)
    {
        if (popupRoot == null)
        {
            Debug.LogWarning("[RestaurantOrderPopupUI] popupRoot is not assigned. Cannot show served message.");
            return;
        }

        popupRoot.SetActive(true);

        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? "Order served" : message;

        if (serveButton != null)
            serveButton.interactable = false;
    }

    private void HandleServeClicked()
    {
        OnServePressed?.Invoke();
    }
}
