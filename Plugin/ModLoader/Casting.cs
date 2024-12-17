using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety.ModLoader
{
    public static class Casting
    {
        static Dictionary<(Type, Type), Func<object, object>> rules = new Dictionary<(Type, Type), Func<object, object>>();

        public static object Cast(object obj, Type to)
        {
            if (obj == null || to == typeof(void))
                return null;

            var from = obj.GetType();

            if (!to.IsAssignableFrom(from))
            {
                if (to == typeof(string))
                    return obj.ToString();

                if (to == typeof(float))
                    return Convert.ToSingle(obj);

                if (to == typeof(double))
                    return Convert.ToDouble(obj);

                if (to == typeof(int))
                    return Convert.ToInt32(obj);

                if (to == typeof(long))
                    return Convert.ToInt64(obj);

                if (to == typeof(Vector3) && obj is Vector3d v3d)
                    return (Vector3)v3d;

                if (to == typeof(Vector3d) && obj is Vector3 v3)
                    return (Vector3d)v3;

                if (to == typeof(Vector3) && obj is IEnumerable ie)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 3)
                    {
                        try
                        {
                            return new Vector3(
                                Convert.ToSingle(list[0]),
                                Convert.ToSingle(list[1]),
                                Convert.ToSingle(list[2])
                            );
                        } catch { }
                    }
                }

                if (to == typeof(Vector3d) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 3)
                    {
                        try
                        {
                            return new Vector3d(
                                Convert.ToDouble(list[0]),
                                Convert.ToDouble(list[1]),
                                Convert.ToDouble(list[2])
                            );
                        }
                        catch { }
                    }
                }

                if (to == typeof(Vector3Int) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 3)
                    {
                        try
                        {
                            return new Vector3Int(
                                Convert.ToInt32(list[0]),
                                Convert.ToInt32(list[1]),
                                Convert.ToInt32(list[2])
                            );
                        }
                        catch { }
                    }
                }

                if (to == typeof(Vector2) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 2)
                    {
                        try
                        {
                            return new Vector2(
                                Convert.ToSingle(list[0]),
                                Convert.ToSingle(list[1])
                            );
                        }
                        catch { }
                    }
                }

                if (to == typeof(Vector2d) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 2)
                    {
                        try
                        {
                            return new Vector3d(
                                Convert.ToDouble(list[0]),
                                Convert.ToDouble(list[1])
                            );
                        }
                        catch { }
                    }
                }

                if (to == typeof(Vector2Int) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 2)
                    {
                        try
                        {
                            return new Vector2Int(
                                Convert.ToInt32(list[0]),
                                Convert.ToInt32(list[1])
                            );
                        }
                        catch { }
                    }
                }

                if (to == typeof(Source<Cubemap>) && (ie = obj as IEnumerable) != null)
                {
                    var list = ie.Cast<object>().ToList();
                    if (list.Count == 6)
                    {
                        var faces = new Source<Texture2D>[]
                        {
                            list[0] as Source<Texture2D>,
                            list[1] as Source<Texture2D>,
                            list[2] as Source<Texture2D>,
                            list[3] as Source<Texture2D>,
                            list[4] as Source<Texture2D>,
                            list[5] as Source<Texture2D>
                        };
                        bool fail = false;
                        foreach (var t2ds in faces)
                            if (t2ds == null)
                                fail = true;
                        if (!fail)
                            return new SourceCubemapFromFaces(faces);
                    }
                }

                if (to.IsConstructedGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return Cast(obj, to.GenericTypeArguments[0]);

                if (to.IsConstructedGenericType && to.GetGenericTypeDefinition() == typeof(List<>) && (ie = obj as IEnumerable) != null)
                {
                    var list = to.GetConstructors().First(c => c.GetParameters().Length == 0).Invoke(null);
                    var add = to.GetMethod("Add");
                    foreach (var item in ie)
                        add.Invoke(list, new object[] { Cast(item, to.GenericTypeArguments[0]) });
                    return list;
                }

                if (obj is List<object> elist && elist.Count == 0)
                {
                    var con = to.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
                    if (con != null)
                        return con.Invoke(null);
                }

                if (obj is IDictionary<string, object> dict)
                {
                    var con = to.GetConstructors().FirstOrDefault(c => c.GetParameters().Length == 0);
                    if (con != null)
                    {
                        obj = con.Invoke(null);

                        foreach (var pair in dict)
                            AST.Assign.Assign_(obj, pair.Key, pair.Value);
                    }
                }

                else
                {
                    foreach (var kvp in rules)
                        if (to.IsAssignableFrom(kvp.Key.Item2) && kvp.Key.Item1.IsAssignableFrom(from))
                            return kvp.Value(obj);
                }
            }

            return obj;
        }

        public static void AddRuleNonGeneric(Type from, Type to, Func<object, object> converter) => rules[(from, to)] = converter;

        public static void AddRule<From, To>(Func<From, To> converter) => rules[(typeof(From), typeof(To))] = o => converter((From)o);

        internal static void Populate()
        {
            AddRule<Color, SourceTexture2DValue<Color32>>(color => new SourceTexture2DValue<Color32>(color, TextureFormat.RGBA32));
            AddRule<double, SourceTexture2DValue<ushort>>(value => new SourceTexture2DValue<ushort>((ushort)(ushort.MaxValue * Math.Max(0.0, Math.Min(1.0, value))), TextureFormat.R16));
            AddRule<double, Rotation>(period => Rotation.Period(period));
        }
    }
}
