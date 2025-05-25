using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Deogen: Enemy
{
    //Values below have to change aswell in DeogenChaseComponent.cs!!
    [Header("Deogen Behavior")]
    public float minSpeed = 1f;
    public float maxSpeed = 5f;
    public float slowestDistance = 2f;
    public float maxEffectiveDistance = 20f;

    
    protected override void Update()
    {
        base.Update();

        float distance = TargetClosestPlayer();

        if (distance <= slowestDistance)
        {
            runSpeed = minSpeed;

        }
        else if (distance >= maxEffectiveDistance)
        {
            runSpeed = maxSpeed;
        }
        else
        {
            float t = (distance - slowestDistance) / (maxEffectiveDistance - slowestDistance);
            runSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);
        }
    }
    private float TargetClosestPlayer()
    {
        Player[] players = PlayerManager.Instance.GetAllPlayers().ToArray();
        GameObject closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        foreach (Player player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.gameObject;
            }
        }

        behaviorGraph.SetVariableValue("Target", closestPlayer);
        return closestDistance;
    }
}
