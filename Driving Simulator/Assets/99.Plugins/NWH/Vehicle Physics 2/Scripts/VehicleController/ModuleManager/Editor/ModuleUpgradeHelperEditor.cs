#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules
{
    [CustomEditor(typeof(ModuleUpgradeHelper), true)]
    [CanEditMultipleObjects]
    public class ModuleUpgradeHelperEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info("Upgrade process:\n" +
                        "- Click on 'Import Modules From Wrappers'.\n" +
                        "- Verify that the modules have been added to VehicleController > Modules.\n" +
                        "- Click on 'Remove Wrappers' to remove MonoBehaviour module wrappers from this object. This can also be done manually.\n");


            drawer.Space(20);

            if (drawer.Button("1) Import Modules From Wrappers"))
            {
                foreach (Object t in targets)
                {
                    ModuleUpgradeHelper helper = (ModuleUpgradeHelper) t;

                    helper.AddModules();

                    VehicleController vc = helper.GetComponent<VehicleController>();
                    EditorUtility.SetDirty(vc);
                    Undo.RecordObject(vc, "VehicleController module upgrade.");
                }
            }

            if (drawer.Button("2) Remove Wrappers"))
            {
                foreach (Object t in targets)
                {
                    ModuleUpgradeHelper helper = (ModuleUpgradeHelper) t;

                    helper.RemoveWrappers();

                    VehicleController vc = helper.GetComponent<VehicleController>();
                    EditorUtility.SetDirty(vc);
                    Undo.RecordObject(vc, "Remove module wrappers.");
                }
            }

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
