using System;
using Unity.Behavior;

[BlackboardEnum]
public enum MovementState
{
    Wandering,
    Patrolling,
    Fleeing,
    Chasing,
    Investigating,
    Idle,
}
