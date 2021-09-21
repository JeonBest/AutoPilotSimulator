using System.Collections;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Input;
using UnityEngine;
using Zenject;

public class CarSpawner : MonoBehaviour
{

    [SerializeField]
    private float distance2ground;
    [SerializeField]
    private GameObject GoodCarPrefab;
    [SerializeField]
    private GameObject BadCarPrefab;
    [SerializeField]
    private CarMover carMover;

    [SerializeField]
    private List<Vector2Int> _goodCars = new List<Vector2Int>();
    [SerializeField]
    private List<Vector2Int> _badCars = new List<Vector2Int>();

    private List<GoodDriverAI> _goodDriverAIs = new List<GoodDriverAI>();
    private List<BadDriverAI> _badDriverAIs = new List<BadDriverAI>();

    [Inject]
    void Injected(GuidePivotManager guidePivotManager, CarMover carMover)
    {
        SpawnOnStart(guidePivotManager, carMover);
        carMover.Init(_goodDriverAIs, _badDriverAIs);
    }

    private void SpawnOnStart(GuidePivotManager guidePivotManager, CarMover carMover)
    {
        int goodCarNum = 1;
        foreach (Vector2Int coor in _goodCars)
        {
            Transform worldTrans = guidePivotManager.GuidePivotPool[coor].cur.transform;
            GameObject newObj = Instantiate(
                GoodCarPrefab,
                worldTrans.position - new Vector3(0, distance2ground, 0),
                worldTrans.rotation,
                transform);
            newObj.name = $"Good Driver {goodCarNum++}";

            GoodDriverAI newAI = newObj.GetComponent<GoodDriverAI>();
            newAI.Init(guidePivotManager, carMover);
            _goodDriverAIs.Add(newAI);
        }
        int badCarNum = 1;
        foreach (Vector2Int coor in _badCars)
        {
            Transform worldTrans = guidePivotManager.GuidePivotPool[coor].cur.transform;
            GameObject newObj = Instantiate(
                BadCarPrefab,
                worldTrans.position - new Vector3(0, distance2ground, 0),
                worldTrans.rotation,
                transform);
            newObj.name = $"Bad Driver {badCarNum++}";

            BadDriverAI newAI = newObj.GetComponent<BadDriverAI>();
            newAI.Init(guidePivotManager, carMover);
            _badDriverAIs.Add(newAI);
        }
    }

}
