using System;
using System.Collections.Generic;
using System.Linq;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
#endif


namespace NWH.VehiclePhysics2.Sound
{
    /// <summary>
    ///     Main class that manages all the sound aspects of the vehicle.
    /// </summary>
    [Serializable]
    public class SoundManager : VehicleComponent
    {
        /// <summary>
        ///     Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played
        ///     when blinker is turning off.
        /// </summary>
        [Tooltip(
            "Tick-tock sound of a working blinker. First clip is played when blinker is turning on and second clip is played when blinker is turning off.")]
        public BlinkerComponent blinkerComponent = new BlinkerComponent();

        /// <summary>
        ///     Sound of air brakes releasing air. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of air brakes releasing air. Supports multiple clips.")]
        public BrakeHissComponent brakeHissComponent = new BrakeHissComponent();

        /// <summary>
        /// Intensity of doppler effect on vehicle audio sources.
        /// </summary>
        public float dopplerLevel = 0f;
        
        /// <summary>
        ///     List of all SoundComponents.
        ///     Empty before the sound manager is initialized.
        ///     If using external sound components add them to this list so they get updated.
        /// </summary>
        [Tooltip(
            "List of all SoundComponents.\r\nEmpty before the sound manager is initialized.\r\nIf using external sound components add them to this list so they get updated.")]
        public List<SoundComponent> components = new List<SoundComponent>();

        /// <summary>
        ///     Sound of vehicle hitting other objects. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of vehicle hitting other objects. Supports multiple clips.")]
        public CrashComponent crashComponent = new CrashComponent();

        /// <summary>
        ///     Mixer group for crash sound effects.
        /// </summary>
        [Tooltip("    Mixer group for crash sound effects.")]
        public AudioMixerGroup crashMixerGroup;

        /// <summary>
        ///     GameObject containing all the crash audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the crash audio sources.")]
        public GameObject crashSourceGO;

        public EngineFanComponent engineFanComponent = new EngineFanComponent();

        public AudioMixerGroup engineMixerGroup;

        /// <summary>
        ///     Sound of engine idling.
        /// </summary>
        [Tooltip("    Sound of engine idling.")]
        public EngineRunningComponent engineRunningComponent = new EngineRunningComponent();

        /// <summary>
        ///     GameObject containing all the engine audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the engine audio sources.")]
        public GameObject engineSourceGO;

        /// <summary>
        ///     Engine start / stop component. First clip is for starting and second one is for stopping.
        /// </summary>
        [Tooltip("    Engine start / stop component. First clip is for starting and second one is for stopping.")]
        public EngineStartComponent engineStartComponent = new EngineStartComponent();

        /// <summary>
        ///     GameObject containing all the exhaust audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all the exhaust audio sources.")]
        public GameObject exhaustSourceGO;

        /// <summary>
        ///     Sound from changing gears. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound from changing gears. Supports multiple clips.")]
        public GearChangeComponent gearChangeComponent = new GearChangeComponent();

        [Tooltip("Horn sound.")]
        public HornComponent hornComponent = new HornComponent();

        /// <summary>
        ///     Sound attenuation inside vehicle.
        /// </summary>
        [Tooltip("    Sound attenuation inside vehicle.")]
        public float interiorAttenuation = -5f;

        public float lowPassFrequency = 1600f;

        [Range(0.01f, 10f)]
        public float lowPassQ = 1f;

        public AudioMixerGroup masterGroup;

        /// <summary>
        ///     Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.
        /// </summary>
        [Range(0, 2)]
        [Tooltip(
            "    Master volume of a vehicle. To adjust volume of all vehicles or their components check audio mixer.")]
        public float masterVolume = 1f;

        /// <summary>
        ///     Optional custom mixer. If left empty default will be used (VehicleAudioMixer in Resources folder).
        /// </summary>
        [Tooltip(
            "    Optional custom mixer. If left empty default will be used (VehicleAudioMixer in Resources folder).")]
        public AudioMixer mixer;

        public AudioMixerGroup otherMixerGroup;

        /// <summary>
        ///     GameObject containing all other audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all other audio sources.")]
        public GameObject otherSourceGO;

        public ReverseBeepComponent reverseBeepComponent = new ReverseBeepComponent();

        /// <summary>
        ///     Spatial blend of all audio sources. Can not be changed at runtime.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Spatial blend of all audio sources. Can not be changed at runtime.")]
        public float spatialBlend = 0.9f;

        public AudioMixerGroup surfaceNoiseMixerGroup;

