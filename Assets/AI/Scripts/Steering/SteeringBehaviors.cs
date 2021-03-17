using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class SteeringBehaviors
    {
        public float PanicDistance = 5; //恐惧范围

        public Vehicle CurVehicle { get; private set; }
        public Vector3 TargetPos { get; private set; }

        public enum Deceleration { slow=3, normal=2, fast=1, }
        public readonly string ObstacleMaskName = "Obstacle";
        public readonly string WallMaskName = "Wall";

        int m_iFlags; // 已开启行为的标识
        float m_memoryTime; // 记忆时间

        Vehicle m_pTargetAgent1; // 目标1
        Vehicle m_pTargetAgent2; // 目标2

        Vector3 m_wanderTarget; // 徘徊的点
        Vector3 m_hideTarget; // 隐藏的点
        Vector3 m_vOffset; // 距离领头的偏移量

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
            Vector3 desiredVelocity = (targetPos - CurVehicle.transform.position).normalized * GameWorldSettings.Instance.MaxSpeed;
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
            Vector3 desiredVelocity = (CurVehicle.transform.position - targetPos).normalized * GameWorldSettings.Instance.MaxSpeed;
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
                speed = Mathf.Clamp(speed, 1f, GameWorldSettings.Instance.MaxSpeed);

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
            float lookAheadTime = toVehicle.magnitude / (GameWorldSettings.Instance.MaxSpeed);

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

            float lookAheadTime = toVehicle.magnitude / (GameWorldSettings.Instance.MaxSpeed);

            return Flee(vehicle.transform.position + vehicle.Velocity * lookAheadTime);
        }

        // 徘徊
        public Vector3 Wander()
        {
            float jitterTimeSlice = CurVehicle.WanderJitter * Time.fixedDeltaTime;
            m_wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * jitterTimeSlice, 0, Random.Range(-1.0f, 1.0f) * jitterTimeSlice);
            // 重新映射到圆上
            m_wanderTarget = m_wanderTarget.normalized * CurVehicle.WanderRadius;
            // 加上与智能体的距离
            Vector3 targetLocal = m_wanderTarget + new Vector3(0, 0, CurVehicle.WanderDistance);

            //Vector3 targetWorld = CurVehicle.transform.TransformPoint(targetLocal);
            Vector3 targetWorld = AIUtils.TransformPointUnscaled(CurVehicle.transform, targetLocal);

            return (targetWorld - CurVehicle.transform.position) / (CurVehicle.WanderDistance + CurVehicle.WanderRadius);
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
            float timeToReachMidPoint = (midPoint - CurVehicle.transform.position).magnitude / GameWorldSettings.Instance.MaxSpeed;

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
        public Vector3 FollowPath()
        {
            PathCreation.PathCreator pathCreator = CurVehicle.gameWorld.pathCreator;
            if (pathCreator != null)
            {
                Vector3 closetPathPoint = pathCreator.path.GetClosestPointOnPath(CurVehicle.transform.position + CurVehicle.transform.forward);
                Vector3 lastPointPos = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
                bool isFinish = false;
                if (!pathCreator.path.isClosedLoop && Vector3.Distance(CurVehicle.transform.position, lastPointPos) < 5f)
                {
                    isFinish = true;
                    closetPathPoint = lastPointPos;
                    TargetPos = closetPathPoint;
                }
                if (isFinish)
                {
                    return Arrive(closetPathPoint, Deceleration.fast);
                }
                else
                {
                    return Seek(closetPathPoint);
                }
            }

            return Vector3.zero;
        }

        // 按照偏移跟随领头
        public Vector3 OffsetPursuit(Vehicle leader, Vector3 offset)
        {
            // 计算偏移的位置
            Vector3 offsetWorldPos = AIUtils.InverseTransformPointUnscaled(CurVehicle.transform, offset);

            // 预测前进的时间
            float lookAheadTime = Vector3.Distance(leader.transform.position, offsetWorldPos) / (leader.Velocity.magnitude + GameWorldSettings.Instance.MaxSpeed);

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

        // 队列： 朝向保持和邻居一致
        public Vector3 Alignment(Vehicle[] neighbors)
        {
            Vector3 averageForward = Vector3.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                averageForward += neighbors[i].transform.forward;
            }

            // 计算出朝向平均值
            if (neighbors.Length > 0)
            {
                averageForward /= neighbors.Length;

                averageForward -= CurVehicle.transform.forward;
            }
            return averageForward;
        }

        // 聚集： 移向邻居得质点
        public Vector3 Cohesion(Vehicle[] neighbors)
        {
            Vector3 averageCenterOfMass, force;
            averageCenterOfMass = force = Vector3.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                averageCenterOfMass += neighbors[i].transform.position;
            }

            // 计算出质点得平均值
            if (neighbors.Length > 0)
            {
                averageCenterOfMass /= neighbors.Length;
                force = Seek(averageCenterOfMass);
            }

            return force;
        }
        #endregion

        public Vector3 Calculate()
        {
            return CalculatePrioritized() * GameWorldSettings.Instance.ForceMultiper;
        }

        /// <summary>
        /// 带优先级的加权截断累计
        /// 在计算行为会根据优先级的高低来优先进行加权运算
        /// </summary>
        /// <returns>Force</returns>
        public Vector3 CalculatePrioritized()
        {
            Vector3 force = Vector3.zero;
            Vector3 steeringFore = Vector3.zero;
            Transform targetPicker = CurVehicle.gameWorld.TargetPicker;

            if (On(BehaviorType.WallAvoidance))
            {
                force = WallAvoidance() * GameWorldSettings.Instance.WallAvoidanceWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.ObstacleAvoidance))
            {
                force = ObstacleAvoidance() * GameWorldSettings.Instance.ObstacleAvoidanceWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Evade) && m_pTargetAgent1!=null)
            {
                force = Evade(m_pTargetAgent1) * GameWorldSettings.Instance.EvadeWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Flee) && targetPicker!=null)
            {
                force = Flee(targetPicker.position) * GameWorldSettings.Instance.FleeWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Separation))
            {
                force = Separation(FindNeighbors()) * GameWorldSettings.Instance.SeparationWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Allignment))
            {
                force = Alignment(FindNeighbors()) * GameWorldSettings.Instance.AlignmentWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Cohesion))
            {
                force = Cohesion(FindNeighbors()) * GameWorldSettings.Instance.CohesionWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Seek) && targetPicker != null)
            {
                force = Seek(targetPicker.position) * GameWorldSettings.Instance.SeekWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Arrive) && targetPicker != null)
            {
                force = Arrive(targetPicker.position) * GameWorldSettings.Instance.ArriveWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Wander))
            {
                force = Wander() * GameWorldSettings.Instance.WanderWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Pursuit) && m_pTargetAgent1!=null)
            {
                force = Pursuit(m_pTargetAgent1) * GameWorldSettings.Instance.PursuitWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.OffsetPursuit) && m_pTargetAgent1!=null)
            {
                force = OffsetPursuit(m_pTargetAgent1, m_vOffset);
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Interpose) && m_pTargetAgent1!=null && m_pTargetAgent2!=null)
            {
                force = Interpose(m_pTargetAgent1, m_pTargetAgent2) * GameWorldSettings.Instance.InterposeWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Hide) && m_pTargetAgent1!=null)
            {
                force = Hide(m_pTargetAgent1) * GameWorldSettings.Instance.HideWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            if (On(BehaviorType.Hide))
            {
                force = FollowPath() * GameWorldSettings.Instance.FollowPathWeight;
                if (!AccumulateForce(ref steeringFore, force)) return steeringFore;
            }

            return steeringFore;
        }

        Vehicle[] FindNeighbors()
        {
            List<Vehicle> neighbors = new List<Vehicle>();
            List<Transform> visibleTarget = CurVehicle.fieldOfView.VisibleTargets;
            for (int i = 0; i < visibleTarget.Count; i++)
            {
                neighbors.Add(visibleTarget[i].GetComponent<Vehicle>());
            }

            return neighbors.ToArray();
        }

        /// <summary>
        /// 计算剩余的牵引力
        /// </summary>
        /// <param name="runingTot">当前运行的牵引力</param>
        /// <param name="forceToAdd">需要附加的力</param>
        /// <returns></returns>
        bool AccumulateForce(ref Vector3 runingTot, Vector3 forceToAdd)
        {
            // remaind force
            float remaindForce = GameWorldSettings.Instance.MaxForce - runingTot.magnitude;
            if (remaindForce < 0) return false;

            float forceToAddMagnitude = forceToAdd.magnitude;

            if (forceToAddMagnitude < remaindForce)
            {
                runingTot += forceToAdd;
            }
            else
            {
                runingTot += remaindForce * forceToAdd.normalized;
            }

            return true;
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

        public bool On(BehaviorType type) { return (m_iFlags & (int)type) == (int)type; }
        // 开启行为
        public void FleeOn() { m_iFlags |= (int)BehaviorType.Flee; }
        public void SeekOn() { m_iFlags |= (int)BehaviorType.Seek; }
        public void ArriveOn() { m_iFlags |= (int)BehaviorType.Arrive; }
        public void WanderOn() { m_iFlags |= (int)BehaviorType.Wander; }
        public void PursuitOn(Vehicle v) { m_iFlags |= (int)BehaviorType.Pursuit; m_pTargetAgent1 = v; }
        public void EvadeOn(Vehicle v) { m_iFlags |= (int)BehaviorType.Evade; m_pTargetAgent1 = v; }
        public void CohesionOn() { m_iFlags |= (int)BehaviorType.Cohesion; }
        public void SeparationOn() { m_iFlags |= (int)BehaviorType.Separation; }
        public void AligmentOn() { m_iFlags |= (int)BehaviorType.Allignment; }
        public void ObstacleAvoidanceOn() { m_iFlags |= (int)BehaviorType.ObstacleAvoidance; }
        public void WallAvoidanceOn() { m_iFlags |= (int)BehaviorType.WallAvoidance; }
        public void FollowOn() { m_iFlags |= (int)BehaviorType.FollowPath; }
        public void InterposeOn(Vehicle v1, Vehicle v2) { m_iFlags |= (int)BehaviorType.Interpose; m_pTargetAgent1 = v1; m_pTargetAgent2 = v2; }
        public void HideOn(Vehicle v) { m_iFlags |= (int)BehaviorType.Hide; m_pTargetAgent1 = v; }
        public void OffsetPursuitOn(Vehicle v, Vector3 offset) { m_iFlags |= (int)BehaviorType.OffsetPursuit; m_pTargetAgent1 = v; m_vOffset = offset; }
        public void FlockingOn() { CohesionOn();SeparationOn();AligmentOn();WanderOn(); }

        // 关闭行为
        public void FleeOff() { if (On(BehaviorType.Flee)) m_iFlags ^= (int)BehaviorType.Flee; }
        public void SeekOff() { if (On(BehaviorType.Seek)) m_iFlags ^= (int)BehaviorType.Seek; }
        public void ArriveOff() { if (On(BehaviorType.Arrive)) m_iFlags ^= (int)BehaviorType.Arrive; }
        public void WanderOff() { if (On(BehaviorType.Wander)) m_iFlags ^= (int)BehaviorType.Wander; }
        public void PursuitOff() { if (On(BehaviorType.Pursuit)) m_iFlags ^= (int)BehaviorType.Pursuit; }
        public void EvadeOff() { if (On(BehaviorType.Evade)) m_iFlags ^= (int)BehaviorType.Evade; }
        public void CohesionOff() { if (On(BehaviorType.Cohesion)) m_iFlags ^= (int)BehaviorType.Cohesion; }
        public void SeparationOff() { if (On(BehaviorType.Separation)) m_iFlags ^= (int)BehaviorType.Separation; }
        public void AligmentOff() { if (On(BehaviorType.Allignment)) m_iFlags ^= (int)BehaviorType.Allignment; }
        public void ObstacleAvoidanceOff() { if (On(BehaviorType.ObstacleAvoidance)) m_iFlags ^= (int)BehaviorType.ObstacleAvoidance; }
        public void WallAvoidanceOff() { if (On(BehaviorType.WallAvoidance)) m_iFlags ^= (int)BehaviorType.WallAvoidance; }
        public void FollowOff() { if (On(BehaviorType.FollowPath)) m_iFlags ^= (int)BehaviorType.FollowPath; }
        public void InterposeOff() { if (On(BehaviorType.Interpose)) m_iFlags ^= (int)BehaviorType.Interpose; }
        public void HideOff() { if (On(BehaviorType.Hide)) m_iFlags ^= (int)BehaviorType.Hide; }
        public void OffsetPursuitOff() { if (On(BehaviorType.OffsetPursuit)) m_iFlags ^= (int)BehaviorType.OffsetPursuit; }
        public void FlockingOff() { CohesionOff();SeparationOff();AligmentOff();WanderOff(); }

        
    }
}
