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
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        float speed = Velocity.magnitude;
        animator.SetFloat("Velocity", Mathf.Lerp(animator.GetFloat("Velocity"), speed, 0.03f));
    }
}
