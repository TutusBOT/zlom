using System.Collections;
using Unity.Behavior;
using Unity.VisualScripting;
using UnityEngine;

public class Banshee : Enemy
{
    public Player targetedPlayer = null;

    public override void OnStartServer()
    {
        base.OnStartServer();
        TargetRandomAlivePlayer();
    }
    private void TargetRandomAlivePlayer()
    {
        Player[] players = PlayerManager.Instance.GetAllAlivePlayers().ToArray();

        if (players.Length == 0)
        {
            return;
        }

        if (targetedPlayer != null)
        {
            targetedPlayer.playerHealth.OnDeath -= OnTargetDeath;
        }
        targetedPlayer = players[Random.Range(0, players.Length)];
        targetedPlayer.playerHealth.OnDeath += OnTargetDeath;
        Debug.Log("Target player by banshee:  "+targetedPlayer.name);
        behaviorGraph.SetVariableValue("Target", targetedPlayer.gameObject);
    }

        private void OnTargetDeath()
        {
            TargetRandomAlivePlayer();
        }
}
