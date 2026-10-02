using MonkeMenu.Mods;
using MonkeMenu.Utilities;

namespace MonkeMenu.Menu
{
    /// <summary>
    /// THIS IS THE FILE YOU EDIT TO ADD YOUR OWN STUFF.
    ///
    /// Each Category is a page. Category 0 is the main page and links to the others.
    /// To add a mod: write a method in the Mods folder, then add a ButtonInfo below that calls it.
    /// </summary>
    public static class Buttons
    {
        public static readonly Category[] Categories =
        {
            // 0: Main page - buttons here open the other categories
            new Category("Main",
                Link("Movement", 1),
                Link("Visual", 2),
                Link("Settings", 3)),

            // 1: Movement
            new Category("Movement",
                new ButtonInfo("Fly", "Hold A to fly where you look")
                {
                    OnUpdate = Movement.Fly
                },
                new ButtonInfo("Platforms", "Hold grip to stand on a platform")
                {
                    OnUpdate = Movement.Platforms,
                    OnDisable = Movement.ClearPlatforms
                },
                new ButtonInfo("Low Gravity", "Floaty jumps")
                {
                    OnUpdate = Movement.LowGravity
                }),

            // 2: Visual (local only - nobody else sees these)
            new Category("Visual",
                new ButtonInfo("Show FPS", "Shows your frame rate")
                {
                    ModdedOnly = false,
                    OnUpdate = Visual.ShowFps,
                    OnDisable = Visual.HideFps
                }),

            // 3: Settings / examples
            new Category("Settings",
                new ButtonInfo("Example Button", "Runs once each time you press it")
                {
                    IsToggle = false,
                    ModdedOnly = false,
                    OnEnable = () => Notifications.Send("Hello from your menu!")
                },
                new ButtonInfo("Disable All Mods", "Turns every toggle off")
                {
                    IsToggle = false,
                    ModdedOnly = false,
                    OnEnable = MenuManager.DisableAll
                }),
        };

        /// <summary>A button that opens another category.</summary>
        private static ButtonInfo Link(string text, int category) =>
            new ButtonInfo(text)
            {
                IsToggle = false,
                ModdedOnly = false,
                OnEnable = () => MenuManager.OpenCategory(category)
            };
    }
}
