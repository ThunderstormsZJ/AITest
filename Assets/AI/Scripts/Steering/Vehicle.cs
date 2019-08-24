using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class Vehicle : MovingEntity
    {
        public GameWorld gameWorld;

        protected Rigidbody entityRigidbody;
        [HideInInspector]
        public SteeringBehaviors steeringBehaviors;

        protected override void OnStart()
        {
            base.OnStart();

            steeringBehaviors = new SteeringBehaviors(this);
            entityRigidbody = GetComponent<Rigidbody>();

            m_fMass = entityRigidbody.mass;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // 加速度
            Vector3 steeringForce = steeringBehaviors.Calculate();
            Debug.Log(steeringForce);
            Vector3 acceleration = steeringForce / m_fMass;

            // 更新速度
            Velocity += acceleration * Time.deltaTime;

            // 确保不超过最大速度
            Velocity = Vector3.ClampMagnitude(Velocity, MaxSpeed);

            Debug.DrawLine(transform.position, transform.position+ acceleration.normalized * 10, Color.green);

            // 更新位置
            //transform.position += Velocity * Time.deltaTime;
            entityRigidbody.velocity = new Vector3(Velocity.x, entityRigidbody.velocity.y, Velocity.z);

            // 速度远大于一个很小的值就更新朝向
            //if (Velocity.magnitude > 0.00000001)
            //{
            //    m_vHeading = new Vector3(Velocity.y, transform.forward.y, Velocity.z);
            //    transform.forward = m_vHeading;
            //}

        }
    }
}