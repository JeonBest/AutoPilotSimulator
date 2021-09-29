using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class GIC_Controller
{
    public ControllerInfo Info;

    public int[] axis = new int[6];
    public int[] slider = new int[2];
    public int[] pov = new int[32];
    public int[] buttons = new int[128];
    public int[] keys = new int[256];
    
    public int[] copyValuesRaw;

    public int[] fAxis = new int[6];
    public int[] fSlider = new int[2];
    public int[] fPov = new int[32];
    public int[] fButtons = new int[128];
    public int[] fKeys = new int[256];
    
    public void Init()
    {
        for (int i = 0; i < axis.Length; i ++)
        {
            axis[i] = 0;
            fAxis[i] = 0;
        }
        for (int i = 0; i < slider.Length; i ++)
        {
            slider[i] = 0;
            fSlider[i] = 0;
        }
        for (int i = 0; i < pov.Length; i ++)
        {
            pov[i] = 0;
            fPov[i] = 0;
        }
        for (int i = 0; i < buttons.Length; i ++)
        {
            buttons[i] = 0;
            fButtons[i] = 0;
        }
        for (int i = 0; i < keys.Length; i ++)
        {
            keys[i] = 0;
            fKeys[i] = 0;
        }

        copyValuesRaw = new int[axis.Length + slider.Length + pov.Length + buttons.Length];
    }

    public void Copy()
    {
        fAxis = axis;
        fSlider = slider;
        fPov = pov;
        fButtons = buttons;
        fKeys = keys;
    }
    
    /// <summary>
    /// from the index of the array, retrieve the type of the associated item
    /// </summary>
    /// <param name="index"></param>
    /// <returns>0: null, 1: axis, 2: slider, 3: pov, 4, buttons</returns>
    public int GetTypeFromIndex(int index)
    {
        if (index < axis.Length)
        {
            return 1;
        }
        else if (index < axis.Length + slider.Length)
        {
            return 2;
        }
        else if (index < axis.Length + slider.Length + pov.Length)
        {
            return 3;
        }
        else if (index < axis.Length + slider.Length + pov.Length + buttons.Length)
        {
            return 4;
        }
        return 0;
    }

    public void Update(int[] values)
    {
        int pValue = 0;
        for (int i = 0; i < axis.Length; i ++)
        {
            axis[i] = values[pValue++];
        }
        for (int i = 0; i < slider.Length; i ++)
        {
            slider[i] = values[pValue++];
        }
        for (int i = 0; i < pov.Length; i ++)
        {
            pov[i] = values[pValue++];
        }
        for (int i = 0; i < buttons.Length; i ++)
        {
            buttons[i] = values[pValue++];
        }
    }
}

public enum DeviceType
{
    AXIS,
    SLIDER,
    POV,
    BUTTON,
    KEYBOARD
};

[StructLayout(LayoutKind.Sequential), Serializable]
public struct ControllerInfo
{
    public bool HasForceFeedback;
    public bool IsXInput;
    public bool IsTracker;
    public string Name;
}

