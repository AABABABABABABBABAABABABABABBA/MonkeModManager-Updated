using MonkeMenu.Utilities;
using UnityEngine;

namespace MonkeMenu.Mods
{
    /// <summary>Example visual mods. These only change what YOU see.</summary>
    public static class Visual
    {
        private static TextMesh fpsText;
        private static float smoothedDelta;

        public static void ShowFps()
        {
            if (fpsText == null)
            {
                fpsText = Draw.Text(Inputs.Head, "", Vector3.zero, 0.005f, FontStyle.Bold);
                fpsText.transform.localPosition = new Vector3(-0.25f, 0.18f, 0.6f);
                fpsText.transform.localRotation = Quaternion.identity;
            }

            smoothedDelta = Mathf.Lerp(smoothedDelta <= 0f ? Time.unscaledDeltaTime : smoothedDelta, Time.unscaledDeltaTime, 0.1f);
            fpsText.text = $"{Mathf.RoundToInt(1f / smoothedDelta)} FPS";
        }

        public static void HideFps()
        {
            if (fpsText != null) Draw.Destroy(fpsText.gameObject);
            fpsText = null;
        }
    }
}
