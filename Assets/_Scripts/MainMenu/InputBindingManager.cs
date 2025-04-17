using System;
using System.Collections.Generic;
using UnityEngine;

public class InputBindingManager : MonoBehaviour
{
    public static InputBindingManager Instance { get; private set; }

    private Dictionary<string, KeyCode> _keyBindings = new Dictionary<string, KeyCode>();
    private Dictionary<string, KeyCode> _defaultBindings = new Dictionary<string, KeyCode>();

    public event Action<string, KeyCode> OnBindingChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultBindings();
            LoadBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDefaultBindings()
    {
        _defaultBindings[InputActions.MoveForward] = KeyCode.W;
        _defaultBindings[InputActions.MoveBackward] = KeyCode.S;
        _defaultBindings[InputActions.MoveLeft] = KeyCode.A;
        _defaultBindings[InputActions.MoveRight] = KeyCode.D;
        _defaultBindings[InputActions.Jump] = KeyCode.Space;
        _defaultBindings[InputActions.Crouch] = KeyCode.LeftControl;
        _defaultBindings[InputActions.Sprint] = KeyCode.LeftShift;
        _defaultBindings[InputActions.VoiceChat] = KeyCode.V;
        _defaultBindings[InputActions.Pause] = KeyCode.Escape;
        _defaultBindings[InputActions.Flashlight] = KeyCode.F;
        _defaultBindings[InputActions.RechargeFlashlight] = KeyCode.R;
        _defaultBindings[InputActions.Flash] = KeyCode.Q;
        _defaultBindings[InputActions.Interact] = KeyCode.E;

        foreach (var binding in _defaultBindings)
        {
            _keyBindings[binding.Key] = binding.Value;
        }
    }

    public bool IsActionPressed(string actionName)
    {
        if (_keyBindings.TryGetValue(actionName, out KeyCode key))
        {
            return Input.GetKey(key);
        }
        return false;
    }

    public bool IsActionTriggered(string actionName)
    {
        if (_keyBindings.TryGetValue(actionName, out KeyCode key))
        {
            return Input.GetKeyDown(key);
        }
        return false;
    }

    public bool IsActionReleased(string actionName)
    {
        if (_keyBindings.TryGetValue(actionName, out KeyCode key))
        {
            return Input.GetKeyUp(key);
        }
        return false;
    }

    public bool SetBinding(string actionName, KeyCode key)
    {
        foreach (var binding in _keyBindings)
        {
            if (binding.Value == key && binding.Key != actionName)
            {
                Debug.LogWarning($"Key {key} is already bound to {binding.Key}");
                return false;
            }
        }

        _keyBindings[actionName] = key;
        OnBindingChanged?.Invoke(actionName, key);
        SaveBindings();
        return true;
    }

    public KeyCode GetBinding(string actionName)
    {
        if (_keyBindings.TryGetValue(actionName, out KeyCode key))
        {
            return key;
        }
        return KeyCode.None;
    }

    public void ResetToDefaults()
    {
        foreach (var binding in _defaultBindings)
        {
            _keyBindings[binding.Key] = binding.Value;
            OnBindingChanged?.Invoke(binding.Key, binding.Value);
        }
        SaveBindings();
    }

    private void SaveBindings()
    {
        foreach (var binding in _keyBindings)
        {
            PlayerPrefs.SetInt($"KeyBinding_{binding.Key}", (int)binding.Value);
        }
        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        foreach (string actionName in _defaultBindings.Keys)
        {
            if (PlayerPrefs.HasKey($"KeyBinding_{actionName}"))
            {
                _keyBindings[actionName] = (KeyCode)PlayerPrefs.GetInt($"KeyBinding_{actionName}");
            }
        }
    }

    // Allows adding new actions during runtime (useful for mods)
    public void RegisterAction(string actionName, KeyCode defaultKey)
    {
        if (!_defaultBindings.ContainsKey(actionName))
        {
            _defaultBindings[actionName] = defaultKey;

            if (!_keyBindings.ContainsKey(actionName))
            {
                _keyBindings[actionName] = defaultKey;
            }
        }
    }
}
