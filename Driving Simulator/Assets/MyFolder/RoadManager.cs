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
        
        public Road(Transform _self, int _laneCount)
        {
            self = _self;
            laneCount = _laneCount;
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

    public Road myRoad;

    private void Awake()
    {
        myRoad = new Road(transform, LaneCount);
        myRoad.InitializeRoad();
    }

    void OnDrawGizmos()
    {
        myRoad = new Road(transform, LaneCount);
        myRoad.InitializeRoad();
    }
}