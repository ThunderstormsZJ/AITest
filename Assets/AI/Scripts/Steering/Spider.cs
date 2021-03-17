using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steering;

public class Spider : Vehicle 
{
    private Animator animator;

    protected override void OnStart()
    {
        base.OnStart();

        animator = GetComponent<Animator>();

        steeringBehaviors.WallAvoidanceOn();
        steeringBehaviors.ObstacleAvoidanceOn();
        steeringBehaviors.WanderOn();
        //steeringBehaviors.ArriveOn();
        steeringBehaviors.FlockingOn();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
    }

    protected override void UpdateAnimator()
    {
        base.UpdateAnimator();

        float speed = Velocity.magnitude;
        animator.SetFloat("Velocity", Mathf.Lerp(animator.GetFloat("Velocity"), speed, 0.03f));
    }
}
