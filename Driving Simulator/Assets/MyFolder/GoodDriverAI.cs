using System.Collections;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    public class GoodDriverAI : InputProvider
    {
        [Header ("Common")]
        public VehicleController myvehicle;
        public GameObject Track;
        public Color lineColor;

        [Header ("AI settings")]
        public float targetspeed;
        public float firstMinPivotDis;
        public float steeringCoefficient;

        [Header ("Only for Read")]
        public float steeringValue;
        public float minPivotDis;

        VehicleController FrontCar;
        GuidePivotManager.GuidePivot currentPivot;
        GuidePivotManager GPM;
        Modules.CruiseControl.CruiseControlModule cruiseControl;
        private bool isEngineStart = false;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = true;
            minPivotDis = firstMinPivotDis;

            Invoke("EngineStart", 4.0f);
            Invoke("RaceStart", 2.0f);
        }

        void RaceStart()
        {
            cruiseControl = myvehicle.moduleManager.GetModule<Modules.CruiseControl.CruiseControlModule>();

            /* 자신과 가장 가까운 GuidePivot 찾기 */
            GPM = Track.GetComponent<GuidePivotManager>();
            float minimunDis = 100f;
            foreach (GuidePivotManager.GuidePivot gp in GPM.guideLine)
            {
                float distance = (myvehicle.vehicleTransform.position - gp.cur.position).sqrMagnitude;
                if (minimunDis > distance)
                {
                    minimunDis = distance;
                    currentPivot = gp;
                }
            }

            // myvehicle.input.TrailerAttachDetach = true;

            cruiseControl.setTargetSpeedOnEnable = false;
            cruiseControl.targetSpeed = targetspeed;
            
            
        }

        void EngineStart()
        {
            cruiseControl.Enable();
            isEngineStart = true;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!isEngineStart)
                return;

            /* 속도에 따라 minPivotDis 조절 */
            if (myvehicle.LocalForwardVelocity > 24)
                minPivotDis = myvehicle.LocalForwardVelocity;
            else if (myvehicle.LocalForwardVelocity > 18)
                minPivotDis = 12;
            else
                minPivotDis = firstMinPivotDis;

            /* currentPivot과의 거리가 minPivotDis보다 작아지면, next로 갱신 */
            if (Vector3.Distance(currentPivot.cur.position, myvehicle.vehicleTransform.position) < minPivotDis)
                currentPivot = currentPivot.next;

            /* 속도 조절 */
            cruiseControl.targetSpeed = targetspeed;

            // 차선간의 거리의 차가 작도록 핸들조작, 차로중앙유지
            Vector3 relativeVector = myvehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = relativeVector.x / relativeVector.magnitude;
            myvehicle.input.Steering = steeringValue * steeringCoefficient;

        }

        private void OnDrawGizmos()
        {
            if (!isEngineStart)
                return;
            Gizmos.color = lineColor;
            Gizmos.DrawSphere(currentPivot.cur.position, 1f);
            Gizmos.DrawLine(myvehicle.vehicleTransform.position, currentPivot.cur.position);
        }
    }
}
