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

    // ���γ����� ���� ����
    public enum ConnectPos
    {
        straight,     // FromRoad���� �������� ToRoad
        rightTurn,    // FromRoad���� ��ȸ������ ToRoad
        leftTurn,     // FromRoad���� ��ȸ������ ToRoad
        rightIC,      // FromRoad�� ���� �����ο��� ToRoad�� ������ �����η� ��������
        leftIC,       // FromRoad�� ���� �����ο��� ToRoad�� ���� �����η� ��������
    }

    /* ���γ����� ���� meta data */
    [System.Serializable]
    public class ConnectMeta
    {
        public Transform FromRoad;              // ��� ���� (��������� ��)
        public int FRLaneCnt;                   // ��� ���� ���� ��
        public Transform ToRoad;                // ���� ���� (��������� ��)
        public int TRLaneCnt;                   // ���� ���� ���� ��
        public ConnectPos connectPos;           // ��������
        public Vector2Int FromRoadTileIdx;      // ��� ������ ����� tile index (straight�� ��, lane number)
        public Vector2Int ToRoadTileIdx;        // ���� ������ ����� tile index (straight�� ��, lane number)
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

        List<List<GuidePivot>> guideLinePerRoad = new List<List<GuidePivot>>();

        for (int i = 0; i < transform.childCount; i++)
        {
            RoadManager RM = transform.GetChild(i).GetComponent<RoadManager>();
            guideLinePerRoad.Add(new List<GuidePivot>());

            GuidePivot[] curLine = { null, null, null, null, null, null };   // �� 5���α��� ����
            GuidePivot[] oldLine = { null, null, null, null, null, null };

            // ���� ����Ÿ�Ϻ��� �� ����Ÿ�ϱ���
            foreach (Transform trackTileTransform in RM.myRoad.trackTiles)
            {
                // ����Ÿ�� ���� GuidePivot 1���κ��� ��ȸ
                foreach (Transform pivotTransform in trackTileTransform.GetComponentsInChildren<Transform>())
                {
                    if (!pivotTransform.CompareTag("DriveGuide"))
                        continue;

                    GuidePivot current = new GuidePivot(pivotTransform);

                    switch (pivotTransform.name)
                    {
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

                // GuidePivot���� ���� ����
                for (int j = 0; j < RM.myRoad.laneCount; j++)
                {
                    if (oldLine[j] != null)
                    {
                        oldLine[j].next = curLine[j];
                        curLine[j].prev = oldLine[j];
                        if (j != RM.myRoad.laneCount - 1)
                            oldLine[j].right = curLine[j + 1];      // !! 6���δ� ���⼭ out of range �� !!
                        if (j != 0)
                            oldLine[j].left = curLine[j - 1];
                    }
                    guideLinePerRoad[i].Add(curLine[j]);
                    oldLine[j] = curLine[j];
                }
            }
        }

        // Road���� ������� GuidePivot���� ���� ����

        foreach (ConnectMeta cm in connectMetas)
        {
            int fromRoadNum = System.Convert.ToInt32(cm.FromRoad.name.Split('.')[1]);
            int toRoadNum = System.Convert.ToInt32(cm.ToRoad.name.Split('.')[1]);

            List<GuidePivot> fromPivots = new List<GuidePivot>();
            List<GuidePivot> toPivots = new List<GuidePivot>();

            switch (cm.connectPos)
            {
                case ConnectPos.straight:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        // straight�� from road�� ���� to road�� ������ �����Ƿ� ������ ���� ���Ҹ� �޾ƿ´�
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][guideLinePerRoad[fromRoadNum - 1].Count - cm.FRLaneCnt + i - 1]);
                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i - 1]);

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
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][guideLinePerRoad[fromRoadNum - 1].Count - cm.FRLaneCnt + i - 1]);

                    for (int i = cm.ToRoadTileIdx.x; i <= cm.ToRoadTileIdx.y; i++)
                        toPivots.Add(guideLinePerRoad[toRoadNum - 1][i - 1]);

                    if (fromPivots.Count != toPivots.Count)
                        Debug.LogError("From Road Idx count don't matches To Road Idx count!!!!");

                    for (int i = 0; i < fromPivots.Count; i++)
                        fromPivots[i].right = toPivots[i];

                    break;

                case ConnectPos.leftTurn:
                    for (int i = cm.FromRoadTileIdx.x; i <= cm.FromRoadTileIdx.y; i++)
                        fromPivots.Add(guideLinePerRoad[fromRoadNum - 1][guideLinePerRoad[fromRoadNum - 1].Count - cm.FRLaneCnt + i - 1]);

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

        // ��� guideLine�� ��ġ��
        foreach (List<GuidePivot> gplist in guideLinePerRoad)
        {
            guideLine.AddRange(gplist);
        }
    }

    private void OnDrawGizmosSelected()
    {
        guideLine = new List<GuidePivot>();

        List<List<GuidePivot>> guideLinePerRoad = new List<List<GuidePivot>>();

        for(int i = 0; i < transform.childCount; i++){
            RoadManager RM = transform.GetChild(i).GetComponent<RoadManager>();
            guideLinePerRoad.Add(new List<GuidePivot>()); 

            GuidePivot[] curLine = { null, null, null, null, null, null };   // �� 5���α��� ����
            GuidePivot[] oldLine = { null, null, null, null, null, null };

            // ���� ����Ÿ�Ϻ��� �� ����Ÿ�ϱ���
            foreach (Transform trackTileTransform in RM.myRoad.trackTiles)
            {
                // ����Ÿ�� ���� GuidePivot 1���κ��� ��ȸ
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
                
                // GuidePivot���� ���� ����
                for(int j = 0; j < RM.myRoad.laneCount; j++)
                {
                    if (oldLine[j] != null)
                    {
                        oldLine[j].next = curLine[j];
                        curLine[j].prev = oldLine[j];
                        if (j != RM.myRoad.laneCount - 1)
                            oldLine[j].right = curLine[j + 1];      // !! 6���δ� ���⼭ out of range �� !!
                        if (j != 0)
                            oldLine[j].left = curLine[j - 1];
                    }
                    guideLinePerRoad[i].Add(curLine[j]);
                    oldLine[j] = curLine[j];
                }
            }
        }

        // Road���� ������� GuidePivot���� ���� ����
        
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
                        // straight�� from road�� ���� to road�� ������ �����Ƿ� ������ ���� ���Ҹ� �޾ƿ´�
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

        // ��� guideLine�� ��ġ��
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
