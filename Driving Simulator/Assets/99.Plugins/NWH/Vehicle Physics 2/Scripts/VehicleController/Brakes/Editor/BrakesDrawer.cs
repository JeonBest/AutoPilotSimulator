#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Property drawer for Brakes.
    /// </summary>
    [CustomPropertyDrawer(typeof(Brakes))]
    public class BrakesDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Braking");
            drawer.Field("maxTorque", true, "Nm");
            drawer.Field("smoothing");
            drawer.Field("brakeWhileIdle");
            drawer.Field("brakeWhileAsleep");
            drawer.EndSubsection();

            drawer.BeginSubsection("Handbrake");
            drawer.Field("handbrakeType");
            drawer.Field("handbrakeValue", false);
            drawer.EndSubsection();

            drawer.BeginSubsection("Off-Throttle Braking");
            drawer.Field("brakeOffThrottle");
            drawer.Field("brakeOffThrottleStrength");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
