using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    public class Road
    {
        public Transform self;                  // 자기자신
        public List<Transform> trackTiles;      // Road 아래의 track tiles들의 List
        public int laneCount;
        public float speedLimit;

        public Road(Transform _self, int _laneCount, float _speedLimit)
        {
            self = _self;
            laneCount = _laneCount;
            speedLimit = _speedLimit;
            trackTiles = new List<Transform>();
        }

        // Road가 생성된 이후 호출하면, 자기 자식으로 있는 track tile을 받아와 저장
        // !! 자식의 순서대로 도로의 방향을 인식하기 때문에 자식 순서가 중요 !!
        // 0번째 : 도로의 시작 tile
        // 마지막: 도로의 끝 tile
        public void InitializeRoad()
        {
            foreach (Transform trans in self.GetComponentsInChildren<Transform>())
            {
                if (!trans.CompareTag("Road"))
                    continue;

                trackTiles.Add(trans);
            }
        }
    }

    [Header("Number of Lane on this road")]
    public int LaneCount;

    [Header("Speed Limit of this road")]
    public float speedLimit;

    public Road myRoad;

    public void Init()
    {
        myRoad = new Road(transform, LaneCount, speedLimit);
        myRoad.InitializeRoad();
    }

    void OnDrawGizmos()
    {
        myRoad = new Road(transform, LaneCount, speedLimit);
        myRoad.InitializeRoad();
    }
}