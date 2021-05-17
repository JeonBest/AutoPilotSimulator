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
        public Transform Track;
        public Color lineColor;

        [Header("Sensors")]
        public Transform frontSensor;

        private Sensoring FSensor;

        [Header ("AI settings")]
        public float firstMinPivotDis;
        public float steeringCoefficient;
        public float targetSpeedDiff;

        [Header ("Only for Read")]
        public float steeringValue;
        public float minPivotDis;
        private float targetSpeed;
        public float targetSpeedKPH;
        public float acceler;
        public float speedKPH;

        VehicleController FrontCar;
        GuidePivotManager.GuidePivot currentPivot;
        GuidePivotManager GPM;
        private bool isEngineStart = false;

        // From CruiseControlModule.cs
        private float _e;
        private float _ed;
        private float _ei;
        private float _eprev;

        private float output;
        // private float targetSpeed;
        private float prevTargetSpeed;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = false;

            targetSpeed = 0f;
            minPivotDis = firstMinPivotDis;

            FSensor = frontSensor.GetComponent<Sensoring>();

            Invoke("RaceStart", 0.1f);
        }

        void RaceStart()
        {
            
            /* 자신과 가장 가까운 GuidePivot 찾기 */
            GPM = Track.GetComponent<GuidePivotManager>();
            float minimunDis = 1000f;
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
            isEngineStart = true;
        }

        void FixedUpdate()
        {
            if (!isEngineStart)
                return;

            /* 속도에 따라 minPivotDis 조절 */
            if (myvehicle.LocalForwardVelocity > 24)
                minPivotDis = myvehicle.LocalForwardVelocity * 0.8f;
            else if (myvehicle.LocalForwardVelocity > 18)
                minPivotDis = 12;
            else
                minPivotDis = firstMinPivotDis;

            /* currentPivot과의 거리가 minPivotDis보다 작아지면, next로 갱신 */
            if (Vector3.Distance(currentPivot.cur.position, myvehicle.vehicleTransform.position) < minPivotDis)
                currentPivot = currentPivot.next;

            if (FSensor.hitCount != 0)
            {
                myvehicle.input.Brakes = 0.4f;
            }
            else
            {
                /* 속도 조절 */
                targetSpeed = Mathf.Lerp(targetSpeed, currentPivot.speedLimit / 3.6f, myvehicle.fixedDeltaTime * 0.2f);
                if (targetSpeed > -targetSpeedDiff / 3.6f)
                {
                    CruiseMode(targetSpeed + targetSpeedDiff / 3.6f);
                    targetSpeedKPH = (targetSpeed + targetSpeedDiff / 3.6f) * 3.6f;
                }
                else
                {
                    CruiseMode(targetSpeed);
                    targetSpeedKPH = targetSpeed * 3.6f;
                }
            }

            /* 차선간의 거리의 차가 작도록 핸들조작, 차로중앙유지 */
            Vector3 relativeVector = myvehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = Mathf.Lerp(steeringValue, relativeVector.x / relativeVector.magnitude * steeringCoefficient, myvehicle.fixedDeltaTime * 10.0f);
            myvehicle.input.Steering = steeringValue;
            
            acceler = myvehicle.LocalForwardAcceleration;
            speedKPH = myvehicle.LocalForwardVelocity * 3.6f;
        }

        /* <크루즈 컨트롤>
         * CruiseControlModule.cs의 소스코드를 인용한 속도조절 함수
         */
        private void CruiseMode(float _targetSpeed)
        {
            float speed = myvehicle.Speed;
            float dt = myvehicle.fixedDeltaTime;

            _eprev = _e;
            _e = _targetSpeed - speed;
            if (_e > -0.5f && _e < 0.5f)
            {
                _ei = 0f;
            }

            if (prevTargetSpeed != _targetSpeed)
            {
                _ei = 0f;
            }

            _ei += _e * dt;
            _ed = (_e - _eprev) / dt;
            float newOutput = _e * 0.5f + _ei * 0.25f + _ed * 0.1f;
            newOutput = newOutput < -1f ? -1f : newOutput > 1f ? 1f : newOutput;
            output = Mathf.Lerp(output, newOutput, dt * 3.0f);

            myvehicle.input.Vertical = output;

            prevTargetSpeed = _targetSpeed;
        }



        /* < 전방의 차량 인식 >
         * RayCast를 이용해 전방의 차량을 감지하여 반환한다.
         */



        /* < 전방 차에 대한 행동 결정 >
         * 전방 차량과의 거리, 속도차를 계산하여 회피, 감속을 결정한다
         * 
         */




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
