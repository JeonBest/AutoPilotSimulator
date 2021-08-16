using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarMover : MonoBehaviour
{
    public int laneNum;
    Transform[] carTr;

    private void Awake()
    {
        carTr = GetComponentsInParent<Transform>();
    }
    public void carMove(Transform t)
    {
        carTr[1].position = t.position;
        carTr[1].rotation = t.rotation;
    }
}
