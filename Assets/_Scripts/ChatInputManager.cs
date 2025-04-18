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
        chatInputField.Select();
        chatInputField.ActivateInputField();

        _player.ToggleControls(false);
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

        if (!string.IsNullOrEmpty(message))
        {
            _player.GetPlayerChatDisplay().SendChatMessageServerRpc(message);
        }

        CloseChat();
    }
}
