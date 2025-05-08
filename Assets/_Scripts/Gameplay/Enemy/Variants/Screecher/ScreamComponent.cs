using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class ScreamComponent : NetworkBehaviour
{
    [Header("Scream Settings")]
    [SerializeField]
    private float screamDuration = 3.0f;

    [SerializeField]
    private float screamRadius = 15.0f;

    [SerializeField]
    private int minStressAmount = 10;

    [SerializeField]
    private int maxStressAmount = 30;

    [SerializeField]
    private AnimationCurve distanceFalloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Audio")]
    [SerializeField]
    private string screamSoundId = "screecher_scream";

    [SerializeField]
    private float soundRadius = 20.0f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string screamAnimTrigger = "Scream";

    // Internal state
    private bool _isScreaming = false;
    private float _screamTimer = 0f;
    private Coroutine _screamCoroutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void StartScreaming()
    {
        if (_isScreaming)
            return;

        _isScreaming = true;

        if (IsServerInitialized)
        {
            if (animator != null)
                animator.SetTrigger(screamAnimTrigger);

            if (_screamCoroutine != null)
                StopCoroutine(_screamCoroutine);

            _screamCoroutine = StartCoroutine(ApplyStressWavesCoroutine());
        }
    }

    public void StopScreaming()
    {
        if (!_isScreaming)
            return;

        _isScreaming = false;

        if (_screamCoroutine != null)
        {
            StopCoroutine(_screamCoroutine);
            _screamCoroutine = null;
        }
    }

    public bool IsScreaming()
    {
        return _isScreaming;
    }

    public bool IsScreamComplete()
    {
        return !_isScreaming && _screamTimer >= screamDuration;
    }

    [ObserversRpc]
    private void ClientRpcPlayScreamEffects()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(screamSoundId, transform.position, 1.0f);
        }

        if (animator != null)
            animator.SetTrigger(screamAnimTrigger);
    }

    private IEnumerator ApplyStressWavesCoroutine()
    {
        _screamTimer = 0f;
        float interval = screamDuration / 3 / 2;

        ClientRpcPlayScreamEffects();
        yield return new WaitForSeconds(interval);

        ApplyStressToNearbyPlayers(0.5f);
        yield return new WaitForSeconds(interval);

        ClientRpcPlayScreamEffects();
        yield return new WaitForSeconds(interval);

        ApplyStressToNearbyPlayers(0.75f);
        yield return new WaitForSeconds(interval);

        ClientRpcPlayScreamEffects();
        yield return new WaitForSeconds(interval);
        ApplyStressToNearbyPlayers(1.0f);

        _screamTimer = screamDuration;
        _isScreaming = false;
    }

    private void ApplyStressToNearbyPlayers(float intensityMultiplier)
    {
        if (!IsServerInitialized)
            return;

        List<Player> nearbyPlayers = PlayerManager.Instance.GetPlayersInRange(
            transform.position,
            screamRadius
        );

        foreach (Player player in nearbyPlayers)
        {
            if (player == null)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            float distanceFactor = distanceFalloff.Evaluate(distance / screamRadius);

            int stressAmount = Mathf.RoundToInt(
                Mathf.Lerp(minStressAmount, maxStressAmount, distanceFactor) * intensityMultiplier
            );

            StressController stressController = player.GetComponent<StressController>();
            if (stressController != null)
            {
                // Using the direct API rather than an RPC as we're already on the server
                stressController.AddStress(stressAmount, "enemy_scream");
            }
        }
    }
}
