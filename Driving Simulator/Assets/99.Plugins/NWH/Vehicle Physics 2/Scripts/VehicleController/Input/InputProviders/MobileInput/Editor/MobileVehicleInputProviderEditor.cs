#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Editor for MobileInputProvider.
    /// </summary>
    [CustomEditor(typeof(MobileVehicleInputProvider))]
    public class MobileVehicleInputProviderEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info("None of the buttons are mandatory. If you do not wish to use an input leave the field empty.");

            MobileVehicleInputProvider mip = target as MobileVehicleInputProvider;
            if (mip == null)
            {
                drawer.EndEditor(this);
                return false;
            }

            drawer.BeginSubsection("Input Type");
            drawer.Field("steeringInputType");
            drawer.Field("verticalInputType");
            drawer.EndSubsection();

            drawer.BeginSubsection("Direction");
            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.SteeringWheel)
            {
                drawer.Field("steeringWheel");
            }

            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.Accelerometer ||
                mip.verticalInputType == MobileVehicleInputProvider.VerticalAxisType.Accelerometer)
            {
                drawer.Field("tiltSensitivity");
            }

            if (mip.steeringInputType == MobileVehicleInputProvider.HorizontalAxisType.Button)
            {
                drawer.Field("steerLeftButton");
                drawer.Field("steerRightButton");
            }

            if (mip.verticalInputType == MobileVehicleInputProvider.VerticalAxisType.Button)
            {
                drawer.Field("throttleButton");
                drawer.Field("brakeButton");
            }

            drawer.EndSubsection();

            drawer.BeginSubsection("Scene Buttons");
            drawer.Info("Since v1.2 scene buttons are assigned through MobileSceneInputProvider component");
            drawer.EndSubsection();

            drawer.BeginSubsection("Vehicle Buttons");
            drawer.Field("engineStartStopButton");
            drawer.Field("handbrakeButton");
            drawer.Field("shiftUpButton");
            drawer.Field("shiftDownButton");
            drawer.Field("extraLightsButton");
            drawer.Field("highBeamLightsButton");
            drawer.Field("lowBeamLightsButton");
            drawer.Field("hazardLightsButton");
            drawer.Field("leftBlinkerButton");
            drawer.Field("rightBlinkerButton");
            drawer.Field("hornButton");
            drawer.Field("trailerAttachDetachButton");
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}

#endif
