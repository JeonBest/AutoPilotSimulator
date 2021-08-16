using UnityEngine;

namespace NWH.Common.CoM
{
    /// <summary>
    /// Represents object that has mass and is a child of VariableCoM.
    /// Affects rigidbody center of mass and inertia.
    /// </summary>
    public interface IMassAffector
    {
        /// <summary>
        /// Name of the mass affector.
        /// </summary>
        string GetName();
        
        /// <summary>
        /// Returns mass of the mass affector in kilograms.
        /// </summary>
        float GetMass();
        
        /// <summary>
        /// Returns position of the mass affector in world coordinates.
        /// </summary>
        Vector3 GetPosition();
    }
}