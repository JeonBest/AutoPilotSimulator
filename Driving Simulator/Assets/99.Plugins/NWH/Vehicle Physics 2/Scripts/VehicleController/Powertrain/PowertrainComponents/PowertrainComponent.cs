using System;
using System.Collections.Generic;
using NWH.Common.Utility;
using UnityEngine;

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public class PowertrainComponent
    {
        /// <summary>
        ///     Angular velocity of the component.
        /// </summary>
        [Tooltip("    Angular velocity of the component.")]
        public float angularVelocity;

        /// <summary>
        ///     Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but
        ///     also slower to spin down. Too high values will result in (apparent) sluggish response while too low values will
        ///     result in vehicle being easy to stall.
        /// </summary>
        [Range(0.002f, 1f)]
        [Tooltip(
            "Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but\r\nalso slower to spin down. Too high values will result in (apparent) sluggish response while too low values will\r\nresult in vehicle being easy to stall.")]
        public float inertia = 0.02f;

        /// <summary>
        ///     Input component. Set automatically.
        /// </summary>
        [Tooltip("    Input component. Set automatically.")]
        public PowertrainComponent input;

        /// <summary>
        ///     Name of the component. Only unique names should be used on the same vehicle.
        /// </summary>
        [Tooltip("    Name of the component. Only unique names should be used on the same vehicle.")]
        public string name;

        /// <summary>
        ///     Output component.
        /// </summary>
        [Tooltip("    Output component.")]
        public PowertrainComponent outputA;

        public bool componentInputIsNull;

        [NonSerialized] protected VehicleController vc;
        [NonSerialized] protected float             _lowerAngularVelocityLimit = -Mathf.Infinity;
        [NonSerialized] protected bool              _outputAIsNull;
        [NonSerialized] protected float             _upperAngularVelocityLimit = Mathf.Infinity;

        [SerializeField]
        protected OutputSelector outputASelector = new OutputSelector();

        /// <summary>
        ///     Damage in range of 0 to 1 that the component has received.
        /// </summary>
        private float _componentDamage;


        public PowertrainComponent()
        {
        }


        public PowertrainComponent(float inertia, string name)
        {
            this.name    = name;
            this.inertia = inertia;
        }


        /// <summary>
        ///     Returns current component damage.
        /// </summary>
        public float ComponentDamage
        {
            get { return _componentDamage; }
            set { _componentDamage = value > 1 ? 1 : value < 0 ? 0 : value; }
        }

        /// <summary>
        ///     Minimum angular velocity a component can physically achieve.
        /// </summary>
        public float LowerAngularVelocityLimit
        {
            get { return _lowerAngularVelocityLimit; }
            set { _lowerAngularVelocityLimit = value; }
        }

        /// <summary>
        ///     RPM of component.
        /// </summary>
        [ShowInTelemetry]
        public float RPM
        {
            get { return UnitConverter.AngularVelocityToRPM(angularVelocity); }
        }

        /// <summary>
        ///     Maximum angular velocity a component can physically achieve.
        /// </summary>
        public float UpperAngularVelocityLimit
        {
            get { return _upperAngularVelocityLimit; }
            set { _upperAngularVelocityLimit = value; }
        }


        /// <summary>
        ///     Initializes PowertrainComponent.
        /// </summary>
        public virtual void Initialize(VehicleController vc)
        {
            if (inertia < 0.001f)
            {
                inertia = 0.001f;
            }

            this.vc              = vc;
            componentInputIsNull = input == null;
            _outputAIsNull       = outputA == null;
        }


        /// <summary>
        ///     Gets called before solver.
        /// </summary>
        public virtual void OnPrePhysicsSubstep(float t, float dt)
        {
        }


        /// <summary>
        ///     Gets called after solver has finished.
        /// </summary>
        public virtual void OnPostPhysicsSubstep(float t, float dt)
        {
            angularVelocity = angularVelocity < _lowerAngularVelocityLimit
                                  ? _lowerAngularVelocityLimit
                                  : angularVelocity;
            angularVelocity = angularVelocity > _upperAngularVelocityLimit
                                  ? _upperAngularVelocityLimit
                                  : angularVelocity;
        }


        public virtual void OnEnable()
        {
        }


        public virtual void OnDisable()
        {
        }


        public virtual void SetDefaults(VehicleController vc)
        {
            inertia = 0.02f;
            outputASelector = new OutputSelector();
        }


        public virtual void Validate(VehicleController vc)
        {
            if (inertia < 0.002f)
            {
                inertia = 0.002f;
                Debug.LogWarning($"{name}: Inertia must be larger than 0.002f. Setting to 0.002f.");
            }
        }


        /// <summary>
        ///     Finds which powertrain component has its output set to this component.
        /// </summary>
        public virtual void FindInput(Powertrain powertrain)
        {
            List<PowertrainComponent> outputs = new List<PowertrainComponent>();
            foreach (PowertrainComponent component in powertrain.Components)
            {
                component.GetAllOutputs(ref outputs);
                foreach (PowertrainComponent output in outputs)
                {
                    if (output != null && output == this)
                    {
                        input                = component;
                        componentInputIsNull = false;
                        return;
                    }
                }
            }

            input                = null;
            componentInputIsNull = true;
        }


        /// <summary>
        ///     Retrieves and sets output powertrain components.
        /// </summary>
        /// <param name="powertrain"></param>
        public virtual void FindOutputs(Powertrain powertrain)
        {
            if (string.IsNullOrEmpty(outputASelector.name))
            {
                return;
            }

            PowertrainComponent output = powertrain.GetComponent(outputASelector.name);
            if (output == null)
            {
                Debug.LogError($"Unknown component '{outputASelector.name}'");
                return;
            }

            outputA = output;
        }


        /// <summary>
        ///     Retruns a list of PowertrainComponents that this component outputs to.
        /// </summary>
        public virtual void GetAllOutputs(ref List<PowertrainComponent> outputs)
        {
            outputs.Clear();
            outputs.Add(outputA);
        }


        public virtual float QueryAngularVelocity(float inputAngularVelocity, float dt)
        {
            angularVelocity = inputAngularVelocity;
            if (_outputAIsNull)
            {
                return 0;
            }

            float Wa = outputA.QueryAngularVelocity(inputAngularVelocity, dt);
            return Wa;
        }


        public virtual float QueryInertia()
        {
            if (_outputAIsNull)
            {
                return inertia;
            }

            float Ii = inertia;
            float Ia = outputA.QueryInertia();
            float I  = Ii + Ia;
            return I;
        }


        public virtual float ForwardStep(float torque, float inertiaSum, float t, float dt, int i)
        {
            if (_outputAIsNull)
            {
                return torque;
            }

            float T = outputA.ForwardStep(torque, inertiaSum + inertia, t, dt, i);
            return T;
        }


        public void SetOutput(PowertrainComponent outputComponent)
        {
            if (string.IsNullOrEmpty(outputComponent.name))
            {
                Debug.LogWarning("Trying to set powertrain component output to a nameless component. " +
                                 "Output will be set to [none]");
            }

            SetOutput(outputComponent.name);
        }


        public void SetOutput(string outputName)
        {
            if (string.IsNullOrEmpty(outputName))
            {
                outputASelector.name = "[none]";
            }
            else
            {
                outputASelector.name = outputName;
            }
        }
    }
}