#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.VehiclePhysics2.Input
{
    [CustomEditor(typeof(InputSystemVehicleInputProvider))]
    public class InputSystemVehicleInputProviderEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info(
                "Input settings for Unity's new input system can be changed by modifying 'VehicleInputActions' " +
                "file (double click on it to open).");
            drawer.Field("mouseInput");

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
