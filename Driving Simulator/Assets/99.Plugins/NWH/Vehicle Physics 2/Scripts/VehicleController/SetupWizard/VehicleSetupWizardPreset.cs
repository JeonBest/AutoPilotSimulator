using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2.SetupWizard
{
    /// <summary>
    ///     A ScriptableObject representing a set of SurfaceMaps. Usually one per scene or project.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics", menuName = "NWH Vehicle Physics/Vehicle Setup Wizard Preset",
                     order    = 1)]
    public class VehicleSetupWizardPreset : ScriptableObject
    {
        public enum VehicleType
        {
            Car, 
            SportsCar,
            OffRoad, 
            MonsterTruck, 
            SemiTruck,
            Trailer
        }
        
        public enum DrivetrainConfiguration
        {
            FWD, 
            AWD, 
            RWD
        }

        // General
        public VehicleType vehicleType = VehicleType.Car;
        
        // Physical properties
        public float mass   = 1500f;
        public float width  = 1.8f;
        public float length = 4.5f;
        public float height = 1.4f;
        
        // Engine
        [Range(10, 600)]
        public float enginePower   = 110f;
        public float engineMaxRPM  = 6000f;

        // Transmission
        public float transmissionGearing = 1f;

        // Drivetrain
        public DrivetrainConfiguration drivetrainConfiguration = DrivetrainConfiguration.RWD;
        
        // Suspension
        [FormerlySerializedAs("suspensionTravel")] public float    suspensionTravelCoeff = 1f;
        [FormerlySerializedAs("suspensionStiffness")] public float suspensionStiffnessCoeff = 1f;
    }
}