using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NWH.Common;
using NWH.Common.Cameras;
using NWH.Common.Utility;
using NWH.NPhysics;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.WheelController3D;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace NWH.VehiclePhysics2.SetupWizard
{
    /// <summary>
    ///     Script used to set up vehicle from a model.
    ///     Can be used through editor or called at run-time.
    ///     Requires model with separate wheels and Unity-correct scale, rotation and pivots.
    /// </summary>
    public class VehicleSetupWizard : MonoBehaviour
    {
        /// <summary>
        ///     Should a default vehicle camera and camera changer be added?
        /// </summary>
        [Tooltip("    Should a default vehicle camera and camera changer be added?")]
        public bool addCamera = true;

        /// <summary>
        ///     Should character enter/exit points be added?
        /// </summary>
        [Tooltip("    Should character enter/exit points be added?")]
        public bool addCharacterEnterExitPoints = true;

        /// <summary>
        ///     Should MeshCollider be added to bodyMeshGO?
        /// </summary>
        [Tooltip("    Should MeshCollider be added to bodyMeshGO?")]
        public bool addCollider;

        /// <summary>
        ///     GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.
        ///     It is not recommended to run the setup without any colliders being previously present as this will void inertia and
        ///     center of mass
        ///     calculations during the setup.
        /// </summary>
        [Tooltip(
            "    GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.\r\n    It is not recommended to run the setup without any colliders being previously present as this will void inertia and\r\n    center of mass\r\n    calculations during the setup.")]
        public GameObject bodyMeshGameObject;

        /// <summary>
        ///     Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.
        /// </summary>
        [Tooltip("    Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.")]
        public List<GameObject> wheelGameObjects = new List<GameObject>();

        public bool removeWizardOnComplete = true;
        
        public VehicleSetupWizardPreset preset;

        private GameObject _cameraParent;
        private GameObject _wheelControllerParent;

        // Group parents
        private GameObject _wheelParent;

        
        /// <summary>
        /// Configures vehicle to the given VehicleSetupWizardSettings.
        /// </summary>
        /// <param name="targetVC">Vehicle to configure.</param>
        /// <param name="preset">Settings with which to configure the vehicle.</param>
        public static void RunConfiguration(VehicleController targetVC, VehicleSetupWizardPreset preset)
        {
            if (preset == null)
            {
                Debug.LogError("Configuration can not be ran with null VehicleSetupWizardPreset.");
                return;
            }

            List<WheelController> wheels = targetVC.GetComponentsInChildren<WheelController>().ToList();
            if (wheels.Count == 0)
            {
                Debug.LogError("Vehicle does not have any wheels. Stopping configuration.");
                return;
            }
            
            // Physical properties
            targetVC.mass = preset.mass;
            targetVC.vehicleDimensions = new Vector3(preset.width, preset.height, preset.length);
            
            // State settings
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                targetVC.stateSettings = Resources.Load("NWH Vehicle Physics/Defaults/SemiTruckStateSettings") as StateSettings;
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.Trailer)
            {
                targetVC.stateSettings = Resources.Load("NWH Vehicle Physics/Defaults/TrailerStateSettings") as StateSettings;
            }
            
            // Powertrain
            float inertiaBase;
            switch (preset.vehicleType)
            {
                case VehicleSetupWizardPreset.VehicleType.SemiTruck: inertiaBase = 0.8f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.MonsterTruck: inertiaBase = 0.4f;
                    break;
                case VehicleSetupWizardPreset.VehicleType.OffRoad: inertiaBase = 0.3f; 
                    break;
                case VehicleSetupWizardPreset.VehicleType.SportsCar: inertiaBase = 0.12f;
                    break;
                default: inertiaBase = 0.22f;
                    break;
            }

            targetVC.powertrain.engine.inertia = inertiaBase;
            targetVC.powertrain.transmission.inertia = inertiaBase * 0.05f;
            targetVC.powertrain.clutch.inertia = inertiaBase * 0.05f;

            // Engine
            targetVC.powertrain.engine.maxPower      = preset.enginePower;
            targetVC.powertrain.engine.maxLossTorque = preset.enginePower * 0.6f;
            targetVC.powertrain.engine.revLimiterRPM = preset.engineMaxRPM * 0.94f;
            targetVC.powertrain.engine.maxRPM        = preset.engineMaxRPM;
            targetVC.powertrain.engine.forcedInduction.useForcedInduction =
                preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar;

            float enginePeakPower = 0;
            float enginePeakPowerRpm = 5000;
            targetVC.powertrain.engine.GetPeakPower(out enginePeakPower, out enginePeakPowerRpm);
            float enginePeakPowerW = UnitConverter.RPMToAngularVelocity(enginePeakPowerRpm);
            float enginePeakTorque = (enginePeakPower * 1000f) / enginePeakPowerW;
            
            // Clutch
            targetVC.powertrain.clutch.baseEngagementRPM          = preset.engineMaxRPM * 0.15f;
            targetVC.powertrain.clutch.variableEngagementRPMRange = preset.engineMaxRPM * 0.07f;

            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                targetVC.powertrain.clutch.baseEngagementRPM          = preset.engineMaxRPM * 0.2f;
                targetVC.powertrain.clutch.variableEngagementRPMRange = preset.engineMaxRPM * 0.15f;
            }
            
            targetVC.powertrain.clutch.slipTorque = enginePeakTorque * 1.3f;
            
            // Transmission
            float shiftDuration = 0.15f;
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                shiftDuration = 0.07f;
                targetVC.powertrain.transmission.TransmissionType = TransmissionComponent.Type.AutomaticSequential;
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                shiftDuration = 0.5f;
                targetVC.powertrain.transmission.TransmissionType = TransmissionComponent.Type.Automatic;
            }
            
            targetVC.powertrain.transmission.shiftDuration  = shiftDuration;
            targetVC.powertrain.transmission.postShiftBan   = 0.3f + shiftDuration;
            targetVC.powertrain.transmission.UpshiftRPM = preset.engineMaxRPM * 0.72f;
            targetVC.powertrain.transmission.DownshiftRPM = preset.engineMaxRPM * 0.2f;

            float finalGearRatio = 4f * (targetVC.Wheels[0].wheelController.radius / 0.45f);
            finalGearRatio *= preset.transmissionGearing;
            
            targetVC.powertrain.transmission.finalGearRatio = finalGearRatio;

            TransmissionGearingProfile gearingProfile;
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                gearingProfile = LoadGearingProfile("SportsCar");
            }
            else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                gearingProfile = LoadGearingProfile("SemiTruck");
            }
            else
            {
                gearingProfile = LoadGearingProfile("CompactCar");
            }
            Debug.Assert(gearingProfile != null, "Could not load gearing profile from resources.");

            targetVC.powertrain.transmission.gearingProfile = gearingProfile;

            // Drivetrain
            targetVC.brakes.maxTorque = preset.mass * 1.4f;
            
            // Assumes diff 0 is front, diff 1 is rear and diff 2 is center - this is the output from the wizard setup.
            if (targetVC.Wheels.Count == 4 && targetVC.powertrain.differentials.Count == 3)
            {
                if (preset.drivetrainConfiguration == VehicleSetupWizardPreset.DrivetrainConfiguration.FWD)
                {
                    targetVC.powertrain.differentials[2].biasAB = 0f;
                }
                else if (preset.drivetrainConfiguration == VehicleSetupWizardPreset.DrivetrainConfiguration.RWD)
                {
                    targetVC.powertrain.differentials[2].biasAB = 1f;
                }

                if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
                {
                    targetVC.powertrain.differentials[1].differentialType = DifferentialComponent.Type.ClutchLSD;
                }
                else if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.OffRoad ||
                         preset.vehicleType == VehicleSetupWizardPreset.VehicleType.MonsterTruck)
                {
                    targetVC.powertrain.differentials[0].differentialType = DifferentialComponent.Type.Locked;
                    targetVC.powertrain.differentials[1].differentialType = DifferentialComponent.Type.Locked;
                    targetVC.powertrain.differentials[2].differentialType = DifferentialComponent.Type.Locked;
                }
            }

            // Suspension
            float springLength = 0.35f;
            float springForce      = (preset.mass * 30f) / wheels.Count;
            float damperForce      = Mathf.Sqrt(springForce) * 16f;
            float slipCoeff        = 1f;
            float forceCoeff       = 1f;

            float weightCoeff = Mathf.Clamp((((preset.mass / 1500f) - 1f) * 0.07f), 0f, 0.4f);
            slipCoeff -= weightCoeff;
            forceCoeff += weightCoeff;

            springLength *= preset.suspensionTravelCoeff;
            springForce *= preset.suspensionStiffnessCoeff;
            damperForce *= preset.suspensionStiffnessCoeff;

            Debug.Assert(springLength > 0.01f);
            Debug.Assert(springForce > 0f);
            Debug.Assert(damperForce > 0f);
            Debug.Assert(slipCoeff > 0f);
            Debug.Assert(forceCoeff > 0f);

            springLength = Mathf.Clamp(springLength, 0.1f, 1f);
            
            foreach (WheelController wheelController in wheels)
            {
                wheelController.spring.maxLength                 = springLength;
                wheelController.spring.maxForce                  = springForce;
                wheelController.damper.reboundForce                     = damperForce * 1.3f;
                wheelController.damper.bumpForce                     = damperForce;
                wheelController.forwardFriction.slipCoefficient  = slipCoeff;
                wheelController.sideFriction.slipCoefficient     = slipCoeff;
                wheelController.forwardFriction.forceCoefficient = forceCoeff;
                wheelController.sideFriction.forceCoefficient    = forceCoeff;
                wheelController.mass = Mathf.Clamp(preset.mass / 1500f, 0.1f, 6f) * 20f;
            }

            float arb = preset.height * 3000;
            foreach (WheelGroup wheelGroup in targetVC.WheelGroups)
            {
                wheelGroup.antiRollBarForce = arb;
            }
            
            // Sound
            AudioClip engineRunningClip;
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SportsCar)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/SportsCar")) as AudioClip;
            }
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.MonsterTruck)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/MuscleCar")) as AudioClip;
            }
            if (preset.vehicleType == VehicleSetupWizardPreset.VehicleType.SemiTruck)
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/SemiTruck")) as AudioClip;
            }
            else
            {
                engineRunningClip = Resources.Load(GetResourcePath("Sounds/Car")) as AudioClip;
            }
            
            targetVC.soundManager.engineRunningComponent.Clip = engineRunningClip;
            targetVC.soundManager.engineRunningComponent.pitchRange = targetVC.powertrain.engine.maxRPM / 2500f;

            targetVC.UpdateCenterOfMass();
            targetVC.UpdateInertiaTensor();
            
            Debug.Log($"Vehicle configured using {preset.name} preset.");
            Debug.Log($"======== VEHICLE CONFIGURATION END ========");
        }

        private static string GetResourcePath(string name)
        {
            return $"NWH Vehicle Physics/VehicleSetupWizard/{name}";
        }
        
        private static TransmissionGearingProfile LoadGearingProfile(string name)
        {
            string path = $"NWH Vehicle Physics/VehicleSetupWizard/GearingProfile/{name}";
            return Resources.Load(path) as TransmissionGearingProfile;
        }


        /// <summary>
        ///     Sets up a vehicle from scratch. Requires only a model with proper scale, rotation and pivots.
        /// </summary>
        /// <param name="targetGO">Root GameObject of the vehicle.</param>
        /// <param name="wheelGOs">Wheel GameObjects in order: front-left, front-right, rear-left, rear-right, etc.</param>
        /// <param name="bodyMeshGO">
        ///     GameObject to which the body MeshCollider will be added. Leave null if it has already been set up.
        ///     It is not recommended to run the setup without any colliders being previously present as this will void inertia and
        ///     center of mass
        ///     calculations during the setup.
        /// </param>
        /// <param name="addCollider">Should MeshCollider be added to bodyMeshGO?</param>
        /// <param name="addCamera">Should a default vehicle camera and camera changer be added?</param>
        /// <param name="addCharacterEnterExitPoints">Should character enter/exit points be added?</param>
        /// <returns>Returns newly created VehicleController if setup is successful or null if not.</returns>
        public static VehicleController RunSetup(GameObject targetGO, List<GameObject> wheelGOs,
            GameObject bodyMeshGO = null,
            bool addCollider = true, bool addCamera = true, bool addCharacterEnterExitPoints = true)
        {
            Debug.Log("======== VEHICLE SETUP START ========");
            
            #if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(targetGO, "Run Vehicle Setup Wizard");
            #endif

            Transform transform = targetGO.transform;

            if (transform.localScale != Vector3.one)
            {
                Debug.LogWarning(
                        "Scale of a parent object should be [1,1,1] for Rigidbody and VehicleController to function properly.");

                return null;
            }

            // Set vehicle tag
            targetGO.tag = "Vehicle";

            // Add body collider
            if (bodyMeshGO != null)
            {
                MeshCollider bodyCollider = bodyMeshGO.GetComponent<MeshCollider>();
                if (bodyCollider == null)
                {
                    Debug.Log($"Adding MeshCollider to body mesh object {bodyMeshGO.name}");

                    // Add mesh collider to body mesh
                    bodyCollider        = bodyMeshGO.AddComponent<MeshCollider>();
                    bodyCollider.convex = true;

                    // Set body mesh layer to 'Ignore Raycast' to prevent wheels colliding with the vehicle itself.
                    // This is the default value, you can use other layers by changing the Ignore Layer settings under WheelController inspector.
                    Debug.Log(
                            "Setting layer of body collider to default layer 'Ignore Raycast' to prevent wheels from detecting the vehicle itself." +
                            " If you wish to use some other layer check Ignore Layer settings (WheelController inspector).");

                    bodyMeshGO.layer = 2;
                }
            }

            // Add rigidbody
            Rigidbody vehicleRigidbody = targetGO.GetComponent<Rigidbody>();
            if (vehicleRigidbody == null)
            {
                Debug.Log($"Adding Rigidbody to {targetGO.name}");

                // Add a rigidbody. No need to change rigidbody values as those are set by the VehicleController
                vehicleRigidbody = targetGO.gameObject.AddComponent<Rigidbody>();
            }

            // Add NRigidbody
            NRigidbody vehicleNRigidbody = targetGO.GetComponent<NRigidbody>();
            if (vehicleNRigidbody == null)
            {
                Debug.Log($"Adding NRigidbody to {targetGO.name}");
                
                vehicleNRigidbody = targetGO.gameObject.AddComponent<NRigidbody>();
            }

            // Create WheelController GOs and add WheelControllers
            foreach (GameObject wheelObject in wheelGOs)
            {
                if (wheelObject.GetComponent<WheelController>())
                {
                    Debug.LogWarning("The wheel object you assigned already has a WheelController component. " +
                                     "This is not allowed and the existing WheelController will be removed.");
                    DestroyImmediate(wheelObject.GetComponent<WheelController>());
                }
                
                string objName = $"{wheelObject.name}_WheelController";
                Debug.Log($"Creating new WheelController object {objName}");

                if (!transform.Find(objName))
                {
                    GameObject wcGo = new GameObject(objName);
                    wcGo.transform.SetParent(transform);

                    // Position the WheelController GO to the same position as the wheel
                    wcGo.transform.SetPositionAndRotation(wheelObject.transform.position,
                                                          wheelObject.transform.rotation);

                    // Move spring anchor to be above the wheel
                    wcGo.transform.position += transform.up * 0.2f;
                    
                    Debug.Log($"   |-> Adding WheelController to {wcGo.name}");

                    // Add WheelController
                    WheelController wheelController = wcGo.AddComponent<WheelController>();

                    // Assign visual to WheelController
                    wheelController.Visual = wheelObject;

                    // Attempt to find radius and width
                    MeshRenderer mr = wheelObject.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        float radius = mr.bounds.extents.y;
                        if (radius < 0.05f || radius > 1f)
                        {
                            Debug.LogWarning(
                                    "Detected unusual wheel radius. Adjust WheelController's radius field manually.");
                        }
                        
                        Debug.Log($"   |-> Setting radius to {radius}");

                        wheelController.wheel.radius = radius;

                        float width = mr.bounds.extents.x * 2f;
                        if (width < 0.02f || width > 1f)
                        {
                            Debug.LogWarning(
                                    "Detected unusual wheel width. Adjust WheelController's width field manually.");
                        }

                        Debug.Log($"   |-> Setting width to {width}");
                        
                        wheelController.wheel.width = width;
                    }
                    else
                    {
                        Debug.LogWarning(
                                $"Radius and width could not be auto configured. Wheel {wheelObject.name} does not contain a MeshFilter.");
                    }
                }
            }

            // Add VehicleController
            VehicleController vehicleController = targetGO.GetComponent<VehicleController>();
            if (vehicleController == null)
            {
                Debug.Log($"Adding VehicleController to {targetGO.name}");

                vehicleController = targetGO.AddComponent<VehicleController>();
                vehicleController.SetDefaults();

                // Setup vehicle controller
                vehicleController.powertrain.clutch.slipTorque =
                    vehicleController.powertrain.engine.maxPower * 2f; // Very approximate.
            }

            // Add camera
            if (addCamera)
            {
                Debug.Log("Adding CameraChanger.");

                GameObject camerasContainer = new GameObject("Cameras");
                camerasContainer.transform.SetParent(transform);
                CameraChanger cameraChanger = camerasContainer.AddComponent<CameraChanger>();
                
                Debug.Log("Adding a camera follow.");

                GameObject cameraGO = new GameObject("Vehicle Camera");
                cameraGO.transform.SetParent(camerasContainer.transform);
                Transform t = vehicleController.transform;
                cameraGO.transform.SetPositionAndRotation(t.position, t.rotation);

                Camera camera = cameraGO.AddComponent<Camera>();
                camera.fieldOfView = 80f;

                cameraGO.AddComponent<AudioListener>();

                CameraMouseDrag cameraMouseDrag = cameraGO.AddComponent<CameraMouseDrag>();
                cameraMouseDrag.target = vehicleController;
                cameraMouseDrag.tag    = "MainCamera";
            }

            if (addCharacterEnterExitPoints)
            {
                Debug.Log("Adding enter/exit points.");

                GameObject leftPoint  = new GameObject("LeftEnterExitPoint");
                GameObject rightPoint = new GameObject("RightEnterExitPoint");

                leftPoint.transform.SetParent(transform);
                rightPoint.transform.SetParent(transform);

                leftPoint.transform.position  = transform.position - transform.right;
                rightPoint.transform.position = transform.position + transform.right;

                leftPoint.tag  = "EnterExitPoint";
                rightPoint.tag = "EnterExitPoint";
            }

            // Validate setup
            Debug.Log("Validating setup.");

            // Run Validate() on VehicleController which will report if there are any problems with the setup.
            vehicleController.Validate();
        
            Debug.Log("Setup done.");
            
            Debug.Log("======== VEHICLE SETUP END ========");

            return vehicleController;
        }
    }
}