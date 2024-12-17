using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Property : Node
    {
        public string property;
        public Node of;
        bool isAssignLeft;

        public override object Eval(Context ctx)
        {
            var obj = of.Eval(ctx);

            if (isAssignLeft && obj is CelestialBodyData cbd)
                cbd.dirty = true;

            if (obj is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue(property, out var val))
                    return val;
                if (ctx.scope.Equals(obj))
                    throw new ScriptException($"No such variable {property}", this);
                throw new ScriptException($"Map has no entry {property}", this);
            }

            var type = obj.GetType();

            while (type != null)
            {
                var field = Fields.Get(type, property);

                if (field != null)
                {
                    var res = field.getter(obj);
                    if (obj is CelestialBodyData cbd2 && res.GetType().IsConstructedGenericType && res.GetType().GetGenericTypeDefinition() == typeof(List<>) && res is not List<AK.Wwise.Event>)
                        cbd2.dirty = true;
                    return res;
                }

                type = type.BaseType;
            }

            throw new ScriptException($"Object of type {obj.GetType().Name} has no property {property}", this);
        }

        public override void MarkAsAssignLeft()
        {
            isAssignLeft = true;
            of.MarkAsAssignLeft();
        }
    }
}
