using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NWH.Common.Input;
namespace NWH.VehiclePhysics2.Input
{
    public class Carmanager : MonoBehaviour
    {
        public SpawnManager sm;
        public LinkedList<GameObject> goodVehicles = new LinkedList<GameObject>();
        public LinkedList<GameObject> badVehicles = new LinkedList<GameObject>();
        public GameObject[] goods;
        public GameObject[] bads;
        public GameObject player;
        public float interval;
        public float delDist;
        // Start is called before the first frame update
        void Start()
        {
            for(int i = 0; i < sm.goodNum; i++)
            {
                goodVehicles.AddLast(goods[i]);
            }
            for (int i = 0; i < sm.badNum; i++)
            {
                badVehicles.AddLast(bads[i]);
            }
            StartCoroutine("CarDel");
        }
        IEnumerator CarDel()
        {
            yield return new WaitForSeconds(interval);
            float maxd = -1f;
            int idx = -1;
            bool isGood = false;
            int tmpIdx = 0;
            GameObject delCar = null;
            foreach(GameObject item in goodVehicles)
            {
                float dist = Vector3.Distance(item.transform.position, player.transform.position);
                if(dist > maxd)
                {
                    idx = tmpIdx;
                    maxd = dist;
                    isGood = true;
                    delCar = item;
                }
                tmpIdx++;
            }
            tmpIdx = 0;
            foreach (GameObject item in badVehicles)
            {
                float dist = Vector3.Distance(item.transform.position, player.transform.position);
                if (dist > maxd)
                {
                    idx = tmpIdx;
                    maxd = dist;
                    isGood = false;
                    delCar = item;
                }
                tmpIdx++;
            }
            if(maxd >= delDist)
            {
                if(isGood)
                {
                    goodVehicles.Remove(delCar);
                    sm.goodNum--;
                }
                else
                {
                    badVehicles.Remove(delCar);
                    sm.badNum--;
                }
                Destroy(delCar);
            }
            yield return new WaitForSeconds(10f);
            StartCoroutine("CarDel");
        }
    }
}
