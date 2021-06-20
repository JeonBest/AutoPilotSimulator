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

            /* �ڽŰ� ���� ����� GuidePivot ã�� */
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

            /* �ӵ��� ���� minPivotDis ���� */
            if (myvehicle.LocalForwardVelocity > 24)
                minPivotDis = myvehicle.LocalForwardVelocity * 0.8f;
            else if (myvehicle.LocalForwardVelocity > 18)
                minPivotDis = 12;
            else
                minPivotDis = firstMinPivotDis;

            /* currentPivot���� �Ÿ��� minPivotDis���� �۾�����, next�� ���� */
            if (Vector3.Distance(currentPivot.cur.position, myvehicle.vehicleTransform.position) < minPivotDis)
                currentPivot = currentPivot.next;

            /* ������ ������ �ֺ� �� ������ Bad behavior ���� */

            if (Time.time - actTime > 2.0f)
                isActing = false;

            // ���� Įġ��
            if (!isActing && LBSensor.hitCount != 0 && LSSensor.hitCount == 0 && currentPivot.left != null)
            {
                Debug.Log("�������� Įġ��!");
                currentPivot = currentPivot.left;
                isActing = true;
                actTime = Time.time;
            }

            // ���� Įġ��
            if (!isActing && RBSensor.hitCount != 0 && RSSensor.hitCount == 0 && currentPivot.right != null)
            {
                Debug.Log("�������� Įġ��!");
                currentPivot = currentPivot.right;
                isActing = true;
                actTime = Time.time;
            }

            // ���� ���� ȸ��
            if (FSensor.hitCount != 0)
            {
                if (!isActing)
                {
                    // ������ Ȯ��
                    if (LSSensor.hitCount == 0 && currentPivot.left != null)
                    {
                        Debug.Log("���濡 �����߰�! �������� ȸ��!");
                        currentPivot = currentPivot.left;
                        isActing = true;
                        actTime = Time.time;
                    }
                    // ������ Ȯ��
                    else if (!isActing && RSSensor.hitCount == 0 && currentPivot.right != null)
                    {
                        Debug.Log("���濡 ���� �߰�! �������� ȸ��!");
                        currentPivot = currentPivot.right;
                        isActing = true;
                        actTime = Time.time;
                    }
                }
                // Įġ�� �ߴµ� �տ� ���� ������, �׳� ��
                else
                {
                    // Debug.Log("Įġ�� �ߴµ� ���濡 ���� �߰�!");
                    myvehicle.input.Brakes = 0.2f;
                }
                    
            }
            else
            {
                /* �ӵ� ���� */
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

            /* current pivot�� �ٶ󺸵��� �ڵ�����, �����߾����� */
            Vector3 relativeVector = myvehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = Mathf.Lerp(steeringValue, relativeVector.x / relativeVector.magnitude * steeringCoefficient, myvehicle.fixedDeltaTime * 10.0f);
            myvehicle.input.Steering = steeringValue;

            acceler = myvehicle.LocalForwardAcceleration;
            speedKPH = myvehicle.LocalForwardVelocity * 3.6f;
        }

        /* <ũ���� ��Ʈ��>
         * CruiseControlModule.cs�� �ҽ��ڵ带 �ο��� �ӵ����� �Լ�
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

        /* < ������ ���� �ν� >
         * FrontSensor�� �̿��� ������ ������ �����Ͽ� ��ȯ�Ѵ�.
         */

        /* < ���� ���� ���� �ൿ ���� >
         * ���� �������� �Ÿ�, �ӵ����� ����Ͽ� ȸ��, ������ �����Ѵ�
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
