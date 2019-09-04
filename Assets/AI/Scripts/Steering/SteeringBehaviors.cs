using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class SteeringBehaviors
    {
        public float PanicDistance = 5; //恐惧范围
        public float ForceMultiple = 2; //力的乘数 -- 可以影响转向的灵敏程度

        public Vehicle CurVehicle { get; private set; }
        public Vehicle EscapeVehicle { get; set; }
        public Vector3 TargetPos { get; private set; }

        public enum Deceleration { slow=3, normal=2, fast=1, }

        Vector3 m_wanderTarget; // 徘徊的点

        public SteeringBehaviors(Vehicle vehicle)
        {
            CurVehicle = vehicle;

            // 初始化在圆上随机一点
            float theta = Mathf.PI * 2 * Random.Range(0, 1.0f);
            m_wanderTarget = new Vector3(CurVehicle.WanderRadius * Mathf.Cos(theta), 0, CurVehicle.WanderRadius * Mathf.Sin(theta));
        }

        // 靠近
        public Vector3 Seek(Vector3 targetPos)
        {
            Vector3 desiredVelocity = (targetPos - CurVehicle.transform.position).normalized * CurVehicle.MaxSpeed;
            return (desiredVelocity - CurVehicle.Velocity);
        }

        // 离开
        public Vector3 Flee(Vector3 targetPos)
        {
            if ((CurVehicle.transform.position - targetPos).sqrMagnitude > PanicDistance * PanicDistance)
            {
                return Vector3.zero;
            }
            // 在一定范围内才离开
            Vector3 desiredVelocity = (CurVehicle.transform.position - targetPos).normalized * CurVehicle.MaxSpeed;
            return (desiredVelocity - CurVehicle.Velocity);
        }

        // 抵达
        public Vector3 Arrive(Vector3 targetPos, Deceleration deceleration = Deceleration.normal)
        {
            Vector3 toTarget = targetPos - CurVehicle.transform.position;
            float dist = toTarget.magnitude;

            if (dist > 0)
            {
                // 计算期望的速度
                float speed = dist / ((int)deceleration * 0.8f);
                speed = Mathf.Clamp(speed, 1f, CurVehicle.MaxSpeed);

                // 不需要标准化向量， 因为能够取得向量长度
                Vector3 desiredVelocity = toTarget * speed / dist;
                return (desiredVelocity - CurVehicle.Velocity);
            }

            return Vector3.zero;
        }

        // 追逐
        public Vector3 Pursuit(Vehicle vehicle)
        {
            // 朝向的角度
            float relativeHeading = Vector3.Dot(CurVehicle.transform.forward, vehicle.transform.forward);
            Vector3 toVehicle = vehicle.transform.position - CurVehicle.transform.position;

            // 在前面 && 夹角小于18度 Cos18 = 0.95
            if (Vector3.Dot(toVehicle, CurVehicle.transform.forward) > 0 &&
                relativeHeading < -0.95)
            {
                return Seek(vehicle.transform.position);
            }

            // 预测逃避位置
            // 预测时间 正比与逃离物体之间距离， 反比追逐物体最大速度
            float lookAheadTime = toVehicle.magnitude / (CurVehicle.MaxSpeed);

            // 加上转向时间
            lookAheadTime += TurnaroundTime(CurVehicle, vehicle.transform.position);

            return Seek(vehicle.transform.position + vehicle.Velocity * lookAheadTime);
        }

        // 逃避
        public Vector3 Evade(Vehicle vehicle)
        {
            // 不用检测正面
            // 逃离预测位置
            Vector3 toVehicle = vehicle.transform.position - CurVehicle.transform.position;

            float lookAheadTime = toVehicle.magnitude / (CurVehicle.MaxSpeed);

            return Flee(vehicle.transform.position + vehicle.Velocity * lookAheadTime);
        }

        // 徘徊
        public Vector3 Wander()
        {
            float jitterTimeSlice = CurVehicle.WanderJitter * Time.deltaTime * 1000;
            m_wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * jitterTimeSlice, 0, Random.Range(-1.0f, 1.0f) * jitterTimeSlice);
            // 重新映射到圆上
            m_wanderTarget = m_wanderTarget.normalized * CurVehicle.WanderRadius;
            // 加上与智能体的距离
            Vector3 targetLocal = m_wanderTarget + new Vector3(0, 0, CurVehicle.WanderDistance);

            //Vector3 targetWorld = CurVehicle.transform.TransformPoint(targetLocal);
            Vector3 targetWorld = AIUtils.TransformPointUnscaled(CurVehicle.transform, targetLocal);

            return (targetWorld - CurVehicle.transform.position);
        }

        // 避开障碍
        public Vector3 ObstacleAvoidance()
        {
            // 找到最近的障碍物
            Vector3 force = Vector3.zero;
            List<FieldOfView.ViewCastInfo> castInfoList = CurVehicle.fieldOfView.GetAllViewCastInfo(LayerMask.GetMask("Obstacle"), true);
            if (castInfoList.Count>0)
            {
                // 获取最近的点
                Vector3 closetPoint = castInfoList[0].pointer;
                for (int i = 1; i < castInfoList.Count; i++)
                {
                    if (closetPoint.sqrMagnitude > castInfoList[i].pointer.sqrMagnitude)
                    {
                        closetPoint = castInfoList[i].pointer;
                    }
                }
                Vector3 localClosetPoint =AIUtils.InverseTransformPointUnscaled(CurVehicle.transform, closetPoint);
                float fieldViewR = CurVehicle.fieldOfView.ViewRadius;
                // 力的大小与距离成反比a
                float forceBase = 1;
                float xDisMul = 1 + (fieldViewR - Mathf.Abs(localClosetPoint.x)) / fieldViewR;
                float zDisMul = 1 + (fieldViewR - Mathf.Abs(localClosetPoint.z)) / fieldViewR;
                // 计算侧向力
                force.x = forceBase * xDisMul * zDisMul * (localClosetPoint.x > 0 ? -1 : 1);

                // 计算制动力
                force.z = - forceBase * zDisMul;

                force = AIUtils.TransformPointUnscaled(CurVehicle.transform, force) - CurVehicle.transform.position;

                Debug.DrawLine(CurVehicle.transform.position, CurVehicle.transform.position + force.normalized * 10, Color.red);

            }
            return force;
        }

        // 避开墙
        public Vector3 WallAvoidance()
        {
            Vector3 force = Vector3.zero;
            List<FieldOfView.ViewCastInfo> castInfoList = CurVehicle.fieldOfView.GetAllViewCastInfo(LayerMask.GetMask("Wall"), true);
            if (castInfoList.Count > 0)
            {
                // 最近的碰撞信息
                FieldOfView.ViewCastInfo closetCastInfo = castInfoList[0];
                for (int i = 1; i < castInfoList.Count; i++)
                {
                    if (closetCastInfo.pointer.sqrMagnitude > castInfoList[i].pointer.sqrMagnitude)
                    {
                        closetCastInfo = castInfoList[i];
                    }
                }

                // 根据碰撞的法线算出力的大小
                float fieldViewR = CurVehicle.fieldOfView.ViewRadius;

                force = closetCastInfo.normal * (fieldViewR - Vector3.Distance(CurVehicle.transform.position, closetCastInfo.pointer));

                Debug.DrawLine(CurVehicle.transform.position, CurVehicle.transform.position + force.normalized * 10, Color.red);
            }

            return force;
        }

        // 插入
        public Vector3 Interpose(Vehicle vehicleA, Vehicle vehicleB)
        {
            // 

            return Vector3.zero;
        }

        public Vector3 Calculate()
        {
            Transform target = CurVehicle.gameWorld.TargetPicker;
            Vector3 targetPos = CurVehicle.transform.position;

            if (target != null)
            {
                targetPos = target.position;
                TargetPos = targetPos;
            }

            if (EscapeVehicle != null)
            {
                TargetPos = EscapeVehicle.transform.position;
                return Evade(EscapeVehicle) * ForceMultiple;
            }

            //return (Arrive(targetPos) + 2 * ObstacleAvoidance()) * ForceMultiple;
            return (Wander() + 2 * ObstacleAvoidance() + 2 * WallAvoidance()) * ForceMultiple;
        }

        /// <summary>
        /// 转向时间
        /// </summary>
        /// <param name="vehicle">需要转向的物体</param>
        /// <param name="targetPos">转向的目标</param>
        /// <returns></returns>
        float TurnaroundTime(Vehicle vehicle, Vector3 targetPos)
        {
            Vector3 toTarget = targetPos - vehicle.transform.position;
            // 计算需要旋转的夹角
            float dot = Vector3.Dot(vehicle.transform.forward, toTarget.normalized);

            float coefficient = 0.5f;
            // dot - 1 为负数， 除负数保证为正数。 
            // 角度越大时间越大
            return (dot - 1) * - coefficient;
        }

        
    }
}
