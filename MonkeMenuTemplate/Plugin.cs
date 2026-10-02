using BepInEx;
using MonkeMenu.Menu;
using MonkeMenu.Utilities;

namespace MonkeMenu
{
    /// <summary>
    /// Change these to your own mod's details before you publish it.
    /// The GUID must be unique, so use something like "com.yourname.gorillatag.yourmenu".
    /// </summary>
    public static class PluginInfo
    {
        public const string Guid = "com.yourname.gorillatag.monkemenu";
        public const string Name = "Monke Menu";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded. Hold Y (left secondary) to open the menu.");
        }

        private void Update()
        {
            MenuManager.Update();
            Notifications.Update();
        }

        public static void Log(string message) => Instance?.Logger.LogInfo(message);
    }
}
