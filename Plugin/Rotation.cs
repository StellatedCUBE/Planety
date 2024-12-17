using System;
using System.Collections.Generic;
using System.Text;

namespace Planety
{
    public class Rotation
    {
        public double offset;
        internal double period;
        internal bool rotating;
        internal bool tidallyLocked;

        private Rotation() { }

        public static Rotation Period(double period) => double.IsInfinity(period) ? None() : new()
        {
            period = period,
            rotating = true
        };

        public static Rotation TidallyLocked() => new()
        {
            rotating = true,
            tidallyLocked = true,
            period = 1
        };

        public static Rotation None() => new() { period = double.PositiveInfinity };
    }
}
