using UnityEngine;

public class BansheeRangeDetector : RangeDetector
{
    public override GameObject UpdateDetector()
    {
        Banshee banshee = GetComponent<Banshee>();

        if (banshee == null || banshee.targetedPlayer == null || banshee.targetedPlayer.IsDead())
        {
            DetectedTarget = null;
            return null;
        }

        Player targetPlayer = banshee.targetedPlayer;
        Vector3 directionToTarget = targetPlayer.transform.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

        bool withinRadius = distance <= detectionRadius;
        bool withinAngle = angle <= detectionAngle / 2f;

        if (withinRadius && withinAngle)
        {
            DetectedTarget = targetPlayer.gameObject;
        }
        else
        {
            DetectedTarget = null;
        }

        return DetectedTarget;
    }
}