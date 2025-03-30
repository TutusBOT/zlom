using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Line of Sight check", story: "Check [Target] with Line Of Sight [detector]", category: "Conditions", id: "96aa4c8006646f7e785644d36e12f167")]
public partial class LineOfSightCheckCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<LineOfSightDetector> Detector;

    public override bool IsTrue(){
        bool result = Detector.Value.PerformDetection(Target.Value) != null;
        return result;
    }
}
