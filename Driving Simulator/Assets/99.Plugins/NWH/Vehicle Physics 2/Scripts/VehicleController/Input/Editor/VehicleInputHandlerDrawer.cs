#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Property drawer for Input.
    /// </summary>
    [CustomPropertyDrawer(typeof(VehicleInputHandler))]
    public class VehicleInputHandlerDrawer : ComponentNUIPropertyDrawer
    {
        private float infoHeight;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("autoSetInput");
            drawer.Info(
                "While autoSetInput is set to 'true' vehicle will automatically get input from all available InputProviders in this " +
                "scene. To disable this behaviour untick the option. While this option is active any input set from external scripts will get" +
                " overridden.");
            drawer.Field("deadzone");
            drawer.Field("swapInputInReverse");

            drawer.BeginSubsection("Input Processing");
            drawer.Field("invertSteering");
            drawer.Field("invertThrottle");
            drawer.Field("invertBrakes");
            drawer.Field("invertClutch");
            drawer.Field("invertHandbrake");
            drawer.EndSubsection();

            drawer.Field("states");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
