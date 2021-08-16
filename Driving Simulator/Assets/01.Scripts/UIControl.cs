using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIControl : MonoBehaviour
{
    public NWH.VehiclePhysics2.VehicleController player;
    public UnityEngine.UI.Text text;

    // Update is called once per frame
    void Update()
    {
        text.text = string.Format("{0:0.0} km/h", player.LocalForwardVelocity * 3.6);
    }
}
