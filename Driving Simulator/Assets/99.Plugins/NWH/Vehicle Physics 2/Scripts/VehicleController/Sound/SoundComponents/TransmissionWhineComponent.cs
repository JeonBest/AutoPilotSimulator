using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of vehicle transmission.
    ///     Most prominent on rally and racing cars with straight cut gears in the gearbox.
    /// </summary>
    [Serializable]
    public class TransmissionWhineComponent : SoundComponent
    {
        /// <summary>
        ///     Maximum speed value [m/s] of the vehicle at which the pitch will be at the top end of the pitchRange.
        /// </summary>
        [Tooltip(
            "Maximum speed value [m/s] of the vehicle at which the pitch will be at the top end of the pitchRange.")]
        public float maxSpeed = 80f;

        /// <summary>
        ///     Volume coefficient when transmission is not under load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Volume coefficient when transmission is not under load.")]
        public float offThrottleVolumeCoeff = 0.2f;

        /// <summary>
        ///     Volume coefficient when transmission is under load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Volume coefficient when transmission is under load.")]
        public float onThrottleVolumeCoeff = 1f;

        /// <summary>
        ///     Pitch range that will be added to the base pitch depending on transmission state.
        /// </summary>
        [Range(0f, 5f)]
        [Tooltip("    Pitch range that will be added to the base pitch depending on transmission state.")]
        public float pitchRange = 0.7f;

        /// <summary>
        ///     Smoothing of the transmission whine.
        /// </summary>
        [Range(0, 0.2f)]
        [Tooltip("    Smoothing of the transmission whine.")]
        public float smoothing = 0.05f;

        private float _volume;
        private float _whineVelocity;
        
        public override bool GetInitLoop()
        {
            return true;
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Clip != null)
            {
                float speed    = vc.Speed;
                float newPitch = basePitch;
                if (vc.powertrain.transmission.Gear != 0)
                {
                    newPitch += Mathf.Clamp01(speed / maxSpeed) * pitchRange;
                }

                SetPitch(newPitch);

                float generatedPowerPercent = vc.powertrain.engine.generatedPower / vc.powertrain.engine.maxPower;
                generatedPowerPercent =
                    generatedPowerPercent < 0 ? 0 : generatedPowerPercent > 1 ? 1 : generatedPowerPercent;
                float speedCoeff = (speed < 0 ? -speed : speed) * 0.2f;
                float newVolume = baseVolume * ((1f - generatedPowerPercent) * offThrottleVolumeCoeff +
                                                generatedPowerPercent * onThrottleVolumeCoeff) *
                                  Mathf.Clamp01(speedCoeff);
                if (smoothing > 0)
                {
                    _volume = Mathf.SmoothDamp(_volume, newVolume, ref _whineVelocity, smoothing);
                    SetVolume(_volume);
                }
                else
                {
                    _volume = newVolume;
                    SetVolume(_volume);
                }
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.03f;
            basePitch  = 0.18f;

            if (Clip == null)
            {
                AddDefaultClip("TransmissionWhine");
            }
        }
    }
}