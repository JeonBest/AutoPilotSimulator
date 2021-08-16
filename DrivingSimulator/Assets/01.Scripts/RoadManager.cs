using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    public class Road
    {
        public Transform self;                  // �ڱ��ڽ�
        public List<Transform> trackTiles;      // Road �Ʒ��� track tiles���� List
        public int laneCount;
        public float speedLimit;
        
        public Road(Transform _self, int _laneCount, float _speedLimit)
        {
            self = _self;
            laneCount = _laneCount;
            speedLimit = _speedLimit;
            trackTiles = new List<Transform>();
        }

        // Road�� ������ ���� ȣ���ϸ�, �ڱ� �ڽ����� �ִ� track tile�� �޾ƿ� ����
        // !! �ڽ��� ������� ������ ������ �ν��ϱ� ������ �ڽ� ������ �߿� !!
        // 0��° : ������ ���� tile
        // ������: ������ �� tile
        public void InitializeRoad()
        {
            foreach(Transform trans in self.GetComponentsInChildren<Transform>())
            {
                if (!trans.CompareTag("Road"))
                    continue;

                trackTiles.Add(trans);
            }
        }
    }

    [Header ("�� ������ ���� ��")]
    public int LaneCount;

    [Header("�� ������ ���Ѽӵ�")]
    public float speedLimit;

    public Road myRoad;

    private void Awake()
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