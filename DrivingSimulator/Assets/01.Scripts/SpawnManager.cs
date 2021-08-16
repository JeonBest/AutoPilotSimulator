using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWH.Common.Input;
namespace NWH.VehiclePhysics2.Input
{
    public class SpawnManager : MonoBehaviour
    {
        public GameObject good;
        public GameObject bad;
        public GameObject player;
        public Transform[] points;
        public Transform track;
        public int idx;
        public int goodNum;
        public int badNum;
        int maxGoodNum;
        int maxBadNum;
        float[] angles;
        public Carmanager cm;

        void Start()
        {
            idx = 0;
            maxBadNum = badNum;
            maxGoodNum = goodNum;
        }

        void FixedUpdate()
        {
            if (goodNum < maxGoodNum)
            {
                goodNum++;
                Invoke("CreateGood", 1.0f);

            }
            if (badNum < maxBadNum)
            {
                badNum++;
                Invoke("CreateBad", 2.0f);

            }
        }

        void CreateGood()
        {
            GameObject tmpGood = Instantiate(good, points[idx].position, points[idx].rotation);
            VehicleController vc = tmpGood.GetComponent<VehicleController>();
            GoodDriverAI tmpAI = tmpGood.GetComponentInChildren<GoodDriverAI>();
            Sensoring sensor = vc.GetComponentInChildren<Sensoring>();
            tmpAI.myvehicle = vc;
            tmpAI.frontSensor = sensor.transform;
            tmpAI.Track = track;
            tmpAI.firstMinPivotDis = 6;
            tmpAI.steeringCoefficient = 2.5f;
            tmpAI.targetSpeedDiff = 2;
            cm.goodVehicles.AddLast(tmpGood);
        }

        void CreateBad()
        {
            GameObject tmpBad = Instantiate(bad, points[idx].position, points[idx].rotation);
            BadDriverAI tmpAI = tmpBad.GetComponentInChildren<BadDriverAI>();
            VehicleController vc = tmpBad.GetComponent<VehicleController>();
            tmpAI.myvehicle = vc;
            tmpAI.Track = track;
            Sensoring[] sensors = tmpAI.GetComponentsInChildren<Sensoring>();
            tmpAI.frontSensor = sensors[0].transform;
            tmpAI.leftSideSensor = sensors[1].transform;
            tmpAI.rightSideSensor = sensors[2].transform;
            tmpAI.leftBackSensor = sensors[3].transform;
            tmpAI.rightBackSensor = sensors[4].transform;
            tmpAI.firstMinPivotDis = 8;
            tmpAI.steeringCoefficient = 3;
            tmpAI.targetSpeedDiff = 10;
            tmpAI.delayTime = 2.0f;
            cm.badVehicles.AddLast(tmpBad);
        }
    }
}
