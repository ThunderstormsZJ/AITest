using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class MovingEntity : BaseGameEntity
    {
        // 最大速度
        [Range(0, 5)]
        public float MaxSpeed;

        // 速度
        [HideInInspector]
        public Vector3 Velocity { get; set; } = new Vector3(0, 0, 0);

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
}