        /// <summary>
        ///     Sound from wheels hitting ground and/or obstracles. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound from wheels hitting ground and/or obstracles. Supports multiple clips.")]
        public SuspensionBumpComponent suspensionBumpComponent = new SuspensionBumpComponent();

        public AudioMixerGroup suspensionMixerGroup;

        public AudioMixerGroup transmissionMixerGroup;

        /// <summary>
        ///     GameObject containing all transmission audio sources.
        /// </summary>
        [Tooltip("    GameObject containing all transmission audio sources.")]
        public GameObject transmissionSourceGO;

        /// <summary>
        ///     Transmission whine from straight cut gears or just a noisy gearbox.
        /// </summary>
        [Tooltip("    Transmission whine from straight cut gears or just a noisy gearbox.")]
        public TransmissionWhineComponent transmissionWhineComponent = new TransmissionWhineComponent();

        /// <summary>
        ///     Sound of turbo's wastegate. Supports multiple clips.
        /// </summary>
        [Tooltip("    Sound of turbo's wastegate. Supports multiple clips.")]
        public TurboFlutterComponent turboFlutterComponent = new TurboFlutterComponent();

        public AudioMixerGroup turboMixerGroup;

        /// <summary>
        ///     Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0
        ///     under engine settings.
        /// </summary>
        [Tooltip(
            "Forced induction whistle component. Can be used for air intake noise or supercharger if spool up time is set to 0 under engine settings.")]
        public TurboWhistleComponent turboWhistleComponent = new TurboWhistleComponent();

        /// <summary>
        ///     Sound produced by wheel skidding over a surface. Tire squeal.
        /// </summary>
        [Tooltip("    Sound produced by wheel skidding over a surface. Tire squeal.")]
        public WheelSkidComponent wheelSkidComponent = new WheelSkidComponent();

        /// <summary>
        ///     Sound produced by wheel rolling over a surface. Tire hum.
        /// </summary>
        [Tooltip("    Sound produced by wheel rolling over a surface. Tire hum.")]
        public WheelTireNoiseComponent wheelTireNoiseComponent = new WheelTireNoiseComponent();

        private float _originalAttenuation;
        private bool  _wasInsideVehicle;


        public override void Initialize()
        {
            if (mixer == null)
            {
                mixer = Resources.Load("Sound/VehicleAudioMixer") as AudioMixer;
            }

            Debug.Assert(mixer != null, "Audio mixer is not assigned. Assign it under Sound tab.");
            
            if (mixer != null)
            {
                masterGroup            = mixer.FindMatchingGroups("Master")[0];
                engineMixerGroup       = mixer.FindMatchingGroups("Engine")[0];
                transmissionMixerGroup = mixer.FindMatchingGroups("Transmission")[0];
                surfaceNoiseMixerGroup = mixer.FindMatchingGroups("SurfaceNoise")[0];
                turboMixerGroup        = mixer.FindMatchingGroups("Turbo")[0];
                suspensionMixerGroup   = mixer.FindMatchingGroups("Suspension")[0];
                crashMixerGroup        = mixer.FindMatchingGroups("Crash")[0];
                otherMixerGroup        = mixer.FindMatchingGroups("Other")[0];
                
                mixer.GetFloat("attenuation", out _originalAttenuation);
            }

            // Initialize individual sound components
            engineStartComponent.InitializeSoundComponent(engineMixerGroup, engineSourceGO);
            engineRunningComponent.InitializeSoundComponent(engineMixerGroup, engineSourceGO);
            engineFanComponent.InitializeSoundComponent(engineMixerGroup, engineSourceGO);
            turboWhistleComponent.InitializeSoundComponent(turboMixerGroup, engineSourceGO);
            turboFlutterComponent.InitializeSoundComponent(turboMixerGroup, engineSourceGO);
            transmissionWhineComponent.InitializeSoundComponent(transmissionMixerGroup, transmissionSourceGO);
            gearChangeComponent.InitializeSoundComponent(transmissionMixerGroup, transmissionSourceGO);
            brakeHissComponent.InitializeSoundComponent(otherMixerGroup, otherSourceGO);
            blinkerComponent.InitializeSoundComponent(otherMixerGroup, otherSourceGO);
            hornComponent.InitializeSoundComponent(otherMixerGroup, otherSourceGO);
            wheelSkidComponent.InitializeSoundComponent(surfaceNoiseMixerGroup, otherSourceGO);
            wheelTireNoiseComponent.InitializeSoundComponent(surfaceNoiseMixerGroup, otherSourceGO);
            crashComponent.InitializeSoundComponent(crashMixerGroup, crashSourceGO);
            suspensionBumpComponent.InitializeSoundComponent(suspensionMixerGroup, otherSourceGO);
            reverseBeepComponent.InitializeSoundComponent(otherMixerGroup, otherSourceGO);

            base.Initialize();
        }


