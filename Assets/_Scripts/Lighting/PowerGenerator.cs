using FishNet.Object;
using UnityEngine;

public class PowerGenerator : NetworkBehaviour, IInteractable
{
    [Header("Generator Settings")]
    [SerializeField]
    private float repairTime = 3f;

    [SerializeField]
    private float cooldownTime = 5f;

    [Header("Visual Feedback")]
    [SerializeField]
    private Light statusLight;

    [SerializeField]
    private Color workingColor = Color.green;

    [SerializeField]
    private Color brokenColor = Color.red;

    [SerializeField]
    private Color repairingColor = Color.yellow;

    [Header("Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip generatorHumSound;

    [SerializeField]
    private AudioClip repairSound;

    [SerializeField]
    private AudioClip startupSound;

    [Header("Effects")]
    [SerializeField]
    private ParticleSystem sparkEffect;

    [SerializeField]
    private ParticleSystem smokeEffect;

    [SerializeField]
    private GameObject[] mechanicalParts;

    private float _repairProgress = 0f;
    private Player _currentRepairer = null;
    private bool _isHovered = false;

    // Generator states
    public enum GeneratorState
    {
        Working,
        Broken,
        Repairing,
        Cooldown,
    }

    private GeneratorState _currentState = GeneratorState.Working;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Subscribe to blackout events to break generator when power outage occurs
        RoomLightingManager.OnBlackoutStateChanged += OnBlackoutStateChanged;

        UpdateVisualState();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        RoomLightingManager.OnBlackoutStateChanged -= OnBlackoutStateChanged;
    }

    private void OnBlackoutStateChanged(bool isInBlackout)
    {
        if (isInBlackout && IsServerInitialized)
        {
            // Generator breaks when blackout occurs
            SetGeneratorStateServerRpc(GeneratorState.Broken);
        }
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        // Handle repair progress - use state instead of boolean
        if (_currentState == GeneratorState.Repairing && _currentRepairer != null)
        {
            _repairProgress += Time.deltaTime;

            // Update repair progress on clients
            UpdateRepairProgressClientRpc(_repairProgress / repairTime);

            if (_repairProgress >= repairTime)
            {
                CompleteRepair();
            }

            // Check if repairer is still close enough
            if (Vector3.Distance(transform.position, _currentRepairer.transform.position) > 3f)
            {
                CancelRepair();
            }
        }
    }

    #region IInteractable Implementation

    public bool CanInteract()
    {
        if (RoomLightingManager.Instance == null)
            return false;

        return _currentState == GeneratorState.Broken
            && RoomLightingManager.Instance.IsInBlackout();
    }

    public void OnHoverEnter()
    {
        _isHovered = true;

        if (statusLight != null)
        {
            statusLight.intensity = 2f; // Brighten the status light
        }
    }

    public void OnHoverExit()
    {
        _isHovered = false;

        if (statusLight != null)
        {
            statusLight.intensity = 1f; // Return to normal intensity
        }
    }

    public void OnInteractStart()
    {
        if (!CanInteract())
            return;

        Player player = FindNearestPlayer();
        if (player == null)
            return;

        StartRepairServerRpc(player.NetworkObject);
    }

    public void OnInteractHold(float duration) { }

    public void OnInteractEnd(bool completed) { }

    #endregion

    // Helper method to find the nearest player
    private Player FindNearestPlayer()
    {
        Player[] players = PlayerManager.Instance.GetAllPlayers().ToArray();
        Player nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (Player player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < nearestDistance && distance <= 3f) // Within interaction range
            {
                nearestDistance = distance;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
    }

    #region Server RPCs

    [ServerRpc(RequireOwnership = false)]
    private void StartRepairServerRpc(NetworkObject playerNetworkObject)
    {
        // Use state instead of boolean
        if (_currentState == GeneratorState.Repairing || _currentState != GeneratorState.Broken)
            return;

        Player player = playerNetworkObject.GetComponent<Player>();
        if (player == null)
            return;

        _currentRepairer = player;
        _repairProgress = 0f;

        SetGeneratorStateServerRpc(GeneratorState.Repairing);

        // Play repair sound on all clients
        PlayRepairSoundClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CancelRepairServerRpc()
    {
        CancelRepair();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGeneratorStateServerRpc(GeneratorState newState)
    {
        _currentState = newState;
        UpdateGeneratorStateClientRpc(newState);
    }

    #endregion

    #region Client RPCs

    [ObserversRpc]
    private void UpdateGeneratorStateClientRpc(GeneratorState newState)
    {
        _currentState = newState;
        UpdateVisualState();
    }

    [ObserversRpc]
    private void UpdateRepairProgressClientRpc(float progress)
    {
        _repairProgress = Mathf.Clamp01(progress) * repairTime;
    }

    [ObserversRpc]
    private void PlayRepairSoundClientRpc()
    {
        if (audioSource != null && repairSound != null)
        {
            audioSource.clip = repairSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (sparkEffect != null)
            sparkEffect.Play();
    }

    [ObserversRpc]
    private void PlayStartupSoundClientRpc()
    {
        if (audioSource != null && startupSound != null)
        {
            audioSource.PlayOneShot(startupSound);
        }
    }

    [ObserversRpc]
    private void StopRepairEffectsClientRpc()
    {
        if (audioSource != null && audioSource.clip == repairSound)
        {
            audioSource.Stop();
        }

        if (sparkEffect != null)
            sparkEffect.Stop();
    }

    #endregion

    private void CompleteRepair()
    {
        _currentRepairer = null;
        _repairProgress = 0f;

        // Stop repair effects
        StopRepairEffectsClientRpc();

        // Reset the blackout
        if (RoomLightingManager.Instance != null)
        {
            RoomLightingManager.Instance.ResetBlackoutServerRpc();
        }

        // Set generator to working state
        SetGeneratorStateServerRpc(GeneratorState.Working);

        // Play startup sound
        PlayStartupSoundClientRpc();

        // Start cooldown
        StartCoroutine(CooldownCoroutine());

        Debug.Log("Generator repaired! Blackout reset.");
    }

    private void CancelRepair()
    {
        _currentRepairer = null;
        _repairProgress = 0f;

        // Stop repair effects
        StopRepairEffectsClientRpc();

        // Return to broken state
        SetGeneratorStateServerRpc(GeneratorState.Broken);

        Debug.Log("Generator repair cancelled.");
    }

    private System.Collections.IEnumerator CooldownCoroutine()
    {
        SetGeneratorStateServerRpc(GeneratorState.Cooldown);
        yield return new WaitForSeconds(cooldownTime);

        // After cooldown, keep working until next blackout
        // The generator will break again when the next blackout occurs
    }

    private void UpdateVisualState()
    {
        // Update status light
        if (statusLight != null)
        {
            switch (_currentState)
            {
                case GeneratorState.Working:
                    statusLight.color = workingColor;
                    statusLight.enabled = true;
                    break;
                case GeneratorState.Broken:
                    statusLight.color = brokenColor;
                    statusLight.enabled = true;
                    break;
                case GeneratorState.Repairing:
                    statusLight.color = repairingColor;
                    statusLight.enabled = true;
                    break;
                case GeneratorState.Cooldown:
                    statusLight.color = workingColor;
                    statusLight.enabled = true;
                    break;
            }

            // Adjust intensity based on hover state
            statusLight.intensity = _isHovered ? 2f : 1f;
        }

        // Update audio
        if (audioSource != null)
        {
            if (_currentState == GeneratorState.Working && generatorHumSound != null)
            {
                if (audioSource.clip != generatorHumSound)
                {
                    audioSource.clip = generatorHumSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }
            }
            else if (_currentState != GeneratorState.Repairing)
            {
                if (audioSource.clip == generatorHumSound)
                {
                    audioSource.Stop();
                }
            }
        }

        // Update smoke effect
        if (smokeEffect != null)
        {
            if (_currentState == GeneratorState.Broken)
            {
                if (!smokeEffect.isPlaying)
                    smokeEffect.Play();
            }
            else
            {
                if (smokeEffect.isPlaying)
                    smokeEffect.Stop();
            }
        }

        // Animate mechanical parts
        AnimateMechanicalParts();
    }

    private void AnimateMechanicalParts()
    {
        if (mechanicalParts == null)
            return;

        bool shouldAnimate = _currentState == GeneratorState.Working;

        foreach (GameObject part in mechanicalParts)
        {
            if (part != null && shouldAnimate)
            {
                part.transform.Rotate(0, 0, 90f * Time.deltaTime);
            }
        }
    }

    // Public getters
    public GeneratorState GetCurrentState() => _currentState;

    public bool IsWorking() => _currentState == GeneratorState.Working;

    public float GetRepairProgress() => _repairProgress / repairTime;
}
