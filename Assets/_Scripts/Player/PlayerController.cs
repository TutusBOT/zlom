using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour
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

            Debug.Log($"Created new camera for player {gameObject.name}");
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
//bagno
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;
    }

    void Update()
    {
        HandleMovement();
        HandleCrouch();
        HandleCamera();
    }

    private void HandleMovement()
    {
        isSprinting = InputBindingManager.Instance.IsActionPressed(InputActions.Sprint) && currentStamina > 0f && !isCrouching;

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

        HandleStamina();
    }

    private void HandleStamina()
{
    bool sprintKeyHeld = InputBindingManager.Instance.IsActionPressed(InputActions.Sprint);

    if (isSprinting)
    {
        currentStamina -= staminaDrainRate * Time.deltaTime;
        if (currentStamina < 0f)
            currentStamina = 0f;
    }
    else
    {
        if (!sprintKeyHeld) // Only regenerate if NOT holding sprint key
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina)
                    currentStamina = maxStamina;
            }
        }
    }
}


    private void HandleCrouch()
    {
        if (InputBindingManager.Instance.IsActionPressed(InputActions.Crouch))
        {
            if (!isCrouching)
            {
                isCrouching = true;
                characterController.height = crouchHeight;
                playerCamera.transform.localPosition = new Vector3(0, crouchCameraYOffset, 0);
            }
        }
        else
        {
            if (isCrouching)
            {
                isCrouching = false;
                characterController.height = normalHeight;
                playerCamera.transform.localPosition = new Vector3(0, cameraYOffset, 0);
            }
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

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
