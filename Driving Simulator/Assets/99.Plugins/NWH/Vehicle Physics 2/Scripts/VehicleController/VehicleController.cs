using System.Collections.Generic;
using NWH.NPhysics;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Input;
using NWH.VehiclePhysics2.Modules;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.VehiclePhysics2.Sound;
using NWH.WheelController3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Main class controlling all the other parts of the vehicle.
    /// </summary>
    [RequireComponent(typeof(NRigidbody))]
    public class VehicleController : Vehicle
    {
        public const string defaultResourcesPath = "NWH Vehicle Physics/Defaults/";

        public Brakes                          brakes          = new Brakes();
        public DamageHandler                   damageHandler   = new DamageHandler();
        public EffectManager                   effectsManager  = new EffectManager();
        public GroundDetection.GroundDetection groundDetection = new GroundDetection.GroundDetection();
        public VehicleInputHandler             input           = new VehicleInputHandler();
        public ModuleManager                   moduleManager   = new ModuleManager();
        public Powertrain.Powertrain           powertrain      = new Powertrain.Powertrain();
        public SoundManager                    soundManager    = new SoundManager();
        public Steering                        steering        = new Steering();

        /// <summary>
        ///     Position of the engine relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the engine relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 enginePosition = new Vector3(0f, 0.4f, 1.5f);

        /// <summary>
        ///     Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 exhaustPosition = new Vector3(0f, 0.1f, -2f);

        /// <summary>
        ///     Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is
        ///     lower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is\r\nlower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.")]
        public float lateralSlipThreshold = 0.2f;

        /// <summary>
        ///     Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is
        ///     lower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and
        ///     sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is\r\nlower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and sound.")]
        public float longitudinalSlipThreshold = 0.7f;

        /// <summary>
        ///     State settings for the current vehicle.
        ///     State settings determine which components are enabled or disabled, as well as which LOD they belong to.
        /// </summary>
        [Tooltip(
            "State settings for the current vehicle.\r\nState settings determine which components are enabled or disabled, as well as which LOD they belong to.")]
        public StateSettings stateSettings;

        /// <summary>
        ///     Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 transmissionPosition = new Vector3(0f, 0.2f, 0.2f);

        /// <summary>
        ///     Constrains vehicle rigidbody position and rotation so that the vehicle is fully immobile when sleeping.
        /// </summary>
        [Tooltip(
            "Constrains vehicle rigidbody position and rotation so that the vehicle is fully immobile when sleeping.")]
        public bool freezeWhileAsleep = true;

        /// <summary>
        ///     Constrains vehicle rigidbody position and rotation so that the vehicle is fully immobile when
        /// velocity is near 0 and there is no input.
        /// </summary>
        public bool freezeWhileIdle = true;

        // ************************
        // ** Sub-stepping
        // ************************

        /// <summary>
        ///     Vehicle NRigidbody (substepped version of Rigidbody).
        /// </summary>
        [Tooltip("    Vehicle NRigidbody (substepped version of Rigidbody).")]
        public NRigidbody vehicleNRigidbody;

        /// <summary>
        ///     Number of substeps when vehicle speed is
        ///     < 1m/ s.
        ///         Larger number reduces creep but decreases performance.
        /// </summary>
        [Range(1, 30)]
        [Tooltip(
            "    Number of substeps when vehicle speed is\r\n    < 1m/ s.\r\n        Larger number reduces creep but decreases performance.")]
        public int lowSpeedSubsteps = 25;

        /// <summary>
        ///     Number of physics substeps when vehicle speed is >= 1m/s.
        /// </summary>
        [Range(1, 30)]
        [Tooltip("    Number of physics substeps when vehicle speed is >= 1m/s.")]
        public int highSpeedSubsteps = 20;

        /// <summary>
        ///     Number of physics substeps when vehicle is asleep.
        /// </summary>
        [Range(1, 20)]
        [Tooltip("    Number of physics substeps when vehicle is asleep.")]
        public int asleepSubsteps = 2;

        // ************************
        // ** Physical properties
        // ************************

        /// <summary>
        ///     Mass of the vehicle in [kg].
        /// </summary>
        [Tooltip("    Mass of the vehicle in [kg].")]
        public float mass = 1400f;

        /// <summary>
        ///     Maximum angular velocity of the rigidbody. Use to prevent vehicle spinning unrealistically fast on collisions.
        ///     Can also be used to artificially limit tank's rotation speed.
        /// </summary>
        [Tooltip(
            "Maximum angular velocity of the rigidbody. Use to prevent vehicle spinning unrealistically fast on collisions.\r\nCan also be used to artificially limit tank's rotation speed.")]
        public float maxAngularVelocity = 8f;

        /// <summary>
        ///     Drag of the vehicle rigidbody.
        /// </summary>
        [Tooltip("    Drag of the vehicle rigidbody.")]
        public float drag;

        /// <summary>
        ///     Angular drag of the vehicle rigidbody.
        /// </summary>
        [Tooltip("    Angular drag of the vehicle rigidbody.")]
        public float angularDrag;

        /// <summary>
        ///     Material that will be used on all vehicle colliders
        /// </summary>
        [Tooltip("    Material that will be used on all vehicle colliders")]
        public PhysicMaterial physicsMaterial;

        /// <summary>
        ///     Center of mass of the rigidbody. Needs to be readjusted when new colliders are added.
        /// </summary>
        [Tooltip(
            "Center of mass of the rigidbody. Needs to be readjusted when new colliders are added.")]
        public Vector3 centerOfMass = Vector3.zero;

        /// <summary>
        ///     Vector by which the inertia tensor of the rigidbody will be scaled on Start().
        ///     Due to the uniform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel
        ///     off.
        ///     Use this to adjust inertia tensor values.
        /// </summary>
        [Tooltip(
            "    Vector by which the inertia tensor of the rigidbody will be scaled on Start().\r\n    Due to the unform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel\r\n    off.\r\n    Use this to adjust inertia tensor values.")]
        public Vector3 inertiaTensor = new Vector3(170f, 1640f, 1350f);


        /// <summary>
        ///     Vehicle dimensions in [m]. X - width, Y - height, Z - length.
        /// </summary>
        [Tooltip("    Vehicle dimensions in [m]. X - width, Y - height, Z - length.")]
        public Vector3 vehicleDimensions = new Vector3(1.5f, 1.5f, 4.6f);

        /// <summary>
        ///     Interpolation of the vehicle Rigidbody.
        /// </summary>
        [Tooltip("Interpolation of the vehicle Rigidbody.")]
        public RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;


        // ************************
        // ** LODs
        // ************************

        /// <summary>
        ///     Distance between camera and vehicle used for determining LOD.
        /// </summary>
        [Tooltip("    Distance between camera and vehicle used for determining LOD.")]
        public float vehicleToCamDistance;

        /// <summary>
        ///     Currently active LOD.
        /// </summary>
        [Tooltip("    Currently active LOD.")]
        public LOD activeLOD;

        /// <summary>
        ///     Currently active LOD index.
        /// </summary>
        [Tooltip("    Currently active LOD index.")]
        public int activeLODIndex;

        /// <summary>
        ///     LODs will only be updated when this value is true.
        ///     Does not affect sleep LOD.
        /// </summary>
        [Tooltip("    LODs will only be updated when this value is true.\r\n    Does not affect sleep LOD.")]
        public bool updateLODs = true;

        /// <summary>
        ///     When enabled Camera.main will be used as lod camera.
        /// </summary>
        [Tooltip("    When enabled Camera.main will be used as lod camera.")]
        public bool useCameraMainForLOD = true;

        /// <summary>
        ///     Camera from which the LOD distance will be measured.
        ///     To use Camera.main instead, set 'useCameraMainForLOD' to true instead.
        /// </summary>
        [Tooltip(
            "Camera from which the LOD distance will be measured.\r\nTo use Camera.main instead, set 'useCameraMainForLOD' to true instead.")]
        public Camera LODCamera;

        /// <summary>
        ///     Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).
        ///     For other vehicles this value will be 0.
        /// </summary>
        [Tooltip(
            "    Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).\r\n    For other vehicles this value will be 0.")]
        public float wheelbase = 4f;

        /// <summary>
        ///     Cached Time.fixedDeltaTime.
        /// </summary>
        [Tooltip("    Cached Time.fixedDeltaTime.")]
        public float fixedDeltaTime = 0.02f;

        /// <summary>
        ///     Cached Time.deltaTime;
        /// </summary>
        [Tooltip("    Cached Time.deltaTime;")]
        public float deltaTime = 0.02f;

        /// <summary>
        ///     Called after vehicle has finished initializing.
        /// </summary>
        [Tooltip("Called after vehicle has finished initializing.")]
        public UnityEvent OnVehicleInitialized = new UnityEvent();

        private Transform            _cameraTransform;
        private RigidbodyConstraints _initialRbConstraints = RigidbodyConstraints.None;
        private bool                 _constraintsApplied;
        private int                  _lodCount;
        private float                _prevAngularDrag;
        private Vector3              _prevCenterOfMass;
        private float                _prevDrag;
        private Vector3              _prevInertiaTensor;
        private float                _prevMass;
        private float                _prevMaxAngularVelocity;
        private float                _sleepTimer;

        /// <summary>
        ///     Wheel groups (i.e. axles) on this vehicle.
        /// </summary>
        public List<WheelGroup> WheelGroups
        {
            get { return powertrain.wheelGroups; }
            private set { powertrain.wheelGroups = value; }
        }

        /// <summary>
        ///     List of all wheels attached to this vehicle.
        /// </summary>
        public List<WheelComponent> Wheels
        {
            get { return powertrain.wheels; }
            private set { powertrain.wheels = value; }
        }

        /// <summary>
        ///     Position of the engine in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldEnginePosition
        {
            get { return transform.TransformPoint(enginePosition); }
        }

        /// <summary>
        ///     Position of the exhaust in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldExhaustPosition
        {
            get { return transform.TransformPoint(exhaustPosition); }
        }

        /// <summary>
        ///     Position of the transmission in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldTransmissionPosition
        {
            get { return transform.TransformPoint(transmissionPosition); }
        }


        public override void Awake()
        {
            #if NVP2_DEBUG
            Debug.Log($"Awake() [{name}]");
            #endif
            
            base.Awake();

            vehicleTransform = transform;

            vehicleNRigidbody = GetComponent<NRigidbody>();
            if (vehicleNRigidbody == null)
            {
                vehicleNRigidbody = gameObject.AddComponent<NRigidbody>();
                if (vehicleNRigidbody == null)
                {
                    Debug.LogError($"NRigidbody could not be added to object {name}.");
                }
            }

            if (OnVehicleInitialized == null)
            {
                OnVehicleInitialized = new UnityEvent();
            }

            if (onSleep == null)
            {
                onSleep = new UnityEvent();
            }

            if (onWake == null)
            {
                onWake = new UnityEvent();
            }
        }


        private void Start()
        {
            #if NVP2_DEBUG
            Debug.Log($"Start() [{name}]");
            #endif
            
            Debug.Assert(vehicleTransform != null,  "Vehicle Transform is null.");
            Debug.Assert(vehicleRigidbody != null,  "Vehicle Rigidbody is null.");
            Debug.Assert(vehicleNRigidbody != null, "Vehicle NRigidbody is null.");

            input.Awake(this);
            steering.Awake(this);
            powertrain.Awake(this);
            soundManager.Awake(this);
            effectsManager.Awake(this);
            damageHandler.Awake(this);
            brakes.Awake(this);
            groundDetection.Awake(this);
            moduleManager.Awake(this);

            ApplyInitialRigidbodyValues();
            SetMultiplayerInstanceType(_multiplayerInstanceType);
            InvokeRepeating("LODCheck", Random.Range(0.2f, 0.4f), Random.Range(0.3f, 0.5f));

            vehicleNRigidbody.OnPrePhysicsSubstep  += OnPrePhysicsSubstep;
            vehicleNRigidbody.OnPhysicsSubstep     += OnPhysicsSubstep;
            vehicleNRigidbody.OnPostPhysicsSubstep += OnPostPhysicsSubstep;

            // Put to sleep immediately after initializing 
            if (!_isAwake)
            {
                Sleep();
            }

            // Calculate wheelbase if there are 2x2 wheels
            if (powertrain.wheelGroups.Count == 2
                && powertrain.wheelGroups[0].Wheels.Count == 2
                && powertrain.wheelGroups[1].Wheels.Count == 2)
            {
                wheelbase = Vector3.Distance(
                    powertrain.wheelGroups[0].LeftWheel.wheelController.transform.position,
                    powertrain.wheelGroups[1].LeftWheel.wheelController.transform.position);
            }

            CheckComponentStates();

            #if NVP2_DEBUG
            Debug.Log("+++ Invoke OnVehicleInitialized +++");
            #endif
            OnVehicleInitialized.Invoke();
        }


        public void Update()
        {
            deltaTime = Time.deltaTime;

            CheckComponentStates();

            if (_multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                input.Update();
                effectsManager.Update();
                soundManager.Update();
                damageHandler.Update();
                moduleManager.Update();
            }
            else
            {
                effectsManager.Update();
                soundManager.Update();
            }
        }


        public void OnPrePhysicsSubstep(float t, float dt)
        {
            fixedDeltaTime = dt;

            ApplyLowSpeedFixes();

            if (_multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                input.FixedUpdate();
                brakes.FixedUpdate();
                steering.FixedUpdate();
                powertrain.OnPrePhysicsSubstep(t, dt);
                moduleManager.FixedUpdate();
            }
            else
            {
                steering.FixedUpdate();
            }
        }


        public void OnPhysicsSubstep(float t, float dt, int i)
        {
            if (_multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                powertrain.OnPhysicsSubstep(t, dt, i);
            }
        }


        public void OnPostPhysicsSubstep(float t, float dt)
        {
            if (_multiplayerInstanceType == MultiplayerInstanceType.Local)
            {
                powertrain.OnPostPhysicsSubstep(t, dt);
                vehicleNRigidbody.Substeps =
                    _isAwake ? Speed < 2 ? lowSpeedSubsteps : highSpeedSubsteps : asleepSubsteps;
            }
            else
            {
                vehicleNRigidbody.Substeps = 1;
            }
        }


        public override void Sleep()
        {
            #if NVP2_DEBUG
            Debug.Log($"Sleep() [{name}]");
            #endif
            
            _isAwake = false;

            if (stateSettings != null)
            {
                activeLODIndex = stateSettings.LODs.Count - 1;
                activeLOD      = stateSettings.LODs[activeLODIndex];
            }

            if (vehicleNRigidbody != null)
            {
                vehicleNRigidbody.Substeps = asleepSubsteps;
            }

            onSleep.Invoke();
        }


        public override void Wake()
        {
            #if NVP2_DEBUG
            Debug.Log($"Wake() [{name}]");
            #endif
            
            _isAwake = true;

            LODCheck();

            if (vehicleNRigidbody != null)
            {
                vehicleNRigidbody.Substeps = lowSpeedSubsteps;
            }

            onWake.Invoke();
        }


        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            // Draw COM
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            Gizmos.color = Color.green;

            Gizmos.color = Color.white;
            Vector3 worldComPosition = transform.TransformPoint(centerOfMass);
            Gizmos.DrawWireSphere(worldComPosition, 0.07f);
            Handles.Label(worldComPosition, new GUIContent("  CoM"));

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(WorldEnginePosition, 0.04f);
            Handles.Label(WorldEnginePosition, new GUIContent("  Engine"));

            Gizmos.DrawWireSphere(WorldTransmissionPosition, 0.04f);
            Handles.Label(WorldTransmissionPosition, new GUIContent("  Transmission"));

            Gizmos.DrawWireSphere(WorldExhaustPosition, 0.04f);
            Handles.Label(WorldExhaustPosition, new GUIContent("  Exhaust"));

            Gizmos.color = Color.white;

            // Assumes that the model is positioned on top of the XZ plane.
            Matrix4x4 initMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(transform.up * vehicleDimensions.y * 0.5f, vehicleDimensions);
            Gizmos.matrix = initMatrix;

            steering.OnDrawGizmosSelected(this);
            powertrain.OnDrawGizmosSelected(this);
            soundManager.OnDrawGizmosSelected(this);
            effectsManager.OnDrawGizmosSelected(this);
            damageHandler.OnDrawGizmosSelected(this);
            brakes.OnDrawGizmosSelected(this);
            groundDetection.OnDrawGizmosSelected(this);
            moduleManager.OnDrawGizmosSelected(this);
            #endif
        }


        /// <summary>
        ///     Calculates a center of mass of the vehicle based on wheel positions.
        ///     Returned value is good enough for general use but manual setting of COM is always recommended if possible.
        /// </summary>
        /// <returns>Center of mass of the vehicle's Rigidbody</returns>
        public void UpdateCenterOfMass()
        {
            Vector3 centerOfMass = Vector3.zero;
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            Vector3 centerPoint = Vector3.zero;
            Vector3 pointSum    = Vector3.zero;
            int     count       = 0;

            foreach (WheelController wheelController in GetComponentsInChildren<WheelController>())
            {
                pointSum += transform.InverseTransformPoint(wheelController.transform.position);
                count++;
            }

            if (count > 0)
            {
                centerOfMass =  pointSum / count;
            }

            centerOfMass -= Wheels[0].wheelController.springLength * 0.15f * transform.up;
            centerOfMass += vehicleDimensions.y * 0.05f * transform.up;
            this.centerOfMass = centerOfMass;
            vehicleRigidbody.centerOfMass = centerOfMass;
        }


        public void UpdateInertiaTensor(float xCoeff = 1f, float yCoeff = 1f, float zCoeff = 1f)
        {
            // Very very approximate as the positions of the individual components are not really known.
            // Still more correct than the Unity calculation which assumes uniform density of all colliders.
            Vector3 bodyInertia = new Vector3(
                (vehicleDimensions.y + vehicleDimensions.z) * 0.086f * mass * xCoeff,
                (vehicleDimensions.z + vehicleDimensions.x) * 0.144f * mass * yCoeff,
                (vehicleDimensions.x + vehicleDimensions.y) * 0.042f * mass * zCoeff
            );
            Vector3 wheelInertia = Vector3.zero;
            foreach (WheelComponent wheelComponent in Wheels)
            {
                Vector3 wheelLocalPos =
                    transform.InverseTransformPoint(wheelComponent.wheelController.Visual.transform.position);
                wheelInertia.x += (Mathf.Abs(wheelLocalPos.y) + Mathf.Abs(wheelLocalPos.z)) *
                                  wheelComponent.wheelController.wheel.mass;
                wheelInertia.y += (Mathf.Abs(wheelLocalPos.x) + Mathf.Abs(wheelLocalPos.z)) *
                                  wheelComponent.wheelController.wheel.mass;
                wheelInertia.z += (Mathf.Abs(wheelLocalPos.x) + Mathf.Abs(wheelLocalPos.y)) *
                                  wheelComponent.wheelController.wheel.mass;
            }

            inertiaTensor = bodyInertia + wheelInertia;
            vehicleRigidbody.inertiaTensor = inertiaTensor;
        }


        /// <summary>
        ///     True if all of the wheels are touching the ground.
        /// </summary>
        public bool IsFullyGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (!Wheels[i].IsGrounded)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        ///     True if any of the wheels are touching ground.
        /// </summary>
        public bool IsGrounded()
        {
            int wheelCount = Wheels.Count;
            for (int i = 0; i < wheelCount; i++)
            {
                if (Wheels[i].IsGrounded)
                {
                    return true;
                }
            }

            return false;
        }


        public void LODCheck()
        {
            #if NVP2_DEBUG
            Debug.Log($"LODCheck() [{name}]");
            #endif
            
            if (stateSettings == null)
            {
                return;
            }

            _lodCount = stateSettings.LODs.Count;

            if (!_isAwake && _lodCount > 0) // Vehicle is sleeping, force the highest lod
            {
                activeLODIndex = _lodCount - 1;
                activeLOD      = stateSettings.LODs[activeLODIndex];
            }
            else if (updateLODs) // Vehicle is awake, determine LOD based on distance
            {
                if (useCameraMainForLOD)
                {
                    LODCamera = Camera.main;
                }
                else
                {
                    if (LODCamera == null)
                    {
                        Debug.LogWarning(
                            "LOD camera is null. Set the LOD camera or enable 'useCameraMainForLOD' instead. Falling back to Camera.main.");
                        LODCamera = Camera.main;
                    }
                }

                if (_lodCount > 0 && LODCamera != null)
                {
                    _cameraTransform = LODCamera.transform;
                    stateSettings.LODs[_lodCount - 2].distance =
                        Mathf.Infinity; // Make sure last non-sleep LOD is always matched

                    vehicleToCamDistance = Vector3.Distance(vehicleTransform.position, _cameraTransform.position);
                    for (int i = 0; i < _lodCount - 1; i++)
                    {
                        if (stateSettings.LODs[i].distance > vehicleToCamDistance)
                        {
                            activeLODIndex = i;
                            activeLOD      = stateSettings.LODs[i];
                            break;
                        }
                    }
                }
                else
                {
                    activeLODIndex = -1;
                    activeLOD      = null;
                }
            }
        }


        public void Reset()
        {
            SetDefaults();
        }


        public void SetColliderMaterial()
        {
            if (physicsMaterial == null)
            {
                return;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.material = physicsMaterial;
            }
        }


        /// <summary>
        ///     Resets the vehicle to default state.
        ///     Sets default values for all fields and assign default objects from resources folder.
        /// </summary>
        public void SetDefaults()
        {
            #if NVP2_DEBUG
            Debug.Log($"SetDefaults() [{name}]");
            #endif
            
            ApplyInitialRigidbodyValues();

            steering.SetDefaults(this);
            powertrain.SetDefaults(this);
            soundManager.SetDefaults(this);
            effectsManager.SetDefaults(this);
            damageHandler.SetDefaults(this);
            brakes.SetDefaults(this);
            groundDetection.SetDefaults(this);
            moduleManager.SetDefaults(this);

            if (stateSettings == null)
            {
                stateSettings =
                    Resources.Load(defaultResourcesPath + "DefaultStateSettings") as StateSettings;
            }

            if (physicsMaterial == null)
            {
                physicsMaterial = Resources.Load(defaultResourcesPath + "VehicleMaterial") as PhysicMaterial;
            }

            UpdateCenterOfMass();
            UpdateInertiaTensor();
        }


        public void Validate()
        {
            #if NVP2_DEBUG
            Debug.Log($"Validate() [{name}]");
            #endif
            
            Debug.Log(
                $"{gameObject.name}: Validating VehicleController setup. If no other messages show up after this one, " +
                "the vehicle is good to go.");

            if (transform.localScale != Vector3.one)
            {
                Debug.LogWarning(
                    "VehicleController Transform scale is other than [1,1,1]. It is recommended to avoid " +
                    " scaling the vehicle parent object" +
                    " and use Scale Factor from Unity model import settings instead.");
            }

            steering.Validate(this);
            powertrain.Validate(this);
            soundManager.Validate(this);
            effectsManager.Validate(this);
            damageHandler.Validate(this);
            brakes.Validate(this);
            groundDetection.Validate(this);
            moduleManager.Validate(this);
        }


        public List<VehicleComponent> GetAllComponents()
        {
            List<VehicleComponent> components = new List<VehicleComponent>();

            components.Add(steering);
            components.Add(powertrain);
            components.Add(soundManager);
            components.Add(effectsManager);
            components.Add(damageHandler);
            components.Add(brakes);
            components.Add(groundDetection);
            components.Add(moduleManager);

            return components;
        }


        private void ApplyInitialRigidbodyValues()
        {
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
                Debug.Assert(vehicleRigidbody != null, "Vehicle does not have a Rigidbody.");
            }

            // Apply initial rigidbody values
            vehicleRigidbody.interpolation          = RigidbodyInterpolation.Interpolate;
            vehicleRigidbody.maxAngularVelocity     = maxAngularVelocity;
            // Use speculative for kinematic rigidbodies (multiplayer).
            vehicleRigidbody.collisionDetectionMode = vehicleRigidbody.isKinematic ? 
                                                          CollisionDetectionMode.ContinuousSpeculative : 
                                                          CollisionDetectionMode.Continuous;
            vehicleRigidbody.drag                   = drag;
            vehicleRigidbody.mass                   = mass;
            vehicleRigidbody.angularDrag            = angularDrag;
            vehicleRigidbody.centerOfMass           = centerOfMass;
            vehicleRigidbody.inertiaTensor          = inertiaTensor;
            vehicleRigidbody.sleepThreshold         = 0;
            vehicleRigidbody.interpolation          = interpolation;
            _initialRbConstraints                   = vehicleRigidbody.constraints;
        }


        private void ApplyLowSpeedFixes() // TODO
        {
            // Increase inertia when still to mitigate jitter at low dt.
            float angVelSqrMag = vehicleRigidbody.angularVelocity.sqrMagnitude;
            float t            = VelocityMagnitude * 0.25f + angVelSqrMag * 1.1f;
            float inertiaScale = Mathf.Lerp(4f, 1f, t);
            vehicleRigidbody.inertiaTensor = inertiaTensor * inertiaScale;
            
            // Apply constraints if needed
            if (!freezeWhileAsleep && !freezeWhileIdle)
            {
                return;
            }

            // Freeze while idle
            if (freezeWhileIdle)
            {
                float verticalInput = input.Vertical;
                float absVertInput = verticalInput < 0 ? -verticalInput : verticalInput;
                float d = absVertInput * 2f + vehicleRigidbody.velocity.magnitude * 4f + vehicleRigidbody.angularVelocity.magnitude * 4f;

                if (d < 1f)
                {
                    vehicleRigidbody.constraints =
                        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                }
                else
                {
                    vehicleRigidbody.constraints = RigidbodyConstraints.None;
                }
            }
            else
            {
                vehicleRigidbody.drag = drag;
            }
 
            
            _sleepTimer += fixedDeltaTime;
        }


        private void CheckComponentStates()
        {
            input.CheckState(activeLODIndex);
            steering.CheckState(activeLODIndex);
            powertrain.CheckState(activeLODIndex);
            soundManager.CheckState(activeLODIndex);
            effectsManager.CheckState(activeLODIndex);
            damageHandler.CheckState(activeLODIndex);
            brakes.CheckState(activeLODIndex);
            groundDetection.CheckState(activeLODIndex);
            moduleManager.CheckState(activeLODIndex);
        }


        private void OnCollisionEnter(Collision collision)
        {
            damageHandler.HandleCollision(collision);
            vehicleRigidbody.drag        = drag;
            vehicleRigidbody.angularDrag = angularDrag;
        }


        private void OnEnable()
        {
            vehicleTransform = transform;
            vehicleRigidbody = GetComponent<Rigidbody>();

            Wake();
        }


        private void OnDisable()
        {
            Sleep();
        }


        public override void SetMultiplayerInstanceType(MultiplayerInstanceType instanceType, bool isKinematic = true)
        {
            #if NVP2_DEBUG
            Debug.Log($"SetMultiplayerInstanceType() [{name}]");
            #endif
            
            _multiplayerInstanceType = instanceType;

            if (_multiplayerInstanceType == MultiplayerInstanceType.Remote)
            {
                // Speculative detection mode is the only supported mode by kinematic Rigidbody
                vehicleRigidbody.collisionDetectionMode = isKinematic
                    ? CollisionDetectionMode.ContinuousSpeculative
                    : vehicleRigidbody.collisionDetectionMode;
                vehicleRigidbody.isKinematic            = isKinematic;
                input.autoSetInput                      = false;

                foreach (WheelComponent wheelComponent in Wheels)
                {
                    wheelComponent.wheelController.visualOnlyUpdate = isKinematic;
                    wheelComponent.wheelController.UseInternalUpdate();
                }
            }
            else
            {
                vehicleRigidbody.isKinematic            = false;
                vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                foreach (WheelComponent wheelComponent in Wheels)
                {
                    wheelComponent.wheelController.visualOnlyUpdate = false;
                }
            }

            base.SetMultiplayerInstanceType(instanceType);
        }
    }
}