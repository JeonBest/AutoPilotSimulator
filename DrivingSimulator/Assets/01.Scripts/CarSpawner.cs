using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{

    [SerializeField]
    private GameObject GoodCarPrefab;
    [SerializeField]
    private GameObject BadCarPrefab;

    [SerializeField]
    private List<Vector2Int> _goodCars = new List<Vector2Int>();
    [SerializeField]
    private List<Vector2Int> _badCars = new List<Vector2Int>();

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
