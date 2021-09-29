using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class GIC_List : MonoBehaviour
{
    [Header("Configuration")]
    public int UpdateEach = 5;
    public int UpdateFeedbackEach = 10;

    [Header("UI objects")]
    public GameObject panelAxis;
    public GameObject panelSlider;
    public GameObject panelPov;
    public GameObject panelButtons;
    
    public List<GameObject> axis;
    public List<GameObject> slider;
    public List<GameObject> pov;
    public List<GameObject> buttons;
    public GameObject feedback;
    public GameObject led;

    bool firstTime = true;

    int controllerSelected = -1;

    int updateCount = 0;
    int updateFeedbackCount = 0;

    // Use this for initialization
    void Start ()
    {

    }

    // Update is called once per frame
    void Update ()
    {
        if (GIC.initalized)
        {
            if (firstTime)
            {
                firstTime = false;

                // UI initialization
                var drop = GameObject.Find("DropListControllers").GetComponent<UnityEngine.UI.Dropdown>();
                drop.ClearOptions();
                drop.AddOptions(GIC.GetControllerList());
                drop.onValueChanged.AddListener(delegate {
                    onControllerChanged(drop);
                });

                panelAxis = GameObject.Find("PanelAxis");
                panelSlider = GameObject.Find("PanelSlider");
                panelPov = GameObject.Find("PanelPov");
                panelButtons = GameObject.Find("PanelButtons");
            }

            if (updateCount >= UpdateEach)
            {
                GIC.Update();
                updateCount = 0;

                if (controllerSelected >= 0)
                {
                    Profiler.BeginSample("GIC UI Update");
                    // Axis
                    for (int i = 0; i < GIC.controllers[controllerSelected].axis.Length; i ++)
                    {
                        axis[i].GetComponent<UnityEngine.UI.Slider>().value = GIC.controllers[controllerSelected].axis[i];
                    }
                    // Slider
                    for (int i = 0; i < GIC.controllers[controllerSelected].slider.Length; i ++)
                    {
                        slider[i].GetComponent<UnityEngine.UI.Slider>().value = GIC.controllers[controllerSelected].slider[i];
                    }
                    // Pov
                    for (int i = 0; i < GIC.controllers[controllerSelected].pov.Length; i ++)
                    {
                        pov[i].GetComponent<UnityEngine.UI.Slider>().value = GIC.controllers[controllerSelected].pov[i];
                    }
                    // Buttons
                    for (int i = 0; i < GIC.controllers[controllerSelected].buttons.Length; i ++)
                    {
                        var value = GIC.controllers[controllerSelected].buttons[i];
                        buttons[i].GetComponent<UnityEngine.UI.Image>().color = value == 0 ? Color.white : Color.red;
                    }
                     Profiler.EndSample();
                }
            }
            else
            {
                updateCount++;
            }
            if (controllerSelected >= 0)
            {
                if (GIC.controllers[controllerSelected].Info.HasForceFeedback)
                {
                    if (updateFeedbackCount >= UpdateFeedbackEach)
                    {
                        updateFeedbackCount = 0;
                        var ffValue = Convert.ToInt32(feedback.GetComponent<UnityEngine.UI.Slider>().value);
                        GIC.UpdateForceFeedback(controllerSelected, ffValue);
                    }
                    else
                    {
                        updateFeedbackCount++;
                    }
                }
                
                var rpmValue = Convert.ToInt32(led.GetComponent<UnityEngine.UI.Slider>().value);
                GIC.UpdateLed(controllerSelected, rpmValue, 0, 9000);
            }
        }
    }

    void onControllerChanged(UnityEngine.UI.Dropdown dd)
    {
        controllerSelected = dd.value -1;
        feedback = null;

        foreach (var item in axis)
        {
            Destroy(item);
        }
        axis.Clear();

        foreach (var item in slider)
        {
            Destroy(item);
        }
        slider.Clear();

        foreach (var item in pov)
        {
            Destroy(item);
        }
        pov.Clear();

        foreach (var item in buttons)
        {
            Destroy(item);
        }
        buttons.Clear();

        if (controllerSelected == -1)
        {
            return;
        }

        // choosen a controller
        var controller = GIC.controllers[controllerSelected];
        for (int i = 0; i < controller.axis.Length; i ++)
        {
            var prefab_instance = Instantiate( Resources.Load<GameObject>("GIC_Slider"));
            prefab_instance.transform.SetParent(panelAxis.transform);
            prefab_instance.GetComponent<RectTransform>().localPosition = new Vector3(5, -35 - i * 25, 0);
            prefab_instance.name = "Axis_" + i.ToString();
            axis.Add(prefab_instance);
        }
        for (int i = 0; i < controller.slider.Length; i ++)
        {
            var prefab_instance = Instantiate( Resources.Load<GameObject>("GIC_Slider"));
            prefab_instance.transform.SetParent(panelSlider.transform);
            prefab_instance.GetComponent<RectTransform>().localPosition = new Vector3(5, -35 - i * 25, 0);
            prefab_instance.name = "Slider_" + i.ToString();
            slider.Add(prefab_instance);
        }
        for (int i = 0; i < controller.pov.Length; i ++)
        {
            var prefab_instance = Instantiate( Resources.Load<GameObject>("GIC_Slider"));
            prefab_instance.transform.SetParent(panelPov.transform);
            prefab_instance.GetComponent<RectTransform>().localPosition = new Vector3(5, -35 - i * 25, 0);
            prefab_instance.name = "Pov_" + i.ToString();
            pov.Add(prefab_instance);
        }
        int pX = 0;
        int pY = 0;
        for (int i = 0; i < controller.buttons.Length; i ++)
        {
            var prefab_instance = Instantiate( Resources.Load<GameObject>("GIC_Button"));
            prefab_instance.transform.SetParent(panelButtons.transform);
            prefab_instance.GetComponent<RectTransform>().localPosition = new Vector3(5 + pX * 35, -35 - pY * 35, 0);
            pX ++;
            if (pX > 5)
            {
                pX =0;
                pY ++;
            }
            prefab_instance.transform.Find("Text").GetComponent<UnityEngine.UI.Text>().text = i.ToString();
            prefab_instance.name = "Buttons_" + i.ToString();
            buttons.Add(prefab_instance);
        }
        if (controller.Info.HasForceFeedback)
        {
            feedback = Instantiate(Resources.Load<GameObject>("FF_Slider"));
            feedback.transform.SetParent(panelAxis.transform);
            feedback.GetComponent<RectTransform>().localPosition = new Vector3(5, -35 - (controller.axis.Length + 1) * 25, 0);
            feedback.name = "Feedback";
        }

        //if (controller.Info.Name.ToLower().Contains("logitech") && (controller.Info.Name.ToLower().Contains("G27") || controller.Info.Name.ToLower().Contains("G29")))
        {
            led = Instantiate(Resources.Load<GameObject>("FF_Led"));
            led.transform.SetParent(panelAxis.transform);
            led.GetComponent<RectTransform>().localPosition = new Vector3(5, -35 - (controller.axis.Length + 3) * 25, 0);
            led.name = "Led";
        }
    }
}
