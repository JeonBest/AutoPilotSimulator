using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.Common.CoM
{
    public class MassAffector : MonoBehaviour, IMassAffector
    {
        public float mass;

        public string GetName()
        {
            return transform.name;
        }
        
        public float GetMass()
        {
            return mass;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
    }
}

