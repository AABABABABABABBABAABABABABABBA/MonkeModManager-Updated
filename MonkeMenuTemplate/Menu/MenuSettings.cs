using UnityEngine;

namespace MonkeMenu.Menu
{
    /// <summary>Everything about how the menu looks and feels. Tweak freely.</summary>
    public static class MenuSettings
    {
        public static string Title = PluginInfo.Name;

        // Colours
        public static Color BackgroundColor = new Color(0.10f, 0.10f, 0.14f);
        public static Color ButtonColor = new Color(0.22f, 0.22f, 0.30f);
        public static Color ButtonOnColor = new Color(0.35f, 0.75f, 0.40f);
        public static Color NavButtonColor = new Color(0.30f, 0.30f, 0.45f);
        public static Color TextColor = Color.white;

        // Layout
        public static int ButtonsPerPage = 6;

        /// <summary>Where the menu sits relative to your left hand.</summary>
        public static Vector3 MenuOffset = new Vector3(0.05f, 0f, 0f);
        public static Vector3 MenuRotation = Vector3.zero;

        /// <summary>Where the "finger" you press buttons with sits relative to your right hand.</summary>
        public static Vector3 PointerOffset = new Vector3(0f, -0.1f, 0f);

        // Feel
        public static float PressCooldown = 0.25f;
        public static float HapticStrength = 0.3f;
        public static float HapticDuration = 0.04f;
    }
}
