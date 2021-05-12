using System;
using UnityEngine;
using UnityEngine.Events;

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [Serializable]
    public class TrailerModule : VehicleModule
    {
        /// <summary>
        ///     True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer
        ///     attached.
        /// </summary>
        [Tooltip(
            "True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer\r\nattached.")]
        public bool attached;

        /// <summary>
        ///     If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle.
        ///     If the vehicle is towing, this is the object placed at point at which trailer will be coneected.
        /// </summary>
        [Tooltip(
            "If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle." +
            " If the vehicle is towing, this is the object placed at point at which trailer will be coneected.")]
        public Transform attachmentPoint;

        public UnityEvent onAttach = new UnityEvent();
        public UnityEvent onDetach = new UnityEvent();

        /// <summary>
        ///     Should the trailer input states be reset when trailer is detached?
        /// </summary>
        [Tooltip("    Should the trailer input states be reset when trailer is detached?")]
        public bool resetInputStatesOnDetach = true;

        /// <summary>
        ///     If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.
        /// </summary>
        [Tooltip(
            "If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.")]
        public bool synchronizeGearShifts = false;

        /// <summary>
        ///     Object that will be disabled when trailer is attached and disabled when trailer is detached.
        /// </summary>
        [Tooltip("    Object that will be disabled when trailer is attached and disabled when trailer is detached.")]
        public GameObject trailerStand;

        [NonSerialized]
        private TrailerHitchModule _trailerHitch;

        public TrailerHitchModule TrailerHitch
        {
            get { return _trailerHitch; }
            set { _trailerHitch = value; }
        }


        public override void Initialize()
        {
            vc.input.autoSetInput = false;
            vc.freezeWhileIdle = false;
            vc.freezeWhileAsleep = false;

            base.Initialize();
        }


        public override void Awake(VehicleController vc)
        {
            base.Awake(vc);
            if (onAttach == null)
            {
                onAttach = new UnityEvent();
            }

            if (onDetach == null)
            {
                onDetach = new UnityEvent();
            }
        }


        public override void Update()
        {
        }


        public override void FixedUpdate()
        {
            if (_trailerHitch == null)
            {
                return;
            }
            
            if (Active && attached)
            {
                vc.powertrain.transmission.ratio = _trailerHitch.VehicleController.powertrain.transmission.ratio; // Make sure that the ratio is the same for flip input check.
                if (synchronizeGearShifts)
                {
                    Debug.Assert(_trailerHitch.VehicleController.powertrain.transmission.ForwardGearCount == vc.powertrain.transmission.ForwardGearCount &&
                                 _trailerHitch.VehicleController.powertrain.transmission.ReverseGearCount == vc.powertrain.transmission.ReverseGearCount, 
                        "When TrailerModule.synchronizeGearShifts is enabled make sure that both truck and trailer have the same number of forward and reverse gears or" +
                        " disable this option.");
                    vc.powertrain.transmission.ShiftInto(_trailerHitch.VehicleController.powertrain.transmission.Gear);
                }
            }
        }


        public override ModuleCategory GetModuleCategory()
        {
            return ModuleCategory.Trailer;
        }


        public void OnAttach(TrailerHitchModule trailerHitch)
        {
            _trailerHitch = trailerHitch;

            vc.Wake();

            vc.input.autoSetInput = false;
            vc.freezeWhileIdle = false;
            vc.freezeWhileAsleep = false;

            // Raise trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(false);
            }

            attached = true;

            onAttach.Invoke();
        }


        public void OnDetach()
        {
            if (resetInputStatesOnDetach)
            {
                vc.input.states.Reset();
            }

            vc.input.autoSetInput = false;


            // Lower trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(true);
            }

            // Assume trailer does not have a power source, cut lights.
            vc.effectsManager.lightsManager.Disable();

            _trailerHitch = null;
            vc.Sleep();

            attached = false;

            onDetach.Invoke();
        }
    }
}