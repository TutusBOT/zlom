using System;
using System.Collections.Generic;
using UnityEngine;

public class InputBindingManager : MonoBehaviour
{
    public static InputBindingManager Instance { get; private set; }

    private Dictionary<string, KeyCode> _defaultBindings = new Dictionary<string, KeyCode>();

    public event Action<string, KeyCode> OnBindingChanged;

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

    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged.AddListener(OnSettingsChanged);

            SyncWithSettingsManager();
        }
        else
        {
            Debug.LogError(
                "SettingsManager instance not found! Input binding won't work properly."
            );
        }
    }

    private void SyncWithSettingsManager()
    {
        foreach (var binding in _defaultBindings)
        {
            // If the key doesn't exist in SettingsManager, add it
            if (SettingsManager.Instance.GetKeyBinding(binding.Key) == KeyCode.None)
            {
                SettingsManager.Instance.SetKeyBinding(binding.Key, binding.Value);
            }
        }
    }

    private void OnSettingsChanged()
    {
        Dictionary<string, KeyCode> bindings = SettingsManager.Instance.GetAllKeyBindings();
        foreach (var binding in bindings)
        {
            OnBindingChanged?.Invoke(binding.Key, binding.Value);
        }
    }

    public bool IsActionPressed(string actionName)
    {
        KeyCode key = SettingsManager.Instance.GetKeyBinding(actionName);
        return Input.GetKey(key);
    }

    public bool IsActionTriggered(string actionName)
    {
        KeyCode key = SettingsManager.Instance.GetKeyBinding(actionName);
        return Input.GetKeyDown(key);
    }

    public bool IsActionReleased(string actionName)
    {
        KeyCode key = SettingsManager.Instance.GetKeyBinding(actionName);
        return Input.GetKeyUp(key);
    }

    public bool SetBinding(string actionName, KeyCode key)
    {
        bool success = SettingsManager.Instance.SetKeyBinding(actionName, key);
        if (success)
        {
            OnBindingChanged?.Invoke(actionName, key);
            SettingsManager.Instance.SaveSettings();
        }
        return success;
    }

    public KeyCode GetBinding(string actionName)
    {
        return SettingsManager.Instance.GetKeyBinding(actionName);
    }

    public Dictionary<string, KeyCode> GetAllBindings()
    {
        return SettingsManager.Instance.GetAllKeyBindings();
    }

    public void ResetToDefaults()
    {
        SettingsManager.Instance.ResetToDefaults();
    }

    public void RegisterAction(string actionName, KeyCode defaultKey)
    {
        if (!_defaultBindings.ContainsKey(actionName))
        {
            _defaultBindings[actionName] = defaultKey;

            if (SettingsManager.Instance.GetKeyBinding(actionName) == KeyCode.None)
            {
                SettingsManager.Instance.SetKeyBinding(actionName, defaultKey);
            }
        }
    }
}
