using System;

namespace NWH.VehiclePhysics2
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowInSettings : Attribute
    {
        public string name;
        public float  min;
        public float  max  = 1f;
        public float  step = 0.1f;


        public ShowInSettings(string name)
        {
            this.name = name;
        }


        public ShowInSettings(float min, float max, float step = 0.1f)
        {
            this.min  = min;
            this.max  = max;
            this.step = step;
        }


        public ShowInSettings(string name, float min, float max, float step = 0.1f)
        {
            this.name = name;
            this.min  = min;
            this.max  = max;
            this.step = step;
        }


        public ShowInSettings()
        {
        }
    }
}