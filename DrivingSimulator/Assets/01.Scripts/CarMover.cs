using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2.Input;
using UnityEngine;
using Zenject;

public class CarMover : MonoBehaviour
{
    [SerializeField]
    private Transform player;
    [SerializeField]
    private List<GoodDriverAI> goodDrivers = new List<GoodDriverAI>();
    [SerializeField]
    private List<BadDriverAI> badDrivers = new List<BadDriverAI>();
    [SerializeField]
    private List<GuidePivotManager.GuidePivot> backSpawn = new List<GuidePivotManager.GuidePivot>();
    [SerializeField]
    private List<GuidePivotManager.GuidePivot> frontSpawn = new List<GuidePivotManager.GuidePivot>();

    public float visibleDistance;

    [SerializeField]
    private List<GameObject> frontPointSpheres;
    [SerializeField]
    private List<GameObject> backPointSpheres;

    [Inject]
    void Injected(GuidePivotManager guidePivotManager)
    {
        InitSpawnPivot(guidePivotManager);

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

    private void InitSpawnPivot(GuidePivotManager guidePivotManager)
    {
        /* Searching Closest Pivot from Sphere */
        foreach (GameObject obj in frontPointSpheres)
        {
            float minDistance = 1000f;
            GuidePivotManager.GuidePivot tmp = null;
            foreach (var gp in guidePivotManager.guideLine)
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
            foreach (var gp in guidePivotManager.guideLine)
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
                if (Vector3.Distance(player.position, frontPoint.cur.position) < visibleDistance)
                {
                    frontSpawn[i] = frontPoint.next;
                    frontPointSpheres[frontPoint.coordinates.x - 1].transform.position = frontPoint.cur.position;
                }
            }
            for (int i = 0; i < backSpawn.Count; i++)
            {
                var backPoint = backSpawn[i];
                if (Vector3.Distance(player.position, backPoint.cur.position) > visibleDistance)
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

        yield return new WaitForSeconds(1f);
    }
}
