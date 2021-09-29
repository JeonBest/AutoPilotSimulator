#define ENABLE_PROFILER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GIC_Loader : MonoBehaviour
{
    [Header("Configuration")]
    public string projectName;
    public int updateEach = 10;
    public string logFile = "GIC.log";

    [Header("Debug info")]
    public List<GIC_Controller> Controllers;

    int updateCount = 0;

    // Use this for initialization
    void Start ()
    {
        Profiler.BeginSample("GIC Initialization");
        
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = Application.productName;
        }

        if (string.IsNullOrEmpty(logFile))
        {
            logFile = "GIC.log";
        }
        
        if (GIC.Init(projectName, logFile))
        {
            Debug.Log("gInputControllers initalized!");
            Debug.Log(string.Format("num controllers: {0}", GIC.numControllers));
            //GIC.AxisMovedEvent += axisMoved;
            //GIC.SliderMovedEvent += sliderMoved;
            //GIC.ButtonDownEvent += buttonDown;
            //GIC.ButtonUpEvent += buttonUp;
            Controllers = GIC.controllers;
        }
        else
        {
            Debug.LogError("error during the init of gInputControllers");
        }
        Profiler.EndSample();
    }

    void axisMoved(int controllerID, int axisID, int oldValue, int newValue)
    {
        Debug.Log(string.Format("{0} Axis moved:{1} from:{2} to:{3}", controllerID, axisID, oldValue, newValue));
    }

    void buttonDown(int controllerID, int buttonID)
    {
        Debug.Log(string.Format("{0} Button down:{1}", controllerID, buttonID));
    }

    void buttonUp(int controllerID, int buttonID)
    {
        Debug.Log(string.Format("{0} Button up:{1}", controllerID, buttonID));
    }

    // Update is called once per frame
    void Update ()
    {
        if (GIC.initalized)
        {
            if (updateCount >= updateEach)
            {
                Profiler.BeginSample("GIC Update");
                GIC.Update();
                Profiler.EndSample();
                updateCount = 0;
            }
            else
            {
                updateCount++;
            }
        }
    }

    void OnApplicationQuit()
    {
        GIC.Destroy();
    }
}
