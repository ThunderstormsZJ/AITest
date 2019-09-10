using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class SteeringBehaviors
    {
        public float PanicDistance = 5; //恐惧范围
        public float ForceMultiple = 2; //力的乘数 -- 可以影响转向的灵敏程度

        public Vehicle CurVehicle { get; private set; }
        public Vector3 TargetPos { get; private set; }

        public enum Deceleration { slow=3, normal=2, fast=1, }
        public readonly string ObstacleMaskName = "Obstacle";
        public readonly string WallMaskName = "Wall";

        Vector3 m_wanderTarget; // 徘徊的点
        Vector3 m_hideTarget; // 隐藏的点
        float m_memoryTime; // 记忆时间

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
            float jitterTimeSlice = CurVehicle.WanderJitter * Time.fixedDeltaTime * 1000;
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
            List<FieldOfView.ViewCastInfo> castInfoList = CurVehicle.fieldOfView.GetAllViewCastInfo(LayerMask.GetMask(ObstacleMaskName), true);
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
            List<FieldOfView.ViewCastInfo> castInfoList = CurVehicle.fieldOfView.GetAllViewCastInfo(LayerMask.GetMask(WallMaskName), true);
            if (castInfoList.Count > 0)
            {
                // 最近的碰撞信息
                FieldOfView.ViewCastInfo closetCastInfo = castInfoList[0];
                for (int i = 1; i < castInfoList.Count; i++)
                {
                    if (closetCastInfo.dst > castInfoList[i].dst)
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
            // 预测时间- 到达两个物体中点的时间
            Vector3 midPoint = (vehicleA.transform.position + vehicleB.transform.position) / 2;
            float timeToReachMidPoint = (midPoint - CurVehicle.transform.position).magnitude / CurVehicle.MaxSpeed;

            // 预测在T时间内两物体的位置
            Vector3 aPoint = vehicleA.transform.position + vehicleA.Velocity * timeToReachMidPoint;
            Vector3 bPoint = vehicleB.transform.position + vehicleB.Velocity * timeToReachMidPoint;

            // 计算目标点， 到达该位置
            midPoint = (aPoint + bPoint) / 2;

            return Arrive(midPoint);
        }

        #region 躲避物体
        public Vector3 Hide(Vehicle vehicle)
        {
            // 加上时间元素
            m_memoryTime += Time.fixedDeltaTime;
            if (m_hideTarget != Vector3.zero && m_memoryTime < 15)
            {
                Debug.DrawLine(CurVehicle.transform.position, m_hideTarget, Color.blue);
                return Arrive(m_hideTarget, Deceleration.fast);
            }

            // 目标是否在物体可视范围
            // 物体是否发现目标
            if(CurVehicle.fieldOfView.CheckInView(vehicle.transform, LayerMask.GetMask(ObstacleMaskName)))
            {
                if (vehicle.fieldOfView.CheckInView(CurVehicle.transform, LayerMask.GetMask(ObstacleMaskName)))
                {
                    // 找一定范围内的躲避物
                    if (GetHidingPosition(out m_hideTarget))
                    {
                        Debug.DrawLine(CurVehicle.transform.position, m_hideTarget, Color.blue);
                        return Arrive(m_hideTarget, Deceleration.fast);
                    }
                    else
                    {
                        // Else 逃离物体
                        return Evade(vehicle);
                    }
                }
            }

            return Vector3.zero;
        }

        bool GetHidingPosition(out Vector3 hidePointer)
        {
            hidePointer = Vector3.zero;
            Transform[] obstacleByHide = CurVehicle.gameWorld.ObstaclesByHide;
            Transform closetObstacle = null;
            float closetDst = float.MaxValue;
            int findRadius = 15;
            for (int i = 0; i < obstacleByHide.Length; i++)
            {
                // 找到最近的躲避物 （在一定范围内的物体）
                float toObstacleDst = Vector3.Distance(obstacleByHide[i].position, CurVehicle.transform.position);
                if (toObstacleDst < findRadius)
                {
                    if (toObstacleDst < closetDst)
                    {
                        closetDst = toObstacleDst;
                        closetObstacle = obstacleByHide[i];
                    }
                }
            }

            if(closetDst != float.MaxValue)
            {
                // 计算躲藏点
                RaycastHit hitInfo;

                if (Physics.Raycast(CurVehicle.transform.position, closetObstacle.position - CurVehicle.transform.position, out hitInfo, findRadius, LayerMask.GetMask(ObstacleMaskName)))
                {
                    hidePointer = (closetObstacle.position - hitInfo.point)*2 + closetObstacle.position;

                    return true;
                }
            }
            return false;
        }
        #endregion

        // 路径跟随
        public Vector3 FollowPath(Vector3 targetPos, bool isFinish = false)
        {
            if (isFinish)
            {
                return Arrive(targetPos, Deceleration.fast);
            }
            else
            {
                return Seek(targetPos);
            }
        }

        // 按照偏移跟随领头
        public Vector3 OffsetPursuit(Vehicle leader, Vector3 offset)
        {
            // 计算偏移的位置
            Vector3 offsetWorldPos = AIUtils.InverseTransformPointUnscaled(CurVehicle.transform, offset);

            // 预测前进的时间
            float lookAheadTime = Vector3.Distance(leader.transform.position, offsetWorldPos) / (leader.Velocity.magnitude + CurVehicle.MaxSpeed);

            return Arrive(leader.Velocity * lookAheadTime + offsetWorldPos, Deceleration.fast);
        }

        #region 组行为
        // 分离
        public Vector3 Separation(Vehicle[] neighbors)
        {
            Vector3 force = Vector3.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector3 toAgent = CurVehicle.transform.position - neighbors[i].transform.position;

                force += toAgent.normalized * CurVehicle.fieldOfView.ViewRadius / toAgent.magnitude;
            }

            Debug.DrawLine(CurVehicle.transform.position, CurVehicle.transform.position + force, Color.red);

            return force;
        }

        // 队列
        public Vector3 Alignment(Vehicle[] neighbors)
        {
            Vector3 force = Vector3.zero;



            return force;
        }

        #endregion

        public Vector3 Calculate()
        {
            GameWorld gameWorld = CurVehicle.gameWorld;
            Transform target = CurVehicle.gameWorld.TargetPicker;
            Vector3 targetPos = CurVehicle.transform.position;

            //if (CurVehicle.gameWorld.pathCreator != null)
            //{
            //    PathCreation.PathCreator pathCreator = gameWorld.pathCreator;
            //    Vector3 closetPathPoint = pathCreator.path.GetClosestPointOnPath(CurVehicle.transform.position+CurVehicle.transform.forward);
            //    Vector3 lastPointPos = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
            //    bool isFinish = false;
            //    if (!pathCreator.path.isClosedLoop && Vector3.Distance(CurVehicle.transform.position, lastPointPos) < 5f)
            //    {
            //        isFinish = true;
            //        closetPathPoint = lastPointPos;
            //        TargetPos = closetPathPoint;
            //    }
            //    return FollowPath(closetPathPoint, isFinish);
            //}

            if (target != null)
            {
                targetPos = target.position;
                TargetPos = targetPos;
            }

            if (CurVehicle.EscapeVehicle != null)
            {
                TargetPos = CurVehicle.EscapeVehicle.transform.position;
                return Evade(CurVehicle.EscapeVehicle) * ForceMultiple;
            }

            if (CurVehicle.HideVehicle != null)
            {
                TargetPos = CurVehicle.HideVehicle.transform.position;
                return (Hide(CurVehicle.HideVehicle) + 2 * ObstacleAvoidance()) * ForceMultiple;
            }

            if (CurVehicle.fieldOfView.VisibleTargets.Count > 0)
            {
                List<Vehicle> neighbors = new List<Vehicle>();
                List<Transform> visibleTarget = CurVehicle.fieldOfView.VisibleTargets;
                for (int i = 0; i < visibleTarget.Count; i++)
                {
                    neighbors.Add(visibleTarget[i].GetComponent<Vehicle>());
                }

                Debug.Log(CurVehicle.transform + " look " + neighbors.Count);
                return Separation(neighbors.ToArray()) * ForceMultiple;
            }

            //return (Arrive(targetPos)) * ForceMultiple;
            return (Wander() + 2 * ObstacleAvoidance() + 3 * WallAvoidance()) * ForceMultiple;
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
