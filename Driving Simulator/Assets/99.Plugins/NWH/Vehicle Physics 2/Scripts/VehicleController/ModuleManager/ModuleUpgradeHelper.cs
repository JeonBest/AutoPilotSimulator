using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules
{
    [RequireComponent(typeof(VehicleController))]
    public class ModuleUpgradeHelper : MonoBehaviour
    {
        public void AddModules()
        {
            VehicleController vehicleController = GetComponent<VehicleController>();

            ModuleWrapper[] moduleWrappers = GetComponents<ModuleWrapper>();
            if (moduleWrappers.Length == 0)
            {
                Debug.LogWarning("No modules found attached to this object. Nothing to upgrade.");
            }

            vehicleController.moduleManager.modules = new List<VehicleModule>();
            foreach (ModuleWrapper wrapper in moduleWrappers)
            {
                Debug.Log($"Adding {wrapper.GetModule().GetType()}");
                vehicleController.moduleManager.modules.Add(wrapper.GetModule());
            }

            Debug.Log("Upgrade finished. Modules can now be found under 'Modules' tab of VehicleController.");
        }


        public void RemoveWrappers()
        {
            ModuleWrapper[] moduleWrappers = GetComponents<ModuleWrapper>();
            for (int i = moduleWrappers.Length - 1; i >= 0; i--)
            {
                DestroyImmediate(moduleWrappers[i]);
            }

            Debug.Log("Finished removing module wrappers.");
        }
    }
}