using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of a wastegate releasing air on turbocharged vehicles.
    /// </summary>
    [Serializable]
    public class TurboFlutterComponent : SoundComponent
    {
        /// <summary>
        ///     Final pitch will be a random value in the interval [pitch - pitchRandomnessRange, pitch + pitchRandomnessRange].
        ///     Make sure that the value is not larger than base pitch value as to avoid negative values.
        /// </summary>
        [Range(0f, 0.4f)]
        [Tooltip(
            "Final pitch will be a random value in the interval [pitch - pitchRandomnessRange, pitch + pitchRandomnessRange].\r\nMake sure that the value is not larger than base pitch value as to avoid negative values.")]
        public float pitchRandomnessRange = 0.3f;


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Clip != null && vc.powertrain.engine.forcedInduction.useForcedInduction &&
                vc.powertrain.engine.forcedInduction.hasWastegate)
            {
                if (vc.powertrain.engine.forcedInduction.wastegateFlag)
                {
                    Source.pitch = basePitch + basePitch * Random.Range(-pitchRandomnessRange, pitchRandomnessRange);
                    float newVolume = baseVolume * vc.powertrain.engine.forcedInduction.wastegateBoost;
                    newVolume = newVolume < 0 ? 0 : newVolume > 1 ? 1 : newVolume;
                    SetVolume(newVolume);
                    Play();
                    vc.powertrain.engine.forcedInduction.wastegateFlag = false;
                }
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume           = 0.05f;
            basePitch            = 1f;
            pitchRandomnessRange = 0.3f;

            if (Clip == null)
            {
                AddDefaultClip("TurboFlutter");
            }
        }
    }
}