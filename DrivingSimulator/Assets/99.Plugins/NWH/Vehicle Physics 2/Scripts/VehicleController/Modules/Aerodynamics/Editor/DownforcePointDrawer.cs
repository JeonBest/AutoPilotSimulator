#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    [CustomPropertyDrawer(typeof(DownforcePoint))]
    public class DownforcePointDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("position");
            drawer.Field("maxForce");

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
