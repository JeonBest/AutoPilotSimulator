using System;
using NWH.Common.Input;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    public class BadDriverAI : InputProvider
    {
        [Header("Common")]
        public VehicleController myvehicle;
        public GameObject rayPivot;

        [Header("AI settings")]
        public float targetspeed;
        public float steeringCoefficient;
        public float LaneChange;
        public float LaneChangeTime;

        [Header("Only for Read")]
        public float steeringValue;
        public bool isChangingLane;
        public int turningRight;
        public int lane;

        private RaycastHit hit;
        private float HitdisR;
        private float HitdisL;
        private float LaneMaxDistance;
        private int LaneMask;
        private float steeringLaneChangeValue;
        private int oldLane;

        // Start is called before the first frame update
        void Start()
        {
            myvehicle.input.autoSetInput = false;
            Invoke("EngineStart", 3.0f);
            HitdisR = 5f;
            HitdisL = 5f;
            LaneMaxDistance = 5f;
            LaneMask = 1 << 8;
            isChangingLane = false;
        }

        void EngineStart()
        {
            myvehicle.input.EngineStartStop = true;
            InvokeRepeating("ChangeLane", 10, 8);
        }

        void ChangeLane()
        {
            oldLane = lane;
            int goRight = UnityEngine.Random.Range(0, 2);
            // ������ ���� ����
            if (goRight == 1 && lane != 3)
            {
                steeringLaneChangeValue = LaneChange;
                isChangingLane = true;
                turningRight = 1;
            }
            // ���� ���� ����
            else if (goRight == 0 && lane != 1)
            {
                steeringLaneChangeValue = -LaneChange;
                isChangingLane = true;
                turningRight = -1;
            }
            else
            {
                isChangingLane = false;
                turningRight = 0;
            }
            Invoke("ChangeLaneStop", LaneChangeTime);
        }

        void ChangeLaneStop()
        {
            steeringLaneChangeValue = 0f;
            isChangingLane = false;
        }

        // Update is called once per frame
        void Update()
        {
            myvehicle.input.Throttle = (targetspeed - myvehicle.VelocityMagnitude / 10f) > 0 ?
                    (targetspeed - myvehicle.VelocityMagnitude / 10f) : 0;

            // lane info from left ray, right ray
            int leftedge = -1, rightedge = -2;

            // ������ �������� �Ÿ� Ȯ��
            if (Physics.Raycast(rayPivot.transform.position, rayPivot.transform.right, out hit, LaneMaxDistance, LaneMask))
            {
                HitdisR = hit.distance;
                String name = hit.collider.gameObject.name;
                if (name == "laneEdgeTriggerL")
                    rightedge = 1;
                if (name == "laneEdgeTriggerR")
                    rightedge = 2;
                if (name == "trackEdgeTriggerR")
                    rightedge = 3;
                // Debug.Log("right edge name : " + hit.collider.gameObject.name);
                Debug.DrawRay(rayPivot.transform.position, rayPivot.transform.right * hit.distance, Color.red);
            }
            // ���� �������� �Ÿ� Ȯ��
            if (Physics.Raycast(rayPivot.transform.position, rayPivot.transform.right * -1, out hit, LaneMaxDistance, LaneMask))
            {
                HitdisL = hit.distance;
                String name = hit.collider.gameObject.name;
                if (name == "trackEdgeTriggerL")
                    leftedge = 1;
                if (name == "laneEdgeTriggerL")
                    leftedge = 2;
                if (name == "laneEdgeTriggerR")
                    leftedge = 3;
                // Debug.Log("left edge name : " + hit.collider.gameObject.name);
                Debug.DrawRay(rayPivot.transform.position, rayPivot.transform.right * -hit.distance, Color.blue);
                
            }
            // ���� ������ ���ι�ȣ
            if (leftedge == rightedge)
                lane = leftedge;

            // ���ΰ� ����Ϸ�Ǿ����� ���κ��� �����Լ� ȣ��
            if (oldLane != lane)
            {
                ChangeLaneStop();
                oldLane = lane;
            }

            // ���������߿� �ڵ������Ʈ ����
            if (!isChangingLane)
            {
                // �������� �Ÿ��� ���� �۵��� �ڵ�����, �����߾�����
                steeringValue = (HitdisR - HitdisL) * Math.Abs(HitdisR - HitdisL) / steeringCoefficient;
            }
            myvehicle.input.Steering = steeringValue + steeringLaneChangeValue;
        }
    }
}
