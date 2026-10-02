using System;

namespace MonkeMenu.Menu
{
    /// <summary>One button on the menu.</summary>
    public class ButtonInfo
    {
        /// <summary>Text shown on the button.</summary>
        public string Text;

        /// <summary>Shown as a notification when the button is pressed.</summary>
        public string Tooltip;

        /// <summary>
        /// true  = stays on/off (OnEnable once, OnUpdate every frame while on, OnDisable once).
        /// false = a one-shot button (only OnEnable runs, each press).
        /// </summary>
        public bool IsToggle = true;

        /// <summary>
        /// If true the mod only runs while offline or in a modded lobby, and switches itself
        /// off if you join a normal public lobby. Leave this on for anything that changes gameplay.
        /// </summary>
        public bool ModdedOnly = true;

        /// <summary>Current on/off state of a toggle.</summary>
        public bool Enabled;

        public Action OnEnable;
        public Action OnUpdate;
        public Action OnDisable;

        public ButtonInfo(string text, string tooltip = null)
        {
            Text = text;
            Tooltip = tooltip;
        }
    }

    /// <summary>A page of buttons. The first category is the main page.</summary>
    public class Category
    {
        public string Name;
        public ButtonInfo[] Buttons;

        public Category(string name, params ButtonInfo[] buttons)
        {
            Name = name;
            Buttons = buttons;
        }
    }
}
