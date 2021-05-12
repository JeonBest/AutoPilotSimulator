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
        public GameObject rayPivot;
        public GameObject FrontSensor;

        [Header ("AI settings")]
        public float targetspeed;
        public float steeringCoefficient;

        [Header ("Only for Read")]
        public float steeringValue;

        private RaycastHit hit;
        private float HitdisR;
        private float HitdisL;
        private float LaneMaxDistance;
        private int LaneMask;
        private GameObject FrontCar;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = false;
            Invoke("EngineStart", 3.0f);
            Invoke("RaceStart", 2.0f);
            HitdisR = 5f;
            HitdisL = 5f;
        }

        void EngineStart()
        {
            myvehicle.input.EngineStartStop = true;
        }

        void RaceStart()
        {
            myvehicle.input.TrailerAttachDetach = true;
        }

        // Update is called once per frame
        void Update()
        {
            // Front Sensor�� �� ���� �����Ǿ����� Ȯ��
            if (!VFS.isFree2Go)
            {
                // Sensor ������ ���� �ִٸ�, �׼��� 0
                myvehicle.input.Throttle = 0f;

                FrontCar = VFS.FrontVehicle;
                float FrontCarDistance = Vector3.Distance(myvehicle.vehicleTransform.position, FrontCar.transform.position);
                float FrontCarSpeed = FrontCar.GetComponent<Rigidbody>().velocity.magnitude;
                Debug.Log("Front Car Distance : " + FrontCarDistance);
                
                // ������ ������ �� ���� �� ������, �극��ũ
                if (myvehicle.VelocityMagnitude > FrontCarSpeed)
                {
                    // ������ ������ �Ÿ��� 10m���� �����ٸ�, ������
                    if (FrontCarDistance < 12f)
                    {
                        myvehicle.input.Handbrake = 1f;
                        myvehicle.input.Brakes = myvehicle.LocalForwardVelocity < 1f ? 0f : 1f;
                        Debug.Log("close local forward velocity : " + myvehicle.LocalForwardVelocity);
                    }
                    // 10m���� �ִٸ�, �Ÿ��� ���� �극��ŷ
                    else
                    {
                        myvehicle.input.Handbrake = 0f;
                        myvehicle.input.Brakes = myvehicle.LocalForwardVelocity < 1f ? 0f : 1f / (FrontCarDistance - 11f);
                        Debug.Log("far local forward velocity : " + myvehicle.LocalForwardVelocity);
                    }
                }
                // ������ ������ �� ���� �� ������, �������� �ۿ� ���� ������ �׼��� 0
                else
                {
                    myvehicle.input.Handbrake = 0f;
                }
            }
            else 
            {
                myvehicle.input.Brakes = 0f;
                myvehicle.input.Handbrake = 0f;
                myvehicle.input.Throttle = (targetspeed - myvehicle.VelocityMagnitude / 10f) > 0 ?
                    (targetspeed - myvehicle.VelocityMagnitude / 10f) : 0;
            }

            // ������ �������� �Ÿ� Ȯ��
            if (Physics.Raycast(rayPivot.transform.position, rayPivot.transform.right, out hit, LaneMaxDistance, LaneMask))
            {
                HitdisR = hit.distance;
                Debug.DrawRay(rayPivot.transform.position, rayPivot.transform.right * hit.distance, Color.red);
                Debug.Log("right collider name : " + hit.collider.gameObject.name);
            }
            // ���� �������� �Ÿ� Ȯ��
            if (Physics.Raycast(rayPivot.transform.position, rayPivot.transform.right * -1, out hit, LaneMaxDistance, LaneMask))
            {
                HitdisL = hit.distance;
                Debug.DrawRay(rayPivot.transform.position, rayPivot.transform.right * -hit.distance, Color.blue);
                Debug.Log("left collider name : " + hit.collider.gameObject.name);
            }
            // �������� �Ÿ��� ���� �۵��� �ڵ�����, �����߾�����
            steeringValue = (HitdisR - HitdisL) / steeringCoefficient;
            myvehicle.input.Steering = steeringValue;
        }
    }
}
