using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Settings")]
    public float maxInteractionDistance = 10f;
    public LayerMask interactionLayer;
    public bool debug = false;

    [Header("References")]
    private Camera _playerCamera;

    private IInteractable _currentHoveredObject;
    private IInteractable _currentInteractingObject;
    private float _interactionHoldTime = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
            if (_playerCamera == null)
            {
                // Skip this frame if no camera is available
                return;
            }
        }
        
        Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        IInteractable hitInteractable = null;

        if (Physics.Raycast(ray, out hit, maxInteractionDistance, interactionLayer))
        {
            if (debug)
                Debug.Log($"Hit: {hit.collider.name}");

            hitInteractable = hit.collider.GetComponent<IInteractable>();

            if (hitInteractable == null)
                hitInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (hitInteractable != null && !hitInteractable.CanInteract())
                hitInteractable = null;
        }

        // Handle hover state changes
        if (hitInteractable != _currentHoveredObject)
        {
            // Exit from previous
            if (_currentHoveredObject != null)
            {
                _currentHoveredObject.OnHoverExit();
                if (debug)
                    Debug.Log($"Hover exit: {_currentHoveredObject}");
            }

            // Enter new
            _currentHoveredObject = hitInteractable;

            if (_currentHoveredObject != null)
            {
                _currentHoveredObject.OnHoverEnter();
                if (debug)
                    Debug.Log($"Hover enter: {_currentHoveredObject}");
            }
        }

        // Handle interactions
        if (Input.GetMouseButtonDown(0) && _currentHoveredObject != null)
        {
            _currentInteractingObject = _currentHoveredObject;
            _interactionHoldTime = 0f;
            _currentInteractingObject.OnInteractStart();
            if (debug)
                Debug.Log($"Interaction started with: {_currentInteractingObject}");
        }

        if (_currentInteractingObject != null && Input.GetMouseButton(0))
        {
            _interactionHoldTime += Time.deltaTime;
            _currentInteractingObject.OnInteractHold(_interactionHoldTime);
        }

        if (_currentInteractingObject != null && Input.GetMouseButtonUp(0))
        {
            bool completed = _currentHoveredObject == _currentInteractingObject;
            _currentInteractingObject.OnInteractEnd(completed);
            if (debug)
                Debug.Log(
                    $"Interaction ended with: {_currentInteractingObject}, completed: {completed}"
                );
            _currentInteractingObject = null;
        }
    }
}
