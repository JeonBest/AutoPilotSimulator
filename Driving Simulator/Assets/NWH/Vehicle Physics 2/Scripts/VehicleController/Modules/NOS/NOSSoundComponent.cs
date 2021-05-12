using System;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.NOS
{
    /// <summary>
    ///     Sound component producing the distinct 'hiss' sound of active NOS.
    /// </summary>
    [Serializable]
    public class NOSSoundComponent : SoundComponent
    {
        [NonSerialized]
        public NOSModule nosModule;

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

            if (nosModule.IsBeingUsed)
            {
                SetVolume(baseVolume);
                SetPitch(basePitch);
                Play();
            }
            else
            {
                Stop();
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.2f;

            if (Clip == null)
            {
                Clip = Resources.Load(VehicleController.defaultResourcesPath + "Sound/NOS") as AudioClip;
                if (Clip == null)
                {
                    Debug.LogWarning(
                        $"Audio Clip for sound component {GetType().Name}  from resources. Source will not play.");
                }
            }
        }
    }
}