using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 5f;
    public LayerMask interactableLayers;

    [Header("Crosshair Colors")]
    public Color defaultColor = Color.cyan;
    public Color pickupColor = Color.green;

    [Header("Animation")]
    public bool animateCrosshair = true;
    public float scaleSpeed = 8f;
    public float hoverScale = 1.2f;

    private Camera _playerCamera;
    private Image crosshairImage;
    private Canvas crosshairCanvas;
    private Vector3 defaultScale;
    private Vector3 targetScale;

    private void Awake()
    {
        enabled = false;
        if (CameraManager.Instance?.PlayerCamera != null)
        {
            OnCameraInitialized(CameraManager.Instance.PlayerCamera);
        }
        else
        {
            CameraManager.Instance.OnPlayerCameraRegistered += OnCameraInitialized;
        }
    }

    private void OnCameraInitialized(Camera camera)
    {
        _playerCamera = camera;

        CreateCrosshairCanvas();
        CreateCrosshairImage();

        defaultScale = crosshairImage.transform.localScale;
        targetScale = defaultScale;

        enabled = true;

        CameraManager.Instance.OnPlayerCameraRegistered -= OnCameraInitialized;
    }

    private void CreateCrosshairCanvas()
    {
        GameObject canvasObj = new GameObject("CrosshairCanvas");

        crosshairCanvas = canvasObj.AddComponent<Canvas>();
        crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add raycaster (needed for UI interactions)
        canvasObj.AddComponent<GraphicRaycaster>();

        DontDestroyOnLoad(canvasObj);
    }

    private void CreateCrosshairImage()
    {
        GameObject imageObj = new GameObject("CrosshairDot");

        imageObj.transform.SetParent(crosshairCanvas.transform, false);

        RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(8, 8);
        rectTransform.anchoredPosition = Vector2.zero;

        crosshairImage = imageObj.AddComponent<Image>();
        crosshairImage.color = defaultColor;
    }

    private void Update()
    {
        DetectTarget();
        AnimateCrosshair();
    }

    private void DetectTarget()
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, detectionRange, interactableLayers))
        {
            if (hit.collider.GetComponent<Valuable>() != null)
            {
                crosshairImage.color = pickupColor;
                targetScale = defaultScale * hoverScale;
                return;
            }
        }

        crosshairImage.color = defaultColor;
        targetScale = defaultScale;
    }

    private void AnimateCrosshair()
    {
        if (animateCrosshair && crosshairImage != null)
        {
            crosshairImage.transform.localScale = Vector3.Lerp(
                crosshairImage.transform.localScale,
                targetScale,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    private void OnDestroy()
    {
        if (CameraManager.Instance != null)
            CameraManager.Instance.OnPlayerCameraRegistered -= OnCameraInitialized;
    }
}
