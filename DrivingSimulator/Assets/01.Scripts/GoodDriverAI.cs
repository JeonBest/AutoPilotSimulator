using System.Collections;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    public class GoodDriverAI : InputProvider
    {
        [Header("Common")]
        public VehicleController myVehicle;
        public Color lineColor;

        [Header("Sensors")]
        public Transform frontSensor;

        private Sensoring FSensor;

        [Header("AI settings")]
        public float firstMinPivotDis;
        public float steeringCoefficient;
        public float targetSpeedDiff;

        [Header("Only for Read")]
        public float steeringValue;
        public float minPivotDis;
        private float targetSpeed;
        public float targetSpeedKPH;
        public float acceler;
        public float speedKPH;

        VehicleController FrontCar;
        GuidePivotManager.GuidePivot currentPivot;
        private bool isReady = false;
        private bool isDamaged = false;

        // From CruiseControlModule.cs
        private float _e;
        private float _ed;
        private float _ei;
        private float _eprev;

        private float output;
        // private float targetSpeed;
        private float prevTargetSpeed;

        public void Init(GuidePivotManager guidePivotManager)
        {
            myVehicle.input.autoSetInput = false;

            targetSpeed = 0f;
            minPivotDis = firstMinPivotDis;

            FSensor = frontSensor.GetComponent<Sensoring>();

            SetStartGP(guidePivotManager);
            InvokeRepeating(nameof(speedChange), 10.0f, 10.0f);
        }

        void SetStartGP(GuidePivotManager guidePivotManager)
        {
            /* 자신과 가장 가까운 GuidePivot 찾기 */
            float minimunDis = 1000f;
            foreach (GuidePivotManager.GuidePivot gp in guidePivotManager.guideLine)
            {
                float distance = (myVehicle.vehicleTransform.position - gp.cur.position).sqrMagnitude;
                if (minimunDis > distance)
                {
                    minimunDis = distance;
                    currentPivot = gp;
                }
            }
            // myVehicle.input.TrailerAttachDetach = true;
            isReady = true;
        }

        void FixedUpdate()
        {
            if (!isReady)
                return;
            if (!isDamaged && myVehicle.damageHandler.lastCollision != null)
            {
                isDamaged = true;
                myVehicle.input.Vertical = 0f;
                myVehicle.input.Handbrake = 1f;
                Invoke("terminateMode", 10f);
            }
            if (isDamaged)
            {
                return;
            }

            /* 속도에 따라 minPivotDis 조절 */
            if (myVehicle.LocalForwardVelocity > 24)
                minPivotDis = myVehicle.LocalForwardVelocity * 0.8f;
            else if (myVehicle.LocalForwardVelocity > 18)
                minPivotDis = 12;
            else
                minPivotDis = firstMinPivotDis;

            /* currentPivot 관리 */
            // currentPivot과의 거리가 minPivotDis보다 작아지면, next로 갱신
            if (Vector3.Distance(currentPivot.cur.position, myVehicle.vehicleTransform.position) < minPivotDis)
            {
                if (currentPivot.next != null)
                {
                    currentPivot = currentPivot.next;
                }
                else
                {
                    // next 가 없으면, left나 right 중 존재하는 쪽을 다음으로 갱신
                    if (currentPivot.right != null)
                        currentPivot = currentPivot.right;
                    else if (currentPivot.left != null)
                        currentPivot = currentPivot.left;
                    else
                        Debug.LogWarning("no next Pivot!");
                }
            }

            if (FSensor.hitCount != 0)
            {
                Debug.Log("Some Vehicle is blocking me!");
                myVehicle.input.Brakes = 0.4f;
            }
            else
            {
                /* 속도 조절 */
                targetSpeed = Mathf.Lerp(targetSpeed, currentPivot.speedLimit / 3.6f, myVehicle.fixedDeltaTime * 0.2f);
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
            Vector3 relativeVector = myVehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = Mathf.Lerp(steeringValue, relativeVector.x / relativeVector.magnitude * steeringCoefficient, myVehicle.fixedDeltaTime * 10.0f);
            myVehicle.input.Steering = steeringValue;

            acceler = myVehicle.LocalForwardAcceleration;
            speedKPH = myVehicle.LocalForwardVelocity * 3.6f;
        }

        private void speedChange()
        {
            targetSpeedDiff = Random.Range(-10.0f, 10.0f);
        }

        /* <크루즈 컨트롤>
         * CruiseControlModule.cs의 소스코드를 인용한 속도조절 함수
         */
        private void CruiseMode(float _targetSpeed)
        {
            float speed = myVehicle.Speed;
            float dt = myVehicle.fixedDeltaTime;

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

            myVehicle.input.Vertical = output;

            prevTargetSpeed = _targetSpeed;
        }

        /* < 운행 정지 >
         * 사고 발생 등의 이유로 차량을 멈출 때 사용
         */
        private void terminateMode()
        {
            myVehicle.input.Vertical = 0;
            myVehicle.input.Steering = 0;
            myVehicle.gameObject.SetActive(false);
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
            if (!isReady)
                return;
            Gizmos.color = lineColor;
            Gizmos.DrawSphere(currentPivot.cur.position, 1f);
            Gizmos.DrawLine(myVehicle.vehicleTransform.position, currentPivot.cur.position);
        }
    }
}
