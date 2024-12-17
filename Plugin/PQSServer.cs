using KSP.Rendering.Planets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    class PQSServer
    {
        bool running = true;
        ManualResetEvent runningCheckHandle = new(false);
        Material block;

        public void Run()
        {
            using (HttpListener listener = new())
            {
                listener.Prefixes.Add("http://*:43213/");
                listener.Start();

                while (running)
                {
                    var ctxResult = listener.BeginGetContext(result =>
                    {
                        var ctx = listener.EndGetContext(result);

                        var arg = ctx.Request.Url.Query.TrimStart('?').Split('=');

                        if (arg.Length == 2)
                        {
                            using (var writer = new StreamWriter(ctx.Response.OutputStream))
                            {
                                writer.WriteLine("OK");
                            }

                            Plugin.RunOnMainThread(() =>
                            {
                                var pqs = UnityEngine.Object.FindObjectOfType<PQS>();
                                if (!pqs) return;
                                var mat = pqs.data.materialSettings.surfaceMaterial;
                                if (!mat) return;

                                Plugin.Log(BepInEx.Logging.LogLevel.Info, $"Setting PQS field {arg[0]}");

                                if (arg[1].Contains("c"))
                                {
                                    var vec = arg[1].Split('c');
                                    mat.SetVector(arg[0], new Vector4(
                                        float.Parse(vec[0]),
                                        float.Parse(vec[1]),
                                        float.Parse(vec[2]),
                                        float.Parse(vec[3])
                                    ));
                                }
                                else if (arg[1].Contains("p"))
                                {
                                    var vec = arg[1].Split('p');
                                    mat.SetColor(arg[0], new Color(
                                        float.Parse(vec[0]),
                                        float.Parse(vec[1]),
                                        float.Parse(vec[2]),
                                        float.Parse(vec[3])
                                    ));
                                }
                                else
                                    mat.SetFloat(arg[0], float.Parse(arg[1]));
                            });

                            return;
                        }

                        if (arg.Length == 1 && arg[0].Length > 4)
                        {
                            using (var writer = new StreamWriter(ctx.Response.OutputStream))
                            {
                                writer.WriteLine("OK");
                            }

                            Plugin.RunOnMainThread(() =>
                            {
                                var pqs = UnityEngine.Object.FindObjectOfType<PQS>();
                                if (!pqs) return;
                                var mat = pqs.data.materialSettings.surfaceMaterial;
                                if (!mat) return;

                                switch (arg[0])
                                {
                                    case "mat_c":
                                        block = mat;
                                        break;

                                    case "mat_p":
                                        mat.CopyPropertiesFromMaterial(block);
                                        break;

                                    case "log_o":
                                        Plugin.Log(BepInEx.Logging.LogLevel.Info, "Logging request by server:\n" + UnityObjectPrinter.printer.StringifyGameObject(pqs.gameObject));
                                        break;

                                    case "log_c":
                                        //Plugin.logNextFrame = true;
                                        HarmonyPatch_LogMethodCall.logging = true;
                                        break;

                                    case "gpqssm":
                                        AssetLoader.gpqssm = new Material(pqs.settings.pqsShadowMaterial);
                                        break;
                                }
                            });

                            return;
                        }

                        Plugin.RunOnMainThread(() =>
                        {
                            var pqs = UnityEngine.Object.FindObjectOfType<PQS>();

                            if (!pqs)
                            {
                                using (var writer = new StreamWriter(ctx.Response.OutputStream))
                                {
                                    writer.WriteLine("No PQS");
                                }
                                return;
                            }

                            var mat = pqs.data.materialSettings.surfaceMaterial;

                            if (!mat)
                            {
                                using (var writer = new StreamWriter(ctx.Response.OutputStream))
                                {
                                    writer.WriteLine("No PQS material");
                                }
                                return;
                            }

                            using (var writer = new StreamWriter(ctx.Response.OutputStream))
                            {
                                writer.WriteLine("<p>");
                                for (var t = pqs.transform; t; t = t.parent)
                                {
                                    writer.Write($"{t.name}/");
                                }
                                writer.WriteLine("</p><p><button onlick='fetch(`/?mat_c`)'>Material copy</button> <button onlick='fetch(`/?mat_p`)'>Material paste</button></p>");

                                foreach ((var name, var type) in UnityObjectPrinter.GetShaderProperties(mat.shader.name))
                                {
                                    switch (type)
                                    {
                                        case UnityObjectPrinter.ShaderPropertyType.Float:
                                            writer.WriteLine($"<p>{name}: <input id='{name}' type=text value={mat.GetFloat(name)} style='width:21em'><button onclick='fetch(`/?{name}=${{document.getElementById(`{name}`).value}}`)'>Set</button></p>");
                                            break;
                                        case UnityObjectPrinter.ShaderPropertyType.Vector:
                                            Vector4d data = mat.GetVector(name);
                                            writer.WriteLine($"<p>{name}: <input id='{name}-0' type=text value={data.x} style='width:5em'><input id='{name}-1' type=text value={data.y} style='width:5em'><input id='{name}-2' type=text value={data.z} style='width:5em'><input id='{name}-3' type=text value={data.w} style='width:5em'><button onclick='fetch(`/?{name}=${{document.getElementById(`{name}-0`).value}}c${{document.getElementById(`{name}-1`).value}}c${{document.getElementById(`{name}-2`).value}}c${{document.getElementById(`{name}-3`).value}}`)'>Set</button></p>");
                                            break;
                                        case UnityObjectPrinter.ShaderPropertyType.Color:
                                            Color col = mat.GetColor(name);
                                            writer.WriteLine($"<p>{name}: <input id='{name}-0' type=text value={col.r} style='width:5em'><input id='{name}-1' type=text value={col.g} style='width:5em'><input id='{name}-2' type=text value={col.b} style='width:5em'><input id='{name}-3' type=text value={col.a} style='width:5em'><button onclick='fetch(`/?{name}=${{document.getElementById(`{name}-0`).value}}p${{document.getElementById(`{name}-1`).value}}p${{document.getElementById(`{name}-2`).value}}p${{document.getElementById(`{name}-3`).value}}`)'>Set</button></p>");
                                            break;
                                    }
                                }

                                writer.WriteLine($"<script>{File.ReadAllText(@"z:\home\gemdude46\games\KSP2\bop.js")}</script>");
                            }
                        });
                    }, null);

                    WaitHandle.WaitAny(new[] { ctxResult.AsyncWaitHandle, runningCheckHandle });
                }
            }
        }

        public void Stop()
        {
            running = false;
            runningCheckHandle.Set();
        }
    }
}
