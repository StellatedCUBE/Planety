using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Assign : Node
    {
        public Node value;
        public string property;
        public Node obj;

        public Assign(Node value, Node to, (int, int) pos)
        {
            this.value = value;
            position = pos;

            if (to is Property prop)
            {
                obj = prop.of;
                property = prop.property;
                obj.MarkAsAssignLeft();
            }
            else
            {
                throw new ScriptException("Unable to assign to " + to.GetType().Name, this);
            }
        }

        public override object Eval(Context ctx)
        {
            var obj = this.obj.Eval(ctx);
            var value = this.value.Eval(ctx);

            Assign_(obj, property, value, this);

            if (obj is CelestialBodyData cbd)
                cbd.dirty = true;

            return value;
        }

        public static void Assign_(object to, string property, object value, Node node = null)
        {
            if (to is IDictionary<string, object> dict)
            {
                dict[property] = value;
            }
            else
            {
                var type = to.GetType();

                Fields.Field field = null;

                while (type != null)
                {
                    field = Fields.Get(type, property);

                    if (field != null)
                        break;

                    type = type.BaseType;
                }

                if (field == null)
                    throw new ScriptException($"Object of type {to.GetType().Name} has no property {property}", node);

                try
                {
                    value = Casting.Cast(value, field.type);
                    field.setter(to, value);
                }
                catch (Exception e)
                {
                    if (node == null || (e is ScriptException se && se.pos.Item1 >= 0))
                        throw e;
                    throw new ScriptException(e.Message, node);
                }
            }
        }
    }
}
