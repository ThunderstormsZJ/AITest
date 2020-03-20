using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class MovingEntity : BaseGameEntity
    {
        // 速度
        [HideInInspector]
        public Vector3 Velocity { get; set; } = new Vector3(0, 0, 0);

        // 质量
        protected float m_fMass;
        // 能旋转的最大速率
        protected float m_fMaxTurnRate;

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
