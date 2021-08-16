using System;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.WheelController3D;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public class WheelComponent : PowertrainComponent
    {
        // Cached values
        [ShowInTelemetry]
        [Tooltip("Cached values")]
        public int surfaceMapIndex = -1;

        public SurfacePreset   surfacePreset;
        public WheelController wheelController;

        [NonSerialized]
        public WheelGroup wheelGroup;

        public WheelGroupSelector wheelGroupSelector = new WheelGroupSelector();

        private float _wheelInertia;
        private bool  _initiallySingleRay;

        /// <summary>
        ///     GameObject cointaining WheelController component.
        /// </summary>
        public GameObject ControllerGO
        {
            get { return wheelController.gameObject; }
        }

        /// <summary>
        ///     Transform to which the wheel controller is attached.
        /// </summary>
        public Transform ControllerTransform
        {
            get { return wheelController.cachedTransform; }
        }

        /// <summary>
        ///     Damage that the wheel has suffered so far.
        /// </summary>
        public float Damage
        {
            get { return wheelController.Damage; }
            set { wheelController.Damage = value; }
        }

        /// <summary>
        ///     Random steer direction of a damaged wheel. Depending on the amount of the damage vehicle has received this
        ///     value will be multiplied by the steer angle making the wheel gradually point more and more in a random direction
        ///     drastically worsening the handling.
        /// </summary>
        public float DamageSteerDirection { get; private set; }

        /// <summary>
        ///     True if lateral slip is larger than side slip threshold.
        /// </summary>
        public bool HasLateralSlip
        {
            get { return NormalizedLateralSlip > vc.lateralSlipThreshold; }
        }

        /// <summary>
        ///     True if longitudinal slip is larger than longitudinal slip threshold.
        /// </summary>
        public bool HasLongitudinalSlip
        {
            get { return NormalizedLongitudinalSlip > vc.longitudinalSlipThreshold; }
        }

        /// <summary>
        ///     True if wheel is touching any object.
        /// </summary>
        public bool IsGrounded
        {
            get { return wheelController.isGrounded; }
        }

        public float LateralSlip
        {
            get { return wheelController.sideFriction.slip; }
        }

        public float LongitudinalSlip
        {
            get { return wheelController.forwardFriction.slip; }
        }

        /// <summary>
        ///     Lateral slip of the wheel.
        /// </summary>
        public float NormalizedLateralSlip
        {
            get
            {
                float value = wheelController.sideFriction.slip < 0
                                  ? -wheelController.sideFriction.slip
                                  : wheelController.sideFriction.slip;
                return value < 0 ? 0 : value > 1 ? 1 : value;
            }
        }

        /// <summary>
        ///     Longitudinal slip percentage where 1 represents slip equal to forward slip threshold.
        /// </summary>
        public float NormalizedLongitudinalSlip
        {
            get
            {
                float value = wheelController.forwardFriction.slip < 0
                                  ? -wheelController.forwardFriction.slip
                                  : wheelController.forwardFriction.slip;
                return value < 0 ? 0 : value > 1 ? 1 : value;
            }
        }

        /// <summary>
        ///     Distance from top to bottom of spring travel.
        /// </summary>
        public float SpringTravel
        {
            get { return wheelController.springCompression * wheelController.springLength; }
        }

        /// <summary>
        ///     Steer angle of the wheel in degrees.
        /// </summary>
        public float SteerAngle
        {
            get { return wheelController.steerAngle; }
            set { wheelController.steerAngle = value; }
        }

        /// <summary>
        ///     Wheel width.
        /// </summary>
        public float Width
        {
            get { return wheelController.width; }
        }


        public override void Initialize(VehicleController vc)
        {
            base.Initialize(vc);

            inertia = _wheelInertia =
                          0.5f * wheelController.wheel.mass * wheelController.wheel.radius *
                          wheelController.wheel.radius;
            Debug.Assert(inertia > 0.00001f, $"Inertia too low for {name}.");
            DamageSteerDirection = Random.Range(-1f, 1f);

            if (vc.VehicleMultiplayerInstanceType == Vehicle.MultiplayerInstanceType.Local)
            {
                if (componentInputIsNull)
                {
                    wheelController.UseInternalUpdate();
                }
                else
                {
                    wheelController.UseExternalUpdate();
                }
            }

            _initiallySingleRay = wheelController.singleRay;
        }


        public override void OnPrePhysicsSubstep(float t, float dt)
        {
            if (wheelController.UsingExternalUpdate)
            {
                wheelController.OnPrePhysicsSubstep(t, dt);
            }
        }


        public override void OnPostPhysicsSubstep(float t, float dt)
        {
            if (wheelController.UsingExternalUpdate)
            {
                wheelController.OnPostPhysicsSubstep(t, dt);
            }
        }


        /// <summary>
        ///     Activates the wheel after it has been suspended by turning off single ray mode. If the wheel is
        ///     in single ray mode by default it will be left on.
        /// </summary>
        public override void OnEnable()
        {
            base.OnEnable();

            wheelController.singleRay = _initiallySingleRay;
            if (vc.VehicleMultiplayerInstanceType == Vehicle.MultiplayerInstanceType.Local)
            {
                if (componentInputIsNull)
                {
                    wheelController.UseInternalUpdate();
                }
                else
                {
                    wheelController.UseExternalUpdate();
                }
            }
        }


        /// <summary>
        ///     Turns on single ray mode to prevent unnecessary raycasting for inactive wheels / vehicles.
        /// </summary>
        public override void OnDisable()
        {
            base.OnDisable();
            
            wheelController.singleRay = true;
            if (vc.VehicleMultiplayerInstanceType == Vehicle.MultiplayerInstanceType.Local)
            {
                wheelController.UseInternalUpdate();
            }
        }

        public override void Validate(VehicleController vc)
        {
            base.Validate(vc);

            if (wheelController == null)
            {
                Debug.LogWarning("WheelController not set.");
                return;
            }

            // Check if wheel actually belongs to this vehicle
            if (wheelController.parent.GetInstanceID() != vc.gameObject.GetInstanceID())
            {
                Debug.LogError(
                    $"Wheel {wheelController.name} on vehicle {vc.name} belongs to {wheelController.parent.name}." +
                    " Make sure that you reassign the wheels when copying the script from one vehicle to another.");
            }

            Debug.Assert(wheelController.Visual != null,
                         $"{wheelController.name}: Visual is null. Assign the wheel model to the Visual field of WheelController.");
            Debug.Assert(wheelController.radius > 0, $"{wheelController.name}: Wheel radius must be positive.");
            Debug.Assert(wheelController.width > 0,  $"{wheelController.name}: Wheel width must be positive.");
            Debug.Assert(wheelController.mass > 0,   $"{wheelController.name}: Wheel mass must be positive.");
            Debug.Assert(wheelController.parent != null,
                         $"{wheelController.name}: Parent of WheelController {name} not assigned");
            Debug.Assert(vc.gameObject.GetInstanceID() == wheelController.parent.gameObject.GetInstanceID(),
                         $"{wheelController.name}: Parent object and the" +
                         " vehicle this wheel is attached to are not the same GameObject." +
                         " This could happen if you copied over VehicleController from one vehicle to another without reassigning the wheels.");
            Debug.Assert(wheelController.spring.maxForce > 0, $"{wheelController.name}: Spring force must be positive");
            Debug.Assert(wheelController.spring.forceCurve.keys.Length > 1,
                         $"{wheelController.name}: Spring curve not set up.");
            Debug.Assert(wheelController.damper.bumpForce > 0f,
                         $"{wheelController.name}: Bump force must be positive.");
            Debug.Assert(wheelController.damper.reboundForce > 0f,
                         $"{wheelController.name}: Rebound force must be positive.");
            Debug.Assert(wheelController.damper.bumpCurve.keys.Length > 1,
                         $"{wheelController.name}: Damper curve not set up");
            Debug.Assert(wheelController.activeFrictionPreset != null,
                         $"{wheelController.name}: Active friction preset not assigned (null).");
        }


        /// <summary>
        ///     Adds brake torque to the wheel on top of the existing torque. Value is clamped to max brake torque.
        /// </summary>
        /// <param name="torque">Torque in Nm that will be applied to the wheel to slow it down.</param>
        /// <param name="registerAsBraking">If true brakes.IsBraking flag will be set. This triggers brake lights.</param>
        public void AddBrakeTorque(float torque, bool isHandbrake = false)
        {
            if (wheelGroup != null)
            {
                torque *= isHandbrake ? wheelGroup.handbrakeCoefficient : wheelGroup.brakeCoefficient;
            }

            if (torque < 0)
            {
                wheelController.brakeTorque += 0f;
            }
            else
            {
                wheelController.brakeTorque += torque;
            }

            if (wheelController.brakeTorque > vc.brakes.maxTorque)
            {
                wheelController.brakeTorque = vc.brakes.maxTorque;
            }

            if (wheelController.brakeTorque < 0)
            {
                wheelController.brakeTorque = 0;
            }
        }


        public override float QueryAngularVelocity(float inputAngularVelocity, float dt)
        {
            return angularVelocity;
        }


        public override float QueryInertia()
        {
            return _wheelInertia;
        }


        public override float ForwardStep(float torque, float inertiaSum, float t, float dt, int i)
        {
            wheelController.motorTorque   = torque;
            wheelController.wheel.inertia = inertiaSum;

            wheelController.OnPhysicsSubstep(t, dt, i);

            angularVelocity = wheelController.wheel.angularVelocity;
            return wheelController.powertrainCounterTorque;
        }


        public void SetWheelGroup(int wheelGroupIndex)
        {
            wheelGroupSelector.index = wheelGroupIndex;
        }
    }
}