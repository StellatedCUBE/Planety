using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader.AST
{
    public class Call : Node
    {
        public Node method;
        public List<Node> arguments;

        public override object Eval(Context ctx)
        {
            var method = this.method.Eval(ctx);

            if (method is ValueTuple<object, MethodInfo> pair)
            {
                (var obj, var mi) = pair;
                var parameters = mi.GetParameters();
                var args = new object[parameters.Length];
                var name = mi.Name;
                if (method is Property prop)
                    name = prop.property;
                int argi = 0;

                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Context))
                    args[argi++] = ctx;

                for (int i = 0; i < arguments.Count; i++)
                {
                    if (argi == args.Length)
                        throw new ScriptException("Too many arguments passed to " + name, this);

                    var arg = arguments[i];
                    var val = arg.Eval(ctx);
                    try
                    {
                        val = Casting.Cast(val, parameters[argi].ParameterType);
                    }
                    catch (Exception e)
                    {
                        throw new ScriptException($"Failed to cast argument {i + 1} of {name} to {parameters[argi].ParameterType.Name}:\n  {e.Message}", arg);
                    }
                    args[argi++] = val;
                }

                while (argi < args.Length)
                {
                    if (parameters[argi].HasDefaultValue)
                        args[argi++] = parameters[argi].DefaultValue;
                    else
                        throw new ScriptException("Not enough arguments passed to " + name, this);
                }

                try
                {
                    return mi.Invoke(obj, args);
                }
                catch (Exception e)
                {
                    throw new ScriptException($"In call to {name}:\n  {e.GetType().Name}: {e.Message}", this);
                }
            }

            if (method.GetType().IsConstructedGenericType && method.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                if (arguments.Count != 1)
                    throw new ScriptException("Index must be single value", this);

                int index = Convert.ToInt32(arguments[0].Eval(ctx));

                while (index < 0)
                    index += (int)method.GetType().GetProperty("Count").GetValue(method);

                foreach (var item in (IEnumerable)method)
                {
                    if (index == 0)
                        return item;
                    index--;
                }

                throw new ScriptException("Index out of range", this);
            }

            throw new ScriptException($"Type {method.GetType().Name} cannot be called", this);
        }

        public void DoWarnings()
        {
            if (method is Property prop && prop.of is Scope && arguments.Count > 0)
            {
                if (!Context.GlobalScopeTemplate.ContainsKey(prop.property))
                {
                    Plugin.PopWarning($"Call to \"{prop.property}\", which is not a known function", sourceFile, prop.position);
                    return;
                }

                switch (prop.property)
                {
                    case "read_texture_from_png":
                    case "read_texture_from_jpg":
                    case "read_texture_from_dds":
                    case "read_heightmap_from_png":
                    case "read_heightmap_raw":
                    case "read_mesh":
                        try
                        {
                            if (arguments[0] is Constant c && c.value is string s && !File.Exists(Path.Combine(Path.GetDirectoryName(sourceFile), s)))
                                Plugin.PopWarning($"Call to {prop.property} reads \"{s}\", but no such file exists", sourceFile, c.position);
                        }
                        catch { }
                        break;
                }

                if (prop.property == "read_heightmap_raw" && arguments.Count > 0 && arguments[1] is Constant c2 && c2.value is double d)
                {
                    if (d != Math.Floor(d))
                        Plugin.PopWarning($"Call to read_heightmap_raw has a non-integer size argument", sourceFile, c2.position);
                    else if (!Plugin.IsPowerOf2((int)d))
                        Plugin.PopWarning($"Call to read_heightmap_raw has a non-power-of-two size argument", sourceFile, c2.position);
                }
            }
        }
    }
}
