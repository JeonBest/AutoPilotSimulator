using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{

    GameObject GoodCarPrefab;
    GameObject BadCarPrefab;

    List<Vector2Int> _goodCars = new List<Vector2Int>();
    List<Vector2Int> _badCars = new List<Vector2Int>();

    void Start()
    {
        SpawnOnStart();

        StartCoroutine(MoveCarCoroutine());
    }

    private void SpawnOnStart()
    {

    }

    private IEnumerator MoveCarCoroutine()
    {
        yield break;
    }
}
