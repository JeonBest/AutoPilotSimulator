#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.Common.CoM
{
    [CustomEditor(typeof(VariableCenterOfMass))]
    [CanEditMultipleObjects]
    public class VariableCenterOfMassEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            VariableCenterOfMass vcom     = (VariableCenterOfMass) target;

            drawer.Field("updateInterval", true, "s");
            
            drawer.BeginSubsection("Mass");

            drawer.Field("baseMass", true, "kg");

            if (Application.isPlaying)
            {
                drawer.Field("totalMass", false, "kg");
                drawer.Info("Total mass is auto-calculated from all the attached MassAffectors at runtime.");
            }
            drawer.Field("centerOfMass",       false, "m");
            drawer.Field("centerOfMassOffset", true,  "m");
            if (drawer.Button("Update Center of Mass"))
            {
                foreach (var o in targets)
                {
                    var t = (VariableCenterOfMass) o;
                    t.UpdateCoM();
                    EditorUtility.SetDirty(t);
                }
            }
            drawer.Space(2);
            drawer.EndSubsection();
            
            drawer.BeginSubsection("Inertia");
            if (!drawer.Field("useDefaultInertia").boolValue)
            {
                drawer.Field("dimensions",    true, "m");
                drawer.Field("inertiaTensor", true, "kg m2");
                drawer.Field("inertiaScale",  true, "x100%");
                if (drawer.Button("Update Inertia Tensor"))
                {
                    foreach (var o in targets)
                    {
                        var t = (VariableCenterOfMass) o;
                        t.UpdateInertiaTensor();
                        EditorUtility.SetDirty(t);
                    } 
                }
            }
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }
    }
}

#endif
