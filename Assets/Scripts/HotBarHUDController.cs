using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HotBarHUDController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("HotBarHUDController: UIDocument component not found!");
            return;
        }

        _root = _uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("HotBarHUDController: Root visual element is null!");
            return;
        }

        // Ensure hotbar is visible at startup
        SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

