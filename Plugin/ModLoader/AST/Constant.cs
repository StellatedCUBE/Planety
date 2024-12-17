﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Constant : Node
    {
        public object value;

        public override object Eval(Context ctx) => value;
    }
}