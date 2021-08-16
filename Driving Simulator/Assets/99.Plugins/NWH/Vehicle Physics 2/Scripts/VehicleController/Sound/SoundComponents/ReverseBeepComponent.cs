using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    [Serializable]
    public class ReverseBeepComponent : SoundComponent
    {
        public bool beepOnNegativeVelocity = true;
        public bool beepOnReverseGear      = true;

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

            int gear = vc.powertrain.transmission.Gear;
            if (beepOnReverseGear && gear < 0 ||
                beepOnNegativeVelocity && vc.LocalForwardVelocity < -0.2f && gear <= 0)
            {
                if (!Source.isPlaying)
                {
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


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            if (Clip == null)
            {
                AddDefaultClip("ReverseBeep");
            }
        }
    }
}