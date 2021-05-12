using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuidePivotManager : MonoBehaviour
{
    public Transform Track;
    public Color lineColor;

    public List<GuidePivot> guideLine;

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

    // Start is called before the first frame update
    void Start()
    {
        guideLine = new List<GuidePivot>();
        Transform[] tmp = Track.GetComponentsInChildren<Transform>();

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
        Transform[] tmp = Track.GetComponentsInChildren<Transform>();

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
