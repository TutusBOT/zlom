using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindingUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI actionNameText;

    [SerializeField]
    private TextMeshProUGUI keyBindingText;

    [SerializeField]
    private Button bindingButton;

    private string actionName;
    private KeyCode currentKey;

    public event Action<string> OnBindingButtonClicked;

    public void Initialize(string action, KeyCode key)
    {
        actionName = action;
        currentKey = key;

        string prettyName = FormatActionName(action);
        actionNameText.text = prettyName;

        UpdateKeyText(key);

        bindingButton.onClick.AddListener(() => OnBindingButtonClicked?.Invoke(actionName));
    }

    private string FormatActionName(string action)
    {
        // Add spaces before capital letters
        string result = "";
        foreach (char c in action)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result += " ";
            result += c;
        }
        return result;
    }

    public void UpdateKeyText(KeyCode key)
    {
        currentKey = key;
        keyBindingText.text = key.ToString();
    }

    public void SetWaitingForInput(bool waiting)
    {
        keyBindingText.text = waiting ? "Press any key..." : currentKey.ToString();
    }
}
