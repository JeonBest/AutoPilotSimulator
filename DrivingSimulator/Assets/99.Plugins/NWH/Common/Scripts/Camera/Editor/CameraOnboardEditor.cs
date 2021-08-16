#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.Common.Cameras
{
    [CustomEditor(typeof(CameraOnboard))]
    [CanEditMultipleObjects]
    public class CameraOnboardEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.Info("This camera has been deprecated and will be removed in the future versions.\n" +
                        "Please use CameraMouseDrag with POV type set to 'First Person' instead.", MessageType.Warning);
            
            
            drawer.Field("target");

            drawer.BeginSubsection("Positioning");
            drawer.Field("maxMovementOffset");
            drawer.Field("movementIntensity");
            drawer.Field("movementSmoothing");
            drawer.Field("axisIntensity");
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
