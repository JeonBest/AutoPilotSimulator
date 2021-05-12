using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of an engine starting / stopping.
    ///     Plays while start is active.
    /// </summary>
    [Serializable]
    public class EngineStartComponent : SoundComponent
    {
        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            // Starting and stopping engine sound
            if (Source != null && Clips.Count > 0)
            {
                if (vc.powertrain.engine.StarterActive)
                {
                    if (!Source.isPlaying)
                    {
                        SetVolume(baseVolume);
                        Play();
                    }
                }
                else
                {
                    if (Source.isPlaying)
                    {
                        Stop();
                    }
                }
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);
            baseVolume = 0.2f;
            basePitch  = 1f;

            if (Clip == null)
            {
                AddDefaultClip("EngineStart");               
            }
        }
    }
}