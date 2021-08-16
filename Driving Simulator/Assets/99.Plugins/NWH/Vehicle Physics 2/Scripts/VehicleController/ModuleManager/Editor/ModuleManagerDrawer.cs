#if UNITY_EDITOR
using System.Linq;
using NWH.NUI;
using UnityEditor;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules
{
    [CustomPropertyDrawer(typeof(ModuleManager))]
    public class ModuleManagerDrawer : ComponentNUIPropertyDrawer
    {
        private int  selectedModule;
        private bool reloadModulesFlag;


        public ModuleManagerDrawer()
        {
            reloadModulesFlag = true;
        }


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            VehicleController vehicleController =
                SerializedPropertyHelper.GetTargetObjectWithProperty(property) as VehicleController;
            if (vehicleController == null)
            {
                drawer.EndProperty();
                return false;
            }

            ModuleManager moduleManager = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as ModuleManager;
            if (moduleManager == null)
            {
                drawer.EndProperty();
                return false;
            }

            if (reloadModulesFlag)
            {
                reloadModulesFlag = false;
                moduleManager.ReloadModulesList(vehicleController);
            }

            moduleManager.VehicleController = vehicleController;

            drawer.Space();

            ModuleWrapper[] wrappers = vehicleController.gameObject.GetComponents<ModuleWrapper>();
            if (wrappers.Length == 0)
            {
                drawer.Info("Use 'Add Component' button to add a module. Modules will appear here as they are added.");
                drawer.EndProperty();
                return true;
            }

            drawer.Label("Module Categories:");
            VehicleModule.ModuleCategory[] moduleCategories =
                moduleManager.modules.Select(m => m.GetModuleCategory()).Distinct().OrderBy(x => x).ToArray();
            int categoryIndex =
                drawer.HorizontalToolbar("moduleCategories", moduleCategories.Select(m => m.ToString()).ToArray());
            if (categoryIndex < 0)
            {
                categoryIndex = 0;
            }

            if (categoryIndex >= moduleCategories.Length)
            {
                drawer.EndProperty();
                return true;
            }

            drawer.Space(3);
            VehicleModule.ModuleCategory activeCategory = moduleCategories[categoryIndex];

            foreach (ModuleWrapper wrapper in wrappers)
            {
                if (wrapper == null || wrapper.GetModule() == null)
                {
                    continue;
                }

                if (wrapper.GetModule().GetModuleCategory() != activeCategory)
                {
                    continue;
                }

                drawer.EmbeddedObjectEditor<NVP_NUIEditor>(wrapper, drawer.positionRect);
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
