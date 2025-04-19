using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class PlayerChatDisplay : NetworkBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    private GameObject chatTextPrefab;

    [Header("Visual Settings")]
    [SerializeField]
    private float displayDuration = 5f;

    [SerializeField]
    private float fadeOutDuration = 0.5f;

    [SerializeField]
    private int maxVisibleMessages = 3;

    [SerializeField]
    private float messageSpacing = 0.5f;
    private Transform _chatAnchor;

    private readonly SyncVar<string> _currentMessage = new SyncVar<string>("");

    [Serializable]
    private class ChatMessage
    {
        public string text;
        public float timeRemaining;
        public GameObject textObject;
        public TextMeshPro textMesh;
        public float alpha = 1.0f;

        public ChatMessage(string message, float duration)
        {
            text = message;
            timeRemaining = duration;
        }
    }

    private List<ChatMessage> _messageQueue = new List<ChatMessage>();
    private Dictionary<GameObject, Coroutine> _fadeCoroutines =
        new Dictionary<GameObject, Coroutine>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        _currentMessage.OnChange += HandleMessageChanged;

        _chatAnchor = new GameObject("ChatAnchor").transform;
        _chatAnchor.SetParent(transform);
        _chatAnchor.localPosition = new Vector3(0, 0.1f, 0);
    }

    [ServerRpc]
    public void SendChatMessageServerRpc(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _currentMessage.Value = message;
    }

    private void HandleMessageChanged(string oldValue, string newValue, bool asServer)
    {
        if (asServer)
            return;

        if (!string.IsNullOrEmpty(newValue))
        {
            AddMessageToQueue(newValue);
        }
    }

    private void AddMessageToQueue(string message)
    {
        ChatMessage chatMessage = new ChatMessage(message, displayDuration);

        GameObject textObj = Instantiate(chatTextPrefab, _chatAnchor);
        TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();

        if (textMesh == null)
        {
            Debug.LogError("Chat text prefab must contain a TextMeshPro component!");
            Destroy(textObj);
            return;
        }

        textMesh.text = message;
        textMesh.alpha = 1.0f;

        chatMessage.textObject = textObj;
        chatMessage.textMesh = textMesh;

        _messageQueue.Add(chatMessage);

        // Limit queue size
        if (_messageQueue.Count > maxVisibleMessages)
        {
            // Remove oldest messages beyond our limit
            while (_messageQueue.Count > maxVisibleMessages)
            {
                ChatMessage oldestMessage = _messageQueue[0];

                // Stop any fade coroutine for this message
                if (_fadeCoroutines.TryGetValue(oldestMessage.textObject, out Coroutine coroutine))
                {
                    StopCoroutine(coroutine);
                    _fadeCoroutines.Remove(oldestMessage.textObject);
                }

                Destroy(oldestMessage.textObject);
                _messageQueue.RemoveAt(0);
            }
        }

        RepositionMessages();

        _fadeCoroutines[textObj] = StartCoroutine(FadeOutMessage(chatMessage));
    }

    private void RepositionMessages()
    {
        float currentHeight = 0;

        // Position from bottom to top (newest at bottom)
        for (int i = 0; i < _messageQueue.Count; i++)
        {
            ChatMessage msg = _messageQueue[i];

            if (msg.textObject != null)
            {
                // Position this message above the previous one
                msg.textObject.transform.localPosition = new Vector3(0, currentHeight, 0);

                // Add height for next message
                currentHeight += messageSpacing;
            }
        }
    }

    private IEnumerator FadeOutMessage(ChatMessage message)
    {
        while (message.timeRemaining > fadeOutDuration)
        {
            message.timeRemaining -= Time.deltaTime;
            yield return null;
        }

        float startTime = Time.time;
        float endTime = startTime + fadeOutDuration;

        while (Time.time < endTime && message.textObject != null)
        {
            float t = (Time.time - startTime) / fadeOutDuration;
            message.alpha = Mathf.Lerp(1, 0, t);

            if (message.textMesh != null)
                message.textMesh.alpha = message.alpha;

            yield return null;
        }

        _messageQueue.Remove(message);
        _fadeCoroutines.Remove(message.textObject);

        if (message.textObject != null)
            Destroy(message.textObject);

        RepositionMessages();
    }

    // Helper to always face camera
    private void LateUpdate()
    {
        foreach (var message in _messageQueue)
        {
            if (message.textObject && message.textObject.activeInHierarchy)
            {
                message.textObject.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up coroutines
        foreach (var coroutine in _fadeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        // Clean up gameobjects
        foreach (var message in _messageQueue)
        {
            if (message.textObject != null)
                Destroy(message.textObject);
        }
    }
}
