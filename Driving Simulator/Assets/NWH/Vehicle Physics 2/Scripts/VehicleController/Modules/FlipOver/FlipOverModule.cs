using System;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.FlipOver
{
    /// <summary>
    ///     Flip over module. Flips the vehicle over to be the right side up if needed.
    /// </summary>
    [Serializable]
    public class FlipOverModule : VehicleModule
    {
        public enum FlipOverType { Gradual, Instant }
        
        /// <summary>
        /// Determines how the vehicle will be flipped over. 
        /// </summary>
        public FlipOverType flipOverType = FlipOverType.Instant;
        
        /// <summary>
        ///     Minimum angle that the vehicle needs to be at for it to be detected as flipped over.
        /// </summary>
        [Tooltip("    Minimum angle that the vehicle needs to be at for it to be detected as flipped over.")]
        public float allowedAngle = 70f;

        /// <summary>
        /// If using instant (not gradual) flip over this value will be applied to the transform.y position to prevent rotating
        /// the object to a position that is underground.
        /// </summary>
        public float instantFlipOverVerticalOffset = 1f;

        /// <summary>
        ///     Is the vehicle flipped over?
        /// </summary>
        [Tooltip("    Is the vehicle flipped over?")]
        public bool flippedOver;

        /// <summary>
        ///     If enabled a prompt will be shown after the timeout, asking player to press the FlipOverModule button.
        /// </summary>
        [Tooltip(
            "If enabled a prompt will be shown after the timeout, asking player to press the FlipOverModule button.")]
        public bool manual;

        /// <summary>
        ///     Flip over detection will be disabled if velocity is above this value [m/s].
        /// </summary>
        [Tooltip("    Flip over detection will be disabled if velocity is above this value [m/s].")]
        public float maxDetectionSpeed = 0.6f;

        /// <summary>
        ///     Rotation speed of the vehicle while being flipped back.
        /// </summary>
        [Tooltip("    Rotation speed of the vehicle while being flipped back.")]
        public float maxRotationSpeed = 1f;

        /// <summary>
        ///     Time after detecting flip over after which vehicle will be flipped back.
        /// </summary>
        [Tooltip(
            "Time after detecting flip over after which vehicle will be flipped back or the manual button can be used.")]
        public float timeout = 5f;

        private float                _timeSinceFlip;
        private float                _vehicleAngle;
        private Quaternion           _targetRotation;
        private RigidbodyConstraints _initConstraints;
        private float                _initMaxAngVel;
        private bool                 _flipOverInput;
        private Quaternion _startRotation;

        public override void Update()
        {
            if (!Active)
            {
                return;
            }

            if (flippedOver && vc.input.FlipOver)
            {
                _flipOverInput    = true;
                vc.input.FlipOver = false;
            }
            else if (!flippedOver)
            {
                vc.input.FlipOver = false;
            }

            _vehicleAngle = Vector3.Angle(vc.transform.up, -Physics.gravity.normalized);

            if (_vehicleAngle < allowedAngle)
            {
                if (flippedOver)
                {
                    EndFlipOver(flipOverType);
                }
                else
                {
                    return;
                }
            }

            // Check if wheels on ground
            int wheelsOnGround = 0;
            foreach (WheelComponent wheel in vc.Wheels)
            {
                if (wheel.IsGrounded)
                {
                    wheelsOnGround++;
                }
            }

            // All wheels on ground, assume not flipped over
            if (wheelsOnGround == vc.Wheels.Count)
            {
                if (flippedOver)
                {
                    EndFlipOver(flipOverType);
                }
                else
                {
                    return;
                }
            }
            
            // Check if the vehicle is flipped
            if (!flippedOver && vc.Speed < maxDetectionSpeed 
                             && vc.vehicleRigidbody.angularVelocity.magnitude < maxDetectionSpeed 
                             && _vehicleAngle > allowedAngle)
            {
                _timeSinceFlip += vc.fixedDeltaTime;
                
                // Flipped over and timeout happened, start flipping the vehicle back
                if (_timeSinceFlip > timeout)
                {
                    flippedOver      = true;
                    _initConstraints = vc.vehicleRigidbody.constraints;
                    _initMaxAngVel   = vc.vehicleRigidbody.maxAngularVelocity;
                    _startRotation = vc.transform.rotation;
                    
                    if (Mathf.Abs(Vector3.Dot(vc.transform.forward, Vector3.up)) > 0.8f)
                    {
                        _targetRotation = Quaternion.LookRotation(
                            Vector3.ProjectOnPlane(vc.transform.up, Vector3.up), 
                            Vector3.up);
                    }
                    else
                    {
                        _targetRotation = Quaternion.LookRotation(
                            Vector3.ProjectOnPlane(vc.transform.forward, Vector3.up), 
                            Vector3.up);
                    }
                }
            }

            // Rotate the vehicle if flipped
            if (flippedOver && (_flipOverInput || !manual))
            {
                if (flipOverType == FlipOverType.Gradual)
                {
                    vc.vehicleRigidbody.constraints =
                        RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    vc.input.Handbrake                        = 1f;
                    vc.vehicleRigidbody.maxAngularVelocity = maxRotationSpeed * 2f;

                    //Quaternion rotationDiff = (_targetRotation * Quaternion.Inverse(vc.transform.rotation)).normalized;
                    Quaternion rotationDiff = Quaternion.RotateTowards(vc.transform.rotation, _targetRotation, Mathf.Infinity);
                    rotationDiff.ToAngleAxis(out float angle, out Vector3 rotationAxis);

                    if (Mathf.Abs(rotationAxis.y) > 0.95f)
                    {
                        rotationAxis = vc.transform.forward;
                    }

                    vc.vehicleRigidbody.AddRelativeTorque(Mathf.Sign(angle) * vc.vehicleRigidbody.mass * 10f * rotationAxis);
                        
                    if (Vector3.Dot(vc.transform.up, Vector3.up) > 0.9f)
                    {
                        EndFlipOver(flipOverType);
                    }
                }
                else
                {
                    EndFlipOver(flipOverType);
                }
            }
        }

        private void EndFlipOver(FlipOverType type)
        {
            if (!flippedOver)
            {
                return;
            }

            flippedOver                            = false;
            _flipOverInput                         = false;
            _timeSinceFlip                         = 0;
            vc.vehicleRigidbody.constraints        = _initConstraints;
            vc.vehicleRigidbody.maxAngularVelocity = _initMaxAngVel;

            if (type == FlipOverType.Instant)
            {
                vc.transform.rotation           =  _targetRotation;
                vc.transform.position           += Vector3.up * instantFlipOverVerticalOffset;
            }
        }

        public override void FixedUpdate()
        {
        }


        public override ModuleCategory GetModuleCategory()
        {
            return ModuleCategory.DrivingAssists;
        }
    }
}