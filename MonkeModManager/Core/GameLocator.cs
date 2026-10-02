using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MonkeModManager.Core
{
    /// <summary>Finds the Gorilla Tag install folder (Steam, any Steam library, or Meta/Oculus).</summary>
    internal static class GameLocator
    {
        private const string SteamAppId = "1533390";
        private const string OculusFolderName = "another-axiom-gorilla-tag";

        public static bool IsGameFolder(string dir) =>
            !string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir, AppInfo.GameExeName));

        public static string Find()
        {
            foreach (var candidate in Candidates())
            {
                try
                {
                    if (IsGameFolder(candidate)) return Path.GetFullPath(candidate);
                }
                catch
                {
                    // bad path in registry/vdf, keep looking
                }
            }
            return null;
        }

        private static IEnumerable<string> Candidates()
        {
            // 1. Steam's uninstall entry for the game
            yield return ReadRegistry(RegistryHive.LocalMachine, RegistryView.Registry64,
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {SteamAppId}", "InstallLocation");
            yield return ReadRegistry(RegistryHive.LocalMachine, RegistryView.Registry32,
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {SteamAppId}", "InstallLocation");

            // 2. Every Steam library folder
            foreach (var lib in SteamLibraries())
                yield return Path.Combine(lib, "steamapps", "common", "Gorilla Tag");

            // 3. Meta / Oculus PC app libraries
            foreach (var lib in OculusLibraries())
                yield return Path.Combine(lib, "Software", OculusFolderName);

            // 4. Common defaults
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Path.Combine(pf86, @"Steam\steamapps\common\Gorilla Tag");
            yield return Path.Combine(pf, @"Oculus\Software\Software", OculusFolderName);
        }

        private static IEnumerable<string> SteamLibraries()
        {
            var steamRoots = new List<string>
            {
                ReadRegistry(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath"),
                ReadRegistry(RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Valve\Steam", "InstallPath"),
                ReadRegistry(RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\Valve\Steam", "InstallPath"),
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in steamRoots.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                string normalizedRoot = root.Replace('/', '\\');
                if (seen.Add(normalizedRoot)) yield return normalizedRoot;

                string vdf = Path.Combine(normalizedRoot, "steamapps", "libraryfolders.vdf");
                string text = null;
                try
                {
                    if (File.Exists(vdf)) text = File.ReadAllText(vdf);
                }
                catch { }
                if (text == null) continue;

                // Matches both the new ("path" "D:\\Games") and old ("1" "D:\\Games") formats.
                foreach (Match m in Regex.Matches(text, "\"(?:path|\\d+)\"\\s+\"(?<p>[^\"]+)\""))
                {
                    string lib = m.Groups["p"].Value.Replace(@"\\", @"\");
                    if (lib.Contains(":") && seen.Add(lib)) yield return lib;
                }
            }
        }

        private static IEnumerable<string> OculusLibraries()
        {
            RegistryKey libraries = null;
            try
            {
                libraries = Registry.CurrentUser.OpenSubKey(@"Software\Oculus VR, LLC\Oculus\Libraries");
            }
            catch { }
            if (libraries == null) yield break;

            using (libraries)
            {
                foreach (var name in libraries.GetSubKeyNames())
                {
                    string path = null;
                    try
                    {
                        using (var key = libraries.OpenSubKey(name))
                            path = key?.GetValue("OriginalPath") as string;
                    }
                    catch { }
                    if (!string.IsNullOrWhiteSpace(path)) yield return path;
                }
            }
        }

        private static string ReadRegistry(RegistryHive hive, RegistryView view, string keyPath, string valueName)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (var key = baseKey.OpenSubKey(keyPath))
                {
                    return key?.GetValue(valueName) as string;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
