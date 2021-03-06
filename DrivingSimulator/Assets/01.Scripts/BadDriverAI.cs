using System.Collections;
using System.Collections.Generic;
using NWH.Common.Input;
using UnityEngine;
using Zenject;

namespace NWH.VehiclePhysics2.Input
{
    public class BadDriverAI : InputProvider
    {
        [Header("Common")]
        public VehicleController myVehicle;
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
        public int laneNum;

        Rigidbody myRigidbody;
        GuidePivotManager.GuidePivot currentPivot;
        private bool isReady = false;
        private bool isDamaged = false;

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

        CarMover _carMover;

        public void Init(GuidePivotManager guidePivotManager, CarMover carMover)
        {
            myVehicle.input.autoSetInput = false;

            targetSpeed = 0f;
            minPivotDis = firstMinPivotDis;

            FSensor = frontSensor.GetComponent<Sensoring>();
            LSSensor = leftSideSensor.GetComponent<Sensoring>();
            RSSensor = rightSideSensor.GetComponent<Sensoring>();
            LBSensor = leftBackSensor.GetComponent<Sensoring>();
            RBSensor = rightBackSensor.GetComponent<Sensoring>();
            myRigidbody = GetComponent<Rigidbody>();

            actTime = delayTime;

            SetStartGP(guidePivotManager);
            _carMover = carMover;
        }

        void SetStartGP(GuidePivotManager guidePivotManager)
        {
            /* ???? ???? ????? GuidePivot ??? */
            float minimunDis = 1000f;
            foreach (var gp in guidePivotManager.guideLine)
            {
                float distance = (myVehicle.vehicleTransform.position - gp.cur.position).sqrMagnitude;
                if (minimunDis > distance)
                {
                    minimunDis = distance;
                    currentPivot = gp;
                }
            }

            // myvehicle.input.TrailerAttachDetach = true;
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
                Invoke("terminateMode", 2f);
                //_carMover.AddDamagedCar(false);
            }
            if (isDamaged)
            {
                return;
            }

            /* ????? ???? minPivotDis ???? */
            if (myVehicle.LocalForwardVelocity > 24)
                minPivotDis = myVehicle.LocalForwardVelocity * 0.8f;
            else if (myVehicle.LocalForwardVelocity > 18)
                minPivotDis = 12;
            else
                minPivotDis = firstMinPivotDis;

            /* currentPivot???? ????? minPivotDis???? ???????, next?? ???? */
            if (Vector3.Distance(currentPivot.cur.position, myVehicle.vehicleTransform.position) < minPivotDis)
                currentPivot = currentPivot.next;

            /* ?????? ?????? ??? ?? ?????? Bad behavior ???? */

            if (Time.time - actTime > 2.0f)
                isActing = false;

            // ???? ????
            if (!isActing && LBSensor.hitCount != 0 && LSSensor.hitCount == 0 && currentPivot.left != null && !LBSensor.isPlayerInvolved)
            {
                currentPivot = currentPivot.left;
                isActing = true;
                actTime = Time.time;
            }

            // ???? ????
            if (!isActing && RBSensor.hitCount != 0 && RSSensor.hitCount == 0 && currentPivot.right != null && !RBSensor.isPlayerInvolved)
            {
                currentPivot = currentPivot.right;
                isActing = true;
                actTime = Time.time;
            }

            // ???? ???? ???
            if (FSensor.hitCount != 0)
            {
                if (!isActing)
                {
                    // ?????? ???
                    if (LSSensor.hitCount == 0 && currentPivot.left != null)
                    {
                        currentPivot = currentPivot.left;
                        isActing = true;
                        actTime = Time.time;
                    }
                    // ?????? ???
                    else if (!isActing && RSSensor.hitCount == 0 && currentPivot.right != null)
                    {
                        currentPivot = currentPivot.right;
                        isActing = true;
                        actTime = Time.time;
                    }
                }
                // ???? ???? ??? ???? ??????, ??? ??
                else
                {
                    // Debug.Log("???? ???? ??????? ???? ???!");
                    myVehicle.input.Brakes = 0.2f;
                }

            }
            else
            {
                /* ??? ???? */
                targetSpeed = Mathf.Lerp(targetSpeed, currentPivot.speedLimit / 3.6f, myVehicle.fixedDeltaTime * 0.2f);
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

            /* current pivot?? ?????? ???????, ??????????? */
            Vector3 relativeVector = myVehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = Mathf.Lerp(steeringValue, relativeVector.x / relativeVector.magnitude * steeringCoefficient, myVehicle.fixedDeltaTime * 10.0f);
            myVehicle.input.Steering = steeringValue;

            acceler = myVehicle.LocalForwardAcceleration;
            speedKPH = myVehicle.LocalForwardVelocity * 3.6f;
            laneNum = currentPivot.coordinates.x;
        }

        public void OnMoved(GuidePivotManager.GuidePivot gp)
        {
            currentPivot = gp.next;
            myRigidbody.velocity = myVehicle.transform.forward * myVehicle.vehicleRigidbody.velocity.magnitude;
        }

        /* <????? ?????>
         * CruiseControlModule.cs?? ?????? ?????????? ??????? ???
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

        /* < ???? ???? >
         * ??? ??? ???? ?????? ?????? ???? ?? ???
         */
        private void terminateMode()
        {
            myVehicle.input.Vertical = 0;
            myVehicle.input.Steering = 0;
            myVehicle.gameObject.SetActive(false);
        }

        /* < ?????? ???? ???????? >
         * FrontSensor?? ????? ?????? ?????? ??????? ??????.
         */

        /* < ???? ???? ???? ?? ???? >
         * ???? ???????? ???, ??????? ?????? ???, ?????? ???????
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
