using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit rendering-only view for the fishing mini-game.
/// Handles layout, animation classes, and visual updates.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class FishingMiniGameView : MonoBehaviour
{
    [Header("Optional Textures")]
    [SerializeField] private Texture2D panelTexture;
    [SerializeField] private Texture2D trimTexture;
    [SerializeField] private Texture2D trackTexture;
    [SerializeField] private Texture2D fishIconTexture;
    [SerializeField] private Texture2D hookIconTexture;
    [SerializeField] private Texture2D bobberIconTexture;

    [Header("Track Texture Fit")]
    [SerializeField, Range(100f, 400f)] private float trackTextureHeightPercent = 280f;

    private UIDocument _document;
    private VisualElement _panel;
    private VisualElement _trackArt;
    private VisualElement _track;
    private VisualElement _fishTarget;
    private VisualElement _zone;
    private VisualElement _catchFill;
    private VisualElement _tensionFill;
    private Label _fishNameLabel;
    private Label _inputHintLabel;
    private Label _statusLabel;
    private VisualElement _resultChip;
    private Label _resultText;
    private Label _rarityLabel;
    private Label _fishGlyphLabel;
    private Label _hookIconLabel;
    private Label _bobberIconLabel;

    private bool _isReady;

    public bool IsReady => _isReady;

    private void Awake()
    {
        BuildReferences();
    }

    public void BuildReferences()
    {
        _document = GetComponent<UIDocument>();
        if (_document == null)
            return;

        VisualElement root = _document.rootVisualElement;
        if (root == null)
            return;

        _panel = root.Q<VisualElement>("fishing-hold-panel");
        _trackArt = root.Q<VisualElement>("track-art");
        _track = root.Q<VisualElement>("fish-track");
        _fishTarget = root.Q<VisualElement>("fish-target");
        _zone = root.Q<VisualElement>("catch-zone");
        _catchFill = root.Q<VisualElement>("catch-fill");
        _tensionFill = root.Q<VisualElement>("tension-fill");
        _fishNameLabel = root.Q<Label>("catchable-name-label");
        _inputHintLabel = root.Q<Label>("input-hint");
        _statusLabel = root.Q<Label>("status-label");
        _resultChip = root.Q<VisualElement>("result-chip");
        _resultText = root.Q<Label>("result-text");
        _rarityLabel = root.Q<Label>("rarity-label");
        _fishGlyphLabel = root.Q<Label>("fish-glyph-label");
        _hookIconLabel = root.Q<Label>("hook-icon-label");
        _bobberIconLabel = root.Q<Label>("bobber-icon-label");

        ApplyOptionalTextures();

        if (_panel != null)
            _panel.style.display = DisplayStyle.None;

        if (_resultChip != null)
            _resultChip.style.display = DisplayStyle.None;

        _isReady = _panel != null && _track != null && _fishTarget != null && _zone != null;
    }

    private void ApplyOptionalTextures()
    {
        if (panelTexture != null && _panel != null)
            ApplyFullPanelBackground(_panel, panelTexture);

        VisualElement trim = _document.rootVisualElement.Q<VisualElement>("panel-trim");
        if (trimTexture != null && trim != null)
            ApplyFullPanelBackground(trim, trimTexture);

        if (trackTexture != null)
        {
            if (_track != null)
                ApplyTrackBackground(_track, trackTexture, trackTextureHeightPercent);

            if (_trackArt != null)
                _trackArt.style.display = DisplayStyle.None;
        }

        if (fishIconTexture != null && _fishTarget != null)
            ApplyFittedBackground(_fishTarget, fishIconTexture);

        if (_fishGlyphLabel != null)
            _fishGlyphLabel.style.display = fishIconTexture == null ? DisplayStyle.Flex : DisplayStyle.None;

        VisualElement hookIcon = _document.rootVisualElement.Q<VisualElement>("hook-icon");
        if (hookIconTexture != null && hookIcon != null)
            ApplyFittedBackground(hookIcon, hookIconTexture);

        if (_hookIconLabel != null)
            _hookIconLabel.style.display = hookIconTexture == null ? DisplayStyle.Flex : DisplayStyle.None;

        VisualElement bobberIcon = _document.rootVisualElement.Q<VisualElement>("bobber-icon");
        if (bobberIconTexture != null && bobberIcon != null)
            ApplyFittedBackground(bobberIcon, bobberIconTexture);

        if (_bobberIconLabel != null)
            _bobberIconLabel.style.display = bobberIconTexture == null ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void ApplyFittedBackground(VisualElement element, Texture2D texture)
    {
        element.style.backgroundImage = new StyleBackground(texture);
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
    }

    private static void ApplyTrackBackground(VisualElement element, Texture2D texture, float heightPercent)
    {
        element.style.backgroundImage = new StyleBackground(texture);
        float clampedHeight = Mathf.Clamp(heightPercent, 100f, 400f);
        element.style.backgroundSize = new BackgroundSize(Length.Percent(100f), Length.Percent(clampedHeight));
        element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
    }

    private static void ApplyFullPanelBackground(VisualElement element, Texture2D texture)
    {
        element.style.backgroundImage = new StyleBackground(texture);
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
    }

    public void Show()
    {
        if (!_isReady)
            return;

        _panel.style.display = DisplayStyle.Flex;
        _panel.RemoveFromClassList("fishing-panel-hidden");
        _panel.AddToClassList("fishing-panel-visible");

        if (_resultChip != null)
            _resultChip.style.display = DisplayStyle.None;
    }

    public void Hide()
    {
        if (!_isReady)
            return;

        _panel.RemoveFromClassList("fishing-panel-visible");
        _panel.AddToClassList("fishing-panel-hidden");
        _panel.style.display = DisplayStyle.None;
    }

    public void SetFishName(string fishName)
    {
        if (_fishNameLabel != null)
            _fishNameLabel.text = string.IsNullOrWhiteSpace(fishName) ? "Unknown Catch" : fishName;
    }

    public void SetRarityText(string rarityText)
    {
        if (_rarityLabel != null)
            _rarityLabel.text = rarityText;
    }

    public void SetInputHint(string hint)
    {
        if (_inputHintLabel != null)
            _inputHintLabel.text = hint;
    }

    public void SetStatus(string text, Color color)
    {
        if (_statusLabel == null)
            return;

        _statusLabel.text = text;
        _statusLabel.style.color = new StyleColor(color);
    }

    public void RenderSnapshot(FishingBarSnapshot snapshot)
    {
        if (!_isReady)
            return;

        float trackWidth = _track.resolvedStyle.width;
        if (trackWidth <= 0f)
            trackWidth = 640f;

        float fishSize = _fishTarget.resolvedStyle.width;
        if (fishSize <= 0f)
            fishSize = 42f;

        float zoneWidthPx = Mathf.Max(38f, trackWidth * snapshot.zoneWidth01);
        float zoneLeft = Mathf.Clamp((snapshot.zoneCenter01 * trackWidth) - zoneWidthPx * 0.5f, 0f, trackWidth - zoneWidthPx);
        float fishLeft = Mathf.Clamp(snapshot.fish01 * trackWidth - fishSize * 0.5f, 0f, trackWidth - fishSize);

        _zone.style.left = zoneLeft;
        _zone.style.width = zoneWidthPx;
        _fishTarget.style.left = fishLeft;

        if (_catchFill != null)
            _catchFill.style.width = Length.Percent(Mathf.Clamp01(snapshot.catchProgress01) * 100f);

        if (_tensionFill != null)
            _tensionFill.style.width = Length.Percent(Mathf.Clamp01(snapshot.tension01) * 100f);

        if (_track != null)
        {
            if (snapshot.warning)
                _track.AddToClassList("danger-pulse");
            else
                _track.RemoveFromClassList("danger-pulse");
        }
    }

    public void ShowResult(string message, bool success, bool perfect)
    {
        if (_resultChip == null || _resultText == null)
            return;

        _resultChip.style.display = DisplayStyle.Flex;
        _resultChip.RemoveFromClassList("result-success");
        _resultChip.RemoveFromClassList("result-fail");
        _resultChip.RemoveFromClassList("result-perfect");

        if (perfect)
            _resultChip.AddToClassList("result-perfect");
        else if (success)
            _resultChip.AddToClassList("result-success");
        else
            _resultChip.AddToClassList("result-fail");

        _resultText.text = message;
    }
}
