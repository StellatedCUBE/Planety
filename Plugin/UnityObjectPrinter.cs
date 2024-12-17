using KSP.Messages;
using KSP.Sim.impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class UnityObjectPrinter
{
    internal enum ShaderPropertyType
    {
        Float, Texture, Vector, Color, TextureArray
    }

    private static Dictionary<string, List<(string, ShaderPropertyType)>> shaderProperties = null;

    private static Dictionary<string, List<(string, ShaderPropertyType)>> GetShaderPropertiesData()
    {
        Dictionary<string, List<(string, ShaderPropertyType)>> properties = new();
        List<(string, ShaderPropertyType)> current = null;

        try
        {
            using (var file = new StreamReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ShaderData.txt"), Encoding.UTF8))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.StartsWith("Shader \""))
                    {
                        current = new();
                        properties.Add(line.Substring(8, line.Length - 11), current);
                    }

                    else
                    {
                        string[] words = line.Split(' ');

                        int equals = words.IndexOf("=");

                        if (equals > 0 && words[equals - 1].EndsWith(")"))
                        {
                            ShaderPropertyType type;

                            switch (words[equals - 1])
                            {
                                case "2D)": type = ShaderPropertyType.Texture; break;
                                case "Float)": type = ShaderPropertyType.Float; break;
                                case "Vector)": type = ShaderPropertyType.Vector; break;
                                case "Color)": type = ShaderPropertyType.Color; break;
                                case "2DArray)": type = ShaderPropertyType.TextureArray; break;
                                default: continue;
                            }

                            current.Add((words[0], type));
                        }
                    }
                }
            }
        }
        catch (IOException) { }

        return properties;
    }

    internal static List<(string, ShaderPropertyType)> GetShaderProperties(string name)
    {
        shaderProperties ??= GetShaderPropertiesData();

        if (shaderProperties.TryGetValue(name, out var result))
            return result;

        return new();
    }

    public int stringMaxLength = 1024;
    public int listMaxLengthLines = 128;

    public static UnityObjectPrinter printer = new UnityObjectPrinter();

    private int StringWidth(string str)
    {
        return str.Length;
    }

    private string HorizontalConcat(string a, string b)
    {
        var a_lines = a.Split('\n');
        var b_lines = b.Split('\n');
        int line_count = Math.Max(a_lines.Length, b_lines.Length);

        if (line_count == 1)
        {
            return a + b;
        }

        int a_width = 0;
        foreach (string line in a_lines)
        {
            a_width = Math.Max(a_width, StringWidth(line));
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < line_count; i++)
        {
            string left = a_lines.ElementAtOrDefault(i) ?? "";
            string right = b_lines.ElementAtOrDefault(i) ?? "";
            string spaces = new string(' ', a_width - StringWidth(left));
            string line = (left + spaces + right).TrimEnd();
            sb.AppendLine(line);
        }

        return sb.ToString().TrimEnd('\n');
    }

    private string StringifyString(string str)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('\"');

        char[] charArray = str.ToCharArray();
        foreach (var c in charArray)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    int codepoint = Convert.ToInt32(c);
                    if ((codepoint >= 32) && (codepoint <= 126))
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        builder.Append("\\u");
                        builder.Append(codepoint.ToString("x4"));
                    }
                    break;
            }
        }

        builder.Append('\"');

        return builder.ToString();
    }

    public string StringifyObject(object o)
    {
        try
        {
            if (o == null)
            {
                return "null";
            }

            if (o is GameObject go)
            {
#if DEBUG
                return $"{go.name} @ 0x{go.GetCachedPtr().ToInt64():X}";
#else
                return go.name;
#endif
            }

            if (o is Component c)
            {
#if DEBUG
                return $"{c.GetType().FullName} ({c.name}) @ 0x{c.GetCachedPtr().ToInt64():X}";
#else
                return c.GetType().FullName;
#endif
            }

            if (o is string str)
            {
                if (str.Length <= stringMaxLength)
                {
                    return StringifyString(str);
                }
                else
                {
                    return $"{StringifyString(str.Substring(0, stringMaxLength))} ... (Length: {str.Length})";
                }
            }

            if (o is Texture2D tex2d)
            {
                return $"{tex2d.name}\nType: Texture2D\nSize: {tex2d.width}x{tex2d.height}\nMipmaps: {tex2d.mipmapCount}\nFormat: {tex2d.format}";
            }

            if (o is Texture tex)
            {
                int? depth = (tex as Texture2DArray)?.depth;
                return $"{tex.name}\nType: {tex.GetType().Name}\nSize: {tex.width}x{tex.height}{(depth.HasValue ? $"x{depth}" : "")}\nMipmaps: {tex.mipmapCount}\nFormat: {tex.graphicsFormat}\nFilter: {tex.filterMode}";
            }

            if (o is Mesh mesh)
            {
                if (mesh.isReadable)
                {
                    return $"{mesh.name}\nVertices: {mesh.vertices.Length}\nTriangles: {mesh.triangles.Length / 3}\nBounds: {StringifyObject(mesh.bounds)}";
                }
                else
                {
                    return $"{mesh.name}\nReadable: False";
                }
            }

            if (o is Color32 c32)
            {
                return $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
            }

            if (o is Color col)
            {
                return StringifyObject((Color32)col);
            }

            if (o is Shader shader)
            {
                return shader.name;
            }

            if (o is Material mat)
            {
                var props = GetShaderProperties(mat.shader.name);

                if (props.Count == 0)
                    return $"{mat.name}\nShader: {StringifyObject(mat.shader)}\nColor: {StringifyObject(mat.color)}\n{HorizontalConcat("Texture: ", StringifyObject(mat.mainTexture))}";

                StringBuilder sb = new($"{mat.name}\nShader: {StringifyObject(mat.shader)}");

                foreach ((var property, var type) in props)
                {
                    object value;

                    switch (type)
                    {
                        case ShaderPropertyType.Float:
                            value = mat.GetFloat(property);
                            break;

                        case ShaderPropertyType.TextureArray:
                        case ShaderPropertyType.Texture:
                            value = mat.GetTexture(property);
                            break;

                        case ShaderPropertyType.Vector:
                            value = mat.GetVector(property);
                            break;

                        case ShaderPropertyType.Color:
                            value = mat.GetColor(property);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("ShaderPropertyType");
                    }

                    sb.Append('\n');
                    sb.Append(HorizontalConcat(property + ": ", StringifyObject(value)));

                    if (type == ShaderPropertyType.TextureArray && value is Texture2DArray arr)
                    {
                        string directory = Path.Combine(Application.dataPath, "..\\dumps", arr.name);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                            Texture2D pass = new Texture2D(arr.width, arr.height);
                            RenderTexture rt = RenderTexture.GetTemporary(arr.width, arr.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                            RenderTexture prev = RenderTexture.active;
                            RenderTexture.active=(rt);

                            for (int i = 0; i < arr.depth; i++)
                            {
                                Graphics.Blit(arr, rt, i, 0);
                                pass.ReadPixels(new(0, 0, arr.width, arr.height), 0, 0);
                                pass.Apply();
                                File.WriteAllBytes($"{directory}\\{i}.png", pass.EncodeToPNG());
                                
                            }

                            RenderTexture.active=(prev);
                        }
                    }
                }

                return sb.ToString();
            }

            if (o is Vector3 v3)
            {
                return v3.ToString("F3").Substring(1).TrimEnd(')');
            }

            if (o is Quaternion q)
            {
                return q.ToString("F4").Substring(1).TrimEnd(')');
            }

            if (o is Array a)
            {
                StringBuilder sb = new();
                for (int i = 0; i < a.Rank; i++)
                    sb.Append($"[{a.GetLength(i)}]");
                return sb.ToString();
            }

            if (o.GetType().IsConstructedGenericType && o.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                return $"[{o.GetType().GetProperty("Count").GetValue(o)}]";
            }

            if (o.GetType().IsConstructedGenericType && o.GetType().GetGenericTypeDefinition() == typeof(NativeList<>))
            {
                return $"[{o.GetType().GetProperty("Length").GetValue(o)}]";
            }

            if (o is ScriptableObject || o is CelestialBodyComponent || o is MessageCenterMessage || o.GetType().Attributes.HasFlag(TypeAttributes.Serializable) && o.GetType().AssemblyQualifiedName.Contains("Assembly-CSharp"))
            {
                StringBuilder sb = new StringBuilder();

                foreach (FieldInfo field in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    /*if (field.Name == "colliderPrefab" && field.GetValue(o) is GameObject g)
                    {
                        sb.AppendLine(HorizontalConcat(field.Name + ": ", StringifyGameObject(g)));
                    }
                    else*/
                        sb.AppendLine(HorizontalConcat(field.Name + ": ", StringifyObject(field.GetValue(o))));
                }
                /*
                foreach (PropertyInfo field in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    /*if (field.Name == "colliderPrefab" && field.GetValue(o) is GameObject g)
                    {
                        sb.AppendLine(HorizontalConcat(field.Name + ": ", StringifyGameObject(g)));
                    }
                    else
                    sb.AppendLine(HorizontalConcat(field.Name + ": ", StringifyObject(field.GetValue(o))));
                }*/

                return sb.ToString().TrimEnd('\n');
            }

            return o.ToString();
        }

        catch (Exception e)
        {
            return "{ERROR}\n" + e.ToString();
        }
    }

    public string StringifyComponent(Component c)
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(StringifyObject(c));
            sb.AppendLine(new string('-', sb.Length - 2));

            if (c is Transform t)
            {
                string local = $"          Local\n          -----\nPosition  {StringifyObject(t.localPosition)}\nRotation  {StringifyObject(t.localRotation)}\nScale     {StringifyObject(t.localScale)}";
                string global = $"  Global\n  ------\n  {StringifyObject(t.position)}\n  {StringifyObject(t.rotation)}\n  {StringifyObject(t.lossyScale)}";
                sb.Append(HorizontalConcat(local, global));
            }

            else
            {
                foreach (FieldInfo field in c.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    sb.AppendLine(HorizontalConcat(field.Name + ": ", StringifyObject(field.GetValue(c))));
                }

                foreach (PropertyInfo prop in c.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (prop.Name == "name") continue;
                    if (prop.Name == "tag") continue;
                    if (prop.Name == "transform") continue;
                    if (prop.Name == "gameObject") continue;
                    if (prop.Name == "isActiveAndEnabled") continue;
                    sb.AppendLine(HorizontalConcat(prop.Name + ": ", StringifyObject(prop.GetValue(c))));
                }
            }

            return sb.ToString();
        }

        catch (Exception e)
        {
            return "{ERROR}\n" + e.ToString();
        }
    }

    public string StringifyGameObject(GameObject go)
    {
        try
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(StringifyObject(go));
            sb.AppendLine(new string('=', sb.Length - 2));

            sb.Append($"Tag: {go.tag}\nLayer: {go.layer}\nActive: {go.activeSelf} ({go.activeInHierarchy})\n\nComponents:\n");

            foreach (Component c in go.GetComponents<Component>().OrderBy(c2 => c2.GetType().Name.ToLowerInvariant()))
            {
                sb.AppendLine(HorizontalConcat(" -> ", StringifyComponent(c)));
                sb.AppendLine();
            }

            if (go.transform.childCount > 0)
            {
                sb.Append("Children:\n");

                foreach (Transform t in go.transform)
                {
                    sb.AppendLine(HorizontalConcat(" ==> ", StringifyGameObject(t.gameObject)));
                    sb.AppendLine();
                }
            }

            else
            {
                sb.Append("Children: (None)");
            }

            return sb.ToString();
        }

        catch (Exception e)
        {
            return "{ERROR}\n" + e.ToString();
        }
    }
}
