using UnityEngine;

[DisallowMultipleComponent]
public class AnimalFeedingSpot : MonoBehaviour
{
    private const int TextureSize = 32;
    private static Sprite s_feedSprite;

    [SerializeField] private int servings = 4;
    [SerializeField] private float attractRadius = 6f;
    [SerializeField] private float timeToLive = 45f;

    private SpriteRenderer _spriteRenderer;

    public bool HasFood => servings > 0;
    public float AttractRadius => attractRadius;

    public static AnimalFeedingSpot Create(Vector3 position, int startingServings)
    {
        GameObject go = new GameObject("AnimalFeedPile");
        go.transform.position = position;

        AnimalFeedingSpot spot = go.AddComponent<AnimalFeedingSpot>();
        spot.servings = Mathf.Max(1, startingServings);
        spot.InitializeVisual();
        return spot;
    }

    private void Awake()
    {
        InitializeVisual();
    }

    private void Update()
    {
        timeToLive -= Time.deltaTime;
        if (timeToLive <= 0f)
            Destroy(gameObject);
    }

    public bool TryTakeServing()
    {
        if (servings <= 0)
            return false;

        servings--;
        RefreshVisualScale();

        if (servings <= 0)
            Destroy(gameObject, 0.15f);

        return true;
    }

    public static AnimalFeedingSpot FindBestSpot(Vector3 position)
    {
        AnimalFeedingSpot[] spots = FindObjectsByType<AnimalFeedingSpot>(FindObjectsSortMode.None);
        AnimalFeedingSpot best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < spots.Length; i++)
        {
            AnimalFeedingSpot spot = spots[i];
            if (spot == null || !spot.HasFood)
                continue;

            float distance = Vector2.Distance(position, spot.transform.position);
            if (distance > spot.AttractRadius || distance >= bestDistance)
                continue;

            best = spot;
            bestDistance = distance;
        }

        return best;
    }

    private void InitializeVisual()
    {
        if (_spriteRenderer != null)
            return;

        if (s_feedSprite == null)
            s_feedSprite = CreateFeedSprite();

        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        _spriteRenderer.sprite = s_feedSprite;
        _spriteRenderer.sortingOrder = 8;
        RefreshVisualScale();

        CircleCollider2D trigger = gameObject.GetComponent<CircleCollider2D>();
        if (trigger == null)
            trigger = gameObject.AddComponent<CircleCollider2D>();

        trigger.isTrigger = true;
        trigger.radius = 0.35f;
    }

    private void RefreshVisualScale()
    {
        float scale = Mathf.Lerp(0.32f, 0.5f, Mathf.Clamp01(servings / 4f));
        transform.localScale = Vector3.one * scale;
    }

    private static Sprite CreateFeedSprite()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        texture.SetPixels(pixels);

        Color shadow = new Color(0.28f, 0.18f, 0.08f, 1f);
        Color grain = new Color(0.95f, 0.72f, 0.28f, 1f);
        Color bright = new Color(1f, 0.86f, 0.42f, 1f);

        DrawEllipse(texture, 16, 12, 12, 5, shadow);
        DrawEllipse(texture, 16, 15, 10, 6, grain);
        DrawCircle(texture, 10, 17, 3, bright);
        DrawCircle(texture, 16, 20, 4, bright);
        DrawCircle(texture, 22, 17, 3, bright);
        DrawCircle(texture, 15, 14, 3, new Color(0.78f, 0.5f, 0.17f, 1f));

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.35f), TextureSize);
    }

    private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= r2)
                    SetPixel(texture, cx + x, cy + y, color);
            }
        }
    }

    private static void DrawEllipse(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
    {
        for (int y = -ry; y <= ry; y++)
        {
            for (int x = -rx; x <= rx; x++)
            {
                float nx = x / (float)rx;
                float ny = y / (float)ry;
                if (nx * nx + ny * ny <= 1f)
                    SetPixel(texture, cx + x, cy + y, color);
            }
        }
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= TextureSize || y < 0 || y >= TextureSize)
            return;

        texture.SetPixel(x, y, color);
    }
}
