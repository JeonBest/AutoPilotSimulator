using NWH.WheelController3D;
using UnityEngine;
using UnityEngine.UI;

namespace NWH.VehiclePhysics2.Demo.VehicleOverview
{
    public class WheelUI : MonoBehaviour
    {
        public WheelController wheelController;

        public Image  bgImage;
        public Slider loadSlider;
        public Slider lngSlipSlider;
        public Slider latSlipSlider;

        public Color noTorqueColor;
        public Color maxTorqueColor;

        private float _smoothTorque;
        private float _smoothLoad;
        private float _smoothLatSlip;
        private float _smoothLngSlip;

        private float             _maxTorque;
        private VehicleController _vc;


        public void Start()
        {
            _vc = VehicleOverview.Instance.vc;
            _maxTorque = 9600f * _vc.powertrain.engine.maxPower / _vc.powertrain.engine.maxRPM *
                         _vc.powertrain.transmission.ForwardGears[0] *
                         _vc.powertrain.transmission.finalGearRatio;
        }


        public void Update()
        {
            float dt = Time.deltaTime;
            _smoothTorque  = Mathf.Lerp(_smoothTorque,  Mathf.Abs(wheelController.motorTorque),          dt * 20f);
            _smoothLoad    = Mathf.Lerp(_smoothLoad,    wheelController.wheel.load,                      dt * 30f);
            _smoothLatSlip = Mathf.Lerp(_smoothLatSlip, Mathf.Abs(wheelController.sideFriction.slip),    dt * 20f);
            _smoothLngSlip = Mathf.Lerp(_smoothLngSlip, Mathf.Abs(wheelController.forwardFriction.slip), dt * 30f);

            bgImage.color       = Color.Lerp(noTorqueColor, maxTorqueColor, _smoothTorque / _maxTorque);
            loadSlider.value    = Mathf.Clamp01(_smoothLoad / 6000f);
            lngSlipSlider.value = Mathf.Clamp01(_smoothLngSlip / 0.2f);
            latSlipSlider.value = Mathf.Clamp01(_smoothLatSlip / 0.4f);
        }
    }
}