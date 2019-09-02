using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class Vehicle : MovingEntity
    {
        public GameWorld gameWorld;

        [Header("=======Wander Param=======")]
        public float WanderRadius = 10; // 徘徊圆的半径
        public float WanderDistance = 3f; //  徘徊圆在智能体前的位置
        public float WanderJitter = 2f; // 徘徊每秒随机的最大值

        [HideInInspector]
        public SteeringBehaviors steeringBehaviors;

        protected Rigidbody entityRigidbody;
        protected BoxCollider boxCollider;
        private Vector3 m_curVelocity;
        private AIThirdPersonUserController userController;
        private bool isUserController;

        protected override void OnStart()
        {
            base.OnStart();

            steeringBehaviors = new SteeringBehaviors(this);
            entityRigidbody = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();
            userController = GetComponent<AIThirdPersonUserController>();

            isUserController = userController != null && userController.enabled;

            m_fMass = entityRigidbody.mass;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Vector3 steeringForce;
            if (isUserController)
            {
                //Velocity = userController.moveVel * MaxSpeed;
                steeringForce = userController.moveVel * 10;
            }
            else
            {
                steeringForce = steeringBehaviors.Calculate();
            }
            // 加速度
            Vector3 acceleration = steeringForce / m_fMass;

            // 更新速度
            Velocity += acceleration * Time.deltaTime;

            // 确保不超过最大速度
            Velocity = Vector3.ClampMagnitude(Velocity, MaxSpeed);
            Velocity = new Vector3(Velocity.x, 0, Velocity.z);
#if UNITY_EDITOR
            Debug.DrawLine(transform.position, transform.position + Velocity.normalized * 10, Color.black);
#endif
            if (steeringBehaviors.TargetPos != Vector3.zero)
            {
                Vector3 targetPos = steeringBehaviors.TargetPos;
                float targetSqrDist = (targetPos - transform.position).sqrMagnitude;
                if (targetSqrDist < 0.03)
                {
                    //临界情况
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref m_curVelocity, 0.5f, 0.5f);
                    Velocity = Vector3.zero;
                }
            }

            if (Velocity != Vector3.zero)
            {
                //transform.forward = Vector3.Slerp(transform.forward, userController.moveVel.normalized, 0.3f);
                // 更新朝向
                Quaternion lookRotation = Quaternion.LookRotation(Velocity);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 0.1f);
                m_curVelocity = Velocity;
            }

            entityRigidbody.velocity = Velocity;

            UpdateAnimator();
        }
        protected virtual void UpdateAnimator() { }
    }
}