using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance => _instance;

    [Header("Audio")]
    [SerializeField]
    private AudioMixer audioMixer;

    [SerializeField]
    private string masterVolumeParam = "MasterVolume";

    [SerializeField]
    private string musicVolumeParam = "MusicVolume";

    [SerializeField]
    private string sfxVolumeParam = "SFXVolume";

    [Header("Graphics")]
    [SerializeField]
    private bool defaultVSync = true;

    [SerializeField]
    private int defaultQualityLevel = 2;

    [SerializeField]
    private FullScreenMode defaultScreenMode = FullScreenMode.FullScreenWindow;

    [SerializeField]
    private int defaultBrightness = 50;

    private const string SETTINGS_FILE = "gamesettings.json";
    private GameSettings _settings;

    public UnityEvent OnSettingsChanged = new UnityEvent();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
    }

    #region Settings IO

    public void LoadSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, SETTINGS_FILE);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _settings = JsonUtility.FromJson<GameSettings>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                _settings = CreateDefaultSettings();
            }
        }
        else
        {
            Debug.LogWarning($"Settings file not found at {path}. Creating default settings.");
            _settings = CreateDefaultSettings();
            SaveSettings();
        }
    }

    public void SaveSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, SETTINGS_FILE);
        string json = JsonUtility.ToJson(_settings, true);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
    }

    private GameSettings CreateDefaultSettings()
    {
        var settings = new GameSettings
        {
            // Audio
            MasterVolume = 1.0f,
            MusicVolume = 0.8f,
            SFXVolume = 1.0f,

            // Graphics
            QualityLevel = defaultQualityLevel,
            ScreenMode = (int)defaultScreenMode,
            VSync = defaultVSync,
            Brightness = defaultBrightness,

            KeyBindingsList = new List<SerializableKeyBinding>
            {
                new SerializableKeyBinding(InputActions.MoveForward, KeyCode.W),
                new SerializableKeyBinding(InputActions.MoveBackward, KeyCode.S),
                new SerializableKeyBinding(InputActions.MoveLeft, KeyCode.A),
                new SerializableKeyBinding(InputActions.MoveRight, KeyCode.D),
                new SerializableKeyBinding(InputActions.Jump, KeyCode.Space),
                new SerializableKeyBinding(InputActions.Crouch, KeyCode.LeftControl),
                new SerializableKeyBinding(InputActions.Sprint, KeyCode.LeftShift),
                new SerializableKeyBinding(InputActions.Flashlight, KeyCode.F),
                new SerializableKeyBinding(InputActions.RechargeFlashlight, KeyCode.R),
                new SerializableKeyBinding(InputActions.Flash, KeyCode.Q),
                new SerializableKeyBinding(InputActions.Interact, KeyCode.E),
                new SerializableKeyBinding(InputActions.Confirm, KeyCode.Return),
                new SerializableKeyBinding(InputActions.TextChat, KeyCode.T),
                new SerializableKeyBinding(InputActions.VoiceChat, KeyCode.V),
                new SerializableKeyBinding(InputActions.Cancel, KeyCode.Escape),
                new SerializableKeyBinding(InputActions.ToggleMap, KeyCode.Tab),
            },
        };

        return settings;
    }

    #endregion

    #region Apply Settings

    public void ApplySettings()
    {
        ApplyAudioSettings();
        ApplyGraphicsSettings();

        OnSettingsChanged.Invoke();
    }

    private void ApplyAudioSettings()
    {
        if (audioMixer != null)
        {
            // Convert from slider range (0-1) to mixer range (logarithmic dB)
            audioMixer.SetFloat(masterVolumeParam, Mathf.Log10(_settings.MasterVolume) * 20);
            audioMixer.SetFloat(musicVolumeParam, Mathf.Log10(_settings.MusicVolume) * 20);
            audioMixer.SetFloat(sfxVolumeParam, Mathf.Log10(_settings.SFXVolume) * 20);
        }
    }

    private void ApplyGraphicsSettings()
    {
        QualitySettings.SetQualityLevel(_settings.QualityLevel);

        Resolution currentResolution = Screen.currentResolution;
        Screen.SetResolution(
            currentResolution.width,
            currentResolution.height,
            (FullScreenMode)_settings.ScreenMode
        );

        QualitySettings.vSyncCount = _settings.VSync ? 1 : 0;

        // Apply brightness to post-processing here
    }

    #endregion

    #region Public Getters/Setters

    // Audio
    public float GetMasterVolume() => _settings.MasterVolume;

    public float GetMusicVolume() => _settings.MusicVolume;

    public float GetSFXVolume() => _settings.SFXVolume;

    public void SetMasterVolume(float volume)
    {
        _settings.MasterVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        _settings.MusicVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }

    public void SetSFXVolume(float volume)
    {
        _settings.SFXVolume = Mathf.Clamp01(volume);
        ApplyAudioSettings();
    }

    // Graphics
    public int GetQualityLevel() => _settings.QualityLevel;

    public bool GetVSync() => _settings.VSync;

    public int GetScreenMode() => _settings.ScreenMode;

    public int GetBrightness() => _settings.Brightness;

    public void SetQualityLevel(int level)
    {
        _settings.QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(_settings.QualityLevel);
    }

    public void SetVSync(bool enabled)
    {
        _settings.VSync = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    public void SetScreenMode(int mode)
    {
        _settings.ScreenMode = Mathf.Clamp(mode, 0, 3); // 0-3 are the FullScreenMode enum values
        Resolution currentResolution = Screen.currentResolution;
        Screen.SetResolution(
            currentResolution.width,
            currentResolution.height,
            (FullScreenMode)_settings.ScreenMode
        );
    }

    public void SetBrightness(int brightness)
    {
        _settings.Brightness = Mathf.Clamp(brightness, 0, 100);
        // Apply brightness change to post-processing
    }

    // Key Bindings
    public KeyCode GetKeyBinding(string action)
    {
        if (_settings.KeyBindings.TryGetValue(action, out KeyCode key))
        {
            return key;
        }

        Debug.LogWarning($"No binding found for {action}");
        return KeyCode.None;
    }

    public Dictionary<string, KeyCode> GetAllKeyBindings() => _settings.KeyBindings;

    public bool SetKeyBinding(string action, KeyCode key)
    {
        // Check if key already in use
        foreach (var binding in _settings.KeyBindings)
        {
            if (binding.Value == key && binding.Key != action)
                return false; // Key already in use
        }

        // Update dictionary
        if (_settings.KeyBindings.ContainsKey(action))
            _settings.KeyBindings[action] = key;
        else
            _settings.KeyBindings.Add(action, key);

        // Also update the serializable list
        bool found = false;
        for (int i = 0; i < _settings.KeyBindingsList.Count; i++)
        {
            if (_settings.KeyBindingsList[i].Action == action)
            {
                _settings.KeyBindingsList[i].KeyValue = (int)key;
                found = true;
                break;
            }
        }

        if (!found)
        {
            _settings.KeyBindingsList.Add(new SerializableKeyBinding(action, key));
        }

        OnSettingsChanged.Invoke();
        return true;
    }

    public void ResetToDefaults()
    {
        _settings = CreateDefaultSettings();
        ApplySettings();
        SaveSettings();
    }

    #endregion
}

[Serializable]
public class GameSettings
{
    // Audio
    public float MasterVolume = 1.0f;
    public float MusicVolume = 0.8f;
    public float SFXVolume = 1.0f;

    public int QualityLevel = 5;
    public bool VSync = true;
    public int ScreenMode = (int)FullScreenMode.FullScreenWindow;
    public int Brightness = 50;

    // Input
    public List<SerializableKeyBinding> KeyBindingsList = new List<SerializableKeyBinding>();

    [NonSerialized]
    private Dictionary<string, KeyCode> _keyBindingsCache;

    // Property to access bindings as a dictionary
    public Dictionary<string, KeyCode> KeyBindings
    {
        get
        {
            if (_keyBindingsCache == null)
            {
                _keyBindingsCache = new Dictionary<string, KeyCode>();
                foreach (var binding in KeyBindingsList)
                {
                    _keyBindingsCache[binding.Action] = (KeyCode)binding.KeyValue;
                }
            }
            return _keyBindingsCache;
        }
    }
}

[Serializable]
public class SerializableKeyBinding
{
    public string Action;
    public int KeyValue;

    public SerializableKeyBinding(string action, KeyCode key)
    {
        Action = action;
        KeyValue = (int)key;
    }
}
