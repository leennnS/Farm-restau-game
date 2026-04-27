using System;
using UnityEngine;

/// <summary>
/// Pure gameplay logic for the horizontal fishing bar mini-game.
/// No UI, no inventory, no scene coupling.
/// </summary>
public class FishingBarMiniGameLogic : MonoBehaviour
{
    [Header("Fish Motion")]
    [SerializeField] private float fishSpeedMin = 0.18f;
    [SerializeField] private float fishSpeedMax = 0.58f;
    [SerializeField] private float fishDirectionChangeMin = 0.45f;
    [SerializeField] private float fishDirectionChangeMax = 1.25f;
    [SerializeField] private float fishJitterStrength = 0.08f;

    [Header("Hook Zone Control")]
    [SerializeField] private float zoneMoveSpeed = 0.68f;
    [SerializeField] private float zonePullBackSpeed = 0.48f;
    [SerializeField] private float zoneWidth = 0.18f;
    [SerializeField] private float holdMoveBoostMultiplier = 1.25f;

    [Header("Accessibility Tuning")]
    [SerializeField] private float fishSpeedMultiplier = 0.85f;

    [Header("Progress Rules")]
    [SerializeField] private float catchFillRate = 0.42f;
    [SerializeField] private float catchDecayRate = 0.24f;
    [SerializeField] private float tensionGainRate = 0.34f;
    [SerializeField] private float tensionRecoverRate = 0.32f;

    [Header("Perfect Catch")]
    [SerializeField] private float perfectMaxTension = 0.22f;
    [SerializeField] private float perfectMinInsideRatio = 0.72f;

    private bool _running;
    private bool _holding;
    private float _difficulty;

    private float _fishPos;
    private float _fishVel;
    private float _nextFishTurnTimer;

    private float _zoneCenter;
    private float _catchProgress;
    private float _tension;

    private float _elapsed;
    private float _insideTime;

    public event Action<FishingBarSnapshot> OnStateChanged;
    public event Action<bool, bool> OnMiniGameFinished;

    public bool IsRunning => _running;

    public void Begin(float difficulty01)
    {
        _difficulty = Mathf.Clamp01(difficulty01);
        _running = true;
        _holding = false;

        _fishPos = 0.5f;
        _zoneCenter = 0.35f;
        _catchProgress = 0f;
        _tension = 0f;
        _elapsed = 0f;
        _insideTime = 0f;

        ResetFishVelocity();
        PublishState();
    }

    public void Stop()
    {
        _running = false;
    }

    public void SetHolding(bool holding)
    {
        _holding = holding;
    }

    public void Tick(float deltaTime)
    {
        if (!_running)
            return;

        float dt = Mathf.Max(0f, deltaTime);
        _elapsed += dt;

        UpdateFish(dt);
        UpdateZone(dt);
        UpdateMeters(dt);
        PublishState();

        if (_catchProgress >= 1f)
        {
            _running = false;
            float insideRatio = _elapsed <= 0.0001f ? 0f : (_insideTime / _elapsed);
            bool perfect = _tension <= perfectMaxTension && insideRatio >= perfectMinInsideRatio;
            OnMiniGameFinished?.Invoke(true, perfect);
            return;
        }

        if (_tension >= 1f)
        {
            _running = false;
            OnMiniGameFinished?.Invoke(false, false);
        }
    }

    private void UpdateFish(float dt)
    {
        _nextFishTurnTimer -= dt;
        if (_nextFishTurnTimer <= 0f)
            ResetFishVelocity();

        float jitter = UnityEngine.Random.Range(-fishJitterStrength, fishJitterStrength) * (0.7f + _difficulty) * fishSpeedMultiplier;
        _fishPos += (_fishVel + jitter) * dt;

        if (_fishPos <= 0f)
        {
            _fishPos = 0f;
            _fishVel = Mathf.Abs(_fishVel);
        }
        else if (_fishPos >= 1f)
        {
            _fishPos = 1f;
            _fishVel = -Mathf.Abs(_fishVel);
        }
    }

    private void UpdateZone(float dt)
    {
        float move = _holding ? zoneMoveSpeed * holdMoveBoostMultiplier : -zonePullBackSpeed;
        _zoneCenter = Mathf.Clamp01(_zoneCenter + move * dt);
    }

    private void UpdateMeters(float dt)
    {
        float halfZone = Mathf.Max(0.04f, zoneWidth * 0.5f);
        bool fishInside = Mathf.Abs(_fishPos - _zoneCenter) <= halfZone;

        float difficultyScale = 1f + _difficulty * 0.45f;

        if (fishInside)
        {
            _insideTime += dt;
            _catchProgress = Mathf.Clamp01(_catchProgress + catchFillRate * difficultyScale * dt);
            _tension = Mathf.Clamp01(_tension - tensionRecoverRate * dt);
        }
        else
        {
            _catchProgress = Mathf.Clamp01(_catchProgress - catchDecayRate * dt);
            _tension = Mathf.Clamp01(_tension + tensionGainRate * difficultyScale * dt);
        }
    }

    private void ResetFishVelocity()
    {
        float speed = UnityEngine.Random.Range(fishSpeedMin, fishSpeedMax) * (0.75f + _difficulty * 0.7f) * fishSpeedMultiplier;
        _fishVel = UnityEngine.Random.value < 0.5f ? -speed : speed;
        _nextFishTurnTimer = UnityEngine.Random.Range(fishDirectionChangeMin, fishDirectionChangeMax);
    }

    private void PublishState()
    {
        if (OnStateChanged == null)
            return;

        float halfZone = Mathf.Max(0.04f, zoneWidth * 0.5f);
        bool fishInside = Mathf.Abs(_fishPos - _zoneCenter) <= halfZone;

        FishingBarSnapshot snapshot = new FishingBarSnapshot
        {
            fish01 = _fishPos,
            zoneCenter01 = _zoneCenter,
            zoneWidth01 = zoneWidth,
            catchProgress01 = _catchProgress,
            tension01 = _tension,
            fishInsideZone = fishInside,
            warning = _tension >= 0.75f
        };

        OnStateChanged.Invoke(snapshot);
    }
}

public struct FishingBarSnapshot
{
    public float fish01;
    public float zoneCenter01;
    public float zoneWidth01;
    public float catchProgress01;
    public float tension01;
    public bool fishInsideZone;
    public bool warning;
}
