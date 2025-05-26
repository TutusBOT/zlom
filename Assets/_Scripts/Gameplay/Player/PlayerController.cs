using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IUpgradeable
{
    [Header("Base setup")]
    public float walkingSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float crouchSpeed = 3.0f;
    public float jumpSpeed = 3.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    [Header("Stamina")]
    public float maxStamina = 5.0f;
    public float staminaDrainRate = 2.0f;
    public float staminaRegenRate = 0.5f;
    private float currentStamina;
    private bool isSprinting = false;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    private bool isAdrenalineActive = false;
    public event System.Action<float, float> OnStaminaChanged;

    [Header("Crouch")]
    public float crouchHeight = 1.0f;
    public float normalHeight = 2.0f;
    public float crouchCameraYOffset = 0.2f;
    private bool isCrouching = false;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    [SerializeField]
    private float cameraYOffset = 0.4f;
    private Camera playerCamera;

    private float baseSprintSpeed;
    private float baseMaxStamina;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            if (Camera.main != null)
            {
                Camera.main.gameObject.SetActive(false);
            }

            GameObject cameraObject = new GameObject("PlayerCamera");
            playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.tag = "MainCamera";

            if (playerCamera.GetComponent<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();

            playerCamera.transform.position = new Vector3(
                transform.position.x,
                transform.position.y + cameraYOffset,
                transform.position.z
            );
            playerCamera.transform.SetParent(transform);
            playerCamera.nearClipPlane = 0.05f;

            GameObject crtEffectObj = new GameObject("CRT_Effect");
            CRTOverlayEffect crtEffect = crtEffectObj.AddComponent<CRTOverlayEffect>();
            crtEffect.Initialize(playerCamera);

            CameraManager.Instance.RegisterPlayerCamera(playerCamera);
        }
        else
        {
            canMove = false;
            enabled = false;
            foreach (Camera cam in GetComponentsInChildren<Camera>())
            {
                cam.gameObject.SetActive(false);
            }

            Debug.Log($"Disabled control for non-owned player {gameObject.name}");
        }
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;

        baseSprintSpeed = sprintSpeed;
        baseMaxStamina = maxStamina;
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
        HandleCamera();

        TempHandleLightSwitch();
    }

    private void HandleMovement()
    {
        bool isSprintButtonPressed = InputBindingManager.Instance.IsActionPressed(
            InputActions.Sprint
        );

        isSprinting = isSprintButtonPressed && currentStamina > 0f && !isCrouching;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float inputX = Input.GetAxis("Vertical");
        float inputY = Input.GetAxis("Horizontal");

        Vector2 inputVector = new Vector2(inputX, inputY);
        if (inputVector.magnitude > 1f)
            inputVector.Normalize();

        float curSpeedX = 0;
        float curSpeedY = 0;

        if (canMove)
        {
            if (isCrouching)
            {
                curSpeedX = crouchSpeed * inputVector.x;
                curSpeedY = crouchSpeed * inputVector.y;
            }
            else if (isSprinting)
            {
                curSpeedX = sprintSpeed * inputVector.x;
                curSpeedY = sprintSpeed * inputVector.y;
            }
            else
            {
                curSpeedX = walkingSpeed * inputVector.x;
                curSpeedY = walkingSpeed * inputVector.y;
            }
        }

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (
            InputBindingManager.Instance.IsActionPressed(InputActions.Jump)
            && canMove
            && characterController.isGrounded
            && !isCrouching
        )
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        CollisionFlags collisions = characterController.Move(moveDirection * Time.deltaTime);

        if ((collisions & CollisionFlags.Above) != 0)
        {
            float horizontalSpeed = new Vector2(moveDirection.x, moveDirection.z).magnitude;

            moveDirection.y = (horizontalSpeed > 0.1f) ? -2.0f : -0.1f;
        }

        HandleStamina(isSprintButtonPressed);
    }

    private void HandleStamina(bool isSprintButtonPressed)
    {
        if (isAdrenalineActive)
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return;
        }

        if (isSprintButtonPressed)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return;
        }

        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }

    private void HandleCrouch()
    {
        if (
            InputBindingManager.Instance.IsActionPressed(InputActions.Crouch)
            && canMove
            && !isCrouching
        )
        {
            isCrouching = true;
            characterController.height = crouchHeight;
            playerCamera.transform.localPosition = new Vector3(0, crouchCameraYOffset, 0);
            return;
        }

        if (isCrouching)
        {
            isCrouching = false;
            characterController.height = normalHeight;
            playerCamera.transform.localPosition = new Vector3(0, cameraYOffset, 0);
        }
    }

    private void HandleCamera()
    {
        if (canMove && playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    public void ToggleControls(bool enabled)
    {
        canMove = enabled;
    }

    public bool CanHandleUpgrade(UpgradeType type)
    {
        return type == UpgradeType.Speed || type == UpgradeType.Stamina;
    }

    public void ApplyUpgrade(UpgradeType type, int level, float value)
    {
        switch (type)
        {
            case UpgradeType.Speed:
                sprintSpeed = baseSprintSpeed * (1f + value);
                Debug.Log($"Speed upgraded to: {sprintSpeed}");
                break;

            case UpgradeType.Stamina:
                float oldMaxStamina = maxStamina;
                maxStamina = baseMaxStamina + value;

                float staminaRatio = currentStamina / oldMaxStamina;
                currentStamina = maxStamina * staminaRatio;

                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                Debug.Log($"Stamina upgraded to: {maxStamina}");
                break;
        }
    }

    private void TempHandleLightSwitch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (
                Physics.Raycast(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    out RaycastHit hit,
                    3f
                )
            )
            {
                LightSwitch lightSwitch = hit.collider.GetComponent<LightSwitch>();
                if (lightSwitch != null)
                {
                    lightSwitch.Interact();
                    return;
                }
            }
        }
    }

    public void ActivateAdrenaline(float duration)
    {
        isAdrenalineActive = true;
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        Invoke(nameof(DeactivateAdrenaline), duration);
    }

    private void DeactivateAdrenaline()
    {
        isAdrenalineActive = false;
    }
}
