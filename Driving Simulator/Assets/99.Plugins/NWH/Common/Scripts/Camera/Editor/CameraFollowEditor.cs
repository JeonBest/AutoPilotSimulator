#if UNITY_EDITOR
using NWH.NUI;
using UnityEditor;

namespace NWH.Common.Cameras
{
    [CustomEditor(typeof(CameraFollow))]
    [CanEditMultipleObjects]
    public class CameraFollowEditor : NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }
            
            drawer.Info("This camera has been deprecated and will be removed in the future versions. Please use CameraMouseDrag instead.", MessageType.Warning);

            drawer.Field("target");

            drawer.BeginSubsection("Positioning");
            drawer.Field("distance");
            drawer.Field("height");
            drawer.Field("smoothing");
            drawer.Field("targetForwardOffset");
            drawer.Field("targetUpOffset");
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
