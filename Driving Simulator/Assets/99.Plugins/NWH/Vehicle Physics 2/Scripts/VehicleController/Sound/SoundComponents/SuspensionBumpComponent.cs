using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using NWH.WheelController3D;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NWH.VehiclePhysics2.Sound.SoundComponents
{
    /// <summary>
    ///     Sound of wheel hitting the surface or obstracle.
    /// </summary>
    [Serializable]
    public class SuspensionBumpComponent : SoundComponent
    {
        private List<bool> prevHasHits = new List<bool>();
        private int _wheelCount;


        public override void Initialize()
        {
            _wheelCount = vc.Wheels.Count;
            for (int index = 0; index < _wheelCount; index++)
            {
                WheelComponent wheel = vc.Wheels[index];
                AudioSource    a     = wheel.ControllerGO.AddComponent<AudioSource>();
                Sources.Add(a);
                prevHasHits.Add(false);
            }
            
            base.Initialize();
        }


        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (Clip != null && Sources != null && Sources.Count == vc.Wheels.Count)
            {
                int wheelCount = vc.Wheels.Count;
                for (int i = 0; i < wheelCount; i++)
                {
                    WheelController wc = vc.Wheels[i].wheelController;
                    if (!Sources[i].isPlaying)
                    {
                        float forwardAngle = wc.wheelHit.angleForward;
                        if (wc.isGrounded && prevHasHits[i] == false || wc.forwardFriction.speed > 0.8f &&
                            (forwardAngle > 15f || forwardAngle < -15f))
                        {
                            float newPitch       = Random.Range(0.8f, 1.2f) * basePitch;
                            float absDamperForce = wc.damperForce < 0 ? -wc.damperForce : wc.damperForce;
                            float newVolume = baseVolume *
                                              Mathf.Clamp01(absDamperForce / Mathf.Max(wc.damper.bumpForce,
                                                                wc.damper.reboundForce));

                            SetVolume(newVolume, i);
                            SetPitch(newPitch, i);

                            if (!Sources[i].isPlaying)
                            {
                                Sources[i].clip = RandomClip;
                                Sources[i].Play();
                            }
                        }
                    }

                    prevHasHits[i] = wc.isGrounded;
                }
            }
        }


        public override void FixedUpdate()
        {
        }


        public override void SetDefaults(VehicleController vc)
        {
            base.SetDefaults(vc);

            baseVolume = 0.12f;
            basePitch  = 1f;

            if (Clip == null)
            {
                AddDefaultClip("SuspensionBump");
            }
        }
    }
}