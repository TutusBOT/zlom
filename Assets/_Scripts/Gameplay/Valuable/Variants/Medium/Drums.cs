using UnityEngine;

public class Drums : Valuable
{
    [Header("Vase Properties")]
    [SerializeField]
    private string drumSoundId = "drum_hit";

    public override void OnStartServer()
    {
        base.OnStartServer();

        size = ValuableSize.Medium;
    }

    public override void OnPickedUp(Player player)
    {
        base.OnPickedUp(player);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(drumSoundId, transform.position);
        }
    }
}
