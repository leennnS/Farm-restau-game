using UnityEngine;

public class WaterRippleScroll : MonoBehaviour
{
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.03f, 0.01f);

    private Material runtimeMaterial;
    private Vector2 currentOffset;

    private void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        runtimeMaterial = new Material(sr.sharedMaterial);
        sr.material = runtimeMaterial;
    }

    private void Update()
    {
        currentOffset += scrollSpeed * Time.deltaTime;
        runtimeMaterial.mainTextureOffset = currentOffset;
    }
}