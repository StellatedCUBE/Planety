using System;
using System.Collections.Generic;
using System.Text;

namespace Planety
{
    public class OrbitData
    {
        [ScriptName("around", true)]
        public string parentBody;
        [ScriptName("i", true)]
        public double inclination;
        [ScriptName("e", true)]
        public double eccentricity;
        [ScriptName("a", true)]
        public double? semiMajorAxis;
        [ScriptName("Ω", true)]
        public double longitudeOfAscendingNode;
        [ScriptName("ω", true)]
        public double argumentOfPeriapsis;
        [ScriptName("ν")]
        [ScriptName("θ")]
        [ScriptName("f", true)]
        public double meanAnomalyAtEpoch;
        [ScriptName("t0", true)]
        public double epoch;

        internal void HasValidFieldSet(out HashSet<string> errors)
        {
            errors = new HashSet<string>();

            if (string.IsNullOrEmpty(parentBody)) errors.Add("\"orbit.parent_body\" not set");
            if (!semiMajorAxis.HasValue) errors.Add("\"orbit.semi_major_axis\" not set");
        }
    }
}
