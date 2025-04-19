using FishNet.Object;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Base setup")]
    public float walkingSpeed = 7.5f;
    public float sprintSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

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

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        bool isSprinting = InputBindingManager.Instance.IsActionPressed(InputActions.Sprint);

        // We are grounded, so recalculate move direction based on axis
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float inputX = Input.GetAxis("Vertical");
        float inputY = Input.GetAxis("Horizontal");

        Vector2 inputVector = new Vector2(inputX, inputY);
        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }

        // Apply normalized values
        float curSpeedX = canMove ? (isSprinting ? sprintSpeed : walkingSpeed) * inputVector.x : 0;
        float curSpeedY = canMove ? (isSprinting ? sprintSpeed : walkingSpeed) * inputVector.y : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (
            InputBindingManager.Instance.IsActionPressed(InputActions.Jump)
            && canMove
            && characterController.isGrounded
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

            if (horizontalSpeed > 0.1f)
            {
                // Stronger downward force when moving to prevent ceiling gliding
                moveDirection.y = -2.0f;
            }
            else
            {
                // Normal downward force when not moving horizontally
                moveDirection.y = -0.1f;
            }
        }

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
