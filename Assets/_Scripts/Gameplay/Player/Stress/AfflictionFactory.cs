using System.Collections.Generic;
using UnityEngine;

public static class AfflictionFactory
{
    private static Dictionary<StressController.AfflictionType, System.Type> afflictionTypes =
        new Dictionary<StressController.AfflictionType, System.Type>
        {
            // { StressController.AfflictionType.Doomed, typeof(DoomedAffliction) },
            { StressController.AfflictionType.Paranoid, typeof(ParanoidAffliction) },
            { StressController.AfflictionType.Fearful, typeof(FearfulAffliction) },
            // { StressController.AfflictionType.Hysterical, typeof(HystericalAffliction) },
            // { StressController.AfflictionType.Delusional, typeof(DelusionalAffliction) },
            // { StressController.AfflictionType.Resigned, typeof(ResignedAffliction) },
            // { StressController.AfflictionType.Clingy, typeof(ClingyAffliction) },
            // { StressController.AfflictionType.Selfish, typeof(SelfishAffliction) },
            // { StressController.AfflictionType.Reckless, typeof(RecklessAffliction) },
        };

    public static IAffliction CreateAffliction(
        StressController.AfflictionType type,
        StressController controller,
        Player player
    )
    {
        if (type == StressController.AfflictionType.None)
            return null;

        if (afflictionTypes.TryGetValue(type, out var afflictionType))
        {
            IAffliction affliction = (IAffliction)System.Activator.CreateInstance(afflictionType);
            affliction.Initialize(controller, player);
            return affliction;
        }

        Debug.LogError($"No affliction class found for type {type}");
        return null;
    }
}
