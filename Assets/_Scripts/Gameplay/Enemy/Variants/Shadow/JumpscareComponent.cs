using System.Collections;
using FishNet.Object;
using UnityEngine;

public class JumpscareComponent : NetworkBehaviour
{
    [Header("Jumpscare Settings")]
    [SerializeField]
    private float jumpscareDuration = 1.5f;

    [SerializeField]
    private int stressAmount = 35;

    [Header("Audio")]
    [SerializeField]
    private string jumpscareSoundId = "shadow_jumpscare";

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string jumpscareAnimTrigger = "Jumpscare";

    // Visual Effects
    [Header("Visual Effects")]
    [SerializeField]
    private GameObject jumpscareVfxPrefab;

    [SerializeField]
    private float vfxDuration = 1.0f;

    // Internal state
    private bool _isJumpscaring = false;
    private float _jumpscareTimer = 0f;
    private Coroutine _jumpscareCoroutine;
    private Player _targetPlayer;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void StartJumpscare(Player target)
    {
        if (_isJumpscaring)
            return;

        _targetPlayer = target;

        if (_targetPlayer == null)
        {
            Debug.LogWarning("JumpscareComponent: No target player specified");
            return;
        }

        _isJumpscaring = true;

        if (IsServerInitialized)
        {
            if (animator != null)
                animator.SetTrigger(jumpscareAnimTrigger);

            if (_jumpscareCoroutine != null)
                StopCoroutine(_jumpscareCoroutine);

            _jumpscareCoroutine = StartCoroutine(PerformJumpscareCoroutine());
        }
    }

    public void StopJumpscare()
    {
        if (!_isJumpscaring)
            return;

        _isJumpscaring = false;

        if (_jumpscareCoroutine != null)
        {
            StopCoroutine(_jumpscareCoroutine);
            _jumpscareCoroutine = null;
        }
    }

    public bool IsJumpscaring()
    {
        return _isJumpscaring;
    }

    public bool IsJumpscareComplete()
    {
        return !_isJumpscaring && _jumpscareTimer >= jumpscareDuration;
    }

    [ObserversRpc]
    private void ClientRpcPlayJumpscareEffects(Vector3 playerPosition)
    {
        // Play jumpscare sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(jumpscareSoundId, transform.position, 1.0f);
        }

        // Play animation
        if (animator != null)
            animator.SetTrigger(jumpscareAnimTrigger);

        // Spawn visual effect if available
        if (jumpscareVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                jumpscareVfxPrefab,
                playerPosition + new Vector3(0, 1.7f, 0),
                Quaternion.identity
            );
            Destroy(vfx, vfxDuration);
        }
    }

    private IEnumerator PerformJumpscareCoroutine()
    {
        _jumpscareTimer = 0f;

        // Get player position for effects
        Vector3 playerPosition = _targetPlayer.transform.position;

        // Play effects on all clients
        ClientRpcPlayJumpscareEffects(playerPosition);

        // Apply stress to target player
        ApplyJumpscareStress();

        // Wait for jumpscare duration
        yield return new WaitForSeconds(jumpscareDuration);

        _jumpscareTimer = jumpscareDuration;
        _isJumpscaring = false;
    }

    private void ApplyJumpscareStress()
    {
        if (!IsServerInitialized || _targetPlayer == null)
            return;

        StressController stressController = _targetPlayer.GetComponent<StressController>();
        if (stressController != null)
        {
            stressController.AddStress(stressAmount, "enemy_jumpscare");
        }
    }
}