        public override void Awake(VehicleController vc)
        {
            base.Awake(vc);

            GetComponentsList(ref components);
            foreach (SoundComponent component in components)
            {
                component.Awake(vc);
            }

            // Create container game objects for positional audio
            CreateSourceGO("EngineAudioSources",       vc.enginePosition,       vc.transform, ref engineSourceGO);
            CreateSourceGO("TransmissionAudioSources", vc.transmissionPosition, vc.transform, ref transmissionSourceGO);
            CreateSourceGO("ExhaustAudioSources",      vc.exhaustPosition,      vc.transform, ref exhaustSourceGO);
            CreateSourceGO("CrashAudioSources",        Vector3.zero,            vc.transform, ref crashSourceGO);
            CreateSourceGO("OtherAudioSources",        new Vector3(0, 0.2f, 0), vc.transform, ref otherSourceGO);
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }
            
            Debug.Assert(components.All(c => c != null), "SoundComponent in components list of SoundManager is null.");

            // Adjust sound if inside vehicle.
            if (!_wasInsideVehicle && vc.cameraInsideVehicle)
            {
                mixer.SetFloat("attenuation",      interiorAttenuation);
                mixer.SetFloat("lowPassFrequency", lowPassFrequency);
                mixer.SetFloat("lowPassQ",         lowPassQ);
            }
            else if (_wasInsideVehicle && !vc.cameraInsideVehicle)
            {
                mixer.SetFloat("attenuation",      _originalAttenuation);
                mixer.SetFloat("lowPassFrequency", 22000f);
                mixer.SetFloat("lowPassQ",         1f);
            }

            _wasInsideVehicle = vc.cameraInsideVehicle;

            if (vc.VehicleMultiplayerInstanceType == Vehicle.MultiplayerInstanceType.Local)
            {
                foreach (SoundComponent sc in components)
                {
                    sc.Update();
                }
            }
        }

        public override void FixedUpdate()
        {
        }


        public override void OnDrawGizmosSelected(VehicleController vc)
        {
            base.OnDrawGizmosSelected(vc);

            Gizmos.color = Color.white;

            if (components == null || components.Count == 0)
            {
                GetComponentsList(ref components);
            }

            foreach (SoundComponent component in components)
            {
                component.OnDrawGizmosSelected(vc);
            }
        }


        /// <summary>
        ///     Sets defaults to all the basic sound components when script is first added or reset is called.
        /// </summary>
        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (mixer == null)
            {
                mixer = Resources.Load<AudioMixer>(VehicleController.defaultResourcesPath +
                                                   "Sound/VehicleAudioMixer");
                if (mixer == null)
                {
                    Debug.LogWarning("VehicleAudioMixer resource could not be loaded from resources.");
                }
            }

            GetComponentsList(ref components);
            foreach (SoundComponent soundComponent in components)
            {
                soundComponent.SetDefaults(vc);
            }
        }


        public override void Validate(VehicleController vc)
        {
            if (mixer == null)
            {
                Debug.LogError("Audio mixer of 'SoundManager' is not assigned.");
            }
        }


        public void CreateSourceGO(string name, Vector3 localPosition, Transform parent, ref GameObject sourceGO)
        {
            sourceGO      = new GameObject();
            sourceGO.name = name;
            sourceGO.transform.SetParent(parent);
            sourceGO.transform.localPosition = localPosition;
        }


        public override void CheckState(int lodIndex)
        {
            base.CheckState(lodIndex);

            foreach (VehicleComponent component in components)
            {
                component.CheckState(lodIndex);
            }
        }


        private void GetComponentsList(ref List<SoundComponent> components)
        {
            components = new List<SoundComponent>
            {
                engineStartComponent,
                engineRunningComponent,
                engineFanComponent,
                turboWhistleComponent,
                turboFlutterComponent,
                transmissionWhineComponent,
                gearChangeComponent,
                brakeHissComponent,
                blinkerComponent,
                hornComponent,
                wheelSkidComponent,
                wheelTireNoiseComponent,
                crashComponent,
                suspensionBumpComponent,
                reverseBeepComponent,
            };
        }
    }
}