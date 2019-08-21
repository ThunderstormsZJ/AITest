using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MovingEntity
{
    protected Rigidbody entityRigidbody;

    protected override void OnStart()
    {
        base.OnStart();

        entityRigidbody = GetComponent<Rigidbody>();

        m_fMass = entityRigidbody.mass;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // 加速度
        Vector3 acceleration = new Vector3(0, 0, 0f) / m_fMass;

        // 更新速度
        m_vVelocity += acceleration * Time.deltaTime;

        // 确保不超过最大速度
        m_vVelocity = Vector3.ClampMagnitude(m_vVelocity, MaxSpeed);

        // 更新位置
        //transform.position += m_vVelocity * Time.deltaTime;
        entityRigidbody.velocity = m_vVelocity;

        // 速度远大于一个很小的值就更新朝向
        if (m_vVelocity.magnitude > 0.00000001)
        {
            m_vHeading = m_vVelocity.normalized;
            transform.forward = m_vHeading;
        }

    }
}
