using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using UnityEngine.Events;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Assigns brake torque to individual wheels. Actual braking happens inside WheelController.
    /// </summary>
    [Serializable]
    public class Brakes : VehicleComponent
    {
        public delegate float BrakeTorqueModifier();

        /// <summary>
        ///     Handbrake type.
        ///     - Standard - handbrake is active while held.
        ///     - Latching - first press of the button activates the handbrake while the second one releases it.
        ///     Latching handbrake also works with analog input and the strength while latched will correspond
        ///     to the highest input (i.e. it will latch on the highest notch that was reached through analog input).
        /// </summary>
        public enum HandbrakeType
        {
            Standard,
            Latching,
        }

        /// <summary>
        ///     Should brakes be applied automatically when throttle is released?
        /// </summary>
        [Tooltip("    Should brakes be applied automatically when throttle is released?")]
        public bool brakeOffThrottle;

        /// <summary>
        ///     Strength of off-throttle braking in percentage [0 to 1] of max braking torque.
        /// </summary>
        [Range(0, 1)] public float brakeOffThrottleStrength = 0.2f;

        /// <summary>
        ///     Collection of functions that modify the braking performance of the vehicle. Used for modules such as ABS where
        ///     brakes need to be overriden or their effect reduced/increase. Return 1 for neutral modifier while returning 0 will
        ///     disable the brakes completely. All brake torque modifiers will be multiplied in order to get the final brake torque
        ///     coefficient.
        /// </summary>
        [Tooltip(
            "Collection of functions that modify the braking performance of the vehicle. Used for modules such as ABS where brakes need to be overriden or their effect reduced/increase. Return 1 for neutral modifier while returning 0 will disable the brakes completely. All brake torque modifiers will be multiplied in order to get the final brake torque coefficient.")]
        public List<BrakeTorqueModifier> brakeTorqueModifiers = new List<BrakeTorqueModifier>();

        /// <summary>
        ///     Should brakes be applied when vehicle is asleep (IsAwake == false)?
        /// </summary>
        [Tooltip("    Should brakes be applied when vehicle is asleep (IsAwake == false)?")]
        public bool brakeWhileAsleep = true;

        /// <summary>
        ///     If true vehicle will break when in neutral and no throttle is applied.
        /// </summary>
        [Tooltip("    If true vehicle will break when in neutral and no throttle is applied.")]
        public bool brakeWhileIdle = true;

        /// <summary>
        ///     Max brake torque that can be applied to each wheel. To adjust braking on per-axle basis change brake coefficients
        ///     under Axle settings.
        /// </summary>
        [Tooltip("Max brake torque that can be applied to each wheel. " +
                 "To adjust braking on per-axle basis change brake coefficients under Axle settings")]
        [ShowInSettings("Brake Torque", 1000f, 10000f, 500f)]
        public float maxTorque = 3000f;

        public HandbrakeType handbrakeType = HandbrakeType.Standard;

        /// <summary>
        ///     Current value of the handbrake. 0 = inactive, 1 = maximum strength.
        ///     Handbrake strength will also be affected by per wheel group handbrake settings.
        /// </summary>
        [Tooltip(
            "    Current value of the handbrake. 0 = inactive, 1 = maximum strength.\r\n    Handbrake strength will also be affected by per wheel group handbrake settings.")]
        public float handbrakeValue;

        /// <summary>
        ///     Higher smoothing will result in brakes being applied more gradually.
        /// </summary>
        [Range(0f, 1f)]
        [ShowInSettings("Brake Smoothing")]
        [Tooltip("    Higher smoothing will result in brakes being applied more gradually.")]
        public float smoothing;

        /// <summary>
        ///     Called each time brakes are activated.
        /// </summary>
        [Tooltip("    Called each time brakes are activated.")]
        public UnityEvent onBrakesActivate = new UnityEvent();

        /// <summary>
        ///     Called each time brakes are released.
        /// </summary>
        [Tooltip("    Called each time brakes are released.")]
        public UnityEvent onBrakesDeactivate = new UnityEvent();

        /// <summary>
        ///     Is the vehicle currently braking?
        /// </summary>
        private bool _isBraking;

        /// <summary>
        ///     Was the vehicle braking the previous frame.
        /// </summary>
        private bool _wasBraking;

        private float _intensity;
        private float _intensityVelocity;
        private float _handbrakeInput;
        private bool  _handbrakeActive;
        private bool  _handbrakeWasReset;

        private float _brakeInput;
        private float _throttleInput;


        /// <summary>
        ///     Returns true if vehicle is currently braking. Will return true if there is ANY brake torque applied to the wheels.
        /// </summary>
        public bool IsBraking
        {
            get { return _isBraking; }
            set { _isBraking = value; }
        }


        public override void Awake(VehicleController vc)
        {
            base.Awake(vc);

            if (onBrakesActivate == null)
            {
                onBrakesActivate = new UnityEvent();
            }

            if (onBrakesDeactivate == null)
            {
                onBrakesDeactivate = new UnityEvent();
            }
        }


        public override void Update()
        {
        }


        public override void FixedUpdate()
        {
            _isBraking = false;

            if (!Active)
            {
                return;
            }

            // Reset brakes for this frame
            foreach (WheelComponent wc in vc.Wheels)
            {
                wc.wheelController.brakeTorque = 0;
            }

            float brakeTorqueModifier = SumBrakeTorqueModifiers();
            _brakeInput          = smoothing == 0 ? vc.input.InputSwappedBrakes :
                                       Mathf.Lerp(_brakeInput, vc.input.InputSwappedBrakes, 
                                              vc.fixedDeltaTime * 20f * (1.05f - Mathf.Clamp01(smoothing)));
            _throttleInput       = vc.input.InputSwappedThrottle;

            if (brakeTorqueModifier < vc.input.Deadzone && _brakeInput < vc.input.Deadzone)
            {
                return;
            }

            // Handbrake 
            _handbrakeInput = vc.input.Handbrake;
            if (handbrakeType == HandbrakeType.Standard)
            {
                handbrakeValue   = _handbrakeInput;
                _handbrakeActive = _handbrakeInput > vc.input.Deadzone;
            }
            else
            {
                if (_handbrakeInput < vc.input.Deadzone)
                {
                    _handbrakeWasReset = true;
                }

                if (_handbrakeInput > vc.input.Deadzone && !_handbrakeActive && _handbrakeWasReset)
                {
                    _handbrakeActive   = true;
                    _handbrakeWasReset = false;
                }

                if (_handbrakeInput > vc.input.Deadzone && _handbrakeActive && _handbrakeWasReset)
                {
                    _handbrakeActive   = false;
                    _handbrakeWasReset = false;
                }

                if (_handbrakeActive)
                {
                    handbrakeValue = _handbrakeInput > handbrakeValue ? _handbrakeInput : handbrakeValue;
                }
                else
                {
                    handbrakeValue = 0;
                }
            }

            if (handbrakeValue > vc.input.Deadzone)
            {
                AddBrakeTorqueAllWheels(handbrakeValue * brakeTorqueModifier * maxTorque, true);
            }

            // Brake off throttle
            int currentGear = vc.powertrain.transmission.Gear;
            if (brakeOffThrottle && _throttleInput < 0.05f)
            {
                AddBrakeTorqueAllWheels(brakeOffThrottleStrength * maxTorque);
                _isBraking = true;
            }

            // Brake while idle or asleep
            bool idleBrake = brakeWhileIdle && _throttleInput < vc.input.Deadzone && currentGear == 0 &&
                             vc.Speed < 0.3f;
            bool sleepBrake = brakeWhileAsleep && !vc.IsAwake;
            if (idleBrake || sleepBrake)
            {
                AddBrakeTorqueAllWheels(brakeTorqueModifier * maxTorque);
                _isBraking = true;
            }

            if (_brakeInput > vc.input.Deadzone)
            {
                AddBrakeTorqueAllWheels(_brakeInput * brakeTorqueModifier * maxTorque);
                _isBraking = true;
            }


            if (_isBraking && !_wasBraking)
            {
                onBrakesActivate.Invoke();
            }
            else if (!_isBraking && _wasBraking)
            {
                onBrakesDeactivate.Invoke();
            }

            _wasBraking = _isBraking;
        }


        public override void Disable()
        {
            base.Disable();

            _isBraking = false;
        }


        public void AddBrakeTorqueAllWheels(float brakeTorque, bool isHandbrake = false)
        {
            foreach (WheelGroup wheelGroup in vc.WheelGroups)
            {
                foreach (WheelComponent wheel in wheelGroup.Wheels)
                {
                    wheel.AddBrakeTorque(brakeTorque, isHandbrake);
                }
            }

            if (brakeTorque > 1f && !isHandbrake)
            {
                _isBraking = true;
            }
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);
            onBrakesActivate   = new UnityEvent();
            onBrakesDeactivate = new UnityEvent();
        }


        private float SumBrakeTorqueModifiers()
        {
            if (brakeTorqueModifiers.Count == 0)
            {
                return 1f;
            }

            float coefficient = 1;
            int   n           = brakeTorqueModifiers.Count;
            for (int i = 0; i < n; i++)
            {
                coefficient *= brakeTorqueModifiers[i].Invoke();
            }

            return Mathf.Clamp(coefficient, 0f, Mathf.Infinity);
        }
    }
}