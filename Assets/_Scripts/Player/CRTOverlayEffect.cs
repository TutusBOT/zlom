using UnityEngine;
using UnityEngine.UI;

public class CRTOverlayEffect : MonoBehaviour
{
    [SerializeField]
    private Sprite borderSprite;

    [Header("CRT Distortion")]
    [SerializeField]
    [Range(0f, 0.3f)]
    private float barrelDistortion = 0.3f;

    [SerializeField]
    [Range(0f, 5f)]
    private float chromaticAberration = 1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float vignetteIntensity = 0.1f;

    private Canvas overlayCanvas;
    private RawImage borderImage;
    private Material distortionMaterial;

    public void Initialize(Camera targetCamera, string spritePath = "Images/Player/crt-border")
    {
        borderSprite = Resources.Load<Sprite>(spritePath);
        if (borderSprite == null)
        {
            Debug.LogWarning("CRT border sprite not found at path: " + spritePath);
        }

        GameObject canvasObj = new GameObject("CRT_Overlay");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.transform.SetParent(targetCamera.transform);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject borderObj = new GameObject("CRT_Border");
        borderObj.transform.SetParent(canvasObj.transform, false);

        borderImage = borderObj.AddComponent<RawImage>();
        borderImage.color = Color.white; // Use white to preserve texture colors
        borderImage.texture = borderSprite ? borderSprite.texture : null;

        RectTransform rt = borderImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        SetupCameraDistortion(targetCamera);
    }

    private void SetupCameraDistortion(Camera camera)
    {
        Shader distortionShader = Shader.Find("Custom/CRTDistortion");
        if (distortionShader == null)
        {
            Debug.LogError("CRT Distortion shader not found! Ensure it's in your project.");
            return;
        }

        distortionMaterial = new Material(distortionShader);
        distortionMaterial.SetFloat("_BarrelPower", barrelDistortion);
        distortionMaterial.SetFloat("_ChromaticAberration", chromaticAberration);
        distortionMaterial.SetFloat("_VignetteIntensity", vignetteIntensity);

        CRTPostProcessor postProcessor = camera.gameObject.AddComponent<CRTPostProcessor>();
        postProcessor.material = distortionMaterial;
    }

    private class CRTPostProcessor : MonoBehaviour
    {
        public Material material;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material != null)
            {
                Graphics.Blit(source, destination, material);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}
