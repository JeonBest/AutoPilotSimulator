using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Security;

public static class GIC
{
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate void DeviceMovedCallback(int device, int type, int element);
    
    public static bool initalized = false;
    public static int numControllers = 0;
    public static int forceFeedbackDevice = -1;
    public static List<GIC_Controller> controllers;

    public delegate void AxisMovedEventHandler(int controllerID, int axisID, int oldValue, int newValue);
    public static event AxisMovedEventHandler AxisMovedEvent;

    public delegate void SliderMovedEventHandler(int controllerID, int sliderID, int oldValue, int newValue);
    public static event SliderMovedEventHandler SliderMovedEvent;
    
    public delegate void PovMovedEventHandler(int controllerID, int povID, int oldValue, int newValue);
    public static event PovMovedEventHandler PovMovedEvent;

    public delegate void ButtonUpEventHandler(int controllerID, int buttonID);
    public static event ButtonUpEventHandler ButtonUpEvent;

    public delegate void ButtonDownEventHandler(int controllerID, int buttonID);
    public static event ButtonDownEventHandler ButtonDownEvent;
    static IntPtr gic;
    
    #region DLL_Imports

    [DllImport("gInput")]
    public static extern int Add10(int a);
    [DllImport("gInput")]
    private static extern IntPtr GetInput(string logFileName);
    [DllImport("gInput")]
    private static extern bool InitDirectInput(IntPtr input, int windowHandle);
    [DllImport("gInput")]
    private static extern bool Process(IntPtr input, [MarshalAs(UnmanagedType.FunctionPtr)] DeviceMovedCallback callbackPointer);
    [DllImport("gInput")]
    private static extern bool ByeBye(IntPtr input);
    [DllImport("gInput")]
    private static extern int GetNumDevices(IntPtr input);
    [DllImport("gInput")]
    private static extern IntPtr GetDeviceInfo(IntPtr input, int deviceID);
    [DllImport("gInput")]
    private static extern bool ReadConfig(IntPtr input, string configFileName);
    [DllImport("gInput")]
    private static extern IntPtr GetValues(IntPtr input, float deltaT);
    [DllImport("gInput")]
    private static extern float GetValueFromID(IntPtr input, int valueID, float deltaT);

