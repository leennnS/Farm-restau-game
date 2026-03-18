using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Visual-only pickup object that rises from a source and then flies to a target.
/// Calls a callback when it reaches the target.
/// </summary>
public class FlyingRewardVisual : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float riseHeight = 0.7f;
    [SerializeField] private float riseDuration = 0.12f;
    [SerializeField] private float flyDuration = 0.28f;
    [SerializeField] private float flyArcHeight = 0.35f;

    [Header("Visual")]
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private float defaultScale = 1f;

    private SpriteRenderer _spriteRenderer;
    private Action _onReached;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        _spriteRenderer.sortingOrder = sortingOrder;
    }

    public void Play(Vector3 spawnPosition, Vector3 targetPosition, Sprite icon, Action onReached)
    {
        Play(spawnPosition, targetPosition, icon, defaultScale, onReached);
    }

    public void Play(Vector3 spawnPosition, Vector3 targetPosition, Sprite icon, float scaleMultiplier, Action onReached)
    {
        _onReached = onReached;

        transform.position = spawnPosition;
        _spriteRenderer.sprite = icon;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, scaleMultiplier);

        StartCoroutine(AnimateRoutine(targetPosition));
    }

    private IEnumerator AnimateRoutine(Vector3 targetPosition)
    {
        Vector3 riseStart = transform.position;
        Vector3 riseEnd = riseStart + Vector3.up * riseHeight;

        float t = 0f;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, riseDuration));
            transform.position = Vector3.Lerp(riseStart, riseEnd, u);
            yield return null;
        }

        Vector3 flyStart = transform.position;
        t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.0001f, flyDuration));
            Vector3 basePos = Vector3.Lerp(flyStart, targetPosition, u);
            float arc = Mathf.Sin(u * Mathf.PI) * flyArcHeight;
            transform.position = basePos + Vector3.up * arc;
            yield return null;
        }

        transform.position = targetPosition;
        _onReached?.Invoke();
        Destroy(gameObject);
    }
}
