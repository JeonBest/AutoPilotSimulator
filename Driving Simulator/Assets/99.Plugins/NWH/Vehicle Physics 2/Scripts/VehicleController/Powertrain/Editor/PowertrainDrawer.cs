#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(Powertrain))]
    public class PowertrainDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            int powertrainTab = drawer.HorizontalToolbar("powertrainTab",
                                                         new[]
                                                         {
                                                             "Engine", "Clutch", "Transmission", "Differentials",
                                                             "Wheels", "Wheel Groups",
                                                         }, true, true);

            switch (powertrainTab)
            {
                case 0:
                    drawer.Property("engine");
                    break;
                case 1:
                    drawer.Property("clutch");
                    break;
                case 2:
                    drawer.Property("transmission");
                    break;
                case 3:
                    drawer.ReorderableList("differentials", null, true, true, null, 5f);
                    break;
                case 4:
                    drawer.Space(3);
                    drawer.Info(
                        "Make sure that wheels are added in left to right, front to back order. E.g.: FrontLeft, FrontRight, RearLeft, RearRight.",
                        MessageType.Warning);
                    drawer.ReorderableList("wheels", null, true, true, null, 5f);
                    break;
                case 5:
                    drawer.ReorderableList("wheelGroups", null, true, true, null, 5f);
                    break;
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
