using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Get Current Light Source",
    story: "Get the current light source affecting [Agent] and assign it to [OutputLightSource]",
    category: "Actions"
)]
public partial class GetCurrentLightSourceAction : Action
{
    [SerializeReference]
    public BlackboardVariable<GameObject> Agent;

    [SerializeReference]
    public BlackboardVariable<RoomLightSource> OutputLightSource;

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || OutputLightSource == null)
            return Status.Failure;

        LightDetector detector = Agent.Value.GetComponent<LightDetector>();
        if (detector == null)
            return Status.Failure;

        if (detector.IsInLight && detector.CurrentLightSource != null)
        {
            OutputLightSource.Value = detector.CurrentLightSource;
            Debug.Log(detector.CurrentLightSource.transform.position);
            return Status.Success;
        }

        return Status.Failure;
    }
}
