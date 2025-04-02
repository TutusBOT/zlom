using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private float fadeTime = 0.5f;

    [SerializeField]
    private bool showLoadingScreen = true;

    [Header("References")]
    [SerializeField]
    private CanvasGroup fadeCanvasGroup;

    [SerializeField]
    private GameObject loadingScreen;

    private string _currentlyLoadingScene;
    private bool _isLoading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize fade canvas if needed
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 0;

            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Load scene by name with optional callback when complete
    public void LoadScene(string sceneName, System.Action onComplete = null)
    {
        if (_isLoading)
            return;

        _currentlyLoadingScene = sceneName;
        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    // Load scene by build index
    public void LoadScene(int sceneIndex, System.Action onComplete = null)
    {
        if (_isLoading)
            return;

        _currentlyLoadingScene = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
        StartCoroutine(LoadSceneRoutine(sceneIndex, onComplete));
    }

    public void ReloadCurrentScene(System.Action onComplete = null)
    {
        LoadScene(SceneManager.GetActiveScene().name, onComplete);
    }

    // Load scene with loading screen
    private IEnumerator LoadSceneRoutine(string sceneName, System.Action onComplete = null)
    {
        _isLoading = true;

        // Fade out current scene
        yield return StartCoroutine(FadeRoutine(1));

        // Show loading screen if configured
        if (showLoadingScreen && loadingScreen != null)
            loadingScreen.SetActive(true);

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until it's almost done
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activate the scene
        asyncLoad.allowSceneActivation = true;

        // Wait for scene to fully load
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Hide loading screen
        if (showLoadingScreen && loadingScreen != null)
            loadingScreen.SetActive(false);

        // Fade in new scene
        yield return StartCoroutine(FadeRoutine(0));

        _isLoading = false;

        // Execute callback if provided
        onComplete?.Invoke();
    }

    // Overload for loading by index
    private IEnumerator LoadSceneRoutine(int sceneIndex, System.Action onComplete = null)
    {
        _isLoading = true;

        // Same logic as above but with sceneIndex
        yield return StartCoroutine(FadeRoutine(1));

        if (showLoadingScreen && loadingScreen != null)
            loadingScreen.SetActive(true);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (showLoadingScreen && loadingScreen != null)
            loadingScreen.SetActive(false);

        yield return StartCoroutine(FadeRoutine(0));

        _isLoading = false;
        onComplete?.Invoke();
    }

    // Fade in/out effect
    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsedTime = 0;

        while (elapsedTime < fadeTime)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}
