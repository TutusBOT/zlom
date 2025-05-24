using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class ChatInputManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private GameObject chatInputPanel;

    [SerializeField]
    private TMP_InputField chatInputField;

    private Player _player;
    private bool _isChatActive = false;
    private TextToSpeech _textToSpeech = new();

    private void Start()
    {
        chatInputPanel.SetActive(false);
    }

    private void Update()
    {
        if (InputBindingManager.Instance.IsActionTriggered(InputActions.TextChat) && !_isChatActive)
        {
            OpenChat();
        }

        if (!_isChatActive)
            return;

        if (InputBindingManager.Instance.IsActionTriggered(InputActions.Confirm))
        {
            SendMessage();
        }

        if (InputBindingManager.Instance.IsActionTriggered(InputActions.Cancel))
        {
            CloseChat();
        }
    }

    private void OpenChat()
    {
        if (_player == null)
        {
            var players = PlayerManager.Instance.GetAllPlayers();

            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    _player = player;
                    break;
                }
            }
        }

        _isChatActive = true;
        chatInputPanel.SetActive(true);
        chatInputField.text = "";

        StartCoroutine(FocusInputField());

        _player.ToggleControls(false);
    }

    private IEnumerator FocusInputField()
    {
        // Wait for the end of the frame for UI to update
        yield return new WaitForEndOfFrame();

        // Now focus the input field
        chatInputField.Select();
        chatInputField.ActivateInputField();

        // Set the caret position to the end
        chatInputField.caretPosition = chatInputField.text.Length;

        // Force the UI to update
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(
            chatInputField.gameObject
        );
    }

    private void CloseChat()
    {
        _isChatActive = false;
        chatInputPanel.SetActive(false);

        _player.ToggleControls(true);
    }

    private void SendMessage()
    {
        string message = chatInputField.text.Trim();

        if (string.IsNullOrEmpty(message))
        {
            CloseChat();
            return;
        }

        if (message.StartsWith("!"))
        {
            ProcessCommand(message);
            CloseChat();
            return;
        }

        _textToSpeech.StartSpeech(message);
        _player.GetPlayerChatDisplay().SendChatMessageServerRpc(message);

        CloseChat();
    }

    private bool ProcessCommand(string input)
    {
        string command = input.ToLower();

        if (command == "!upgrade stamina")
        {
            UpgradePlayerStat(UpgradeType.Stamina);
            return true;
        }

        if (command == "!upgrade speed")
        {
            UpgradePlayerStat(UpgradeType.Speed);
            return true;
        }

        if (command == "!upgrade health")
        {
            UpgradePlayerStat(UpgradeType.Health);
            return true;
        }

        if (command == "!upgrade strength")
        {
            UpgradePlayerStat(UpgradeType.Strength);
            return true;
        }

        if (command == "!upgrade range")
        {
            UpgradePlayerStat(UpgradeType.Range);
            return true;
        }

        if (command.StartsWith("!spawn "))
        {
            string itemName = input.Substring(7).Trim();
            SpawnItem(itemName);
            return true;
        }

        if (command.StartsWith("!enemy "))
        {
            string enemyName = input.Substring(7).Trim();
            SpawnEnemy(enemyName);
            return true;
        }

        Debug.Log($"Unknown command: {command}");
        return false;
    }

    private void UpgradePlayerStat(UpgradeType upgradeType)
    {
        if (_player == null)
            return;

        PlayerUpgrades playerUpgrades = _player.GetComponent<PlayerUpgrades>();
        if (playerUpgrades == null)
        {
            Debug.LogWarning("PlayerUpgrades component not found on player");
            return;
        }

        playerUpgrades.ApplyUpgrade(upgradeType);
    }

    private void SpawnItem(string itemName)
    {
        if (_player == null)
            return;

        string prefabPath = "Assets/_Prefab/Item/Variants/";

        // Default to Healthpack if itemName is empty
        if (string.IsNullOrEmpty(itemName))
            prefabPath += "Healthpack.prefab";
        else
            prefabPath += itemName + ".prefab";

        GameObject prefab = null;

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
        Debug.LogError("AssetDatabase is only available in editor mode!");
        return;
#endif
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab: {prefabPath}");
            return;
        }

        Vector3 spawnPosition =
            _player.transform.position + _player.transform.forward * 1.5f + Vector3.up * 0.5f;

        GameObject item = Instantiate(prefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = item.GetComponent<NetworkObject>();
        InstanceFinder.ServerManager.Spawn(networkObject);
    }

    private void SpawnEnemy(string enemyName)
    {
        if (_player == null)
            return;

        string prefabPath = "Assets/_Prefab/DungeonGenerator/Enemy/Variants/";

        if (string.IsNullOrEmpty(enemyName))
            prefabPath += "WeepingAngel.prefab";
        else
            prefabPath += enemyName + ".prefab";

        GameObject prefab = null;

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#else
        Debug.LogError("AssetDatabase is only available in editor mode!");
        return;
#endif

        if (prefab == null)
        {
            Debug.LogError($"Failed to load enemy prefab: {prefabPath}");
            return;
        }

        // Calculate spawn position in front of player
        Vector3 spawnPosition = _player.transform.position + _player.transform.forward * 4f;

        try
        {
            EnemiesManager.Instance.SpawnEnemy(prefab, spawnPosition, new List<GameObject>());
            Debug.Log($"Spawned enemy: {prefab.name} at {spawnPosition}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn enemy: {e.Message}");
        }
    }
}
