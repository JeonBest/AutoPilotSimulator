using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.WheelController3D
{
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics", menuName = "NWH Vehicle Physics/Friction Preset Collection",
                     order    = 1)]
    public class FrictionPresetCollection : ScriptableObject
    {
        public List<FrictionPreset> frictionPresets = new List<FrictionPreset>();
    }
}