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

        [Header ("AI settings")]
        public float targetSpeedKPerH;
        public float firstMinPivotDis;
        public float steeringCoefficient;

        [Header ("Only for Read")]
        public float steeringValue;
        public float minPivotDis;

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
        private float targetSpeed;
        private float prevTargetSpeed;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = false;

            targetSpeed = targetSpeedKPerH / 3.6f;
            minPivotDis = firstMinPivotDis;

            Invoke("RaceStart", 2.0f);
        }

        void RaceStart()
        {
            
            /* �ڽŰ� ���� ����� GuidePivot ã�� */
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

            /* �ӵ� ���� */
            CruiseMode(targetSpeed);

            /* �������� �Ÿ��� ���� �۵��� �ڵ�����, �����߾����� */
            Vector3 relativeVector = myvehicle.vehicleTransform.InverseTransformPoint(currentPivot.cur.position);
            steeringValue = relativeVector.x / relativeVector.magnitude;
            myvehicle.input.Steering = steeringValue * steeringCoefficient;

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
            output = Mathf.Lerp(output, newOutput, myvehicle.fixedDeltaTime * 5f);

            myvehicle.input.Vertical = output;

            prevTargetSpeed = _targetSpeed;
        }



        /* < ������ ���� �ν� >
         * RayCast�� �̿��� ������ ������ �����Ͽ� ��ȯ�Ѵ�.
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
