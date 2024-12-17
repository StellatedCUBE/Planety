using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    public static class Fields
    {
        public class Field
        {
            public Type type;
            public Action<object, object> setter;
            public Func<object, object> getter;
        }

        static Dictionary<(Type, string), Field> fields = new Dictionary<(Type, string), Field>();
        static HashSet<Type> built = new HashSet<Type>();

        public static Field Get(Type type, string field)
        {
            if (built.Add(type))
                Build(type);

            fields.TryGetValue((type, field), out var ret);
            return ret;
        }

        static void Build(Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(field, typeof(NoScriptAccessAttribute)))
                    continue;

                bool keepExistingName = !Attribute.IsDefined(field, typeof(ScriptNameAttribute));

                var names = new List<string>();

                if (!keepExistingName)
                {
                    foreach (var a in Attribute.GetCustomAttributes(field, typeof(ScriptNameAttribute)))
                    {
                        var c = (ScriptNameAttribute)a;
                        keepExistingName |= c.keepExistingName;
                        names.Add(c.name);
                    }
                }

                if (keepExistingName)
                    names.Add(TransformName(field.Name));

                var obj = new Field
                {
                    type = field.FieldType,
                    getter = o => field.GetValue(o),
                    setter = (o, v) => field.SetValue(o, v)
                };

                foreach (var name in names)
                    fields[(type, name)] = obj;
            }

            foreach (var field in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(field, typeof(NoScriptAccessAttribute)))
                    continue;

                bool keepExistingName = !Attribute.IsDefined(field, typeof(ScriptNameAttribute));

                var names = new List<string>();

                if (!keepExistingName)
                {
                    foreach (var a in Attribute.GetCustomAttributes(field, typeof(ScriptNameAttribute)))
                    {
                        var c = (ScriptNameAttribute)a;
                        keepExistingName |= c.keepExistingName;
                        names.Add(c.name);
                    }
                }

                if (keepExistingName)
                    names.Add(TransformName(field.Name));

                var obj = new Field
                {
                    type = field.PropertyType,
                    getter = o => field.GetValue(o),
                    setter = (o, v) => field.SetValue(o, v)
                };

                foreach (var name in names)
                    fields[(type, name)] = obj;
            }

            foreach (var field in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Attribute.IsDefined(field, typeof(NoScriptAccessAttribute)))
                    continue;

                bool keepExistingName = !Attribute.IsDefined(field, typeof(ScriptNameAttribute));

                var names = new List<string>();

                if (!keepExistingName)
                {
                    foreach (var a in Attribute.GetCustomAttributes(field, typeof(ScriptNameAttribute)))
                    {
                        var c = (ScriptNameAttribute)a;
                        keepExistingName |= c.keepExistingName;
                        names.Add(c.name);
                    }
                }

                if (keepExistingName)
                    names.Add(TransformName(field.Name));

                var obj = new Field
                {
                    type = typeof(void),
                    getter = o => (o, field),
                    setter = (o, v) => throw new ScriptException($"Cannot assign to method \"{field.Name}\"")
                };

                foreach (var name in names)
                    fields[(type, name)] = obj;
            }
        }

        internal static string TransformName(string name)
        {
            var sb = new StringBuilder();

            foreach (char c in name)
            {
                var lc = c.ToString().ToLowerInvariant();

                if (sb.Length > 0 && lc != c.ToString())
                    sb.Append('_');

                sb.Append(lc);
            }

            return sb.ToString();
        }
    }
}
