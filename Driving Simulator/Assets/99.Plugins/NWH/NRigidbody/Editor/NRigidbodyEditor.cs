#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.NPhysics
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NRigidbody))]
    public class NRigidbodyEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Field("substeps", !Application.isPlaying);
            drawer.Info("If used with a vehicle controller substep setting might get overwritten by that controller at runtime.");
            
            drawer.EndEditor(this);
            return true;
        }
    }
}
#endif