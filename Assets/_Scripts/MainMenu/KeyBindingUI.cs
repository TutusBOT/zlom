using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindingUI : MonoBehaviour
{
    [SerializeField]
    private GameObject bindingEntryPrefab;

    [SerializeField]
    private Transform bindingContainer;

    [SerializeField]
    private Button resetButton;

    private Dictionary<string, TextMeshProUGUI> bindingTexts =
        new Dictionary<string, TextMeshProUGUI>();
    private string currentlyRebinding = null;

    void Start()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetAllBindings);

        InputBindingManager.Instance.OnBindingChanged += UpdateBindingText;
        CreateBindingUI();
    }

    void OnDestroy()
    {
        if (InputBindingManager.Instance != null)
            InputBindingManager.Instance.OnBindingChanged -= UpdateBindingText;
    }

    private void CreateBindingUI()
    {
        List<string> actionNames = new List<string>
        {
            "MoveForward",
            "MoveBackward",
            "MoveLeft",
            "MoveRight",
            "Jump",
            "Crouch",
            "Sprint",
        };

        foreach (var actionName in actionNames)
        {
            GameObject entry = Instantiate(bindingEntryPrefab, bindingContainer);

            TextMeshProUGUI actionText = entry
                .transform.Find("ActionName")
                .GetComponent<TextMeshProUGUI>();
            if (actionText != null)
                actionText.text = FormatActionName(actionName);

            TextMeshProUGUI keyText = entry
                .transform.Find("KeyName")
                .GetComponent<TextMeshProUGUI>();
            if (keyText != null)
            {
                keyText.text = InputBindingManager.Instance.GetBinding(actionName).ToString();
                bindingTexts[actionName] = keyText;
            }

            Button rebindButton = entry.transform.Find("RebindButton").GetComponent<Button>();
            if (rebindButton != null)
            {
                string capturedActionName = actionName;
                rebindButton.onClick.AddListener(() => StartRebinding(capturedActionName));
            }
        }
    }

    private string FormatActionName(string actionName)
    {
        string result = actionName[0].ToString();

        for (int i = 1; i < actionName.Length; i++)
        {
            if (char.IsUpper(actionName[i]))
                result += " " + actionName[i];
            else
                result += actionName[i];
        }

        return result;
    }

    private void StartRebinding(string actionName)
    {
        currentlyRebinding = actionName;

        if (bindingTexts.TryGetValue(actionName, out TextMeshProUGUI text))
        {
            text.text = "Press any key...";
        }
    }

    private void UpdateBindingText(string actionName, KeyCode key)
    {
        if (bindingTexts.TryGetValue(actionName, out TextMeshProUGUI text))
        {
            text.text = key.ToString();
        }
    }

    private void ResetAllBindings()
    {
        InputBindingManager.Instance.ResetToDefaults();
    }

    void Update()
    {
        if (currentlyRebinding != null)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key) && IsValidKeyForBinding(key))
                {
                    InputBindingManager.Instance.SetBinding(currentlyRebinding, key);
                    currentlyRebinding = null;
                    break;
                }
            }

            // Escape cancels the rebinding process
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (bindingTexts.TryGetValue(currentlyRebinding, out TextMeshProUGUI text))
                {
                    text.text = InputBindingManager
                        .Instance.GetBinding(currentlyRebinding)
                        .ToString();
                }
                currentlyRebinding = null;
            }
        }
    }

    private bool IsValidKeyForBinding(KeyCode key)
    {
        // Exclude system keys and other keys that shouldn't be remappable
        return key != KeyCode.Escape
            && key != KeyCode.F1
            && key != KeyCode.F2
            && key != KeyCode.F3
            && key != KeyCode.Print
            && key != KeyCode.SysReq;
    }
}
