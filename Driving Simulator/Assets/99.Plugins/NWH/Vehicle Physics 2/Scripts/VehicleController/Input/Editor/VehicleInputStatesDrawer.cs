#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Property drawer for InputStates.
    /// </summary>
    [CustomPropertyDrawer(typeof(VehicleInputStates))]
    public class VehicleInputStatesDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("steering");
            drawer.Field("throttle");
            drawer.Field("brakes");
            drawer.Field("clutch");
            drawer.Field("handbrake");
            drawer.Field("shiftUp");
            drawer.Field("shiftDown");
            drawer.Field("shiftInto");
            drawer.Field("leftBlinker");
            drawer.Field("rightBlinker");
            drawer.Field("lowBeamLights");
            drawer.Field("highBeamLights");
            drawer.Field("hazardLights");
            drawer.Field("extraLights");
            drawer.Field("trailerAttachDetach");
            drawer.Field("horn");
            drawer.Field("engineStartStop");
            drawer.Field("cruiseControl");
            drawer.Field("boost");
            drawer.Field("flipOver");
            EditorGUI.EndDisabledGroup();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
