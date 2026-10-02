using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MonkeModManager.Internals.SimpleJSON;

namespace MonkeModManager.Core
{
    /// <summary>The list of mods (mods.json) and the order of their groups.</summary>
    internal class ModCatalog
    {
        public readonly List<ModInfo> Mods = new List<ModInfo>();
        public readonly List<string> Groups = new List<string>();
        public bool FromEmbeddedCopy;

        public ModInfo BepInEx => Mods.FirstOrDefault(m => m.IsBepInEx);

        public static async Task<ModCatalog> LoadAsync()
        {
            string json = null;
            bool embedded = false;
            try
            {
                json = await Http.GetStringAsync(AppInfo.ModListUrl).ConfigureAwait(false);
                JSON.Parse(json); // make sure it is valid before trusting it
            }
            catch
            {
                json = null;
            }

            if (json == null)
            {
                json = ReadEmbeddedModList();
                embedded = true;
            }

            var catalog = Parse(json);
            catalog.FromEmbeddedCopy = embedded;
            return catalog;
        }

        private static string ReadEmbeddedModList()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonkeModManager.mods.json"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static ModCatalog Parse(string json)
        {
            var root = JSON.Parse(json);
            var catalog = new ModCatalog();

            var mods = root["mods"].AsArray;
            if (mods != null)
            {
                foreach (JSONNode node in mods.Children)
                {
                    var mod = ModInfo.FromJson(node);
                    if (!string.IsNullOrEmpty(mod.Name) && !string.IsNullOrEmpty(mod.GitPath))
                        catalog.Mods.Add(mod);
                }
            }

            var groups = root["groups"].AsArray;
            if (groups != null)
            {
                foreach (var g in groups.Children.OrderBy(g => g["rank"].AsInt))
                {
                    string name = g["name"];
                    if (catalog.Mods.Any(m => m.Group == name))
                        catalog.Groups.Add(name);
                }
            }
            if (catalog.Mods.Any(m => m.Group == null || !catalog.Groups.Contains(m.Group)))
                catalog.Groups.Add("Uncategorized");

            foreach (var mod in catalog.Mods)
            {
                foreach (var dep in mod.Dependencies)
                    catalog.Mods.FirstOrDefault(m => m.Name == dep)?.Dependents.Add(mod.Name);
            }

            return catalog;
        }

        public string GroupOf(ModInfo mod) =>
            mod.Group != null && Groups.Contains(mod.Group) ? mod.Group : "Uncategorized";
    }
}
