using UnityEngine;

namespace NWH.VehiclePhysics2.Tests
{
    public class VehicleGeneralTests : MonoBehaviour
    {
        public VehicleController vc;


        public void Run()
        {
            vc = GetComponent<VehicleController>();

            EnableDisableTest();
            ComponentStateTest();
        }


        public void EnableDisableTest()
        {
            GameObject vehicleGO = vc.gameObject;
            vehicleGO.SetActive(false);
            vehicleGO.SetActive(true);
            vehicleGO.SetActive(false);
            vehicleGO.SetActive(true);
        }


        public void ComponentStateTest()
        {
            foreach (VehicleComponent component in vc.GetAllComponents())
                if (component.IsOn)
                {
                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");

                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");

                    component.Disable();
                    Debug.Assert(!component.IsEnabled, $"Component {component.GetType()} failed to disable.");

                    component.Disable();
                    Debug.Assert(!component.IsEnabled, $"Component {component.GetType()} failed to disable.");

                    component.Enable();
                    Debug.Assert(component.IsEnabled, $"Component {component.GetType()} failed to enable.");
                }
        }
    }
}