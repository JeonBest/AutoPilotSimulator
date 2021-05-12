using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.WheelController3D;
using UnityEngine;

namespace NWH.VehiclePhysics2.Demo.VehicleOverview
{
    public class WheelGroupUI : MonoBehaviour
    {
        public GameObject wheelUIPrefab;
        public GameObject axleUIPrefab;

        private WheelGroup _wheelGroup;


        public void Initialize(WheelGroup wheelGroup)
        {
            _wheelGroup = wheelGroup;
        }


        private void Start()
        {
            if (_wheelGroup.Wheels.Count == 2)
            {
                InstantiateWheelUI(_wheelGroup.Wheels[0].wheelController);
                InstantiateAxleUI();
                InstantiateWheelUI(_wheelGroup.Wheels[1].wheelController);
            }
        }


        private WheelUI InstantiateWheelUI(WheelController wheelController)
        {
            WheelUI wheelUI = Instantiate(wheelUIPrefab, transform).GetComponent<WheelUI>();
            wheelUI.wheelController = wheelController;
            return wheelUI;
        }


        private void InstantiateAxleUI()
        {
            GameObject axleUI = Instantiate(axleUIPrefab, transform);
        }
    }
}