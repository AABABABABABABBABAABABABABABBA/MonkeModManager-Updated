using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace MonkeModManager.Core
{
    /// <summary>Puts downloaded files into the game folder and reads what is already installed.</summary>
    internal static class Installer
    {
        public static string BepInExDir(string gameDir) => Path.Combine(gameDir, "BepInEx");
        public static string PluginsDir(string gameDir) => Path.Combine(gameDir, "BepInEx", "plugins");
        public static string ConfigDir(string gameDir) => Path.Combine(gameDir, "BepInEx", "config");

        public static bool IsGameRunning() =>
            Process.GetProcessesByName(AppInfo.GameProcessName).Length > 0;

        /// <summary>Version of the installed BepInEx, or null if BepInEx isn't installed.</summary>
        public static Version GetInstalledBepInExVersion(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir)) return null;
            string dll = Path.Combine(gameDir, "BepInEx", "core", "BepInEx.dll");
            if (!File.Exists(dll)) return null;
            try
            {
                var info = FileVersionInfo.GetVersionInfo(dll);
                return ReleaseResolver.ParseVersion(info.ProductVersion)
                       ?? ReleaseResolver.ParseVersion(info.FileVersion)
                       ?? new Version(0, 0);
            }
            catch
            {
                return new Version(0, 0);
            }
        }

        /// <summary>Rough check whether a mod already has files in the plugins folder.</summary>
        public static bool IsModInstalled(string gameDir, ModInfo mod)
        {
            if (string.IsNullOrEmpty(gameDir)) return false;
            if (mod.IsBepInEx) return GetInstalledBepInExVersion(gameDir) != null;

            string plugins = PluginsDir(gameDir);
            string compact = FolderName(mod);
            return Directory.Exists(Path.Combine(plugins, compact))
                   || Directory.Exists(Path.Combine(plugins, mod.Name))
                   || File.Exists(Path.Combine(plugins, compact + ".dll"));
        }

        public static void Install(string gameDir, ModInfo mod, byte[] data)
        {
            string fileName = Path.GetFileName(new Uri(mod.DownloadUrl).LocalPath);
            string target = mod.InstallLocation != null ? Path.Combine(gameDir, mod.InstallLocation) : null;

            if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                string dir = target ?? Path.Combine(PluginsDir(gameDir), FolderName(mod));
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, fileName), data);

                // Remove a stale loose copy so the plugin doesn't get loaded twice.
                string loose = Path.Combine(PluginsDir(gameDir), fileName);
                if (!PathsEqual(Path.Combine(dir, fileName), loose) && File.Exists(loose))
                    File.Delete(loose);
            }
            else
            {
                ExtractZip(data, target ?? gameDir);
            }

            if (mod.IsBepInEx)
            {
                // Make sure the folders people expect exist straight away.
                Directory.CreateDirectory(PluginsDir(gameDir));
                Directory.CreateDirectory(ConfigDir(gameDir));
            }
        }

        public static void ExtractZip(byte[] data, string destination)
        {
            string root = Path.GetFullPath(destination);
            if (!root.EndsWith(Path.DirectorySeparatorChar.ToString())) root += Path.DirectorySeparatorChar;

            using (var ms = new MemoryStream(data))
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullPath = Path.GetFullPath(Path.Combine(root, entry.FullName));

                    // Never write outside the destination ("zip slip").
                    if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(fullPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                    entry.ExtractToFile(fullPath, true);
                }
            }
        }

        public static void ExtractZipFile(string zipPath, string destination)
        {
            ExtractZip(File.ReadAllBytes(zipPath), destination);
        }

        public static void UninstallAllMods(string gameDir)
        {
            string plugins = PluginsDir(gameDir);
            if (!Directory.Exists(plugins)) return;
            foreach (var d in Directory.GetDirectories(plugins)) Directory.Delete(d, true);
            foreach (var f in Directory.GetFiles(plugins)) File.Delete(f);
        }

        /// <summary>Completely removes BepInEx (and therefore every mod) from the game.</summary>
        public static void UninstallBepInEx(string gameDir)
        {
            string bepinex = BepInExDir(gameDir);
            if (Directory.Exists(bepinex)) Directory.Delete(bepinex, true);

            foreach (var file in new[] { "winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt" })
            {
                string path = Path.Combine(gameDir, file);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public static void BackupMods(string gameDir, string zipPath)
        {
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(PluginsDir(gameDir), zipPath, CompressionLevel.Optimal, false);
        }

        public static void RestoreMods(string gameDir, string zipPath)
        {
            string plugins = PluginsDir(gameDir);
            Directory.CreateDirectory(plugins);
            ExtractZipFile(zipPath, plugins);
        }

        private static string FolderName(ModInfo mod) => Regex.Replace(mod.Name, @"\s+", string.Empty);

        private static bool PathsEqual(string a, string b) =>
            string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
