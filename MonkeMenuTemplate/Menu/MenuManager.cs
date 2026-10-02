using System;
using System.Collections.Generic;
using System.Linq;
using MonkeMenu.Utilities;
using UnityEngine;

namespace MonkeMenu.Menu
{
    /// <summary>
    /// Builds the menu on your left hand while Y is held, handles button presses
    /// from your right hand, and runs every enabled mod each frame.
    /// You normally don't need to touch this file - add buttons in Buttons.cs instead.
    /// </summary>
    public static class MenuManager
    {
        // Menu-local axes: +X points out of the menu face, +Y is sideways, +Z is up.
        private const float MenuWidth = 0.30f;
        private const float MenuHeight = 0.40f;
        private const float ButtonHeight = 0.04f;
        private const float ButtonSpacing = 0.048f;
        private const float Depth = 0.01f;

        private static GameObject menuRoot;
        private static GameObject pointer;
        private static readonly List<(GameObject obj, Action onPress)> pressables = new List<(GameObject, Action)>();

        private static int category;
        private static int page;
        private static float nextPressTime;
        private static bool wasTouching;
        private static bool lastLobbySafe = true;

        public static void Update()
        {
            if (!Inputs.Ready) return;

            bool wantOpen = Inputs.LeftSecondary;
            if (wantOpen && menuRoot == null) Open();
            else if (!wantOpen && menuRoot != null) Close();

            if (menuRoot != null) HandlePointer();

            RunMods();
        }

        #region Opening / drawing

        private static void Open()
        {
            Build();

            pointer = Draw.Sphere(0.012f, Color.white);
            pointer.name = "MonkeMenu Pointer";
            pointer.transform.SetParent(Inputs.RightHand, false);
            pointer.transform.localPosition = MenuSettings.PointerOffset;
        }

        private static void Close()
        {
            Draw.Destroy(menuRoot);
            Draw.Destroy(pointer);
            menuRoot = null;
            pointer = null;
            pressables.Clear();
        }

        /// <summary>Redraws the menu (call after changing pages or toggling something).</summary>
        public static void Refresh()
        {
            if (menuRoot == null) return;
            Draw.Destroy(menuRoot);
            pressables.Clear();
            Build();
        }

        private static void Build()
        {
            menuRoot = new GameObject("MonkeMenu");
            menuRoot.transform.SetParent(Inputs.LeftHand, false);
            menuRoot.transform.localPosition = MenuSettings.MenuOffset;
            menuRoot.transform.localRotation = Quaternion.Euler(MenuSettings.MenuRotation);

            var background = Draw.Cube(menuRoot.transform, Vector3.zero, new Vector3(Depth, MenuWidth, MenuHeight), MenuSettings.BackgroundColor);
            background.name = "Background";

            var cat = Buttons.Categories[category];
            int pageCount = PageCount(cat);
            page = Mathf.Clamp(page, 0, pageCount - 1);

            float top = MenuHeight / 2f;
            string title = category == 0 ? MenuSettings.Title : $"{MenuSettings.Title} - {cat.Name}";
            if (pageCount > 1) title += $" ({page + 1}/{pageCount})";
            Draw.Text(menuRoot.transform, title, new Vector3(Depth, 0f, top - 0.03f), 0.0055f, FontStyle.Bold);

            float y = top - 0.075f;
            foreach (var button in cat.Buttons.Skip(page * MenuSettings.ButtonsPerPage).Take(MenuSettings.ButtonsPerPage))
            {
                var b = button;
                var color = b.IsToggle && b.Enabled ? MenuSettings.ButtonOnColor : MenuSettings.ButtonColor;
                AddButton(b.Text, new Vector3(0f, 0f, y), MenuWidth - 0.03f, color, () => Press(b));
                y -= ButtonSpacing;
            }

            // Bottom row: < Back >
            float bottom = -top + 0.035f;
            float third = (MenuWidth - 0.04f) / 3f;
            // In menu space -Y is the viewer's right, so "next" goes on -Y.
            AddButton("<", new Vector3(0f, third + 0.005f, bottom), third, MenuSettings.NavButtonColor, () => ChangePage(-1));
            if (category != 0)
                AddButton("Back", new Vector3(0f, 0f, bottom), third, MenuSettings.NavButtonColor, () => OpenCategory(0));
            AddButton(">", new Vector3(0f, -third - 0.005f, bottom), third, MenuSettings.NavButtonColor, () => ChangePage(1));
        }

