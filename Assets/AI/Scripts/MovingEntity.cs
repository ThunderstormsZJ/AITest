using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingEntity : BaseGameEntity
{
    // 最大速度
    public float MaxSpeed;

    // 速度
    protected Vector3 m_vVelocity = new Vector3(0, 0, 0.8f);
    // 实体的朝向
    protected Vector3 m_vHeading;
    // 垂直于朝向的向量
    //protected Vector3 m_vSide;
    // 质量
    protected float m_fMass;
    // 产生供已自己最大的动力
    protected float m_fMaxForce;
    // 能旋转的最大速率
    protected float m_fMaxTurnRate;

    protected override void OnUpdate()
    {
        base.OnUpdate();
    }
}
