using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Shifter sound played when changing gears.
    ///     Supports multiple audio clips of which one is chosen at random each time this effect is played.
    /// </summary>
    [Serializable]
    public class GearChangeComponent : SoundComponent
    {
        /// <summary>
        ///     Determines how much pitch of the gear shift sound can vary from one shift to another.
        ///     Final pitch is calculated as base pitch +- randomPitchRange.
        /// </summary>
        [Range(0, 0.5f)]
        [Tooltip(
            "Determines how much pitch of the gear shift sound can vary from one shift to another.\r\nFinal pitch is calculated as base pitch +- randomPitchRange.")]
        public float randomPitchRange = 0.2f;

        /// <summary>
        ///     Determines how much volume of the gear shift sound car vary.
        ///     Final volume is caulculated as base volume +- randomVolumeRange.
        /// </summary>
        [Range(0, 0.5f)]
        [Tooltip(
            "Determines how much volume of the gear shift sound car vary.\r\nFinal volume is caulculated as base volume +- randomVolumeRange.")]
        public float randomVolumeRange = 0.1f;


        public override void Initialize()
        {
            base.Initialize();

            vc.powertrain.transmission.onShift.AddListener(PlayShiftSound);
        }


        private void PlayShiftSound(GearShift gearShift)
        {
            Source.clip = RandomClip;
            if (gearShift.ToGear == 0)
            {
                SetVolume(0);
            }
            else
            {
                SetVolume(baseVolume + baseVolume * Random.Range(-randomVolumeRange, randomVolumeRange));
            }

            SetPitch(basePitch + basePitch * Random.Range(-randomPitchRange, randomPitchRange));
            if (Source.enabled)
            {
                Play();
            }
        }


        public override void Update()
        {
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.16f;
            basePitch  = 0.8f;

            if (Clip == null)
            {
                AddDefaultClip("GearChange");              
            }
        }
    }
}