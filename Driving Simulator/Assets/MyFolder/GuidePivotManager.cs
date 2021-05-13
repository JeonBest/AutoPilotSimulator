using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidePivotManager : MonoBehaviour
{
    public class GuidePivot
    {
        public Transform cur;
        public GuidePivot left;
        public GuidePivot next;
        public GuidePivot right;
        public GuidePivot prev;

        public GuidePivot() { }

        public GuidePivot(Transform _cur)
        {
            cur = _cur;
        }
    }

    // 도로끼리의 연결 형태
    public enum ConnectPos
    {
        straight,     // FromRoad에서 직진으로 ToRoad
        rightTurn,    // FromRoad에서 우회전으로 ToRoad
        leftTurn,     // FromRoad에서 좌회전으로 ToRoad
        rightIC,      // FromRoad의 왼쪽 끝차로에서 ToRoad의 오른쪽 끝차로로 평행진입
        leftIC,       // FromRoad의 왼쪽 끝차로에서 ToRoad의 왼쪽 끝차로로 평행진입
    }

    /* 도로끼리의 연결 meta data */
    [System.Serializable]
    public class ConnectMeta
    {
        public Transform FromRoad;              // 출발 도로 (진행방향의 뒤)
        public int FRLaneCnt;                   // 출발 도로 차로 수
        public Transform ToRoad;                // 도착 도로 (진행방향의 앞)
        public int TRLaneCnt;                   // 도착 도로 차로 수
        public ConnectPos connectPos;           // 연결형태
        public Vector2Int FromRoadTileIdx;      // 출발 도로의 연결된 tile index (straight일 땐, lane number)
        public Vector2Int ToRoadTileIdx;        // 도착 도로의 연결된 tile index (straight일 땐, lane number)
    }

    // *********************************************************************************

    [Header (" ** For Debugging ** ")]
    public Color lineColor;

    [Header (" ** Road Connection Settings ** ")]
    public List<ConnectMeta> connectMetas;

    // List of every GuidePivots  => to Find Starting Pivot
    public List<GuidePivot> guideLine;

    // *********************************************************************************

    // Start is called before the first frame update
    void Start()
    {
        guideLine = new List<GuidePivot>();
        Transform[] tmp = transform.GetComponentsInChildren<Transform>();

        GuidePivot[] curLine = { null, null, null };
        GuidePivot[] oldLine = { null, null, null };
        foreach (Transform pivotTransform in tmp)
        {
            if (!pivotTransform.CompareTag("DriveGuide"))
                continue;

            GuidePivot current = new GuidePivot(pivotTransform);
            if (pivotTransform.name == "GuidePivot.1")
            {
                curLine[0] = current;
            }
            if (pivotTransform.name == "GuidePivot.2")
            {
                curLine[1] = current;
            }
            if (pivotTransform.name == "GuidePivot.3")
            {
                curLine[2] = current;

                if (oldLine[0] != null)
                {
                    oldLine[0].next = curLine[0];
                    oldLine[0].right = curLine[1];
                    curLine[0].prev = oldLine[0];
                }
                if (oldLine[1] != null)
                {
                    oldLine[1].left = curLine[0];
                    oldLine[1].next = curLine[1];
                    oldLine[1].right = curLine[2];
                    curLine[1].prev = oldLine[1];
                }
                if (oldLine[2] != null)
                {
                    oldLine[2].left = curLine[1];
                    oldLine[2].next = curLine[2];
                    curLine[2].prev = oldLine[2];
                }

                guideLine.Add(curLine[0]);
                guideLine.Add(curLine[1]);
                guideLine.Add(curLine[2]);

                oldLine[0] = curLine[0];
                oldLine[1] = curLine[1];
                oldLine[2] = curLine[2];
            }
        }
        int pivotNum = guideLine.Count;
        guideLine[0].prev = guideLine[pivotNum - 3];
        guideLine[1].prev = guideLine[pivotNum - 2];
        guideLine[2].prev = guideLine[pivotNum - 1];

        guideLine[pivotNum - 3].next = guideLine[0];
        guideLine[pivotNum - 3].right = guideLine[1];

        guideLine[pivotNum - 2].left = guideLine[0];
        guideLine[pivotNum - 2].next = guideLine[1];
        guideLine[pivotNum - 2].right = guideLine[2];

        guideLine[pivotNum - 1].left = guideLine[1];
        guideLine[pivotNum - 1].next = guideLine[2];

    }

    private void OnDrawGizmosSelected()
    {
        guideLine = new List<GuidePivot>();

        List<List<GuidePivot>> guideLinePerRoad = new List<List<GuidePivot>>();

        for(int i = 0; i < transform.childCount; i++){
            RoadManager RM = transform.GetChild(i).GetComponent<RoadManager>();
            guideLinePerRoad.Add(new List<GuidePivot>()); 

            GuidePivot[] curLine = { null, null, null, null, null, null };   // 편도 5차로까지 지원
            GuidePivot[] oldLine = { null, null, null, null, null, null };

            // 시작 도로타일부터 끝 도로타일까지
            foreach (Transform trackTileTransform in RM.myRoad.trackTiles)
            {
                // 도로타일 내의 GuidePivot 1차로부터 순회
                foreach (Transform pivotTransform in trackTileTransform.GetComponentsInChildren<Transform>())
                {
                    if (!pivotTransform.CompareTag("DriveGuide"))
                        continue;

                    GuidePivot current = new GuidePivot(pivotTransform);

                    switch (pivotTransform.name) {
                        case "GuidePivot.1":
                            curLine[0] = current;
                            break;
                        case "GuidePivot.2":
                            curLine[1] = current;
                            break;
                        case "GuidePivot.3":
                            curLine[2] = current;
                            break;
                        case "GuidePivot.4":
                            curLine[3] = current;
                            break;
                        case "GuidePivot.5":
                            curLine[4] = current;
                            break;
                        case "GuidePivot.6":
                            curLine[5] = current;
                            break;
                        default:
                            Debug.LogWarning("Guide Pivot Name Wrong.");
                            break;
                    }
                }
                
                // GuidePivot끼리 관계 형성
                for(int j = 0; j < RM.myRoad.laneCount; j++)
                {
                    if (oldLine[j] != null)
                    {
                        oldLine[j].next = curLine[j];
                        curLine[j].prev = oldLine[j];
                        if (j != RM.myRoad.laneCount - 1)
                            oldLine[j].right = curLine[j + 1];      // !! 6차로는 여기서 out of range 남 !!
                        if (j != 0)
                            oldLine[j].left = curLine[j - 1];
                    }
                    guideLinePerRoad[i].Add(curLine[j]);
                    oldLine[j] = curLine[j];
                }
            }
        }

        // Road끼리 연결부의 GuidePivot끼리 관계 형성
        
        foreach(ConnectMeta cm in connectMetas)
        {
            int fromRoadNum = System.Convert.ToInt32(cm.FromRoad.name.Split('.')[1]);
            int toRoadNum = System.Convert.ToInt32(cm.ToRoad.name.Split('.')[1]);

            List<GuidePivot> fromPivots = new List<GuidePivot>();
            List<GuidePivot> toPivots = new List<GuidePivot>();

            switch (cm.connectPos)
            {
                case ConnectPos.straight:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        // straight는 from road의 끝과 to road의 시작이 만나므로 마지막 줄의 원소를 받아온다
                        fromPivots.Add(guideLinePerRoad[fromRoadNum-1][guideLinePerRoad[fromRoadNum-1].Count - cm.FRLaneCnt+i-1]);
                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i-1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                    {
                        fromPivots[i].next = toPivots[i];
                        toPivots[i].prev = fromPivots[i];
                    }

                    break;

                case ConnectPos.rightTurn:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][guideLinePerRoad[fromRoadNum-1].Count - cm.FRLaneCnt+i-1]);

                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i - 1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                        fromPivots[i].right = toPivots[i];

                    break;

                case ConnectPos.leftTurn:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][guideLinePerRoad[fromRoadNum-1].Count - cm.FRLaneCnt + i - 1]);

                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i - 1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                        fromPivots[i].left = toPivots[i];

                    break;

                case ConnectPos.rightIC:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][i * cm.FRLaneCnt + cm.FRLaneCnt - 1]);

                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i * cm.TRLaneCnt + cm.TRLaneCnt - 1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                        fromPivots[i].left = toPivots[i];

                    break;

                case ConnectPos.leftIC:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][i * cm.FRLaneCnt + cm.FRLaneCnt - 1]);

                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i * cm.TRLaneCnt + cm.TRLaneCnt - 1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                        fromPivots[i].right = toPivots[i];

                    break;
            }
            
        }

        // 결과 guideLine에 합치기
        foreach (List<GuidePivot> gplist in guideLinePerRoad)
        {
            guideLine.AddRange(gplist);
        }

        Gizmos.color = lineColor;
        
        foreach(GuidePivot gp in guideLine)
        {
            Gizmos.DrawWireSphere(gp.cur.position, 0.5f);
            if (gp.next != null)
                Gizmos.DrawLine(gp.cur.position, gp.next.cur.position);
            if (gp.left != null)
                Gizmos.DrawLine(gp.cur.position, gp.left.cur.position);
            if (gp.right != null)
                Gizmos.DrawLine(gp.cur.position, gp.right.cur.position);
            
        }
    }
}
