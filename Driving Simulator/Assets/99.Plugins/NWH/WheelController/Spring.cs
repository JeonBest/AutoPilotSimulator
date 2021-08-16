using System;
using UnityEngine;

namespace NWH.WheelController3D
{
    /// <summary>
    ///     Suspension spring.
    /// </summary>
    [Serializable]
    public class Spring
    {
        /// <summary>
        ///     Is the suspension currently bottomed out? True when spring.length <= 0.
        /// </summary>
        [Tooltip("    Is the suspension currently bottomed out? True when spring.length <= 0.")]
        public bool bottomedOut;

        /// <summary>
        ///     How much is spring currently compressed. 0 means fully relaxed and 1 fully compressed.
        /// </summary>
        [Tooltip("    How much is spring currently compressed. 0 means fully relaxed and 1 fully compressed.")]
        public float compressionPercent;

        /// <summary>
        ///     Current force the spring is exerting in [N].
        /// </summary>
        [Tooltip("    Current force the spring is exerting in [N].")]
        public float force;

        /// <summary>
        ///     Force curve where X axis represents spring travel [0,1] and Y axis represents force coefficient [0, 1].
        ///     Force coefficient is multiplied by maxForce to get the final spring force.
        /// </summary>
        [Tooltip(
            "Force curve where X axis represents spring travel [0,1] and Y axis represents force coefficient [0, 1].\r\nForce coefficient is multiplied by maxForce to get the final spring force.")]
        public AnimationCurve forceCurve;

        /// <summary>
        ///     Current length of the spring.
        /// </summary>
        [Tooltip("    Current length of the spring.")]
        public float length;

        /// <summary>
        ///     Maximum force spring can exert.
        /// </summary>
        [Tooltip("    Maximum force spring can exert.")]
        public float maxForce = 16000.0f;

        /// <summary>
        ///     Length of fully relaxed spring.
        /// </summary>
        [Tooltip("    Length of fully relaxed spring.")]
        public float maxLength = 0.35f;

        /// <summary>
        ///     Is the spring over extended. Opposite of bottomed out.
        /// </summary>
        [Tooltip("    Is the spring over extended. Opposite of bottomed out.")]
        public bool overExtended;

        public float prevLength;

        public Vector3 targetPoint;

        /// <summary>
        ///     Rate of change of the length of the spring in [m/s].
        /// </summary>
        [Tooltip("    Rate of change of the length of the spring in [m/s].")]
        public float velocity;
    }
}