        private static void AddButton(string text, Vector3 position, float width, Color color, Action onPress)
        {
            var cube = Draw.Cube(menuRoot.transform, position + new Vector3(Depth * 0.6f, 0f, 0f),
                new Vector3(Depth, width, ButtonHeight), color);
            cube.name = "Button: " + text;
            Draw.Text(menuRoot.transform, text, position + new Vector3(Depth * 1.2f, 0f, 0f), 0.0045f, FontStyle.Normal);
            pressables.Add((cube, onPress));
        }

        private static int PageCount(Category cat) =>
            Math.Max(1, Mathf.CeilToInt(cat.Buttons.Length / (float)MenuSettings.ButtonsPerPage));

        #endregion

        #region Input

        private static void HandlePointer()
        {
            Vector3 tip = pointer.transform.position;
            (GameObject obj, Action onPress)? touched = null;

            foreach (var p in pressables)
            {
                if (p.obj == null) continue;
                // Work in the button's own space, where the cube spans -0.5..0.5 on each axis.
                Vector3 local = p.obj.transform.InverseTransformPoint(tip);
                if (Mathf.Abs(local.y) <= 0.5f && Mathf.Abs(local.z) <= 0.5f && Mathf.Abs(local.x) <= 3f)
                {
                    touched = p;
                    break;
                }
            }

            // Only fire once per touch, and not faster than the cooldown.
            if (touched.HasValue && !wasTouching && Time.time >= nextPressTime)
            {
                nextPressTime = Time.time + MenuSettings.PressCooldown;
                Inputs.Vibrate(right: true, MenuSettings.HapticStrength, MenuSettings.HapticDuration);
                touched.Value.onPress();
            }
            wasTouching = touched.HasValue;
        }

        private static void Press(ButtonInfo button)
        {
            if (button.ModdedOnly && !LobbyCheck.IsSafe)
            {
                Notifications.Send($"{button.Text} only works offline or in a modded lobby.");
                return;
            }

            if (!button.IsToggle)
            {
                Safe(button.OnEnable, button);
                if (!string.IsNullOrEmpty(button.Tooltip)) Notifications.Send(button.Tooltip);
            }
            else
            {
                SetEnabled(button, !button.Enabled);
                Notifications.Send($"{button.Text}: {(button.Enabled ? "ON" : "OFF")}" +
                                   (button.Enabled && !string.IsNullOrEmpty(button.Tooltip) ? $" - {button.Tooltip}" : ""));
            }
            Refresh();
        }

        public static void SetEnabled(ButtonInfo button, bool enabled)
        {
            if (button.Enabled == enabled) return;
            button.Enabled = enabled;
            Safe(enabled ? button.OnEnable : button.OnDisable, button);
        }

        public static void OpenCategory(int index)
        {
            category = Mathf.Clamp(index, 0, Buttons.Categories.Length - 1);
            page = 0;
            Refresh();
        }

        private static void ChangePage(int delta)
        {
            int count = PageCount(Buttons.Categories[category]);
            page = (page + delta + count) % count;
            Refresh();
        }

        public static void DisableAll()
        {
            foreach (var b in AllButtons()) SetEnabled(b, false);
        }

        #endregion

        #region Running mods

        private static IEnumerable<ButtonInfo> AllButtons() => Buttons.Categories.SelectMany(c => c.Buttons);

        private static void RunMods()
        {
            bool safe = LobbyCheck.IsSafe;

            // Joined a normal public lobby: switch off anything that's modded-only.
            if (!safe && lastLobbySafe)
            {
                bool any = false;
                foreach (var b in AllButtons().Where(b => b.Enabled && b.ModdedOnly))
                {
                    SetEnabled(b, false);
                    any = true;
                }
                if (any) Notifications.Send("Public lobby: gameplay mods turned off.");
                Refresh();
            }
            lastLobbySafe = safe;

            foreach (var b in AllButtons())
            {
                if (b.IsToggle && b.Enabled && (safe || !b.ModdedOnly))
                    Safe(b.OnUpdate, b);
            }
        }

        private static void Safe(Action action, ButtonInfo button)
        {
            if (action == null) return;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // One broken mod shouldn't take down the whole menu.
                Plugin.Log($"[{button.Text}] {ex}");
                if (button.IsToggle && button.Enabled)
                {
                    button.Enabled = false;
                    Notifications.Send($"{button.Text} crashed and was turned off. Check the BepInEx log.");
                }
            }
        }

        #endregion
    }
}
