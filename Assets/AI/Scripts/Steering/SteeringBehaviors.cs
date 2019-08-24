using System;
using System.Collections.Generic;
using UnityEngine;

namespace Steering
{
    public class SteeringBehaviors
    {
        public Vehicle CurVehicle { get; private set; }

        public SteeringBehaviors(Vehicle vehicle)
        {
            CurVehicle = vehicle;
        }

        public Vector3 Seek(Vector3 targetPos)
        {
            Vector3 desiredVelocity = (targetPos - CurVehicle.transform.position).normalized;
            return (desiredVelocity - CurVehicle.Velocity);
        }

        public Vector3 Calculate()
        {
            Transform target = CurVehicle.gameWorld.TargetPicker;
            Vector3 targetPos = CurVehicle.transform.position;
            if (target != null)
            {
                targetPos = target.position;
            }
            return Seek(targetPos);
        }
    }
}
