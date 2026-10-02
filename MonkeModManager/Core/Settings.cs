using System;
using System.Collections.Generic;
using System.IO;

namespace MonkeModManager.Core
{
    /// <summary>Tiny key=value settings file in %AppData%\MonkeModManager.</summary>
    internal class Settings
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonkeModManager", "settings.ini");

        public string GamePath;
        public bool AutoUpdateBepInEx = true;

        public static Settings Load()
        {
            var settings = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return settings;
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0) values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }

                if (values.TryGetValue("GamePath", out var path)) settings.GamePath = path;
                if (values.TryGetValue("AutoUpdateBepInEx", out var auto) && bool.TryParse(auto, out var b))
                    settings.AutoUpdateBepInEx = b;
            }
            catch
            {
                // corrupt settings are not worth crashing over
            }
            return settings;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, new[]
                {
                    "GamePath=" + (GamePath ?? ""),
                    "AutoUpdateBepInEx=" + AutoUpdateBepInEx,
                });
            }
            catch { }
        }
    }
}
