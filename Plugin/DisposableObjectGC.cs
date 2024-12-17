using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Planety
{
    public static class DisposableObjectGC
    {
        internal class BodyLock : MonoBehaviour { public string bodyId; }

        static List<string> bodyLocks = new();

        static Dictionary<string, List<UnityEngine.Object>> objects = new();

        static Dictionary<string, List<Action>> onFree = new();

        public static void RegisterObject(string bodyId, UnityEngine.Object obj) => Plugin.RunOnMainThread(() =>
        {
            if (objects.TryGetValue(bodyId, out var list))
                list.Add(obj);
            else
                objects.Add(bodyId, new List<UnityEngine.Object> { obj });
        });

        public static void RegisterObject(CelestialBodyData body, UnityEngine.Object obj) => RegisterObject(body.id, obj);

        public static void OnFree(string bodyId, Action action) => Plugin.RunOnMainThread(() =>
        {
            if (onFree.TryGetValue(bodyId, out var list))
                list.Add(action);
            else
                onFree.Add(bodyId, new List<Action> { action });
        });

        public static bool IsLocked(string bodyId) => bodyLocks.Contains(bodyId) || BodyLock.FindObjectsOfType<BodyLock>().Any(l => l.bodyId == bodyId);

        public static void Lock(string bodyId) => bodyLocks.Add(bodyId);

        public static void Lock(CelestialBodyData body) => Lock(body.id);

        public static void Release(string bodyId) => bodyLocks.Remove(bodyId);

        public static void Release(CelestialBodyData body) => Release(body.id);

        public static void FreeUnused()
        {
            foreach (var pair in objects.Where(p => !IsLocked(p.Key)).ToList())
            {
                Plugin.ALLog("GC: Freeing " + pair.Key);
                objects.Remove(pair.Key);
                foreach (var obj in pair.Value)
                    if (obj)
                        BodyLock.Destroy(obj);
                if (onFree.TryGetValue(pair.Key, out var actions))
                    foreach (var action in actions)
                        action();
            }
        }

        internal static void UseCache<T>(ref Source<T> field, CelestialBodyData body) where T: UnityEngine.Object
        {
            if (field == null || field is SourceDisposableObjectCache<T> || field is SourceConstant<T>)
                return;

            if (typeof(T) == typeof(Texture2D))
            {
                if (body.textureCacheSourceMap.TryGetValue((Source<Texture2D>)field, out var cache))
                    field = (Source<T>)cache;
                else
                {
                    var old = (Source<Texture2D>)field;
                    body.textureCacheSourceMap.Add(old, (SourceDisposableObjectCache<Texture2D>)(field = new SourceDisposableObjectCache<T>(field, body.id)));
                }
            }
            else
                field = new SourceDisposableObjectCache<T>(field, body.id);
        }
    }
}
