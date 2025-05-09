using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Is In Light",
    story: "[Agent] is in light",
    category: "Conditions"
)]
public partial class IsInLightCondition : Condition
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [Tooltip("Include 'recently in light' memory?")]
    public bool useMemory = true;

    public override bool IsTrue()
    {
        if (Agent.Value == null)
            return false;

        LightDetector detector = Agent.Value.GetComponent<LightDetector>();
        if (detector == null)
            return false;

        if (detector.IsInLight)
            return true;

        // Optional memory check
        return useMemory && detector.WasRecentlyInLight;
    }
}
