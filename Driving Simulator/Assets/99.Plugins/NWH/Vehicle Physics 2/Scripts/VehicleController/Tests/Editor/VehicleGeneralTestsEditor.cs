#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.VehiclePhysics2.Tests
{
    [CustomEditor(typeof(VehicleGeneralTests))]
    [CanEditMultipleObjects]
    public class VehicleGeneralTestsEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            VehicleGeneralTests test = (VehicleGeneralTests) target;

            if (drawer.Button("Run"))
            {
                test.Run();
            }

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