    // values
    [DllImport("gInput")]
    private static extern int GetAxis(IntPtr input, int device, int element);
    [DllImport("gInput")]
    private static extern int GetSlider(IntPtr input, int device, int element);
    [DllImport("gInput")]
    private static extern int GetPov(IntPtr input, int device, int element);
    [DllImport("gInput")]
    private static extern bool GetButton(IntPtr input, int device, int element);
    [DllImport("gInput")]
    private static extern int GetValue(IntPtr input, DeviceType type, int device, int element);
    [DllImport("gInput")]
    private static extern bool SetForce(IntPtr input, int device, int force);
    [DllImport("gInput")]
    private static extern void SetLeds(IntPtr input, float currentRPM, float rpmFirstLedTurnsOn, float rpmRedLine);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool SetDllDirectory(string lpPathName);
    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder strText, int maxCount);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32")]
    static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32")]
    static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextLength(IntPtr hWnd);
    static bool EnumTheWindows(IntPtr hWnd, IntPtr lParam)
    {
        int size = GetWindowTextLength(hWnd);
        if (size++ > 0 && IsWindowVisible(hWnd))
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(size);
            GetWindowText(hWnd, sb, size);
            //GetClassName(hWnd, sb, size);
            processes.Add(hWnd, sb.ToString());
            //str.Add( hWnd, sb.ToString());
            //Console.WriteLine(sb.ToString());
        }
        return true;
    }
    static Dictionary<IntPtr, string> processes;
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
    #endregion DLL_Imports

    /// <summary>
    /// init the GIC system
    /// </summary>
    /// <param name="projectName">Name of the project</param>
    /// <param name="logFile">Name of the logger file</param>
    /// <returns>true if everything is ok</returns>
	public static bool Init(string projectName, string logFile)
    {
        initalized = false;
        int cucu = 17;
        Debug.Log(cucu);
        cucu = Add10(cucu);
        Debug.Log(cucu);

        gic = GetInput(logFile);
        
        var handle = GetHandle(projectName);
        var handle32 = handle.ToInt32();

        if (InitDirectInput(gic, handle32))
        {
            controllers = new List<GIC_Controller>();

            numControllers = GetNumDevices(gic);
            Debug.Log(string.Format("Num controllers:{0}", numControllers));
            for (int i = 0; i < numControllers; i++)
            {
                var controller = new GIC_Controller();
                controller.Info = GetControllerInfo(i);
                if (controller.Info.HasForceFeedback && forceFeedbackDevice == -1)
                {
                    forceFeedbackDevice = i;
                }
                
                controller.Init();
                controllers.Add(controller);
            }

            initalized = true;
        }
        return initalized;
    }

    public static List<string> GetControllerList()
    {
        List<string> ret = new List<string>();
        ret.Add("Choose a controller:");
        foreach (var item in controllers)
        {
            ret.Add(item.Info.Name);
        }
        
        return ret;
    }
    
    private static void CallbackDeviceMoved(int device, DeviceType type, int element)
    {
        switch (type)
        {
            case DeviceType.AXIS:
                break;
            case DeviceType.SLIDER:
                break;
            case DeviceType.POV:
                break;
            case DeviceType.BUTTON:
                break;
            case DeviceType.KEYBOARD:
                break;
            default:
                break;
        }
        Debug.Log(string.Format("Callback: device:{0} type:{1}: element:{2}", device, type, element));
    }    

    public static void Update()
    {
        DeviceMovedCallback callback =
            (device, type, element) =>
            {
                CallbackDeviceMoved(device, (DeviceType)type, element);
            };
        Process(gic, callback);
        
        for (int c = 0; c < numControllers; c ++)
        {
            // Axis
            for (int i = 0; i < 6; i++)
            {
                controllers[c].axis[i] = GetValue(gic, DeviceType.AXIS, c, i);
            }
            // Slider
            for (int i = 0; i < 2; i++)
            {
                controllers[c].slider[i] = GetValue(gic, DeviceType.SLIDER, c, i);
            }
            // Pov
            for (int i = 0; i < 32; i++)
            {
                controllers[c].pov[i] = GetValue(gic, DeviceType.POV, c, i);
            }
            // Button
            for (int i = 0; i < 128; i++)
            {
                controllers[c].buttons[i] = GetValue(gic, DeviceType.BUTTON, c, i);
            }

            // detect the differences
            // Axis
            for (int i = 0; i < 6; i++)
            {
                if (controllers[c].axis[i] != controllers[c].fAxis[i])
                {
                    if (AxisMovedEvent != null)
                    {
                        AxisMovedEvent(c, i, controllers[i].fAxis[i], controllers[i].axis[i]);
                    }
                }
            }
            // Slider
            for (int i = 0; i < 2; i++)
            {
                if (controllers[c].slider[i] != controllers[c].fSlider[i])
                {
                    if (SliderMovedEvent != null)
                    {
                        SliderMovedEvent(c, i, controllers[i].fSlider[i], controllers[i].slider[i]);
                    }
                }
            }
            // Pov
            for (int i = 0; i < 32; i++)
            {
                if (controllers[c].pov[i] != controllers[c].fPov[i])
                {
                    if (PovMovedEvent != null)
                    {
                        PovMovedEvent(c, i, controllers[i].fPov[i], controllers[i].pov[i]);
                    }
                }
            }
            // Button
            for (int i = 0; i < 128; i++)
            {
                if (controllers[c].buttons[i] != controllers[c].fButtons[i])
                {
                    if (controllers[c].buttons[i] == 1)
                    {
                        if (ButtonDownEvent != null)
                        {
                            ButtonDownEvent(c, i);
                        }
                    }
                    else
                    {
                        if (ButtonUpEvent != null)
                        {
                            ButtonUpEvent(c, i);
                        }
                    }
                }
            }

            controllers[c].Copy();
        }
    }

    /// <summary>
    /// update the force feedback for the relative controller
    /// </summary>
    /// <param name="controllerID">controller</param>
    /// <param name="value">value with range -10000 / 10000</param>
    public static void UpdateForceFeedback(int controllerID, int value)
    {
        SetForce(gic, controllerID, value);
    }

    public static void UpdateLed(int controllerID, int value, int firstLed, int redLine)
    {
        SetLeds(gic, value, firstLed, redLine);
    }

    public static ControllerInfo GetControllerInfo(int controllerID)
    {
        var ret = GetDeviceInfo(gic, controllerID);
        var mapping = (ControllerInfo)System.Runtime.InteropServices.Marshal.PtrToStructure(ret, typeof(ControllerInfo));
        Debug.Log("---------------------------------------------------------------------------------");
        Debug.Log(string.Format("ControllerInfo #{0}: {1}", controllerID, mapping.Name));
        Debug.Log(string.Format("XInput: {0}", mapping.IsXInput));
        Debug.Log(string.Format("Tracker: {0}", mapping.IsTracker));
        Debug.Log(string.Format("ForceFeedback: {0}", mapping.HasForceFeedback));
        Debug.Log("---------------------------------------------------------------------------------");
        return mapping;
    }

    /// <summary>
    /// Destroy the library and clean up the memory
    /// </summary>
    public static void Destroy()
    {
        if (initalized)
        {
            initalized = false;
            ByeBye(gic);
            Debug.Log("GIC says goodbye!");
        }
    }
    
    static IntPtr GetHandle(string windowName)
    {
        var projectNameLower = windowName.ToLower();
        var windowHandle = IntPtr.Zero;

        processes = new Dictionary<IntPtr, string>();
        EnumWindows(new EnumWindowsProc(EnumTheWindows), IntPtr.Zero);
        foreach (var v in processes)
        {
            var procName = v.Value.ToLower();
               
#if UNITY_EDITOR
            if (procName.Contains(projectNameLower) && procName.Contains("unity "))
            {
                windowHandle = v.Key;
                break;
            }
#endif
            if (procName.Equals(projectNameLower))
            {
                windowHandle = v.Key;
                break;
            }
        }
        return windowHandle;
    }
    
}