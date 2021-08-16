#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(TransmissionComponent))]
    public class TransmissionComponentDrawer : PowertrainComponentDrawer
    {
        private TransmissionComponent _transmissionComponent;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            _transmissionComponent =
                SerializedPropertyHelper.GetTargetObjectOfProperty(property) as TransmissionComponent;
            SerializedProperty         transmissionType = property.FindPropertyRelative("_transmissionType");
            TransmissionComponent.Type type             = (TransmissionComponent.Type) transmissionType.enumValueIndex;

            DrawCommonProperties();

            drawer.BeginSubsection("General");
            drawer.Field("_transmissionType");
            if (type == TransmissionComponent.Type.CVT)
            {
                drawer.Field("cvtMaxInputTorque", true, "Nm", "CVT Max Input Torque");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Gearing");
            if (Application.isPlaying)
            {
                drawer.Label($"Current Total Gear Ratio:\t{_transmissionComponent.Ratio}");
            }
            drawer.Field("finalGearRatio");
            drawer.Field("gearingProfile");
            drawer.EmbeddedObjectEditor<NVP_NUIEditor>(_transmissionComponent.gearingProfile, drawer.positionRect);
            
            drawer.EndSubsection();

            if (type != TransmissionComponent.Type.CVT)
            {
                drawer.BeginSubsection("Shifting");
                drawer.Field("revMatch");
                drawer.Field("shiftDuration", true, "s");
                drawer.Field("postShiftBan",  true, "s");

                if (type != TransmissionComponent.Type.Manual)
                {
                    drawer.Field("automaticTransmissionReverseType");
                    drawer.Field("_upshiftRPM",   true, "rpm");
                    drawer.Field("_downshiftRPM", true, "rpm");
                    drawer.Field("_currentGearIndex");
                    if (drawer.Field("variableShiftPoint").boolValue)
                    {
                        drawer.Field("variableShiftIntensity");
                        drawer.Field("inclineEffectCoeff");
                        drawer.Info(
                            "High Incline Effect Coefficient values can prevent vehicle from changing gears as it is possible to get the Target Upshift RPM value higher than Rev Limiter RPM value. " +
                            "This is intentional to prevent heavy vehicles from upshifting on steep inclines.");
                        drawer.Field("_targetUpshiftRPM",   false, "rpm");
                        drawer.Field("_targetDownshiftRPM", false, "rpm");
                    }

                    drawer.EndSubsection();

                    drawer.BeginSubsection("Shift Conditions");
                    drawer.Field("shiftCheckCooldown");
                    drawer.Field("noWheelSpin",              false);
                    drawer.Field("noWheelSkid",              false);
                    drawer.Field("noWheelAir",               false);
                    drawer.Field("clutchEngaged",            false);
                    drawer.Field("externalShiftChecksValid", false);
                }
                else
                {
                    drawer.Field("holdToKeepInGear");
                    drawer.Field("ignorePostShiftBanInManual");
                    drawer.EndSubsection();
                }

                drawer.BeginSubsection("Events");
                drawer.Space(2);
                drawer.Field("onShift");
                drawer.Field("onUpshift");
                drawer.Field("onDownshift");
                drawer.EndSubsection();
            }

            EditorGUI.EndDisabledGroup();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
