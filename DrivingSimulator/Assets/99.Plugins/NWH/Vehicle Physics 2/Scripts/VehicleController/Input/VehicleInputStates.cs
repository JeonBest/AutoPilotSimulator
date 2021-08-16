using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Struct for storing input states of the vehicle.
    ///     Allows for input to be copied between the vehicles.
    /// </summary>
    [Serializable]
    public struct VehicleInputStates
    {
        [Range(-1f, 1f)]
        public float steering;

        [Range(0, 1f)]
        public float throttle;

        [Range(0, 1f)]
        public float brakes;

        [Range(0f, 1f)]
        public float clutch;

        public bool engineStartStop;
        public bool extraLights;
        public bool highBeamLights;

        [Range(0f, 1f)]
        public float handbrake;

        public bool hazardLights;

        public bool horn;
        public bool leftBlinker;
        public bool lowBeamLights;
        public bool rightBlinker;
        public bool shiftDown;
        public int  shiftInto;
        public bool shiftUp;
        public bool trailerAttachDetach;
        public bool cruiseControl;
        public bool boost;
        public bool flipOver;


        public void Reset()
        {
            steering            = 0;
            throttle            = 0;
            clutch              = 0;
            handbrake           = 0;
            shiftUp             = false;
            shiftDown           = false;
            shiftInto           = -999;
            leftBlinker         = false;
            rightBlinker        = false;
            lowBeamLights       = false;
            highBeamLights      = false;
            hazardLights        = false;
            extraLights         = false;
            trailerAttachDetach = false;
            horn                = false;
            engineStartStop     = false;
            cruiseControl       = false;
            boost               = false;
            flipOver            = false;
        }
    }
}