using Planety.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planety.ModLoader
{
    public static class Content
    {
        public static List<Mod> mods;
        static List<Mod.Extension> extensions;

        internal static string currentFileBeingParsed;

        public static void Load()
        {
            extensions = new();
            mods = Walk(Plugin.folder).Concat(Walk(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".."))).OrderBy(m => m.runTime).ToList();
            var map = mods.ToDictionary(m => m.id);
            foreach (var ex in extensions)
            {
                if (map.TryGetValue(ex.id, out var mod))
                    mod.code.contents.Add(ex.code);
                else if (!ex.failSilently)
                    Plugin.PopError(new ScriptException($"Mod \"{ex.id}\" not found") { pos = ex.pos, sourceFile = ex.path });
            }

        }

        public static void LoadAsync()
        {
            PopMessage.ClearAll();
            mods = null;
            Task.Run(Load);
        }

        static IEnumerable<Mod> Walk(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.planety"))
            {
                var mods = TryParse(Path.Combine(path, file));
                if (mods != null)
                    foreach (var mod in mods)
                        yield return mod;
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
                foreach (var mod in Walk(Path.Combine(path, dir)))
                    yield return mod;
        }

        static List<Mod> TryParse(string file)
        {
            Lexer lexer = null;

            try
            {
                List<Mod> list = new();
                var text = File.ReadAllText(file, Encoding.UTF8);
                currentFileBeingParsed = file;
                lexer = new Lexer(text);
                var parser = new Parser(lexer);

                Mod modDef;
                while ((modDef = parser.ParseModDefinition()) != null)
                {
                    modDef.path = file;
                    if (modDef is Mod.Extension ex)
                        extensions.Add(ex);
                    else
                        list.Add(modDef);
                }

                return list;
            }
            catch (Exception e)
            {
                Plugin.PopError(e);
            }

            return null;
        }
    }
}
