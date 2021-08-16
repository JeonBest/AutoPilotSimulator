using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Vehicle horn sound.
    /// </summary>
    [Serializable]
    public class HornComponent : SoundComponent
    {
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

            if (Source != null && Clip != null)
            {
                if (vc.input.Horn && !Source.isPlaying)
                {
                    SetPitch(basePitch);
                    SetVolume(baseVolume);
                    Play();
                }
                else if (!vc.input.Horn && Source.isPlaying)
                {
                    Stop();
                }
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (Clip == null)
            {
                AddDefaultClip("BlinkerOn");
            }
        }
    }
}