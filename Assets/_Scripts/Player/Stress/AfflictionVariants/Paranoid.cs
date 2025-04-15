using System.Collections;
using UnityEngine;

public class ParanoidAffliction : AbstractAffliction
{
    private Coroutine voiceDisruptionCoroutine;
    private float disruptionChance = 0.001f;
    private Coroutine _randomSoundCoroutine;

    public override StressController.AfflictionType Type =>
        StressController.AfflictionType.Paranoid;
    public override string QuoteText => "I heard them… whispering.";

    public override void Update()
    {
        if (!IsOwner() || !isActive)
            return;

        // Random voice chat disruption
        if (Random.value < disruptionChance)
        {
            voiceDisruptionCoroutine = stressController.StartCoroutine(
                TemporarilyDisableVoiceChat()
            );
        }
    }

    public override void OnAfflictionActivated()
    {
        base.OnAfflictionActivated();

        // Start the random sound coroutine when affliction activates
        if (_randomSoundCoroutine == null && IsOwner())
        {
            _randomSoundCoroutine = stressController.StartCoroutine(PlayRandomSoundsRoutine());
            Debug.Log("Started random paranoia sound effects");
        }
    }

    public override void OnAfflictionDeactivated()
    {
        base.OnAfflictionDeactivated();

        // Clean up any running coroutines
        if (voiceDisruptionCoroutine != null)
        {
            stressController.StopCoroutine(voiceDisruptionCoroutine);
            voiceDisruptionCoroutine = null;
        }
    }

    private IEnumerator PlayRandomSoundsRoutine()
    {
        while (isActive && IsOwner())
        {
            // Wait for random interval between 10-60 seconds
            float waitTime = Random.Range(10f, 60f);
            yield return new WaitForSeconds(waitTime);

            if (!isActive)
                break;

            PlayIllusionarySound();
        }
    }

    private IEnumerator TemporarilyDisableVoiceChat()
    {
        var voiceChat = GetVoiceChat();
        if (voiceChat == null)
            yield break;

        float muteDuration = Random.Range(3f, 10f);

        // Implement this method in VoiceChatManager
        // voiceChat.SetTemporarilyMuted(true);

        yield return new WaitForSeconds(muteDuration);

        // voiceChat.SetTemporarilyMuted(false);
    }

    private void PlayIllusionarySound()
    {
        if (player == null)
            return;

        // Get random position around player
        Vector3 randomDir = Random.insideUnitSphere.normalized;
        float distance = Random.Range(3f, 8f);
        Vector3 soundPosition = player.transform.position + randomDir * distance;

        AudioManager.Instance.PlayRandomSound(true, soundPosition, 0.7f);

        Debug.Log("Playing random paranoia sound");
    }

    public override GameObject CreateVisualIndicator(Transform parent)
    {
        // Create a visual indicator for other players to see
        // (e.g., particle effects of whispering shadows)
        return null; // Replace with actual implementation
    }
}
