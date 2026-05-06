using System.Collections;
using UnityEngine;

public enum AnimalMoodIcon
{
    None,
    Heart,
    Sleep,
    Food,
    Egg,
    Alert
}

public class AnimalMoodBubble : MonoBehaviour
{
    private const int TextureSize = 32;

    private static Sprite s_bubbleSprite;
    private static Sprite s_heartSprite;
    private static Sprite s_sleepSprite;
    private static Sprite s_foodSprite;
    private static Sprite s_eggSprite;
    private static Sprite s_alertSprite;

    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.05f, 0f);
    [SerializeField] private float bubbleScale = 0.42f;
    [SerializeField] private int sortingOrderOffset = 25;

    private SpriteRenderer _bubbleRenderer;
    private SpriteRenderer _iconRenderer;
    private Coroutine _timedRoutine;
    private AnimalMoodIcon _persistentIcon;
    private float _bobSeed;
    private float _baseScale;

    public void Initialize(SpriteRenderer ownerRenderer, Vector3 offset)
    {
        EnsureSprites();

        localOffset = offset;
        _bobSeed = Random.Range(0f, 10f);
        _baseScale = bubbleScale;

        GameObject bubbleGo = new GameObject("MoodBubble");
        bubbleGo.transform.SetParent(transform, false);
        bubbleGo.transform.localPosition = localOffset;

        _bubbleRenderer = bubbleGo.AddComponent<SpriteRenderer>();
        _bubbleRenderer.sprite = s_bubbleSprite;
        _bubbleRenderer.sortingLayerID = ownerRenderer != null ? ownerRenderer.sortingLayerID : 0;
        _bubbleRenderer.sortingOrder = ownerRenderer != null ? ownerRenderer.sortingOrder + sortingOrderOffset : sortingOrderOffset;

        GameObject iconGo = new GameObject("MoodIcon");
        iconGo.transform.SetParent(bubbleGo.transform, false);
        iconGo.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        iconGo.transform.localScale = Vector3.one * 0.72f;

        _iconRenderer = iconGo.AddComponent<SpriteRenderer>();
        _iconRenderer.sortingLayerID = _bubbleRenderer.sortingLayerID;
        _iconRenderer.sortingOrder = _bubbleRenderer.sortingOrder + 1;

        SetVisible(false, 0f);
    }

    public void SetLocalOffset(Vector3 offset)
    {
        localOffset = offset;
    }

    public void ShowTimed(AnimalMoodIcon icon, float duration)
    {
        if (icon == AnimalMoodIcon.None)
            return;

        if (_timedRoutine != null)
            StopCoroutine(_timedRoutine);

        _timedRoutine = StartCoroutine(ShowTimedRoutine(icon, Mathf.Max(0.1f, duration)));
    }

    public void SetPersistent(AnimalMoodIcon icon)
    {
        _persistentIcon = icon;

        if (_timedRoutine != null)
            return;

        if (icon == AnimalMoodIcon.None)
            SetVisible(false, 0f);
        else
            SetIcon(icon, 0.9f);
    }

    private IEnumerator ShowTimedRoutine(AnimalMoodIcon icon, float duration)
    {
        float fadeSeconds = Mathf.Min(0.18f, duration * 0.25f);
        SetIcon(icon, 0f);

        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            SetVisible(true, Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }

        SetVisible(true, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, duration - fadeSeconds * 2f));

        t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            SetVisible(true, 1f - Mathf.Clamp01(t / fadeSeconds));
            yield return null;
        }

        _timedRoutine = null;

        if (_persistentIcon == AnimalMoodIcon.None)
            SetVisible(false, 0f);
        else
            SetIcon(_persistentIcon, 0.9f);
    }

    private void LateUpdate()
    {
        if (_bubbleRenderer == null)
            return;

        Transform bubbleTransform = _bubbleRenderer.transform;
        float bob = Mathf.Sin((Time.time + _bobSeed) * 3.2f) * 0.045f;
        float pulse = 1f + Mathf.Sin((Time.time + _bobSeed) * 5.5f) * 0.035f;
        bubbleTransform.localPosition = localOffset + Vector3.up * bob;
        bubbleTransform.localScale = Vector3.one * (_baseScale * pulse);
    }

    private void SetIcon(AnimalMoodIcon icon, float alpha)
    {
        if (_iconRenderer == null)
            return;

        _iconRenderer.sprite = GetSprite(icon);
        SetVisible(icon != AnimalMoodIcon.None, alpha);
    }

    private void SetVisible(bool visible, float alpha)
    {
        if (_bubbleRenderer == null || _iconRenderer == null)
            return;

        float a = visible ? Mathf.Clamp01(alpha) : 0f;
        Color bubbleColor = new Color(1f, 0.96f, 0.84f, a * 0.94f);
        Color iconColor = new Color(1f, 1f, 1f, a);
        _bubbleRenderer.color = bubbleColor;
        _iconRenderer.color = iconColor;
        _bubbleRenderer.enabled = a > 0.01f;
        _iconRenderer.enabled = a > 0.01f;
    }

    private static Sprite GetSprite(AnimalMoodIcon icon)
    {
        EnsureSprites();

        return icon switch
        {
            AnimalMoodIcon.Heart => s_heartSprite,
            AnimalMoodIcon.Sleep => s_sleepSprite,
            AnimalMoodIcon.Food => s_foodSprite,
            AnimalMoodIcon.Egg => s_eggSprite,
            AnimalMoodIcon.Alert => s_alertSprite,
            _ => null
        };
    }

    private static void EnsureSprites()
    {
        if (s_bubbleSprite != null)
            return;

        s_bubbleSprite = CreateSprite(DrawBubble);
        s_heartSprite = CreateSprite(DrawHeart);
        s_sleepSprite = CreateSprite(DrawSleep);
        s_foodSprite = CreateSprite(DrawFood);
        s_eggSprite = CreateSprite(DrawEgg);
        s_alertSprite = CreateSprite(DrawAlert);
    }

    private static Sprite CreateSprite(System.Action<Texture2D> drawer)
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
        drawer(texture);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
    }

    private static void DrawBubble(Texture2D texture)
    {
        Color fill = Color.white;
        Color edge = new Color(0.42f, 0.25f, 0.18f, 1f);
        DrawCircle(texture, 16, 17, 13, edge);
        DrawCircle(texture, 16, 17, 11, fill);
        DrawCircle(texture, 9, 5, 4, edge);
        DrawCircle(texture, 9, 5, 3, fill);
    }

    private static void DrawHeart(Texture2D texture)
    {
        Color c = new Color(0.95f, 0.1f, 0.2f, 1f);
        DrawCircle(texture, 11, 20, 5, c);
        DrawCircle(texture, 21, 20, 5, c);
        DrawTriangle(texture, new Vector2Int(6, 18), new Vector2Int(26, 18), new Vector2Int(16, 6), c);
    }

    private static void DrawSleep(Texture2D texture)
    {
        Color c = new Color(0.18f, 0.42f, 0.95f, 1f);
        DrawLine(texture, 8, 23, 20, 23, c, 2);
        DrawLine(texture, 20, 23, 8, 14, c, 2);
        DrawLine(texture, 8, 14, 20, 14, c, 2);
        DrawLine(texture, 17, 11, 25, 11, c, 2);
        DrawLine(texture, 25, 11, 17, 5, c, 2);
        DrawLine(texture, 17, 5, 25, 5, c, 2);
    }

    private static void DrawFood(Texture2D texture)
    {
        Color body = new Color(0.98f, 0.47f, 0.12f, 1f);
        Color leaf = new Color(0.18f, 0.7f, 0.25f, 1f);
        DrawTriangle(texture, new Vector2Int(10, 22), new Vector2Int(23, 18), new Vector2Int(13, 7), body);
        DrawCircle(texture, 13, 24, 3, leaf);
        DrawCircle(texture, 18, 25, 3, leaf);
    }

    private static void DrawEgg(Texture2D texture)
    {
        Color edge = new Color(0.63f, 0.42f, 0.2f, 1f);
        Color fill = new Color(1f, 0.92f, 0.72f, 1f);
        DrawEllipse(texture, 16, 15, 8, 11, edge);
        DrawEllipse(texture, 16, 15, 6, 9, fill);
    }

    private static void DrawAlert(Texture2D texture)
    {
        Color c = new Color(0.95f, 0.56f, 0.08f, 1f);
        DrawLine(texture, 16, 24, 16, 12, c, 4);
        DrawCircle(texture, 16, 7, 2, c);
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

    private static void DrawTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
    {
        int minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        int maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x, y);
                float d1 = Sign(p, a, b);
                float d2 = Sign(p, b, c);
                float d3 = Sign(p, c, a);
                bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
                bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;

                if (!(hasNeg && hasPos))
                    SetPixel(texture, x, y, color);
            }
        }
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            for (int y = -thickness / 2; y <= thickness / 2; y++)
            {
                for (int x = -thickness / 2; x <= thickness / 2; x++)
                    SetPixel(texture, x0 + x, y0 + y, color);
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
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
