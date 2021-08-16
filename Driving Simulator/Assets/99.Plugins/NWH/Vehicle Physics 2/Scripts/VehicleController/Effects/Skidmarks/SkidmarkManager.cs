using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace NWH.VehiclePhysics2.Effects
{
    [Serializable]
    public class SkidmarkManager : Effect
    {
        /// <summary>
        ///     Higher value will give darker skidmarks for the same slip. Check corresponding SurfacePreset (GroundDetection ->
        ///     Presets)
        ///     for per-surface settings.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Higher value will give darker skidmarks for the same slip. Check corresponding SurfacePreset (GroundDetection -> Presets)\r\nfor per-surface settings.")]
        public float globalSkidmarkIntensity = 0.6f;

        /// <summary>
        ///     Height above ground at which skidmarks will be drawn. If too low clipping between skidmark and ground surface will
        ///     occur.
        /// </summary>
        [Tooltip(
            "Height above ground at which skidmarks will be drawn. If too low clipping between skidmark and ground surface will\r\noccur.")]
        public float groundOffset = 0.025f;

        /// <summary>
        ///     When skidmark alpha value is below this value skidmark mesh will not be generated.
        /// </summary>
        [Tooltip("    When skidmark alpha value is below this value skidmark mesh will not be generated.")]
        public float lowerIntensityThreshold = 0.05f;

        /// <summary>
        ///     Number of triangles that will be drawn per one section, before mesh is saved and new one is generated.
        /// </summary>
        [Tooltip(
            "Number of triangles that will be drawn per one section, before mesh is saved and new one is generated.")]
        public int maxTrisPerSection = 360;

        /// <summary>
        /// Total number of skidmark mesh triangles per wheel before the oldest skidmark section gets destroyed.
        /// </summary>
        public int maxTotalTris = 1440;
        
        /// <summary>
        ///     Max skidmark texture alpha.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Max skidmark texture alpha.")]
        public float maxSkidmarkAlpha = 0.6f;

        /// <summary>
        ///     Distance from the last skidmark section needed to generate a new one.
        /// </summary>
        [Tooltip("    Distance from the last skidmark section needed to generate a new one.")]
        public float minDistance = 0.12f;

        /// <summary>
        ///     Skidmarks get deleted when distance from the parent vehicle is higher than this.
        /// </summary>
        [Tooltip("    Sersistent skidmarks get deleted when distance from the parent vehicle is higher than this.")]
        public float skidmarkDestroyDistance = 100f;

        /// <summary>
        ///     Time after which the skidmark will get destroyed. Set to 0 to disable.
        /// </summary>
        [Tooltip("    Time after which the skidmark will get destroyed.")]
        public float skidmarkDestroyTime = 0f;

        /// <summary>
        ///     Smoothing between skidmark triangles. Value represents time required for alpha to go from 0 to 1.
        /// </summary>
        [Range(0.01f, 0.1f)]
        [Tooltip(
            "    Smoothing between skidmark triangles. Value represents time required for alpha to go from 0 to 1.")]
        public float smoothing = 0.07f;
        
        /// <summary>
        /// Game object that contains all the skidmark objects.
        /// </summary>
        public GameObject skidmarkContainer;

        /// <summary>
        /// Material that will be used if no material is assigned to current surface or current surface is null.
        /// </summary>
        public Material fallbackMaterial;

        private int                     prevWheelCount;
        private List<SkidmarkGenerator> skidmarkGenerators = new List<SkidmarkGenerator>();


        public override void Initialize()
        {
            if (vc.groundDetection.groundDetectionPreset == null)
            {
                return;
            }

            skidmarkContainer = GameObject.Find("SkidContainer");
            if (skidmarkContainer == null)
            {
                skidmarkContainer          = new GameObject("SkidContainer");
                skidmarkContainer.isStatic = true;
            }

            fallbackMaterial = vc.groundDetection.groundDetectionPreset.fallbackSurfacePreset
                                          .skidmarkMaterial;
            List<Material> materials = new List<Material>();
            foreach (SurfaceMap surfaceMap in vc.groundDetection.groundDetectionPreset.surfaceMaps)
            {
                materials.Add(surfaceMap.surfacePreset.skidmarkMaterial);
            }

            foreach (WheelComponent wheelComponent in vc.powertrain.wheels)
            {
                SkidmarkGenerator skidmarkGenerator = new SkidmarkGenerator();
                skidmarkGenerator.Initialize(this, wheelComponent);
                skidmarkGenerators.Add(skidmarkGenerator);
            }

            float minPersistentDistance = maxTrisPerSection * minDistance * 0.75f;
            if (skidmarkDestroyDistance < minPersistentDistance)
            {
                skidmarkDestroyDistance = minPersistentDistance;
            }

            if (maxTrisPerSection * 2 > maxTotalTris)
            {
                maxTotalTris = maxTrisPerSection * 2 + 1;
                Debug.LogWarning("MaxTotalTris must be at least double the value of MaxTrisPerSection. Adjusting.");
            }

            prevWheelCount = vc.Wheels.Count;

            base.Initialize();
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Check if can be updated
            if (vc.groundDetection == null || !vc.groundDetection.IsEnabled)
            {
                return;
            }

            // Check for added/removed wheels and re-init if needed
            int wheelCount = vc.powertrain.wheels.Count;
            if (prevWheelCount != wheelCount)
            {
                initialized = false;
                Initialize();
            }

            prevWheelCount = wheelCount;

            // Update skidmarks
            Debug.Assert(skidmarkGenerators.Count == vc.powertrain.wheels.Count,
                         "Skidmark generator count must equal wheel count");

            int n = skidmarkGenerators.Count;
            for (int i = 0; i < n; i++)
            {
                WheelComponent wheelComponent = vc.powertrain.wheels[i];
                SurfacePreset  surfacePreset  = wheelComponent.surfacePreset;
                if (surfacePreset == null || !surfacePreset.drawSkidmarks)
                {
                    continue;
                }

                bool surfaceMapIsNull = surfacePreset == null;

                int surfaceMapIndex = -1;
                if (!surfaceMapIsNull)
                {
                    surfaceMapIndex = wheelComponent.surfaceMapIndex;
                }

                float intensity = 1f;
                if (surfaceMapIndex >= 0)
                {
                    float latFactor = wheelComponent.NormalizedLateralSlip;
                    latFactor = latFactor < vc.lateralSlipThreshold ? 0 : latFactor - vc.lateralSlipThreshold;
                    float lonFactor = wheelComponent.NormalizedLongitudinalSlip;
                    lonFactor = lonFactor < vc.lateralSlipThreshold ? 0 : lonFactor - vc.lateralSlipThreshold;

                    float slipIntensity = latFactor + lonFactor;
                    float weightCoeff   = wheelComponent.wheelController.wheel.load / 5000f;
                    weightCoeff   =  weightCoeff < 0 ? 0f : weightCoeff > 1f ? 1f : weightCoeff;
                    slipIntensity *= wheelComponent.surfacePreset.slipFactor * weightCoeff;

                    intensity = wheelComponent.surfacePreset.skidmarkBaseIntensity + slipIntensity;
                    intensity = intensity > 1f ? 1f : intensity < 0f ? 0f : intensity;
                }

                intensity *= globalSkidmarkIntensity;
                intensity =  intensity < 0f ? 0f : intensity > maxSkidmarkAlpha ? maxSkidmarkAlpha : intensity;

                skidmarkGenerators[i].Update(surfaceMapIndex, intensity, wheelComponent.wheelController.pointVelocity, vc.fixedDeltaTime);
            }
        }


        public override void FixedUpdate()
        {
        }
    }
}