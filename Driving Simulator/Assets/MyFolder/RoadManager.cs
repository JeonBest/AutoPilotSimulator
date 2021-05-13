using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    // 도로끼리의 연결 형태
    public enum ConnectPos
    {
        straightIn,     // 이 도로로 직진해서 진입
        straightOut,    // 이 도로에서 직진해서 진출
        rightTurnIn,    // 이 도로로 우회전해서 진입
        rightTurnOut,   // 이 도로에서 우회전해서 진출
        leftTurnIn,     // 이 도로로 좌회전해서 진입
        leftTurnOut,    // 이 도로에서 좌회전해서 진출
        rightIcIn,      // 이 도로 오른쪽에서 IC처럼 평행 진입
        rightIcOut,     // 이 도로 오른쪽으로 IC처럼 평행 진출
        leftIcIn,       // 이 도로 왼쪽에서 IC처럼 평행 진입
        leftIcOut       // 이 도로 왼쪽으로 IC처럼 평행 진출
    }

    /* 도로끼리의 연결 meta data */
    [System.Serializable]
    public class ConnectMeta
    {
        public Transform connectedRoad;         // 연결된 도로
        public ConnectPos connectPos;           // 연결형태
        public Vector2Int connectedTileFromTo;  // 연결된 track Tile의 index
        public Vector2Int connectedLaneFromTo;  // 연결된 차로 번호
    }

    public class Road
    {
        public Transform self;                  // 자기자신
        public List<Transform> trackTiles;      // Road 아래의 track tiles들의 List
        public List<ConnectMeta> connectMetas;  // Road와 연결된 Road들의 Meat data들의 List
        public int laneCount;                   // 몇 차로짜리 도로인지
        
        public Road(Transform _self)
        {
            self = _self;
            trackTiles = new List<Transform>();
            connectMetas = new List<ConnectMeta>();
        }

        // Road가 생성된 이후 호출하면, 자기 자식으로 있는 track tile을 받아와 저장
        // !! 자식의 순서대로 도로의 방향을 인식하기 때문에 자식 순서가 중요 !!
        // 0번째 : 도로의 시작 tile
        // 마지막: 도로의 끝 tile
        public void InitializeRoad()
        {
            foreach(Transform trans in self.GetComponentsInChildren<Transform>())
            {
                if (!trans.CompareTag("Road"))
                    continue;

                trackTiles.Add(trans);
            }

            int cnt = 0;
            foreach(Transform gptrans in self.GetChild(0).GetComponentsInChildren<Transform>())
            {
                if (gptrans.CompareTag("DriveGuide"))
                    cnt += 1;
            }
            laneCount = cnt;
        }
    }

    [Header ("Put self object here")]
    public Transform selfTransform;

    [Header ("Road connection settings")]
    public List<ConnectMeta> connectedRoadList;

    public Road myRoad;

    private void Start()
    {
        myRoad = new Road(selfTransform);
        myRoad.InitializeRoad();

        foreach (ConnectMeta cm in connectedRoadList)
        {
            myRoad.connectMetas.Add(cm);
        }
    }

    void OnDrawGizmos()
    {
        myRoad = new Road(selfTransform);
        myRoad.InitializeRoad();

        foreach (ConnectMeta cm in connectedRoadList)
        {
            myRoad.connectMetas.Add(cm);
        }
    }
}