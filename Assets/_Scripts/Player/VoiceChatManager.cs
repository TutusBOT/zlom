using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class VoiceChatManager : NetworkBehaviour
{
    // Voice chat settings
    [Header("Voice Settings")]
    [SerializeField]
    private KeyCode pushToTalkKey = KeyCode.V;

    [SerializeField]
    private float maxVoiceDistance = 15f;

    [SerializeField]
    private float voiceUpdateRate = 0.05f; // REDUCED: 50ms for more real-time feel

    [SerializeField]
    private int sampleRate = 16000; // 16kHz - good for voice

    [SerializeField]
    private int recordingLength = 1; // 1 second clips

    // Voice processing
    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private float _recordingTimer = 0f;
    private string _deviceName;
    private Dictionary<int, float> _lastPlayTime = new Dictionary<int, float>();

    // Voice playback
    private Dictionary<int, AudioSource> _playerVoiceSources = new Dictionary<int, AudioSource>();

    // Track speaking state
    private Dictionary<int, bool> _playerIsSpeaking = new Dictionary<int, bool>();
    private readonly SyncVar<int> _networkId = new SyncVar<int>(0);

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            InitializeMicrophone();
        }

        // Create audio source for this player's voice
        GameObject voiceObj = new GameObject($"Voice_{gameObject.name}");
        voiceObj.transform.SetParent(transform);
        voiceObj.transform.localPosition = Vector3.zero;

        AudioSource source = voiceObj.AddComponent<AudioSource>();
        source.spatialBlend = 1.0f; // Full 3D
        source.minDistance = 1f;
        source.maxDistance = maxVoiceDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.playOnAwake = false;
        source.loop = false; // IMPORTANT: Don't loop voice audio

        // Store reference to this player's voice source
        _networkId.Value = (int)GetComponent<NetworkObject>().ObjectId;
        _playerVoiceSources[_networkId.Value] = source;
        _playerIsSpeaking[_networkId.Value] = false;
        _lastPlayTime[_networkId.Value] = 0f;
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            _deviceName = Microphone.devices[0];
            _isRecording = true;
            Debug.Log($"Microphone initialized: {_deviceName}");
            _recordingClip = Microphone.Start(_deviceName, true, recordingLength, sampleRate);

            // Wait until microphone starts recording
            while (!(Microphone.GetPosition(_deviceName) > 0)) { }
        }
        else
        {
            Debug.LogError("No microphone found!");
        }
    }

    private void Update()
    {
        if (!IsOwner || !_isRecording)
            return;

        bool wasTalking = _recordingTimer > 0;
        bool isTalking = Input.GetKey(pushToTalkKey);

        // Push to talk implementation
        if (isTalking)
        {
            _recordingTimer += Time.deltaTime;

            // Send voice data at regular intervals
            if (_recordingTimer >= voiceUpdateRate)
            {
                CaptureAndSendVoiceData();
                _recordingTimer = 0;
            }
        }
        else
        {
            if (wasTalking)
            {
                // Just stopped talking - send the stop talking signal
                StopTalkingServerRpc();
            }
            _recordingTimer = 0;
        }

        // Clean up voice playback
        CleanupVoiceSources();
    }

    private void CleanupVoiceSources()
    {
        // Stop any source that hasn't received data in a while
        float currentTime = Time.time;
        List<int> stoppedPlayers = new List<int>();

        foreach (var player in _playerIsSpeaking.Keys)
        {
            if (
                _playerIsSpeaking[player]
                && currentTime - _lastPlayTime[player] > voiceUpdateRate * 3
            )
            {
                if (_playerVoiceSources.ContainsKey(player))
                {
                    AudioSource source = _playerVoiceSources[player];
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                    }
                }
                stoppedPlayers.Add(player);
            }
        }

        foreach (var player in stoppedPlayers)
        {
            _playerIsSpeaking[player] = false;
        }
    }

    private void CaptureAndSendVoiceData()
    {
        // Get current microphone position
        int pos = Microphone.GetPosition(_deviceName);
        if (pos <= 0)
            return;

        // Calculate how many samples to get (smaller chunks for real-time feel)
        int sampleLength = (int)(sampleRate * voiceUpdateRate);
        float[] samples = new float[sampleLength];

        // Figure out where to start reading (handle looping)
        int startPos = pos - sampleLength;
        if (startPos < 0)
            startPos += _recordingClip.samples;

        // Get audio data
        _recordingClip.GetData(samples, startPos);

        // Check if there's actual sound to transmit
        if (HasSound(samples))
        {
            // Convert float array to byte array (simple PCM conversion)
            byte[] audioData = ConvertAudioToBytes(samples);

            // Send to server
            SendVoiceDataServerRpc(audioData);
        }
    }

    [ServerRpc]
    private void SendVoiceDataServerRpc(byte[] audioData)
    {
        // On server, distribute to nearby players
        foreach (var player in Object.FindObjectsOfType<VoiceChatManager>())
        {
            // Skip sending to self
            if (player == this)
                continue;

            // Calculate distance between players
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // Only send to players in range
            if (distance <= maxVoiceDistance)
            {
                // Calculate volume falloff based on distance
                float volumeScale = 1f - (distance / maxVoiceDistance);

                // Send to that player
                player.ReceiveVoiceDataClientRpc(audioData, _networkId.Value, volumeScale);
            }
        }
    }

    [ServerRpc]
    private void StopTalkingServerRpc()
    {
        // Tell everyone this player stopped talking
        StopTalkingClientRpc(_networkId.Value);
    }

    [ObserversRpc]
    private void StopTalkingClientRpc(int senderId)
    {
        if (_playerVoiceSources.ContainsKey(senderId))
        {
            var source = _playerVoiceSources[senderId];
            if (source != null)
            {
                source.Stop();
            }
            _playerIsSpeaking[senderId] = false;
        }
    }

    [ObserversRpc]
    private void ReceiveVoiceDataClientRpc(byte[] audioData, int senderId, float volumeScale)
    {
        if (IsOwner && senderId == _networkId.Value)
            return; // Don't play your own voice back

        // Update last play time
        _lastPlayTime[senderId] = Time.time;
        _playerIsSpeaking[senderId] = true;

        // Convert bytes back to audio
        float[] samples = ConvertBytesToAudio(audioData);

        // Create audio clip from samples
        AudioClip voiceClip = AudioClip.Create("VoiceData", samples.Length, 1, sampleRate, false);
        voiceClip.SetData(samples, 0);

        // Make sure we have an audio source for this player
        if (!_playerVoiceSources.ContainsKey(senderId))
        {
            GameObject voiceObj = new GameObject($"Voice_Player_{senderId}");
            voiceObj.transform.SetParent(transform);
            AudioSource source = voiceObj.AddComponent<AudioSource>();
            source.spatialBlend = 1.0f;
            source.minDistance = 1f;
            source.maxDistance = maxVoiceDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.playOnAwake = false;
            source.loop = false; // IMPORTANT: Don't loop voice
            _playerVoiceSources[senderId] = source;
            _playerIsSpeaking[senderId] = true;
            _lastPlayTime[senderId] = Time.time;
        }

        // Get the appropriate audio source
        AudioSource playerSource = _playerVoiceSources[senderId];

        // Stop if it was already playing
        if (playerSource.isPlaying)
        {
            playerSource.Stop();
        }

        // Play the voice directly (NO LOOPING)
        playerSource.clip = voiceClip;
        playerSource.volume = volumeScale;
        playerSource.Play();
    }

    // Helper methods for audio processing
    private bool HasSound(float[] samples)
    {
        // More sensitive volume detection
        float sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        float average = sum / samples.Length;
        return average > 0.005f; // Lowered threshold for better sensitivity
    }

    private byte[] ConvertAudioToBytes(float[] samples)
    {
        // Convert float samples to 16-bit PCM
        byte[] byteArray = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            // Convert to 16 bit
            short value = (short)(samples[i] * 32767);
            byteArray[i * 2] = (byte)(value & 0xFF);
            byteArray[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }
        return byteArray;
    }

    private float[] ConvertBytesToAudio(byte[] byteArray)
    {
        // Convert 16-bit PCM back to float array
        int sampleCount = byteArray.Length / 2;
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short value = (short)((byteArray[i * 2 + 1] << 8) | byteArray[i * 2]);
            samples[i] = value / 32767f;
        }
        return samples;
    }

    private void OnDestroy()
    {
        // Stop microphone when object is destroyed
        if (_isRecording && IsOwner)
        {
            Microphone.End(_deviceName);
        }
    }

    // For voice indicators
    public bool IsPlayerTalking(int playerId)
    {
        if (_playerIsSpeaking.ContainsKey(playerId))
        {
            return _playerIsSpeaking[playerId];
        }
        return false;
    }
}
