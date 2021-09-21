using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Input;
using UnityEngine;
using Zenject;

public class CarMover : MonoBehaviour
{
    [SerializeField]
    private VehicleController player;
    [SerializeField]
    private GameObject GoodCarPrefab;
    [SerializeField]
    private GameObject BadCarPrefab;
    [SerializeField]
    private List<GoodDriverAI> goodDrivers = new List<GoodDriverAI>();
    [SerializeField]
    private List<BadDriverAI> badDrivers = new List<BadDriverAI>();
    [SerializeField]
    private List<GuidePivotManager.GuidePivot> backSpawn = new List<GuidePivotManager.GuidePivot>();
    [SerializeField]
    private List<GuidePivotManager.GuidePivot> frontSpawn = new List<GuidePivotManager.GuidePivot>();

    public float visibleDistance;
    public float distance2ground;

    [SerializeField]
    private List<GameObject> frontPointSpheres;
    [SerializeField]
    private List<GameObject> backPointSpheres;
    private bool[] spawnPool = { false, false, false, false };

    [Inject]
    GuidePivotManager _guidePivotManager;

    [Inject]
    void Injected()
    {
        InitSpawnPivot();

        StartCoroutine(SpawnUpdateCoroutine());
        StartCoroutine(MoveCar());
    }

    public void Init(List<GoodDriverAI> goodDriverAIs, List<BadDriverAI> badDriverAIs)
    {
        goodDrivers.AddRange(goodDriverAIs);
        badDrivers.AddRange(badDriverAIs);
    }

    private void SetSpheres()
    {
        List<GameObject> tmp = GetComponentsInChildren<GameObject>()
            .Where(obj => obj.CompareTag("EnterExitPoint") && obj.name.Contains("Front"))
            .ToList();
        frontPointSpheres.AddRange(tmp);
        tmp = GetComponentsInChildren<GameObject>()
            .Where(obj => obj.CompareTag("EnterExitPoint") && obj.name.Contains("Back"))
            .ToList();
        backPointSpheres.AddRange(tmp);
    }

    private void InitSpawnPivot()
    {
        /* Searching Closest Pivot from Sphere */
        foreach (GameObject obj in frontPointSpheres)
        {
            float minDistance = 1000f;
            GuidePivotManager.GuidePivot tmp = null;
            foreach (var gp in _guidePivotManager.guideLine)
            {
                float distance = (obj.transform.position - gp.cur.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    tmp = gp;
                }
            }
            frontSpawn.Add(tmp);
        }
        foreach (GameObject obj in backPointSpheres)
        {
            float minDistance = 1000f;
            GuidePivotManager.GuidePivot tmp = null;
            foreach (var gp in _guidePivotManager.guideLine)
            {
                float distance = (obj.transform.position - gp.cur.position).sqrMagnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    tmp = gp;
                }
            }
            backSpawn.Add(tmp);
        }
    }

    IEnumerator SpawnUpdateCoroutine()
    {
        while (true)
        {
            for (int i = 0; i < frontSpawn.Count; i++)
            {
                var frontPoint = frontSpawn[i];
                if (Vector3.Distance(player.transform.position, frontPoint.cur.position) < visibleDistance)
                {
                    frontSpawn[i] = frontPoint.next;
                    frontPointSpheres[frontPoint.coordinates.x - 1].transform.position = frontPoint.cur.position;
                }
            }
            for (int i = 0; i < backSpawn.Count; i++)
            {
                var backPoint = backSpawn[i];
                if (Vector3.Distance(player.transform.position, backPoint.cur.position) > visibleDistance)
                {

                    backSpawn[i] = backPoint.next;
                    backPointSpheres[backPoint.coordinates.x - 1].transform.position = backPoint.cur.position;
                }

            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator MoveCar()
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                spawnPool[i] = false;
            }
            foreach (var goodDriver in goodDrivers)
            {
                if (Vector3.Distance(player.transform.position, goodDriver.transform.position) < visibleDistance)
                    continue;

                if (goodDriver.myVehicle.LocalForwardVelocity > player.LocalForwardVelocity)
                {
                    var gp = backSpawn[goodDriver.laneNum - 1];
                    spawnPool[gp.coordinates.x - 1] = true;
                    goodDriver.transform.position
                        = gp.cur.position
                        - new Vector3(0, distance2ground, 0);
                    goodDriver.transform.rotation = gp.cur.rotation;
                    goodDriver.OnMoved(gp);
                }
                else
                {
                    var gp = frontSpawn[goodDriver.laneNum - 1];
                    spawnPool[gp.coordinates.x - 1] = true;
                    goodDriver.transform.position
                        = gp.cur.position
                        - new Vector3(0, distance2ground, 0);
                    goodDriver.transform.rotation = gp.cur.rotation;
                    goodDriver.OnMoved(gp);
                }
            }

            foreach (var badDriver in badDrivers)
            {
                if (Vector3.Distance(player.transform.position, badDriver.transform.position) < visibleDistance)
                    continue;

                if (badDriver.myVehicle.LocalForwardVelocity > player.LocalForwardVelocity)
                {
                    var gp = backSpawn[badDriver.laneNum - 1];
                    spawnPool[gp.coordinates.x - 1] = true;
                    badDriver.transform.position
                        = gp.cur.position
                        - new Vector3(0, distance2ground, 0);
                    badDriver.transform.rotation = gp.cur.rotation;
                    badDriver.OnMoved(gp);
                }
                else
                {
                    var gp = frontSpawn[badDriver.laneNum - 1];
                    spawnPool[gp.coordinates.x - 1] = true;
                    badDriver.transform.position
                        = gp.cur.position
                        - new Vector3(0, distance2ground, 0);
                    badDriver.transform.rotation = gp.cur.rotation;
                    badDriver.OnMoved(gp);
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private int nextSpawnLane = 1;
    private int damagedCnt = 0;

    public void AddDamagedCar(bool isGood)
    {
        damagedCnt += 1;
        while (spawnPool[nextSpawnLane - 1])
        {
            StartCoroutine(WaitSec(0.5f));
        }
        Transform worldTrans = frontSpawn[nextSpawnLane++ % 4].next.next.next.next.next.next.next.cur;
        GameObject newObj;
        if (isGood)
        {
            newObj = Instantiate(
                GoodCarPrefab,
                worldTrans.position - new Vector3(0, distance2ground, 0),
                worldTrans.rotation,
                transform);
            newObj.name = $"Good Driver D {damagedCnt + 1}";
            GoodDriverAI newAI = newObj.GetComponent<GoodDriverAI>();
            newAI.Init(_guidePivotManager, this);
        }
        else
        {
            newObj = Instantiate(
                BadCarPrefab,
                worldTrans.position - new Vector3(0, distance2ground, 0),
                worldTrans.rotation,
                transform);
            newObj.name = $"Bad Driver D {damagedCnt + 1}";
            BadDriverAI newAI = newObj.GetComponent<BadDriverAI>();
            newAI.Init(_guidePivotManager, this);
        }
    }

    IEnumerator WaitSec(float delay)
    {
        yield return new WaitForSeconds(delay);
    }
}
