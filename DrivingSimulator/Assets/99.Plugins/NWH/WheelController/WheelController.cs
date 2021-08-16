using System;
using System.Collections.Generic;
using System.Linq;
using NWH.NPhysics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.WheelController3D
{
    /// <summary>
    ///     Main class of WheelController package that controls all the aspects of the wheel.
    /// </summary>
    [Serializable]
    public class WheelController : MonoBehaviour
    {
        /// <summary>
        ///     Side of the vehicle.
        /// </summary>
        public enum Side
        {
            Left   = -1,
            Right  = 1,
            Center = 0,
            Auto   = 2,
        }

        public bool showAdvancedSettings;

        /// <summary>
        ///     Friction implementation.
        /// </summary>
        public IFrictionModel frictionModel = new StandardFrictionModel();

        /// <summary>
        ///     Instance of the spring.
        /// </summary>
        [SerializeField]
        [Tooltip("    Instance of the spring.")]
        public Spring spring;

        /// <summary>
        ///     Instance of the damper.
        /// </summary>
        [SerializeField]
        [Tooltip("    Instance of the damper.")]
        public Damper damper;

        /// <summary>
        ///     Instance of the wheel.
        /// </summary>
        [SerializeField]
        [Tooltip("    Instance of the wheel.")]
        public Wheel wheel;

        /// <summary>
        ///     Forward (longitudinal) friction info.
        /// </summary>
        [FormerlySerializedAs("fFriction")]
        [Tooltip("    Forward (longitudinal) friction info.")]
        public Friction forwardFriction;

        /// <summary>
        ///     Side (lateral) friction info.
        /// </summary>
        [FormerlySerializedAs("sFriction")]
        [Tooltip("    Side (lateral) friction info.")]
        public Friction sideFriction;

        /// <summary>
        ///     Current active friction preset.
        /// </summary>
        [Tooltip("    Current active friction preset.")]
        public FrictionPreset activeFrictionPreset;

        /// <summary>
        ///     Should forces be applied to other rigidbodies when wheel is in contact with them?
        /// </summary>
        [Tooltip("    Should forces be applied to other rigidbodies when wheel is in contact with them?")]
        public bool applyForceToOthers;

        public bool autoSetupLayerMask = true;

        /// <summary>
        ///     Cached value of visual's transform.
        /// </summary>
        [Tooltip("    Cached value of visual's transform.")]
        public Transform cachedVisualTransform;

        /// <summary>
        ///     If set to true draws detailed debug info.
        /// </summary>
        [Tooltip("    If set to true draws detailed debug info.")]
        public bool debug;

        /// <summary>
        ///     Constant torque acting similar to brake torque.
        ///     Imitates rolling resistance.
        /// </summary>
        [Range(0, 200)]
        [Tooltip("    Constant torque acting similar to brake torque.\r\n    Imitates rolling resistance.")]
        public float dragTorque = 10f;

        /// <summary>
        ///     True if wheel touching ground.
        /// </summary>
        [Tooltip("    True if wheel touching ground.")]
        public bool hasHit = true;

        /// <summary>
        ///     Only the layers that are selected/ticked will be detected.
        ///     It is important that the vehicle is not one of them as it will self-detect itself as ground.
        /// </summary>
        public LayerMask layerMask = ~(1 << Physics.IgnoreRaycastLayer);

        /// <summary>
        ///     Root object of the vehicle.
        /// </summary>
        [SerializeField]
        [Tooltip("    Root object of the vehicle.")]
        public GameObject parent;

        /// <summary>
        ///     NRigidbody to which the forces will be applied.
        /// </summary>
        [Tooltip("    NRigidbody to which the forces will be applied.")]
        public NRigidbody parentNRigidbody;

        /// <summary>
        ///     Rigidbody to which the forces will be applied.
        /// </summary>
        [Tooltip("    Rigidbody to which the forces will be applied.")]
        public Rigidbody parentRigidbody;
        
        /// <summary>
        ///     When enabled only a single raycast is used to detect ground.
        ///     Very fast and should be used when performance is critical.
        /// </summary>
        [Tooltip(
            "When enabled only a single raycast is used to detect ground.\r\nVery fast and should be used when performance is critical.")]
        public bool singleRay;

        // Amount of torque transferred from the wheel to the chassis of the vehicle. 
        // Lower values for vehicles that have anti-squat. Vehicle with wheel fixed at center directly to the chassis would have
        // value of 1f, while depending on rear suspension configuration this value can be <0 (rear end of the vehicle rises instead of squats on accelerationMag).
        // Small amount of squat is recommended on RWD cars as this loads the rear tires more giving the vehicle more traction.
        [Range(-1, 1f)]
        [Tooltip(
            "Amount of torque transferred from the wheel to the chassis of the vehicle. \r\nLower values for vehicles that have anti-squat. Vehicle with wheel fixed at center directly to the chassis would have\r\nvalue of 1f, while depending on rear suspension configuration this value can be <0 (rear end of the vehicle rises instead of squats on accelerationMag).\r\nSmall amount of squat is recommended on RWD cars as this loads the rear tires more giving the vehicle more traction.")]
        public float squat = 0.1f;

        /// <summary>
        ///     If enabled mesh collider mimicking the shape of rim and wheel will be positioned so that wheel can not pass through
        ///     objects in case raycast does not detect the surface in time.
        /// </summary>
        [Tooltip(
            "If enabled mesh collider mimicking the shape of rim and wheel will be positioned so that wheel can not pass through\r\nobjects in case raycast does not detect the surface in time.")]
        public bool useRimCollider = true;

        /// <summary>
        ///     Side the wheel is on.
        /// </summary>
        [SerializeField]
        [Tooltip("    Side the wheel is on.")]
        public Side vehicleSide = Side.Auto;

        public int vehicleWheelCount;

        public bool visualOnlyUpdate;

        /// <summary>
        ///     Contains point in which wheel touches ground. Not valid if !isGrounded.
        /// </summary>
        [Tooltip("    Contains point in which wheel touches ground. Not valid if !isGrounded.")]
        public WheelHit wheelHit = new WheelHit();

        /// <summary>
        ///     Equal to gameObject.tranform, just cached for performance.
        /// </summary>
        public Transform cachedTransform;

        /// <summary>
        ///     The amount of torque returned by the wheel.
        ///     Under perfect grip conditions this will be equal to the torque that was put down.
        ///     While in air the value will be equal to the source torque minus torque that is result of dW of the wheel.
        /// </summary>
        public float powertrainCounterTorque;

        /// <summary>
        ///     Shape of the slip circle / ellipse. Higher value will result in more elliptic shape, 1 for circle.
        ///     Determines how lateral friction affects longitudinal friction and vice versa.
        ///     Larger values will result in less lateral friction with larger longitudinal slip (i.e. when there is wheel spin
        ///     there will be less sideways friction)..
        /// </summary>
        [Range(1f, 2f)]
        public float slipCircleShape = 1.05f;

        /// <summary>
        ///     If set to true load-related friction coefficient will be automatically calculated.
        ///     Otherwise, loadFrictionCurve is used.
        /// </summary>
        public bool curveBasedLoadCoefficient;

        /// <summary>
        ///     Curve that represents relationship between load (X-axis, N) and maximum force the tire can exert upon the surface
        ///     (Y-axis, N).
        /// </summary>
        public AnimationCurve loadFrictionCurve;

        /// <summary>
        /// Determines depenetration strength.
        /// When the wheel goes through the ground either due to high vertical velocity or due to suspension bottoming out,
        /// such as after large jumps, a force is applied to return the wheel back to the above-ground position.
        /// Too low value will result in wheels clipping through ground while a too high value will result
        /// in bounce when suspension bottoms out.
        /// </summary>
        [Range(0, 5)]
        public float depenetrationSpring = 1f;

        /// <summary>
        /// Dampens the depenetration strength to prevent it from over-reacting and causing a bounce-back.
        /// </summary>
        [Range(0, 5)]
        public float depenetrationDamping = 1f;

        /// <summary>
        ///     When true Step() will not be called each FixedUpdate().
        ///     Used when execution order is important and/or the other script is waiting on the result of Step().
        /// </summary>
        [Tooltip(
            "When true Step() will not be called each FixedUpdate().\r\nUsed when execution order is important and/or the other script is waiting on the result of Step().")]
        private bool _usingExternalUpdate;

        private Vector3    _alternateForwardNormal;
        private Quaternion _axleRotation;
        private float      _bottomOutDistance;

        private float   _boundsX, _boundsY, _boundsZ, _boundsW;
        private Vector3 _contactVelocity;

        private float   _damage;
        private float   _fixedDeltaTime;
        private Vector3 _hitPointSum = Vector3.zero;
        private int     _minDistRayIndex;
        private Vector3 _normal;
        private Vector3 _normalSum = Vector3.zero;
        private Vector3 _offsetPrecalc;

        private Vector3                     _origin;
        private Vector3                     _point;
        private Vector3                     _prevMpPosition;
        private float                       _prevRadius;
        private float                       _prevWidth;
        private NativeArray<RaycastCommand> _raycastCommands;
        private RaycastCommand[]            _raycastCommandsArray;

        // Raycast command
        private NativeArray<RaycastHit> _raycastHits;
        private RaycastHit[]            _raycastHitsArray;
        private JobHandle               _raycastJobHandle;
        private float                   _rayLength;
        private float                   _stepX, _stepY;
        private Vector3                 _surfaceForceVector;
        private Vector3                 _transformForward;
        private Vector3                 _transformPosition;
        private Vector3                 _transformRight;
        private Quaternion              _transformRotation;
        private Vector3                 _transformUp;
        private float                   _weight;
        private Vector3                 _wheelDown;

        private Quaternion _camberQuaternion;
        private bool       _initialized;

        /// <summary>
        ///     Total force applied to the vehicle during this FixedUpdate
        /// </summary>
        private Vector3 _totalForceThisUpdate;

        /// <summary>
        ///     Velocity of the surface the wheel is on.
        ///     Zero for static objects, >=0 for Rigidbodies and moving platforms.
        /// </summary>
        private Vector3 _surfaceVelocity;

        private Rigidbody _hitRigidbody;
        private bool      _hasHitARigidbody;

        private Matrix4x4 _localToWorldMatrix;
        private Matrix4x4 _worldToLocalMatrix;

        private bool _hasBeenEnabledThisFrame;

        /// <summary>
        ///     Number of raycasts in the side / lateral direction.
        /// </summary>
        [SerializeField]
        private int lateralScanResolution = 3; // number of scan planes (side-to-side)

        /// <summary>
        ///     Number of raycasts in the forward / longitudinal direction.
        /// </summary>
        [SerializeField]
        private int longitudinalScanResolution = 8; // axisResolution of the first scan pass


        private WheelHit singleWheelHit = new WheelHit();

        // Wheel rotation
        private Quaternion steerQuaternion;

        [SerializeField]
        private float suspensionForceMagnitude;

        private Quaternion totalRotation;

        /// <summary>
        ///     Array of rays and related data that are shot each frame to detect surface features.
        ///     Contains offsets, hit points, normals, etc. of each point.
        /// </summary>
        [SerializeField]
        private WheelHit[] wheelHits;

        private float _longitudinalLoadCoefficient;
        private float _lateralLoadCoefficient;
        private float _chassisTorque;
        private float _bottomOutTimer;

        /// <summary>
        ///     Read only. Use UseExternalUpdate() and UseInternalUpdate() to set state.
        ///     When using external update OnPrePhysicsSubstep and other physics related properties will not be automatically
        ///     called
        ///     but rather depend on external script to call them. Used for NWH Vehicle Physics 2.
        /// </summary>
        public bool UsingExternalUpdate
        {
            get { return _usingExternalUpdate; }
        }


        public float Damage
        {
            get { return _damage; }
            set { ApplyDamage(value); }
        }

        /// <summary>
        ///     Returns angular velocity of the wheel in radians. Multiply by wheel radius to get linear speed.
        /// </summary>
        public float angularVelocity
        {
            get { return wheel.angularVelocity; }
            set { wheel.angularVelocity = value; }
        }

        /// <summary>
        ///     Brake torque on the wheel axle. [Nm]
        ///     Must be positive (zero included).
        /// </summary>
        public float brakeTorque
        {
            get { return wheel.brakeTorque; }
            set
            {
                if (value >= 0)
                {
                    wheel.brakeTorque = value;
                }
                else
                {
                    wheel.brakeTorque = 0;
                    Debug.LogWarning("Brake torque must be positive. Received <0.");
                }
            }
        }

        /// <summary>
        ///     Camber angle of the wheel. [deg]
        ///     Negative angle means that the top of the wheel in closer to the vehicle than the bottom.
        /// </summary>
        public float camber
        {
            get { return wheel.camberAngle; }
        }

        /// <summary>
        ///     The center of the wheel, measured in the world space.
        /// </summary>
        public Vector3 center
        {
            get { return wheel.Visual.transform.position; }
        }

        /// <summary>
        ///     Bump force at 1 m/s spring velocity
        /// </summary>
        public float damperBumpForce
        {
            get { return damper.bumpForce; }
            set { damper.bumpForce = value; }
        }

        /// <summary>
        ///     Point in which spring and swingarm are in contact.
        /// </summary>
        public Vector3 springTravelPoint
        {
            get { return cachedTransform.position - cachedTransform.up * spring.length; }
        }


        /// <summary>
        ///     Spring velocity in relation to local vertical axis. [m/s]
        ///     Positive on rebound (extension), negative on bump (compression).
        /// </summary>
        public float springVelocity
        {
            get { return spring.velocity; }
        }


        /// <summary>
        ///     Damper bump curve.
        /// </summary>
        public AnimationCurve DamperBumpCurve
        {
            get { return damper.bumpCurve; }
            set { damper.bumpCurve = value; }
        }

        /// <summary>
        ///     Damper rebound curve.
        /// </summary>
        public AnimationCurve DamperReboundCurve
        {
            get { return damper.reboundCurve; }
            set { damper.reboundCurve = value; }
        }

        /// <summary>
        ///     Current damper force.
        /// </summary>
        public float damperForce
        {
            get { return damper.force; }
        }

        /// <summary>
        ///     Rebounding force at 1 m/s spring velocity
        /// </summary>
        public float damperReboundForce
        {
            get { return damper.reboundForce; }
            set { damper.reboundForce = value; }
        }

        /// <summary>
        ///     Ground scan axisResolution in forward direction.
        /// </summary>
        public int ForwardScanResolution
        {
            get { return longitudinalScanResolution; }
            set
            {
                longitudinalScanResolution = value;

                if (longitudinalScanResolution < 1)
                {
                    longitudinalScanResolution = 1;
                    Debug.LogWarning("Forward scan axisResolution must be > 0.");
                }

                InitializeScanParams();
            }
        }

        /// <summary>
        ///     Is the tractive surface touching the ground?
        ///     Returns false if vehicle tipped over / tire sidewall is in contact.
        /// </summary>
        public bool isGrounded
        {
            get { return hasHit; }
        }

        /// <summary>
        ///     Only layers with value of 1 (ticked) will get detected by the wheel.
        /// </summary>
        public LayerMask LayerMask
        {
            get { return layerMask; }

            set { layerMask = value; }
        }

        /// <summary>
        ///     Mass of the wheel. [kg]
        ///     Typical values would be in range [20, 200]
        /// </summary>
        public float mass
        {
            get { return wheel.mass; }
            set { wheel.mass = value; }
        }

        /// <summary>
        ///     Motor torque on the wheel axle. [Nm]
        ///     Can be positive or negative based on direction.
        /// </summary>
        public float motorTorque
        {
            get { return wheel.motorTorque; }
            set { wheel.motorTorque = value; }
        }

        /// <summary>
        ///     Object that follows the wheel position in everything but rotation around the axle.
        ///     Can be used for brake calipers, external fenders, etc.
        /// </summary>
        public GameObject NonRotatingVisual
        {
            get { return wheel.NonRotatingVisual; }
            set { wheel.NonRotatingVisual = value; }
        }

        /// <summary>
        ///     Returns wheel's parent object.
        /// </summary>
        public GameObject Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        ///     Returns velocity at the wheel's center position in [m/s].
        /// </summary>
        public Vector3 pointVelocity
        {
            get { return parentNRigidbody.GetPointVelocity(wheel.worldPosition); }
        }

        /// <summary>
        ///     Radius (height) of the tire. [meters]
        /// </summary>
        public float radius
        {
            get { return wheel.radius; }
            set
            {
                wheel.radius = value;
                InitializeScanParams();
            }
        }

        /// <summary>
        ///     Side offset of the rim. Positive value will result if wheel further from the vehicle. [meters]
        /// </summary>
        public float rimOffset
        {
            get { return wheel.rimOffset; }
            set { wheel.rimOffset = value; }
        }

        /// <summary>
        ///     Rotations per minute of the wheel around the axle. [RPM]
        /// </summary>
        public float rpm
        {
            get { return wheel.RPM; }
        }

        /// <summary>
        ///     Maximum extension distance of wheel suspension, measured in local space.
        ///     Same as spring.maxLength
        /// </summary>
        public float suspensionDistance
        {
            get { return spring.maxLength; }
            set { spring.maxLength = value; }
        }

        /// <summary>
        ///     Number of scan planes parallel to the wheel.
        /// </summary>
        public int SideToSideScanResolution
        {
            get { return lateralScanResolution; }
            set
            {
                lateralScanResolution = value;
                if (lateralScanResolution < 1)
                {
                    lateralScanResolution = 1;
                    Debug.LogWarning("Side to side scan axisResolution must be > 0.");
                }

                InitializeScanParams();
            }
        }

        /// <summary>
        ///     Returns vehicle speed in meters per second [m/s], multiply by 3.6 for [kph] or by 2.24 for [mph].
        /// </summary>
        public float speed
        {
            get { return forwardFriction.speed; }
        }

        /// <summary>
        ///     True when spring is fully compressed, i.e. there is no more spring travel.
        /// </summary>
        public bool springBottomedOut
        {
            get { return spring.bottomedOut; }
        }

        /// <summary>
        ///     Returns value in range [0,1] where 1 means spring is fully compressed.
        /// </summary>
        public float springCompression
        {
            get { return 1f - spring.compressionPercent; }
        }

        /// <summary>
        ///     Spring force curve in relation to spring length.
        /// </summary>
        public AnimationCurve springCurve
        {
            get { return spring.forceCurve; }
            set { spring.forceCurve = value; }
        }

        /// <summary>
        ///     Length of the spring when fully extended.
        /// </summary>
        public float springLength
        {
            get { return spring.maxLength; }
            set { spring.maxLength = value; }
        }

        /// <summary>
        ///     Maximum spring force. [N]
        /// </summary>
        public float springMaximumForce
        {
            get { return spring.maxForce; }
            set { spring.maxForce = value; }
        }

        /// <summary>
        ///     True when spring is fully extended.
        /// </summary>
        public bool springOverExtended
        {
            get { return spring.overExtended; }
        }

        /// <summary>
        ///     Current length (travel) of spring.
        /// </summary>
        public float springTravel
        {
            get { return spring.length; }
        }

        /// <summary>
        ///     Steer angle around the wheel's up axis (with add-ons ignored). [deg]
        /// </summary>
        public float steerAngle
        {
            get { return wheel.steerAngle; }
            set { wheel.steerAngle = value; }
        }

        /// <summary>
        ///     Current spring force. [N]
        ///     Can be written to for use in Anti-roll Bar script or similar.
        /// </summary>
        public float suspensionForce
        {
            get { return spring.force; }
            set { spring.force = value; }
        }

        /// <summary>
        ///     Returns Enum [Side] with the corresponding side of the vehicle a wheel is at [Left, Right]
        /// </summary>
        public Side VehicleSide
        {
            get { return vehicleSide; }
            set { vehicleSide = value; }
        }

        /// <summary>
        ///     Returns object that represents wheel's visual representation. Can be null in case the object is not assigned (not
        ///     mandatory).
        /// </summary>
        public GameObject Visual
        {
            get { return wheel.Visual; }
            set { wheel.Visual = value; }
        }

        /// <summary>
        ///     Width of the wheel. [meters]
        /// </summary>
        public float width
        {
            get { return wheel.width; }
            set
            {
                wheel.width = value;
                InitializeScanParams();
            }
        }

        /// <summary>
        ///     Cached value of longitudinal load coefficient calculated on PreUpdatePhysicsSubsteps that can be
        ///     used when calling IFrictionModel.Step externally.
        /// </summary>
        public float LongitudinalLoadCoefficient
        {
            get { return _longitudinalLoadCoefficient; }
            private set { _longitudinalLoadCoefficient = value; }
        }

        /// <summary>
        ///     Cached value of lateral load coefficient calculated on PreUpdatePhysicsSubsteps that can be
        ///     used when calling IFrictionModel.Step externally.
        /// </summary>
        public float LateralLoadCoefficient
        {
            get { return _lateralLoadCoefficient; }
            private set { _lateralLoadCoefficient = value; }
        }


        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            cachedTransform = transform;
            _fixedDeltaTime = Time.fixedDeltaTime;

            SetDefaults();

            // Set the world position to the position of the wheel
            if (wheel.Visual != null)
            {
                cachedVisualTransform = wheel.Visual.transform;
                wheel.worldPosition   = cachedVisualTransform.position;
                wheel.up              = cachedVisualTransform.up;
                wheel.forward         = cachedVisualTransform.forward;
                wheel.right           = cachedVisualTransform.right;
            }

            if (wheel.NonRotatingVisual != null)
            {
                wheel.nonRotatingPositionOffset =
                    wheel.Visual.transform.InverseTransformDirection(
                        wheel.NonRotatingVisual.transform.position - cachedVisualTransform.position);
            }

            // Initialize the wheel params
            wheel.Initialize(this);

            InitializeScanParams();

            // Initialize spring length to starting value.
            spring.length = spring.maxLength * 0.5f;

            _prevRadius = wheel.radius;
            _prevWidth  = wheel.width;

            vehicleWheelCount = transform.GetComponentsInChildren<WheelController>().Length;

            // Use internal update by default
            if (!_usingExternalUpdate)
            {
                _usingExternalUpdate = true; // Just to prevent UseInternalUpdate() from returning first time.
                UseInternalUpdate();
            }

            _initialized = true;
        }


        private void Awake()
        {
            Initialize();
        }


        public void OnPrePhysicsSubstep(float t, float dt)
        {
            if (!_initialized)
            {
                Initialize();
            }

            _fixedDeltaTime = dt;

            // Check if wheel dimensions have changed and if so update relevant data
            if (wheel.radius != _prevRadius || wheel.width != _prevWidth)
            {
                wheel.Initialize(this);
                InitializeScanParams();
            }

            // Spring max length of 0 can cause issues and is not a valid state.
            if (spring.maxLength < 0.0001f)
            {
                spring.maxLength = 0.0001f;
            }

            _prevRadius = wheel.radius;
            _prevWidth  = wheel.width;

            _localToWorldMatrix = cachedTransform.localToWorldMatrix;
            _worldToLocalMatrix = _localToWorldMatrix.inverse;
            _transformPosition  = cachedTransform.position;
            _transformRotation  = cachedTransform.rotation;
            _transformForward   = cachedTransform.forward;
            _transformRight     = cachedTransform.right;
            _transformUp        = cachedTransform.up;

            HitUpdate();

            if (visualOnlyUpdate)
            {
                CalculateWheelDirectionsAndRotations();
                VisualUpdate();
            }
            else
            {
                SuspensionUpdate();
                CalculateWheelDirectionsAndRotations();
                WheelUpdate();
            }

            LongitudinalLoadCoefficient = GetLongitudinalLoadCoefficient(wheel.load, curveBasedLoadCoefficient);
            LateralLoadCoefficient      = GetLateralLoadCoefficient(wheel.load, curveBasedLoadCoefficient);
        }


        /// <summary>
        ///     Runs one physics update of the wheel.
        /// </summary>
        public void OnPhysicsSubstep(float t, float dt, int i)
        {
            _fixedDeltaTime = dt;

            _transformPosition = parentNRigidbody.TransformPosition;
            _transformRotation = parentNRigidbody.TransformRotation;
            _transformForward  = parentNRigidbody.TransformForward;
            _transformRight    = parentNRigidbody.TransformRight;
            _transformUp       = parentNRigidbody.TransformUp;

            if (i > 0)
            {
                Vector3    stepPositionDelta = parentNRigidbody.StepPositionDelta;
                Quaternion stepRotationDelta = parentNRigidbody.StepRotationDelta;

                wheel.forward = stepRotationDelta * wheel.forward;
                wheel.up      = stepRotationDelta * wheel.up;
                wheel.right   = stepRotationDelta * wheel.right;

                wheel.worldPosition.x += stepPositionDelta.x;
                wheel.worldPosition.y += stepPositionDelta.y;
                wheel.worldPosition.z += stepPositionDelta.z;

                wheel.worldRotation = stepRotationDelta * wheel.worldRotation;

                wheelHit.raycastHit.point += stepPositionDelta;

                wheelHit.forwardDir  = stepRotationDelta * wheelHit.forwardDir;
                wheelHit.sidewaysDir = stepRotationDelta * wheelHit.sidewaysDir;
            }

            // Update friction
            _contactVelocity = parentNRigidbody.GetPointVelocity(wheel.worldPosition) - _surfaceVelocity;

            if (hasHit)
            {
                forwardFriction.speed = Vector3.Dot(_contactVelocity, wheelHit.forwardDir);
                sideFriction.speed    = Vector3.Dot(_contactVelocity, wheelHit.sidewaysDir);
            }
            else
            {
                forwardFriction.speed = sideFriction.speed = 0;
            }

            frictionModel.StepLongitudinal(wheel.motorTorque, wheel.brakeTorque + dragTorque, forwardFriction.speed,
                                           sideFriction.speed,
                                           ref wheel.angularVelocity, LongitudinalLoadCoefficient, _fixedDeltaTime,
                                           wheel.radius, wheel.inertia, activeFrictionPreset.Curve,
                                           activeFrictionPreset.BCDE.z,
                                           forwardFriction.forceCoefficient, forwardFriction.slipCoefficient,
                                           ref forwardFriction.slip, ref forwardFriction.force,
                                           ref powertrainCounterTorque);

            frictionModel.StepLateral(forwardFriction.speed, sideFriction.speed,
                                      LateralLoadCoefficient, _fixedDeltaTime,
                                      activeFrictionPreset.Curve, activeFrictionPreset.BCDE.z,
                                      sideFriction.forceCoefficient, sideFriction.slipCoefficient,
                                      ref sideFriction.slip, ref sideFriction.force);


            // Convert angular velocity to RPM
            wheel.RPM = wheel.angularVelocity * 9.55f;

            // Fill in WheelHit info for Unity WheelCollider compatibility
            if (hasHit)
            {
                wheelHit.forwardSlip  = forwardFriction.slip;
                wheelHit.sidewaysSlip = sideFriction.slip;
            }

            frictionModel.SlipCircle(ref forwardFriction.slip, ref sideFriction.slip, ref forwardFriction.force,
                                     ref sideFriction.force, slipCircleShape);

            if (hasHit)
            {
                _surfaceForceVector = wheelHit.sidewaysDir * sideFriction.force +
                                      wheelHit.forwardDir * forwardFriction.force;
                parentNRigidbody.AddForceAtPosition(_surfaceForceVector, wheelHit.raycastHit.point);
            }
            else
            {
                _surfaceForceVector = Vector3.zero;
            }

            // Add chassis torque
            if (squat != 0 && forwardFriction.force > 0)
            {
                _chassisTorque += forwardFriction.force * wheel.radius * squat;
            }

            _totalForceThisUpdate += _surfaceForceVector * (1f / parentNRigidbody.Substeps);
        }


        public void OnPostPhysicsSubstep(float t, float dt)
        {
            if (!_hasBeenEnabledThisFrame)
            {
                _chassisTorque += (wheel.angularVelocity - wheel.prevAngularVelocity) * wheel.baseInertia / _fixedDeltaTime;
            }

            if (_chassisTorque < -0.1f || _chassisTorque > 0.1f)
            {
                Vector3 torqueForce = wheel.forward * (_chassisTorque * 0.5f);
                parentNRigidbody.AddForceAtPosition(-torqueForce, wheel.worldPosition + wheel.up, true);
                parentNRigidbody.AddForceAtPosition(torqueForce,  wheel.worldPosition - wheel.up, true);
            }
            
            if (applyForceToOthers && hasHit && _hasHitARigidbody)
            {
                _hitRigidbody.AddForceAtPosition(-_totalForceThisUpdate, wheelHit.point);
            }
            
            _totalForceThisUpdate = Vector3.zero;
            _chassisTorque        = 0;
            
            _hasBeenEnabledThisFrame  = false;
            wheel.prevAngularVelocity = wheel.angularVelocity;
        }


        /// <summary>
        ///     Visual representation of the wheel and it's more important Vectors.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 tp = transform.position;
            
            // Draw spring travel
            Gizmos.color = Color.green;
            Vector3 forwardOffset = transform.forward * 0.07f;
            Vector3 springOffset  = transform.up * spring.maxLength;
            Gizmos.DrawLine(tp - forwardOffset, tp + forwardOffset);
            Gizmos.DrawLine(tp - springOffset - forwardOffset,
                            tp - springOffset + forwardOffset);
            Gizmos.DrawLine(tp, tp - springOffset);

            // Set dummy variables when in inspector.
            if (!Application.isPlaying)
            {
                if (wheel.Visual != null)
                {
                    wheel.worldPosition = wheel.Visual.transform.position;
                    wheel.up            = wheel.Visual.transform.up;
                    wheel.forward       = wheel.Visual.transform.forward;
                    wheel.right         = wheel.Visual.transform.right;
                }
            }

            Gizmos.DrawSphere(wheel.worldPosition, 0.02f);

            // Draw wheel
            Gizmos.color = Color.green;
            DrawWheelGizmo(wheel.radius, wheel.width, wheel.worldPosition, wheel.up, wheel.forward, wheel.right);

            if (debug && Application.isPlaying)
            {
                // Draw wheel anchor normals
                Gizmos.color = Color.red;
                Gizmos.DrawRay(new Ray(wheel.worldPosition, wheel.up));
                Gizmos.color = Color.green;
                Gizmos.DrawRay(new Ray(wheel.worldPosition, wheel.forward));
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(new Ray(wheel.worldPosition, wheel.right));
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(new Ray(wheel.worldPosition, wheel.inside));

                // Draw axle location
                if (spring.length < 0.01f)
                {
                    Gizmos.color = Color.red;
                }
                else if (spring.length > spring.maxLength - 0.01f)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.green;
                }

                if (hasHit)
                {
                    // Draw hit points
                    float weightSum = 0f;
                    float minWeight = Mathf.Infinity;
                    float maxWeight = 0f;

                    foreach (WheelHit hit in wheelHits)
                    {
                        weightSum += hit.weight;
                        if (hit.weight < minWeight)
                        {
                            minWeight = hit.weight;
                        }

                        if (hit.weight > maxWeight)
                        {
                            maxWeight = hit.weight;
                        }
                    }

                    foreach (WheelHit hit in wheelHits)
                    {
                        float t = (hit.weight - minWeight) / (maxWeight - minWeight);
                        Gizmos.color = Color.Lerp(Color.black, Color.white, t);
                        Gizmos.DrawSphere(hit.point, 0.04f);
                        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                        Gizmos.DrawLine(hit.point, hit.point + wheel.up * hit.distanceFromTire);
                    }

                    //Draw hit forward and sideways
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(wheelHit.point,
                                    wheelHit.point + wheelHit.forwardDir * (forwardFriction.force * 0.001f));
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(wheelHit.point,
                                    wheelHit.point - wheelHit.sidewaysDir * (sideFriction.force * 0.001f));

                    // Draw ground point
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(wheelHit.point, 0.04f);
                    Gizmos.DrawLine(wheelHit.point, wheelHit.point + wheelHit.normal * 1f);

                    Gizmos.color = Color.yellow;
                    Vector3 alternateNormal = (wheel.worldPosition - wheelHit.point).normalized;
                    Gizmos.DrawLine(wheelHit.point, wheelHit.point + alternateNormal * 1f);

                    // Spring travel point
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawCube(spring.targetPoint, new Vector3(0.1f, 0.1f, 0.04f));
                    
                    // Draw total force this update
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(wheelHit.point, wheelHit.point + _totalForceThisUpdate * 1e-4f);
                }
            }
        }


        /// <summary>
        ///     Calculates maximum friction that can be exerted upon the surfaced based on vertical load on the tire in
        ///     longitudinal direction.
        /// </summary>
        /// <param name="useCurve">If true loadFrictionCurve will be used instead of automatic calculation.</param>
        /// <returns>Maximum friction force in N.</returns>
        public float GetLongitudinalLoadCoefficient(float load, bool useCurve = false)
        {
            if (!useCurve)
            {
                return 11000f * (1f - Mathf.Exp(-0.00014f * load));
            }

            return loadFrictionCurve.Evaluate(load);
        }


        /// <summary>
        ///     Calculates maximum friction that can be exerted upon the surfaced based on vertical load on the tire in lateral
        ///     direction.
        /// </summary>
        /// <param name="useCurve">If true loadFrictionCurve will be used instead of automatic calculation.</param>
        /// <returns>Maximum friction force in N.</returns>
        public float GetLateralLoadCoefficient(float load, bool useCurve = false)
        {
            if (!useCurve)
            {
                return 18000f * (1f - Mathf.Exp(-0.0001f * load));
            }

            return loadFrictionCurve.Evaluate(load);
        }


        public void UseExternalUpdate()
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_usingExternalUpdate)
            {
                return;
            }

            _usingExternalUpdate = true;

            parentNRigidbody.OnPrePhysicsSubstep  -= OnPrePhysicsSubstep;
            parentNRigidbody.OnPhysicsSubstep     -= OnPhysicsSubstep;
            parentNRigidbody.OnPostPhysicsSubstep -= OnPostPhysicsSubstep;
        }


        public void UseInternalUpdate()
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (!_usingExternalUpdate)
            {
                return;
            }

            _usingExternalUpdate = false;

            parentNRigidbody.OnPrePhysicsSubstep  += OnPrePhysicsSubstep;
            parentNRigidbody.OnPhysicsSubstep     += OnPhysicsSubstep;
            parentNRigidbody.OnPostPhysicsSubstep += OnPostPhysicsSubstep;
        }


        private void OnEnable()
        {
            _hasBeenEnabledThisFrame = true;
        }


        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            OnDestroy();
        }


        /// <summary>
        ///     Sets default values if they have not already been set.
        ///     Gets called each time Reset() is called in editor - such as adding the script to a GameObject.
        /// </summary>
        /// <param name="reset">Sets default values even if they have already been set.</param>
        /// <param name="findWheelVisuals">Should script attempt to find wheel visuals automatically by name and position?</param>
        public void SetDefaults(bool reset = false, bool findWheelVisuals = true)
        {
            // Objects
            if (parent == null || reset)
            {
                parent = FindParent();
            }

            Debug.Assert(parent != null,
                         $"Parent Rigidbody of WheelController {name} could not be found. It will have to be assigned manually.");

            // Find parent Nrigidbody
            parentNRigidbody = parent.GetComponent<NRigidbody>();
            if (parentNRigidbody == null)
            {
                parentNRigidbody = parent.AddComponent<NRigidbody>();
            }

            Debug.Assert(parentNRigidbody != null, "Parent does not contain a NRigidbody.");
            
            // Find parent Rigidbody
            parentRigidbody = parent.GetComponent<Rigidbody>();
            if (parentRigidbody == null)
            {
                parentRigidbody = parent.AddComponent<Rigidbody>();
            }

            Debug.Assert(parentRigidbody != null, "Parent does not contain a Rigidbody.");

            if (frictionModel == null || reset)
            {
                frictionModel = new StandardFrictionModel();
            }

            if (wheel == null || reset)
            {
                wheel = new Wheel();
            }

            if (spring == null || reset)
            {
                spring = new Spring();
            }

            if (damper == null || reset)
            {
                damper = new Damper();
            }

            if (forwardFriction == null || reset)
            {
                forwardFriction = new Friction();
            }

            if (sideFriction == null || reset)
            {
                sideFriction = new Friction();
            }

            // Friction preset
            if (activeFrictionPreset == null || reset)
            {
                activeFrictionPreset =
                    Resources.Load<FrictionPreset>("Wheel Controller 3D/Defaults/DefaultTireFrictionPreset");
            }

            // Curves
            if (springCurve == null || springCurve.keys.Length == 0 || reset)
            {
                springCurve = GenerateDefaultSpringCurve();
            }

            if (DamperBumpCurve == null || DamperBumpCurve.keys.Length == 0 || reset)
            {
                DamperBumpCurve = GenerateDefaultDamperBumpCurve();
            }

            if (DamperReboundCurve == null || DamperReboundCurve.keys.Length == 0 || reset)
            {
                DamperReboundCurve = GenerateDefaultDamperReboundCurve();
            }

            // Side
            if (vehicleSide == Side.Auto && parent != null || reset)
            {
                vehicleSide = DetermineSide(transform.position, parent.transform);
            }

            // Attempt to find wheel visuals
            if (findWheelVisuals && wheel.Visual == null && parent != null)
            {
                Transform   thisTransform = transform;
                Transform[] children      = parent.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    Vector3 p1       = thisTransform.position;
                    Vector3 p2       = child.position;
                    float   x        = p2.x - p1.x;
                    float   z        = p2.z - p1.z;
                    float   distance = Mathf.Sqrt(x * x + z * z);

                    if (distance < 0.2f)
                    {
                        string lowerName = child.name.ToLower();
                        if ((lowerName.Contains("wheel") || lowerName.Contains("whl")) &&
                            child.GetComponent<WheelController>() == null)
                        {
                            wheel.Visual = child.gameObject;
                        }
                    }
                }
            }

            // Assign layer mask
            if (autoSetupLayerMask && parent != null)
            {
                SetupLayerMask();
            }

            // Assign default load/friction curve
            if (loadFrictionCurve == null || loadFrictionCurve.keys.Length == 0 || reset)
            {
                loadFrictionCurve = new AnimationCurve();
                for (float i = 0; i < 20000f; i += 2000f)
                {
                    loadFrictionCurve.AddKey(i, GetLongitudinalLoadCoefficient(i));
                }

                for (int i = 0; i < loadFrictionCurve.keys.Length; i++)
                {
                    loadFrictionCurve.SmoothTangents(i, 1);
                }
            }
        }


        /// <summary>
        ///     Sets up coordinates, arrays and other fields for ground detection.
        ///     Needs to be called each time a dimension of the wheel or ground detection axisResolution changes.
        /// </summary>
        public void InitializeScanParams()
        {
            // Scan start point
            _boundsX = -wheel.width / 2f;
            _boundsY = -wheel.radius;

            // Scan end point
            _boundsZ = wheel.width / 2f + 0.000001f;
            _boundsW = wheel.radius + 0.000001f;

            // Increment
            _stepX = lateralScanResolution == 1 ? 1 : wheel.width / (lateralScanResolution - 1);
            _stepY = longitudinalScanResolution == 1 ? 1 : wheel.radius * 2f / (longitudinalScanResolution - 1);

            // Initialize wheel rays
            int n = longitudinalScanResolution * lateralScanResolution;
            wheelHits = new WheelHit[n];

            int w = 0;
            for (float x = _boundsX; x <= _boundsZ; x += _stepX)
            {
                int h = 0;
                for (float y = _boundsY; y <= _boundsW; y += _stepY)
                {
                    int index = w * longitudinalScanResolution + h;

                    WheelHit wr = new WheelHit();
                    wr.angleForward    = Mathf.Asin(y / (wheel.radius + 0.000001f));
                    wr.curvatureOffset = Mathf.Cos(wr.angleForward) * wheel.radius;

                    float xOffset                           = x;
                    if (lateralScanResolution == 1)
                    {
                        xOffset = 0;
                    }

                    wr.offset        = new Vector2(xOffset, y);
                    wheelHits[index] = wr;

                    h++;
                }

                w++;
            }

            if (_raycastCommands.IsCreated)
            {
                _raycastCommands.Dispose();
            }

            if (_raycastHits.IsCreated)
            {
                _raycastHits.Dispose();
            }

            GenerateRaycastArraysIfNeeded(n);
        }


        /// <summary>
        ///     Searches for wheel hit point by iterating WheelScan() function to the requested scan depth.
        /// </summary>
        private void HitUpdate()
        {
            // Hit flag     
            float minDistance = 9999999f;
            _wheelDown = -wheel.up;

            float distanceThreshold = spring.maxLength - spring.length;
            _rayLength = wheel.radius * 1.02f + distanceThreshold;

            _offsetPrecalc.x = _transformPosition.x - _transformUp.x * spring.length + wheel.up.x * wheel.radius -
                               wheel.inside.x * wheel.rimOffset;
            _offsetPrecalc.y = _transformPosition.y - _transformUp.y * spring.length + wheel.up.y * wheel.radius -
                               wheel.inside.y * wheel.rimOffset;
            _offsetPrecalc.z = _transformPosition.z - _transformUp.z * spring.length + wheel.up.z * wheel.radius -
                               wheel.inside.z * wheel.rimOffset;

            _minDistRayIndex = -1;
            hasHit           = false;

            bool initQueriesHitTriggers  = Physics.queriesHitTriggers;
            bool initQueriesHitBackfaces = Physics.queriesHitBackfaces;

            if (autoSetupLayerMask)
            {
                Physics.queriesHitTriggers = false;
            }

            Physics.queriesHitBackfaces = false;

            if (singleRay)
            {
                singleWheelHit.valid = false;

                bool grounded = Physics.Raycast(_offsetPrecalc, _wheelDown, out singleWheelHit.raycastHit,
                                                _rayLength + wheel.radius, layerMask);

                if (grounded)
                {
                    float distanceFromTire = singleWheelHit.raycastHit.distance - wheel.radius - wheel.radius;
                    if (distanceFromTire > distanceThreshold)
                    {
                        return;
                    }

                    hasHit                          = true;
                    singleWheelHit.valid            = true;
                    singleWheelHit.distanceFromTire = distanceFromTire;

                    wheelHit.raycastHit       =  singleWheelHit.raycastHit;
                    wheelHit.angleForward     =  singleWheelHit.angleForward;
                    wheelHit.distanceFromTire =  singleWheelHit.distanceFromTire;
                    wheelHit.offset           =  singleWheelHit.offset;
                    wheelHit.weight           =  singleWheelHit.weight;
                    wheelHit.curvatureOffset  =  singleWheelHit.curvatureOffset;
                    wheelHit.groundPoint      =  wheelHit.raycastHit.point;
                    wheelHit.raycastHit.point += wheel.up * wheel.radius;
                    wheelHit.curvatureOffset  =  wheel.radius;
                }
            }
            else
            {
                int n = wheelHits.Length;
                GenerateRaycastArraysIfNeeded(n);

                for (int i = 0; i < n; i++)
                {
                    Vector3 offset = wheelHits[i].offset;
                    _origin.x = wheel.forward.x * offset.y + wheel.right.x * offset.x + _offsetPrecalc.x;
                    _origin.y = wheel.forward.y * offset.y + wheel.right.y * offset.x + _offsetPrecalc.y;
                    _origin.z = wheel.forward.z * offset.y + wheel.right.z * offset.x + _offsetPrecalc.z;

                    _raycastCommandsArray[i].from      = _origin;
                    _raycastCommandsArray[i].direction = _wheelDown;
                    _raycastCommandsArray[i].distance  = _rayLength + wheelHits[i].curvatureOffset;
                    _raycastCommandsArray[i].layerMask = layerMask;
                    _raycastCommandsArray[i].maxHits   = 1;
                }

                _raycastCommands.CopyFrom(_raycastCommandsArray);
                _raycastJobHandle = RaycastCommand.ScheduleBatch(_raycastCommands, _raycastHits, 4);
                _raycastJobHandle.Complete();
                _raycastHits.CopyTo(_raycastHitsArray);

                WheelHit tmpWheelHit;
                for (int i = 0; i < n; i++)
                {
                    tmpWheelHit       = wheelHits[i];
                    tmpWheelHit.valid = false;

                    if (_raycastHitsArray[i].distance > 0)
                    {
                        // Negative distance = point in tire, positive = point outside of tire.
                        float distanceFromTire =
                            _raycastHitsArray[i].distance - tmpWheelHit.curvatureOffset - wheel.radius;

                        // If distance is larger than available spring travel, ignore the hit.
                        if (distanceFromTire > distanceThreshold)
                        {
                            continue;
                        }

                        tmpWheelHit.valid            = true;
                        hasHit                       = true;
                        tmpWheelHit.raycastHit       = _raycastHitsArray[i];
                        tmpWheelHit.distanceFromTire = distanceFromTire;

                        if (distanceFromTire < minDistance)
                        {
                            minDistance      = distanceFromTire;
                            _minDistRayIndex = i;
                        }
                    }
                }

                if (hasHit)
                {
                    CalculateAverageWheelHit();
                }
            }

            // Friction force directions
            _surfaceVelocity = Vector3.zero;
            if (hasHit)
            {
                wheelHit.forwardDir  = Vector3.Normalize(Vector3.Cross(wheelHit.normal, -wheel.right));
                wheelHit.sidewaysDir = Quaternion.AngleAxis(90f, wheelHit.normal) * wheelHit.forwardDir;

                _hitRigidbody     = wheelHit.raycastHit.rigidbody;
                _hasHitARigidbody = _hitRigidbody != null;

                // Account for moving surfaces
                if (_hasHitARigidbody)
                {
                    _surfaceVelocity = _hitRigidbody.GetPointVelocity(wheelHit.point);
                }
            }
            else
            {
                _surfaceVelocity  = Vector3.zero;
                _hitRigidbody     = null;
                _hasHitARigidbody = false;
            }

            if (autoSetupLayerMask)
            {
                Physics.queriesHitTriggers = initQueriesHitTriggers;
            }

            Physics.queriesHitBackfaces = initQueriesHitBackfaces;
        }


        private void SuspensionUpdate()
        {
            if (hasHit)
            {
                spring.bottomedOut = spring.overExtended = false;

                // Calculate spring length from ground hit, position of the wheel and transform position.     
                spring.bottomedOut = spring.overExtended = false;

                // Calculate spring length from ground hit, position of the wheel and transform position.     
                Vector3 hitPoint  = wheelHit.raycastHit.point;
                float   rimOffset = wheel.rimOffset * (int) vehicleSide;

                if (singleRay)
                {
                    Vector3 correctedHitPoint = wheelHit.raycastHit.point - _transformUp * (wheel.radius * 0.07f);
                    spring.targetPoint.x = correctedHitPoint.x - wheel.right.x * rimOffset;
                    spring.targetPoint.y = correctedHitPoint.y - wheel.right.y * rimOffset;
                    spring.targetPoint.z = correctedHitPoint.z - wheel.right.z * rimOffset;
                }
                else
                {
                    spring.targetPoint.x = hitPoint.x - wheel.forward.x * wheelHit.offset.y
                                                      - wheel.right.x * wheelHit.offset.x - wheel.right.x * rimOffset;
                    spring.targetPoint.y = hitPoint.y - wheel.forward.y * wheelHit.offset.y
                                                      - wheel.right.y * wheelHit.offset.x - wheel.right.y * rimOffset;
                    spring.targetPoint.z = hitPoint.z - wheel.forward.z * wheelHit.offset.y
                                                      - wheel.right.z * wheelHit.offset.x - wheel.right.z * rimOffset;
                }

                spring.length = -_worldToLocalMatrix.MultiplyPoint3x4(spring.targetPoint).y;

                if (spring.length < 0f)
                {
                    _bottomOutDistance = -spring.length;
                    spring.length      = 0f;
                    spring.bottomedOut = true;
                }
                else if (spring.length > spring.maxLength)
                {
                    spring.length       = spring.maxLength;
                    spring.overExtended = true;
                }
            }
            else
            {
                // If the wheel suddenly gets in the air smoothly extend it.
                spring.length = Mathf.Lerp(spring.length, spring.maxLength, _fixedDeltaTime * 8f);
                damper.force  = 0;
            }

            if (_hasBeenEnabledThisFrame)
            {
                spring.prevLength = spring.length;
            }

            // Calculate spring velocity even when in air
            spring.velocity = (spring.length - spring.prevLength) / _fixedDeltaTime;
            spring.compressionPercent = (spring.maxLength - spring.length) / spring.maxLength;
            spring.force = hasHit ? spring.maxForce * spring.forceCurve.Evaluate(spring.compressionPercent) : 0;

            suspensionForceMagnitude =  0;
            _bottomOutTimer          += _fixedDeltaTime;
            
            if (spring.bottomedOut)
            {
                _bottomOutTimer          =  0;
                suspensionForceMagnitude += CalculateDepenetrationForce();
            }

            if (hasHit)
            {
                if (!hasHit)
                {
                    damper.force = 0;
                }

                float absSpringVel = spring.velocity < 0 ? -spring.velocity : spring.velocity;
                damper.force = spring.velocity < 0
                    ? damper.bumpForce * damper.bumpCurve.Evaluate(absSpringVel / 10f)
                    : -damper.reboundForce * damper.bumpCurve.Evaluate(absSpringVel / 10f);
                damper.force *= 10f; // Multiply by 10 to keep rough compatibility with pre-1.5.

                suspensionForceMagnitude += Mathf.Clamp(spring.force + damper.force, 0.0f, Mathf.Infinity);
            }


            spring.prevLength = spring.length;

            if (!hasHit)
            {
                return;
            }

            Vector3 surfaceNormal = wheelHit.normal;
            if (!singleRay && forwardFriction.speed < 3f)
            {
                Vector3 altNormal     = Vector3.Normalize(wheel.worldPosition - wheelHit.point);
                float   surfaceAltDot = Vector3.Dot(surfaceNormal, altNormal);
                if (surfaceAltDot < 0.99f)
                {
                    surfaceNormal = altNormal;

                    // Calculate bump force
                    Vector3 cross          = Vector3.Cross(_transformUp, surfaceNormal);
                    float   crossMagnitude = cross.magnitude;
                    float   dirSign        = Vector3.Dot(cross, wheel.right) >= 0f ? 1f : -1f;
                    Vector3 bumpForce      = wheel.forward * (dirSign * wheel.load * crossMagnitude);
                    parentNRigidbody.AddForceAtPosition(bumpForce, wheelHit.point, false);
                }
            }

            Vector3 suspensionForceVector = suspensionForceMagnitude * surfaceNormal;
            parentNRigidbody.AddForceAtPosition(suspensionForceVector,
                                                _transformPosition, false);

            _totalForceThisUpdate += suspensionForceVector;
        }


        private void WheelUpdate()
        {
            wheel.worldPosition = _transformPosition - _transformUp * spring.length - wheel.inside * wheel.rimOffset;

            // Calculate camber based on spring travel
            wheel.camberAngle = Mathf.Lerp(wheel.camberAtTop, wheel.camberAtBottom, 1f - spring.compressionPercent);

            // Tire load calculated from spring and damper force for wheelcollider compatibility
            wheel.load = Mathf.Clamp(suspensionForceMagnitude, 0.0f, Mathf.Infinity);
            if (hasHit)
            {
                wheelHit.force = wheel.load;
            }

            // Calculate visual rotation angle between 0 and 2PI radians.
            wheel.rotationAngle =
                wheel.rotationAngle % 360.0f + wheel.angularVelocity * Mathf.Rad2Deg * _fixedDeltaTime;

            _axleRotation = Quaternion.AngleAxis(wheel.rotationAngle, _transformRight);

            // Set rotation
            wheel.worldRotation = totalRotation * _axleRotation * _transformRotation;

            // Calculate position
            Vector3 newPosition =
                wheel.worldPosition + wheel.Visual.transform.TransformVector(wheel.visualPositionOffset);

            // Smooth Y movement
            // float absSpringVel = spring.velocity < 0 ? -spring.velocity : spring.velocity;
            // if (smoothMovement && hasHit && !spring.bottomedOut && !spring.overExtended)
            // {
            //     Vector3 localPos = cachedTransform.InverseTransformPoint(newPosition);
            //     wheel.smoothYPosition = Mathf.Lerp(wheel.smoothYPosition, localPos.y,
            //                                        _fixedDeltaTime * 12f + absSpringVel * 0.3f);
            //     localPos.y  = wheel.smoothYPosition;
            //     newPosition = cachedTransform.TransformPoint(localPos);
            // }

            // Calculate rotation
            Quaternion rotation = wheel.worldRotation * Quaternion.Euler(wheel.visualRotationOffset);

            // Apply position and rotation
            cachedVisualTransform.SetPositionAndRotation(newPosition, rotation);

            // Apply rotation and position to the non-rotationg objects if assigned
            if (!wheel.nonRotatingVisualIsNull)
            {
                Vector3 pos = wheel.right * wheel.nonRotatingPositionOffset.x
                              + wheel.up * wheel.nonRotatingPositionOffset.y
                              + wheel.forward * wheel.nonRotatingPositionOffset.z;
                wheel.NonRotatingVisual.transform.SetPositionAndRotation(wheel.worldPosition + pos,
                                                                         totalRotation * _transformRotation);
            }

            // Apply rotation to rim collider 
            if (useRimCollider)
            {
                float verticalOffset = -Mathf.Clamp(parentNRigidbody.velocity.y * 0.15f, -0.5f, 0f) * spring.length;
                wheel.rimColliderGO.transform.SetPositionAndRotation(wheel.worldPosition + wheel.up * verticalOffset,
                                                                     steerQuaternion * _camberQuaternion *
                                                                     _transformRotation);
            }
        }


        /// <summary>
        ///     Positions wheel to stick to the ground, does not calculate any forces at all.
        ///     Intended to be used with multiplayer for client vehicles.
        /// </summary>
        public void VisualUpdate()
        {
            spring.targetPoint = wheelHit.raycastHit.point - wheel.right * (wheel.rimOffset * (int) vehicleSide);
            spring.length      = -_worldToLocalMatrix.MultiplyPoint3x4(spring.targetPoint).y;
            spring.length      = Mathf.Clamp(spring.length, 0, spring.maxLength);
            wheel.camberAngle  = Mathf.Lerp(wheel.camberAtTop, wheel.camberAtBottom, spring.length / spring.maxLength);

            _prevMpPosition     = wheel.worldPosition;
            wheel.worldPosition = _transformPosition - _transformUp * spring.length - wheel.inside * wheel.rimOffset;
            wheel.worldRotation = totalRotation * _transformRotation;

            Vector3 wheelVelocity = (wheel.worldPosition - _prevMpPosition) / _fixedDeltaTime;
            wheel.angularVelocity = transform.InverseTransformVector(wheelVelocity).z / wheel.radius;
            wheel.rotationAngle =
                wheel.rotationAngle % 360.0f + wheel.angularVelocity * Mathf.Rad2Deg * _fixedDeltaTime;
            steerQuaternion     = Quaternion.AngleAxis(wheel.steerAngle,    _transformUp);
            _axleRotation       = Quaternion.AngleAxis(wheel.rotationAngle, _transformRight);
            wheel.worldRotation = steerQuaternion * _axleRotation * _transformRotation;

            // Apply rotation and position to visuals if assigned
            Vector3 position = wheel.worldPosition + wheel.Visual.transform.TransformVector(wheel.visualPositionOffset);
            wheel.Visual.transform.SetPositionAndRotation(position,
                                                          wheel.worldRotation);

            // Apply rotation and position to the non-rotationg objects if assigned
            if (!wheel.nonRotatingVisualIsNull)
            {
                Vector3 pos = wheel.right * wheel.nonRotatingPositionOffset.x
                              + wheel.up * wheel.nonRotatingPositionOffset.y
                              + wheel.forward * wheel.nonRotatingPositionOffset.z;
                wheel.NonRotatingVisual.transform.SetPositionAndRotation(wheel.worldPosition + pos,
                                                                         totalRotation * _transformRotation);
            }
        }


        private float CalculateDepenetrationForce()
        {
            if (spring.length > 0 || wheelHit == null)
            {
                return 0;
            }

            Vector3 point       = wheelHit.raycastHit.point;
            Vector3 normal      = wheelHit.raycastHit.normal;
            float   penetration = _bottomOutDistance;

            Rigidbody rigidbodyB       = wheelHit?.raycastHit.collider?.attachedRigidbody;
            bool      rigidbodyBIsNull = rigidbodyB == null;

            Vector3 contactVelocityVector =
                parentNRigidbody.GetPointVelocity(point) - (rigidbodyBIsNull ? Vector3.zero : rigidbodyB.GetPointVelocity(point));
            float contactVelocity = Vector3.Dot(contactVelocityVector, normal);

            float f = -Physics.gravity.y * parentRigidbody.mass;
            float j = penetration * f * depenetrationSpring * 22f 
                      + Mathf.Clamp(-contactVelocity, 0, -Physics.gravity.y) * f * depenetrationDamping;

            Vector3 fullImpulse = normal * (10f * j * (_fixedDeltaTime <= 0 ? 1f : (0.02f / _fixedDeltaTime)));
            
            // Apply force to other rigidbody. Force to the vehicle will be applied later in the suspension update.
            if (rigidbodyB != null)
            {
                rigidbodyB.AddForceAtPosition(-fullImpulse, point);
            }

            return j;
        }


        /// <summary>
        ///     Returns Raycast info of the wheel's hit.
        ///     Always check if the function returns true before using hit info
        ///     as data will only be updated when wheel is hitting the ground (isGrounded == True).
        /// </summary>
        /// <param name="h">Standard Unity RaycastHit</param>
        /// <returns></returns>
        public bool GetGroundHit(out WheelHit hit)
        {
            hit = wheelHit;
            return hasHit;
        }


        /// <summary>
        ///     Returns the position and rotation of the wheel.
        /// </summary>
        public void GetWorldPose(out Vector3 pos, out Quaternion quat)
        {
            pos  = wheel.worldPosition;
            quat = wheel.worldRotation;
        }


        /// <summary>
        ///     Sets linear camber betwen the two values.
        /// </summary>
        /// <param name="camberAtTop"></param>
        /// <param name="camberAtBottom"></param>
        public void SetCamber(float camberAtTop, float camberAtBottom)
        {
            wheel.camberAtTop    = camberAtTop;
            wheel.camberAtBottom = camberAtBottom;
        }


        /// <summary>
        ///     Sets fixed camber.
        /// </summary>
        /// <param name="camber"></param>
        public void SetCamber(float camber)
        {
            wheel.camberAtTop = wheel.camberAtBottom = camber;
        }


        private void Reset()
        {
            SetDefaults();

            if (parentNRigidbody.mass > 1.1f)
            {
                int   wheelCount     = parent.GetComponentsInChildren<WheelController>().Length;
                float gravity        = -Physics.gravity.y;
                float weightPerWheel = parentNRigidbody.mass * gravity / wheelCount;

                spring.maxForce     = weightPerWheel * 6f;
                damper.bumpForce    = weightPerWheel * 0.8f;
                damper.reboundForce = weightPerWheel * 1f;
            }
        }


        private void OnDestroy()
        {
            try
            {
                _raycastCommands.Dispose();
                _raycastHits.Dispose();
            }
            catch
            {
                // Avoids possible bug where the above commands might throw an error even if the job is finished.
            }
        }


        private void GenerateRaycastArraysIfNeeded(int size)
        {
            if (_raycastCommands == null || !_raycastCommands.IsCreated)
            {
                _raycastCommands      = new NativeArray<RaycastCommand>(size, Allocator.Persistent);
                _raycastCommandsArray = new RaycastCommand[size];
            }

            if (_raycastHits == null || !_raycastHits.IsCreated)
            {
                _raycastHits      = new NativeArray<RaycastHit>(size, Allocator.Persistent);
                _raycastHitsArray = new RaycastHit[size];
            }
        }


        private void CalculateWheelDirectionsAndRotations()
        {
            steerQuaternion   = Quaternion.AngleAxis(wheel.steerAngle,                       _transformUp);
            _camberQuaternion = Quaternion.AngleAxis(-(int) vehicleSide * wheel.camberAngle, _transformForward);
            totalRotation     = steerQuaternion * _camberQuaternion;

            wheel.up      = totalRotation * _transformUp;
            wheel.forward = totalRotation * _transformForward;
            wheel.right   = totalRotation * _transformRight;
            wheel.inside  = wheel.right * -(int) vehicleSide;
        }


        private void CalculateAverageWheelHit()
        {
            int count = 0;

            // Weighted average
            WheelHit wheelRay;
            float    n          = wheelHits.Length;
            float    minWeight  = Mathf.Infinity;
            float    maxWeight  = 0f;
            float    weightSum  = 0f;
            int      validCount = 0;

            n = wheelHits.Length;

            _hitPointSum = Vector3.zero;
            _normalSum   = Vector3.zero;
            _weight      = 0;

            float longSum   = 0;
            float latSum    = 0;
            float angleSum  = 0;
            float offsetSum = 0;
            validCount = 0;

            for (int i = 0; i < n; i++)
            {
                wheelRay = wheelHits[i];
                if (wheelRay.valid)
                {
                    _weight = (spring.maxLength - wheelRay.distanceFromTire) / spring.maxLength;
                    _weight = _weight * _weight * _weight * _weight * _weight;

                    if (_weight < minWeight)
                    {
                        minWeight = _weight;
                    }
                    else if (_weight > maxWeight)
                    {
                        maxWeight = _weight;
                    }

                    weightSum += _weight;
                    validCount++;

                    _normal = wheelRay.raycastHit.normal;
                    _point  = wheelRay.raycastHit.point;

                    _hitPointSum.x += _point.x * _weight;
                    _hitPointSum.y += _point.y * _weight;
                    _hitPointSum.z += _point.z * _weight;

                    _normalSum.x += _normal.x * _weight;
                    _normalSum.y += _normal.y * _weight;
                    _normalSum.z += _normal.z * _weight;

                    longSum   += wheelRay.offset.y * _weight;
                    latSum    += wheelRay.offset.x * _weight;
                    angleSum  += wheelRay.angleForward * _weight;
                    offsetSum += wheelRay.curvatureOffset * _weight;

                    count++;
                }
            }

            if (validCount == 0 || _minDistRayIndex < 0)
            {
                hasHit = false;
                return;
            }

            wheelHit.raycastHit        = wheelHits[_minDistRayIndex].raycastHit;
            wheelHit.raycastHit.point  = _hitPointSum / weightSum;
            wheelHit.offset.y          = longSum / weightSum;
            wheelHit.offset.x          = latSum / weightSum;
            wheelHit.angleForward      = angleSum / weightSum;
            wheelHit.raycastHit.normal = Vector3.Normalize(_normalSum / weightSum);
            wheelHit.curvatureOffset   = offsetSum / weightSum;

            wheelHit.raycastHit.point += wheel.up * wheelHit.curvatureOffset;
            wheelHit.groundPoint      =  wheelHit.raycastHit.point - wheel.up * wheelHit.curvatureOffset;
        }


        public void ApplyDamage(float damage)
        {
            _damage                      = damage < 0 ? 0 : damage > 1 ? 1 : damage;
            wheel.visualRotationOffset.z = _damage * 10f;
        }


        private GameObject FindParent()
        {
            return GetComponentInParent<Rigidbody>().gameObject;
        }


        private AnimationCurve GenerateDefaultSpringCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0.0f, 0.0f);
            ac.AddKey(1.0f, 1.0f);
            return ac;
        }


        private AnimationCurve GenerateDefaultDamperBumpCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0f, 0f);
            ac.AddKey(1f, 1f);
            return ac;
        }


        private AnimationCurve GenerateDefaultDamperReboundCurve()
        {
            AnimationCurve ac = new AnimationCurve();
            ac.AddKey(0f, 0f);
            ac.AddKey(1f, 1f);
            return ac;
        }


        private AnimationCurve GenerateDefaultLoadGripCurve()
        {
            AnimationCurve ac = new AnimationCurve
            {
                keys = new[]
                {
                    new Keyframe(0f,    0f,   0,  1f),
                    new Keyframe(0.35f, 0.6f, 1f, 1f),
                    new Keyframe(1f,    1f),
                },
            };

            return ac;
        }


        /// <summary>
        ///     Average of multiple Vector3's
        /// </summary>
        private Vector3 Vector3Average(List<Vector3> vectors)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 v in vectors)
            {
                sum += v;
            }

            return sum / vectors.Count;
        }


        /// <summary>
        ///     Calculates an angle between two vectors in relation a normal.
        /// </summary>
        /// <param name="v1">First Vector.</param>
        /// <param name="v2">Second Vector.</param>
        /// <param name="n">Angle around this vector.</param>
        /// <returns>Angle in degrees.</returns>
        private float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                       Vector3.Dot(n,  Vector3.Cross(v1, v2)),
                       Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }


        /// <summary>
        ///     Determines on what side of the vehicle a point is.
        /// </summary>
        /// <param name="pointPosition">Position of the point in question.</param>
        /// <param name="referenceTransform">Position of the reference transform.</param>
        /// <returns>Enum Side [Left,Right] (int)[-1,1]</returns>
        public Side DetermineSide(Vector3 pointPosition, Transform referenceTransform)
        {
            Vector3 relativePoint = referenceTransform.InverseTransformPoint(pointPosition);

            if (relativePoint.x < 0.0f)
            {
                return Side.Left;
            }

            return Side.Right;
        }


        /// <summary>
        ///     Determines if layer is in layermask.
        /// </summary>
        public static bool IsInLayerMask(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }


        private void SetupLayerMask()
        {
            if (parent == null)
            {
                Debug.LogError("Cannot set up layer mask for null parent.");
                return;
            }

            List<GameObject> colliderGOs = new List<GameObject>();
            GetVehicleColliders(parent.transform, ref colliderGOs);
            List<string> layers = new List<string>();
            int          n      = colliderGOs.Count;
            for (int i = 0; i < n; i++)
            {
                string layer = LayerMask.LayerToName(colliderGOs[i].layer);
                if (layers.All(l => l != layer))
                {
                    layers.Add(layer);
                }
            }

            layers.Add(LayerMask.LayerToName(2));
            layerMask = ~LayerMask.GetMask(layers.ToArray());
        }


        private void GetVehicleColliders(Transform parent, ref List<GameObject> colliderGOs)
        {
            colliderGOs = new List<GameObject>();
            foreach (Collider collider in parent.GetComponentsInChildren<Collider>())
            {
                if (collider.gameObject.layer == 0)
                {
                    collider.gameObject.layer = 2;
                }

                colliderGOs.Add(collider.gameObject);
            }
        }


        /// <summary>
        ///     Draw a wheel radius on both side of the wheel, interconected with lines perpendicular to wheel axle.
        /// </summary>
        private void DrawWheelGizmo(float radius, float width, Vector3 position, Vector3 up, Vector3 forward,
            Vector3                       right)
        {
            float   halfWidth = width / 2.0f;
            float   theta     = 0.0f;
            float   x         = radius * Mathf.Cos(theta);
            float   y         = radius * Mathf.Sin(theta);
            Vector3 pos       = position + up * y + forward * x;
            Vector3 newPos    = pos;

            for (theta = 0.0f; theta <= Mathf.PI * 2; theta += Mathf.PI / 12.0f)
            {
                x      = radius * Mathf.Cos(theta);
                y      = radius * Mathf.Sin(theta);
                newPos = position + up * y + forward * x;

                // Left line
                Gizmos.DrawLine(pos - right * halfWidth, newPos - right * halfWidth);

                // Right line
                Gizmos.DrawLine(pos + right * halfWidth, newPos + right * halfWidth);

                // Center Line
                Gizmos.DrawLine(pos - right * halfWidth, pos + right * halfWidth);

                // Diagonal
                Gizmos.DrawLine(pos - right * halfWidth, newPos + right * halfWidth);

                pos = newPos;
            }
        }
    }
}