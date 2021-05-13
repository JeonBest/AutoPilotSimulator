using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    // ���γ����� ���� ����
    public enum ConnectPos
    {
        straightIn,     // �� ���η� �����ؼ� ����
        straightOut,    // �� ���ο��� �����ؼ� ����
        rightTurnIn,    // �� ���η� ��ȸ���ؼ� ����
        rightTurnOut,   // �� ���ο��� ��ȸ���ؼ� ����
        leftTurnIn,     // �� ���η� ��ȸ���ؼ� ����
        leftTurnOut,    // �� ���ο��� ��ȸ���ؼ� ����
        rightIcIn,      // �� ���� �����ʿ��� ICó�� ���� ����
        rightIcOut,     // �� ���� ���������� ICó�� ���� ����
        leftIcIn,       // �� ���� ���ʿ��� ICó�� ���� ����
        leftIcOut       // �� ���� �������� ICó�� ���� ����
    }

    /* ���γ����� ���� meta data */
    [System.Serializable]
    public class ConnectMeta
    {
        public Transform connectedRoad;         // ����� ����
        public ConnectPos connectPos;           // ��������
        public Vector2Int connectedTileFromTo;  // ����� track Tile�� index
        public Vector2Int connectedLaneFromTo;  // ����� ���� ��ȣ
    }

    public class Road
    {
        public Transform self;                  // �ڱ��ڽ�
        public List<Transform> trackTiles;      // Road �Ʒ��� track tiles���� List
        public List<ConnectMeta> connectMetas;  // Road�� ����� Road���� Meat data���� List
        public int laneCount;                   // �� ����¥�� ��������
        
        public Road(Transform _self)
        {
            self = _self;
            trackTiles = new List<Transform>();
            connectMetas = new List<ConnectMeta>();
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