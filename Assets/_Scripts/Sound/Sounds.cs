using UnityEngine;

public static class Sounds
{
    public static void MakeSound(Sound sound)
    {
        Collider[] colliders = Physics.OverlapSphere(
            sound.pos,
            sound.range,
            LayerMask.GetMask("Enemy")
        );

        foreach (Collider collider in colliders)
        {
            IHear hearer = collider.GetComponent<IHear>();

            if (hearer == null)
                hearer = collider.GetComponentInParent<IHear>();

            if (hearer != null)
            {
                hearer.RespondToSound(sound);
            }
        }
    }
}
