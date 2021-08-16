using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using NWH.Common.Utility;

namespace NWH.Common.CoM
{
    /// <summary>
    /// Script used for adjusting Rigidbody properties at runtime based on
    /// attached IMassAffectors. This allows for vehicle center of mass and inertia changes
    /// as the fuel is depleted, cargo is added, etc. without the need of physically parenting Rigidbodies to the object.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class VariableCenterOfMass : MonoBehaviour
    {
        /// <summary>
        /// Base mass of the object, without IMassAffectors.
        /// </summary>
        public float baseMass = 1000f;

        /// <summary>
        /// Total mass of the object with masses of IMassAffectors counted in.
        /// </summary>
        public float totalMass = 1000f;
        
        /// <summary>
        /// Object dimensions in [m]. X - width, Y - height, Z - length.
        /// It is important to set the correct dimensions or otherwise inertia might be calculated incorrectly.
        /// </summary>
        [Tooltip("    Vehicle dimensions in [m]. X - width, Y - height, Z - length.")]
        public Vector3 dimensions = new Vector3(1.5f, 1.5f, 4.6f);
        
        /// <summary>
        /// Center of mass of the object. Auto calculated. To adjust center of mass use centerOfMassOffset.
        /// </summary>
        [Tooltip(
            "AerodynamicCenter of mass of the rigidbody. Needs to be readjusted when new colliders are added.")]
        public Vector3 centerOfMass = Vector3.zero;

        /// <summary>
        /// Used to adjust actual center of mass location in reference to the auto-calculated center of mass.
        /// </summary>
        public Vector3 centerOfMassOffset = Vector3.zero;

        /// <summary>
        /// When true inertia settings will be ignored and default Rigidbody inertia tensor will be used.
        /// </summary>
        public bool useDefaultInertia = true;
        
        /// <summary>
        ///     Vector by which the inertia tensor of the rigidbody will be scaled on Start().
        ///     Due to the uniform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel
        ///     off.
        ///     Use this to adjust inertia tensor values.
        /// </summary>
        [Tooltip(
            "    Vector by which the inertia tensor of the rigidbody will be scaled on Start().\r\n    Due to the unform density of the rigidbodies, versus the very non-uniform density of a vehicle, inertia can feel\r\n    off.\r\n    Use this to adjust inertia tensor values.")]
        public Vector3 inertiaTensor = new Vector3(170f, 1640f, 1350f);

        /// <summary>
        /// Used to adjust result given by automatic inertia calculation.
        /// </summary>
        public Vector3 inertiaScale = new Vector3(1f, 1f, 1f);

        /// <summary>
        /// Update interval in seconds.
        /// On each update center of mass and inertia tensor will be updated based on values from IMassAffectors.
        /// </summary>
        public float updateInterval = 1f;

        /// <summary>
        /// Current global center of mass.
        /// </summary>
        public Vector3 CenterOfMass
        {
            get { return transform.TransformPoint(centerOfMass + centerOfMassOffset); }
        }

        /// <summary>
        /// Objects attached or part of the vehicle affecting its center of mass and inertia.
        /// </summary>
        [NonSerialized] public IMassAffector[] affectors;

        private Rigidbody _rigidbody;
        private float _timer = 999f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            SyncAffectors();
            UpdateCoM();
        }

        private void OnValidate()
        {
            _rigidbody = GetComponent<Rigidbody>();
            SyncAffectors();
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;
            
            if (_timer > updateInterval)
            {
                UpdateCoM();
                UpdateInertiaTensor();
                _timer = 0;
            }
        }

        /// <summary>
        /// Calculates new CoM and inertia from child IMassAffectors.
        /// </summary>
        public void UpdateCoM()
        {
            totalMass    = baseMass + affectors.Sum(a => a.GetMass());
            centerOfMass = CalculateCenterOfMass();
            UpdateRigidbodyProperties();
        }
        
        
        public void UpdateInertiaTensor()
        {
            inertiaTensor = CalculateInertiaTensor(dimensions);
            UpdateRigidbodyProperties();
        }

