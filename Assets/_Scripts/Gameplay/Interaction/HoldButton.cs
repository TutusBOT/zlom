using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Events;

public class Physical3DButton : NetworkBehaviour, IInteractable
{
    [Header("Button Settings")]
    public float requiredHoldTime = 1.0f;
    public float buttonTravelDistance = 0.05f;
    public float buttonReturnSpeed = 5f;

    [Header("Visual Feedback")]
    public Transform buttonMesh;
    public GameObject progressIndicator;
    public Material normalMaterial;
    public Material hoveredMaterial;
    public Material pressedMaterial;
    public Material activatedMaterial;

    [Header("Events")]
    public UnityEvent onButtonActivated;
    public UnityEvent onButtonPressed;
    public UnityEvent onButtonReleased;

    // Private fields
    private Vector3 _originalPosition;
    private Vector3 _pressedPosition;
    private bool _isBeingPressed = false;
    private bool _isHovered = false;
    public readonly SyncVar<bool> _buttonActivated = new SyncVar<bool>();

    private Renderer _buttonRenderer;
    private Material _originalButtonMaterial;

    void Start()
    {
        if (buttonMesh != null)
        {
            _originalPosition = buttonMesh.localPosition;
            _pressedPosition = _originalPosition - new Vector3(0, buttonTravelDistance, 0);
            _buttonRenderer = buttonMesh.GetComponent<Renderer>();

            if (_buttonRenderer != null && normalMaterial != null)
            {
                _originalButtonMaterial = _buttonRenderer.material;
                _buttonRenderer.material = normalMaterial;
            }
        }

        if (progressIndicator != null)
            progressIndicator.SetActive(false);
    }

    void Update()
    {
        // Handle button animation
        if (_isBeingPressed && buttonMesh != null)
        {
            buttonMesh.localPosition = Vector3.Lerp(
                buttonMesh.localPosition,
                _pressedPosition,
                Time.deltaTime * 10f
            );
        }
        else if (!_isBeingPressed && buttonMesh != null)
        {
            buttonMesh.localPosition = Vector3.Lerp(
                buttonMesh.localPosition,
                _originalPosition,
                Time.deltaTime * buttonReturnSpeed
            );
        }
    }

    #region IInteractable Implementation

    public bool CanInteract()
    {
        // Buttons can only be interacted with if they're not already activated
        // You could change this if you want buttons to be reusable
        return !_buttonActivated.Value;
    }

    public void OnHoverEnter()
    {
        _isHovered = true;

        // Change material to hovered state if available
        if (
            _buttonRenderer != null
            && hoveredMaterial != null
            && !_isBeingPressed
            && !_buttonActivated.Value
        )
        {
            _buttonRenderer.material = hoveredMaterial;
        }
    }

    public void OnHoverExit()
    {
        _isHovered = false;

        // Restore normal material if not pressed or activated
        if (
            _buttonRenderer != null
            && normalMaterial != null
            && !_isBeingPressed
            && !_buttonActivated.Value
        )
        {
            _buttonRenderer.material = normalMaterial;
        }
    }

    public void OnInteractStart()
    {
        if (!_buttonActivated.Value)
        {
            _isBeingPressed = true;

            // Change material to pressed state
            if (_buttonRenderer != null && pressedMaterial != null)
            {
                _buttonRenderer.material = pressedMaterial;
            }

            // Show progress indicator
            if (progressIndicator != null)
                progressIndicator.SetActive(true);

            // Fire pressed event
            onButtonPressed.Invoke();
        }
    }

    public void OnInteractHold(float duration)
    {
        if (_isBeingPressed && !_buttonActivated.Value)
        {
            // Update progress indicator based on hold duration
            UpdateProgressIndicator(duration / requiredHoldTime);

            // Check if held long enough to activate
            if (duration >= requiredHoldTime)
            {
                _buttonActivated.Value = true;

                // Change material to activated state
                if (_buttonRenderer != null && activatedMaterial != null)
                {
                    _buttonRenderer.material = activatedMaterial;
                }

                // Fire activated event
                onButtonActivated.Invoke();
            }
        }
    }

    public void OnInteractEnd(bool completed)
    {
        _isBeingPressed = false;

        // Hide progress indicator
        if (progressIndicator != null)
            progressIndicator.SetActive(false);

        // Change material back to normal or hovered state if not activated
        if (_buttonRenderer != null && !_buttonActivated.Value)
        {
            if (_isHovered && hoveredMaterial != null)
                _buttonRenderer.material = hoveredMaterial;
            else if (normalMaterial != null)
                _buttonRenderer.material = normalMaterial;
        }

        // Fire released event
        onButtonReleased.Invoke();

        // Reset activation after a delay (optional - remove if button should stay activated)
        // if (_buttonActivated)
        // {
        //     Invoke("ResetButton", 2f);
        // }
    }

    #endregion

    private void ResetButton()
    {
        _buttonActivated.Value = false;

        // Restore normal material or hovered material
        if (_buttonRenderer != null)
        {
            if (_isHovered && hoveredMaterial != null)
                _buttonRenderer.material = hoveredMaterial;
            else if (normalMaterial != null)
                _buttonRenderer.material = normalMaterial;
        }
    }

    private void UpdateProgressIndicator(float progress)
    {
        // Clamp progress to 0-1 range
        progress = Mathf.Clamp01(progress);

        // Handle different indicator types
        if (progressIndicator != null)
        {
            // For particle system
            ParticleSystem ps = progressIndicator.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.rateOverTime = progress * 50f;
            }

            // For light
            Light light = progressIndicator.GetComponent<Light>();
            if (light != null)
            {
                light.intensity = progress * 2f;
            }

            // For UI slider or radial
            UnityEngine.UI.Image fillImage = progressIndicator.GetComponent<UnityEngine.UI.Image>();
            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }
        }
    }
}
