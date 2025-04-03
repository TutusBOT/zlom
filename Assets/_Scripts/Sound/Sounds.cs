using UnityEngine;

public static class Sounds
{
    public static void MakeSound(Sound sound)
    {
        Collider[] colliders = Physics.OverlapSphere(sound.pos, sound.range);

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out IHear hearer))
            {
                hearer.RespondToSound(sound);
            }
        }
    }
}