        /// <summary>
        /// Updates list of IMassAffectors attached to this object.
        /// Call after IMassAffector has been added or removed from the object.
        /// </summary>
        public void SyncAffectors()
        {
            affectors = GetComponentsInChildren<IMassAffector>();
        }

        public Vector3 CalculateCenterOfMass()
        {
            _rigidbody.ResetCenterOfMass();

            Vector3 newCoM  = _rigidbody.centerOfMass;
            float   massSum = 0;
            foreach (IMassAffector affector in affectors)
            {
                float affectorMass = affector.GetMass();
                massSum += affectorMass;
                newCoM += transform.InverseTransformPoint(affector.GetPosition()) * affectorMass;
            }

            return massSum == 0 ? newCoM : newCoM / massSum;
        }
        
        public Vector3 CalculateInertiaTensor(Vector3 dimensions)
        {
            if (useDefaultInertia)
            {
                _rigidbody.ResetInertiaTensor();
                return _rigidbody.inertiaTensor;
            }
            
            Vector3 inertiaTensor = new Vector3(
                (dimensions.y + dimensions.z) * 0.34f * totalMass,
                (dimensions.z + dimensions.x) * 0.42f * totalMass,
                (dimensions.x + dimensions.y) * 0.24f * totalMass
            );

            Vector3 affectorInertia = Vector3.zero;
            foreach (IMassAffector affector in affectors)
            {
                float affectorMass = affector.GetMass();
                Vector3 affectorLocalPos = transform.InverseTransformPoint(affector.GetPosition());
                affectorInertia.x += (Mathf.Abs(affectorLocalPos.y) + Mathf.Abs(affectorLocalPos.z)) * affectorMass;
                affectorInertia.y += (Mathf.Abs(affectorLocalPos.x) + Mathf.Abs(affectorLocalPos.z)) * affectorMass;
                affectorInertia.z += (Mathf.Abs(affectorLocalPos.x) + Mathf.Abs(affectorLocalPos.y)) * affectorMass;
            }

            return Vector3.Scale(inertiaTensor, inertiaScale);
        }

        private void UpdateRigidbodyProperties()
        {
            _rigidbody.mass = totalMass;

            // Inertia tensor of constrained rigidbody will be 0 which causes errors when trying to set.
            if (inertiaTensor.x > 0 && inertiaTensor.y > 0 && inertiaTensor.z > 0)
            {
                _rigidbody.inertiaTensor = inertiaTensor;
            }
            
            _rigidbody.centerOfMass = centerOfMass + centerOfMassOffset;
        }

        private void OnDrawGizmos()
        {
            // CoM
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(CenterOfMass, 0.1f);
            
            // Mass Affectors
            Gizmos.color = Color.cyan;
            foreach (IMassAffector affector in affectors)
            {
                Gizmos.DrawSphere(affector.GetPosition(), 0.05f);
            }

            // Dimensions
            Transform t = transform;
            Vector3 tPosition = t.position;
            Vector3 fwdOffset   = t.forward * dimensions.z * 0.5f;
            Vector3 rightOffset = t.right * dimensions.x * 0.5f;
            Vector3 upOffset    = t.up * dimensions.y * 0.5f;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tPosition + fwdOffset, tPosition - fwdOffset);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(tPosition + rightOffset, tPosition - rightOffset);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(tPosition + upOffset, tPosition - upOffset);
        }

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Bounds bounds = gameObject.FindBoundsIncludeChildren();
            dimensions = new Vector3(bounds.extents.x * 2f, bounds.extents.y * 2f, bounds.extents.z * 2f);
            if (dimensions.x < 0.001f) dimensions.x = 0.001f;
            if (dimensions.y < 0.001f) dimensions.y = 0.001f;
            if (dimensions.z < 0.001f) dimensions.z = 0.001f;
            centerOfMass = _rigidbody.centerOfMass;
            baseMass     = dimensions.x * dimensions.y * dimensions.z * 1.2f;
        }
    }
}