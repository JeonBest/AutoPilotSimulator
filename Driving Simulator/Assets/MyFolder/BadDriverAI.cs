using System.Collections;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    public class BadDriverAI : InputProvider
    {
        [Header("Common")]
        public VehicleController myvehicle;
        public Transform Track;
        public Color lineColor;

        [Header("Sensors")]
        public Transform frontSensor;
        public Transform leftSideSensor;
        public Transform rightSideSensor;
        public Transform leftBackSensor;
        public Transform rightBackSensor;

        private Sensoring FSensor;
        private Sensoring LSSensor;
        private Sensoring RSSensor;
        private Sensoring LBSensor;
        private Sensoring RBSensor;

        [Header("AI settings")]
        public float firstMinPivotDis;
        public float steeringCoefficient;
        public float targetSpeedDiff;
        public float delayTime;

        [Header("Only for Read")]
        public float steeringValue;
        public float minPivotDis;
        public float targetSpeedKPH;
        public float acceler;
        public float speedKPH;

        GuidePivotManager.GuidePivot currentPivot;
        GuidePivotManager GPM;
        private bool isEngineStart = false;

        // From CruiseControlModule.cs
        private float _e;
        private float _ed;
        private float _ei;
        private float _eprev;
        private float output;

        private float targetSpeed;
        private float prevTargetSpeed;

        private bool isActing = false;
        private float actTime;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = false;

            targetSpeed = 0f;
            minPivotDis = firstMinPivotDis;

            FSensor = frontSensor.GetComponent<Sensoring>();
            LSSensor = leftSideSensor.GetComponent<Sensoring>();
            RSSensor = rightSideSensor.GetComponent<Sensoring>();
            LBSensor = leftBackSensor.GetComponent<Sensoring>();
            RBSensor = rightBackSensor.GetComponent<Sensoring>();

            actTime = delayTime;

            Invoke("RaceStart", delayTime);
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

            /* 센서로 감지한 주변 차 정보로 Bad behavior 동작 */

            if (Time.time - actTime > 2.0f)
                isActing = false;

            // 좌측 칼치기
            if (!isActing && LBSensor.hitCount != 0 && LSSensor.hitCount == 0 && currentPivot.left != null)
            {
                Debug.Log("좌측차량 칼치기!");
                currentPivot = currentPivot.left;
                isActing = true;
                actTime = Time.time;
            }

            // 우측 칼치기
            if (!isActing && RBSensor.hitCount != 0 && RSSensor.hitCount == 0 && currentPivot.right != null)
            {
                Debug.Log("우측차량 칼치기!");
                currentPivot = currentPivot.right;
                isActing = true;
                actTime = Time.time;
            }

            // 전방 차량 회피
            if (FSensor.hitCount != 0)
            {
                if (!isActing)
                {
                    // 좌측을 확인
                    if (LSSensor.hitCount == 0 && currentPivot.left != null)
                    {
                        Debug.Log("전방에 차량발견! 좌측으로 회피!");
                        currentPivot = currentPivot.left;
                        isActing = true;
                        actTime = Time.time;
                    }
                    // 우측을 확인
                    else if (!isActing && RSSensor.hitCount == 0 && currentPivot.right != null)
                    {
                        Debug.Log("전방에 차량 발견! 우측으로 회피!");
                        currentPivot = currentPivot.right;
                        isActing = true;
                        actTime = Time.time;
                    }
                }
                // 칼치기 했는데 앞에 차가 있으면, 그냥 감
                else
                {
                    // Debug.Log("칼치기 했는데 전방에 차량 발견!");
                    myvehicle.input.Brakes = 0.2f;
                }
                    
            }
            else
            {
                /* 속도 조절 */
                targetSpeed = Mathf.Lerp(targetSpeed, currentPivot.speedLimit / 3.6f, myvehicle.fixedDeltaTime * 0.2f);
                if (targetSpeed > -targetSpeedDiff / 3.6f && targetSpeed > 70f / 3.6f)
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

            /* current pivot을 바라보도록 핸들조작, 차로중앙유지 */
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
         * FrontSensor를 이용해 전방의 차량을 감지하여 반환한다.
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
