using System.Collections;
using UnityEngine;

public class ParanoidAffliction : AbstractAffliction
{
    private Coroutine voiceDisruptionCoroutine;
    private float disruptionChance = 0.001f;

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

    public override GameObject CreateVisualIndicator(Transform parent)
    {
        // Create a visual indicator for other players to see
        // (e.g., particle effects of whispering shadows)
        return null; // Replace with actual implementation
    }
}
