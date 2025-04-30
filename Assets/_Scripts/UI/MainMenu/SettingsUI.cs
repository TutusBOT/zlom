using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField]
    private GameObject graphicsPanel;

    [SerializeField]
    private GameObject audioPanel;

    [SerializeField]
    private GameObject controlsPanel;

    [Header("Graphics Settings")]
    [SerializeField]
    private TMP_Dropdown qualityDropdown;

    [SerializeField]
    private TMP_Dropdown screenModeDropdown;

    [SerializeField]
    private Toggle vsyncToggle;

    [SerializeField]
    private Slider brightnessSlider;

    [SerializeField]
    private TextMeshProUGUI brightnessValue;

    [Header("Audio Settings")]
    [SerializeField]
    private Slider masterVolumeSlider;

    [SerializeField]
    private Slider musicVolumeSlider;

    [SerializeField]
    private Slider sfxVolumeSlider;

    [SerializeField]
    private TextMeshProUGUI masterVolumeValue;

    [SerializeField]
    private TextMeshProUGUI musicVolumeValue;

    [SerializeField]
    private TextMeshProUGUI sfxVolumeValue;

    [Header("Controls Settings")]
    [SerializeField]
    private Transform keyBindingsContainer;

    [SerializeField]
    private GameObject keyBindingPrefab;

    [SerializeField]
    private Button resetToDefaultsButton;

    private Dictionary<string, KeyBindingUI> keyBindingUIElements =
        new Dictionary<string, KeyBindingUI>();
    private string waitingForInputAction = null;

    private void Start()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogError("SettingsManager instance not found!");
            return;
        }

        InitializeUI();
        PopulateValues();
        SetupListeners();

        ShowGraphicsPanel();
    }

    private void InitializeUI()
    {
        // Initialize quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        }

        // Initialize screen mode dropdown
        if (screenModeDropdown != null)
        {
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(
                new List<string>
                {
                    "Fullscreen",
                    "Windowed",
                    "Borderless Windowed",
                    "Maximized Window",
                }
            );
        }

        SetupKeyBindingUI();
    }

    private void SetupKeyBindingUI()
    {
        Transform contentTransform = keyBindingsContainer.Find("Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("Could not find Content transform in key bindings container");
            return;
        }

        RectTransform contentRect = contentTransform as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = new Vector2(0, 0);
            contentRect.sizeDelta = new Vector2(0, contentRect.sizeDelta.y);
        }

        ScrollRect scrollRect = keyBindingsContainer.GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.content = contentRect;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.viewport = keyBindingsContainer.Find("Viewport") as RectTransform;
            scrollRect.scrollSensitivity = 30.0f;
        }

        ContentSizeFitter fitter = contentTransform.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = contentTransform.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 10, 10);

        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }
        keyBindingUIElements.Clear();

        var keyBindings = SettingsManager.Instance.GetAllKeyBindings();

        foreach (var binding in keyBindings)
        {
            GameObject newBindingObj = Instantiate(keyBindingPrefab, contentTransform);
            KeyBindingUI bindingUI = newBindingObj.GetComponent<KeyBindingUI>();

            RectTransform bindingRect = newBindingObj.GetComponent<RectTransform>();
            if (bindingRect != null)
            {
                bindingRect.anchorMin = new Vector2(0, 0);
                bindingRect.anchorMax = new Vector2(1, 0);
                bindingRect.pivot = new Vector2(0.5f, 0);
            }

            if (bindingUI != null)
            {
                bindingUI.Initialize(binding.Key, binding.Value);
                bindingUI.OnBindingButtonClicked += StartRebindingKey;
                keyBindingUIElements[binding.Key] = bindingUI;
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1.0f;
            Debug.Log("Set scroll position to top");
        }
    }

    private void PopulateValues()
    {
        // Graphics
        if (qualityDropdown != null)
            qualityDropdown.value = SettingsManager.Instance.GetQualityLevel();

        if (screenModeDropdown != null)
            screenModeDropdown.value = SettingsManager.Instance.GetScreenMode();

        if (vsyncToggle != null)
            vsyncToggle.isOn = SettingsManager.Instance.GetVSync();

        if (brightnessSlider != null)
        {
            brightnessSlider.value = SettingsManager.Instance.GetBrightness();
            UpdateBrightnessText();
        }

        // Audio
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SettingsManager.Instance.GetMasterVolume();
            UpdateMasterVolumeText();
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = SettingsManager.Instance.GetMusicVolume();
            UpdateMusicVolumeText();
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SettingsManager.Instance.GetSFXVolume();
            UpdateSFXVolumeText();
        }
    }

    private void SetupListeners()
    {
        // Graphics
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);

        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);

        // Audio
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Reset button
        if (resetToDefaultsButton != null)
            resetToDefaultsButton.onClick.AddListener(OnResetToDefaults);

        // Settings changed event
        SettingsManager.Instance.OnSettingsChanged.AddListener(RefreshKeyBindingUI);
    }

    private void RefreshKeyBindingUI()
    {
        var keyBindings = SettingsManager.Instance.GetAllKeyBindings();

        foreach (var binding in keyBindings)
        {
            if (keyBindingUIElements.TryGetValue(binding.Key, out KeyBindingUI bindingUI))
            {
                bindingUI.UpdateKeyText(binding.Value);
            }
        }
    }

    #region Tab Control

    public void ShowGraphicsPanel()
    {
        graphicsPanel.SetActive(true);
        audioPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    public void ShowAudioPanel()
    {
        graphicsPanel.SetActive(false);
        audioPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    public void ShowControlsPanel()
    {
        graphicsPanel.SetActive(false);
        audioPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    #endregion

    #region Graphics Callbacks

    private void OnQualityChanged(int value)
    {
        SettingsManager.Instance.SetQualityLevel(value);
        SettingsManager.Instance.SaveSettings();
    }

    private void OnScreenModeChanged(int value)
    {
        SettingsManager.Instance.SetScreenMode(value);
        SettingsManager.Instance.SaveSettings();
    }

    private void OnVSyncChanged(bool value)
    {
        SettingsManager.Instance.SetVSync(value);
        SettingsManager.Instance.SaveSettings();
    }

    private void OnBrightnessChanged(float value)
    {
        SettingsManager.Instance.SetBrightness(Mathf.RoundToInt(value));
        UpdateBrightnessText();
        SettingsManager.Instance.SaveSettings();
    }

    private void UpdateBrightnessText()
    {
        if (brightnessValue != null)
            brightnessValue.text = SettingsManager.Instance.GetBrightness().ToString();
    }

    #endregion

    #region Audio Callbacks

    private void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
        UpdateMasterVolumeText();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(value);
        UpdateMusicVolumeText();
        SettingsManager.Instance.SaveSettings();
    }

    private void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(value);
        UpdateSFXVolumeText();
        SettingsManager.Instance.SaveSettings();
    }

    private void UpdateMasterVolumeText()
    {
        if (masterVolumeValue != null)
            masterVolumeValue.text =
                Mathf.RoundToInt(SettingsManager.Instance.GetMasterVolume() * 100) + "%";
    }

    private void UpdateMusicVolumeText()
    {
        if (musicVolumeValue != null)
            musicVolumeValue.text =
                Mathf.RoundToInt(SettingsManager.Instance.GetMusicVolume() * 100) + "%";
    }

    private void UpdateSFXVolumeText()
    {
        if (sfxVolumeValue != null)
            sfxVolumeValue.text =
                Mathf.RoundToInt(SettingsManager.Instance.GetSFXVolume() * 100) + "%";
    }

    #endregion

    #region Key Binding

    private void StartRebindingKey(string actionName)
    {
        // Don't allow rebinding if already waiting for input
        if (waitingForInputAction != null)
            return;

        waitingForInputAction = actionName;

        if (keyBindingUIElements.TryGetValue(actionName, out KeyBindingUI bindingUI))
        {
            bindingUI.SetWaitingForInput(true);
        }
    }

    private void Update()
    {
        if (waitingForInputAction != null)
        {
            // Check for any keypress
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    AssignNewKeyBinding(keyCode);
                    break;
                }
            }

            // Cancel with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRebinding();
            }
        }
    }

    private void AssignNewKeyBinding(KeyCode newKey)
    {
        if (waitingForInputAction == null)
            return;

        // Some keys shouldn't be rebindable
        if (newKey == KeyCode.Escape)
        {
            CancelRebinding();
            return;
        }

        bool success = SettingsManager.Instance.SetKeyBinding(waitingForInputAction, newKey);

        if (success)
        {
            if (keyBindingUIElements.TryGetValue(waitingForInputAction, out KeyBindingUI bindingUI))
            {
                bindingUI.SetWaitingForInput(false);
            }

            SettingsManager.Instance.SaveSettings();

            waitingForInputAction = null;
        }
        else
        {
            Debug.Log("Key already in use");
        }
    }

    private void CancelRebinding()
    {
        if (waitingForInputAction == null)
            return;

        if (keyBindingUIElements.TryGetValue(waitingForInputAction, out KeyBindingUI bindingUI))
        {
            bindingUI.SetWaitingForInput(false);
        }

        waitingForInputAction = null;
    }

    #endregion

    private void OnResetToDefaults()
    {
        SettingsManager.Instance.ResetToDefaults();
        PopulateValues();
    }
}
