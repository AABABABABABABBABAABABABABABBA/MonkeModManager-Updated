using UnityEngine;

namespace MonkeMenu.Utilities
{
    /// <summary>Short messages that float in front of your face for a few seconds.</summary>
    public static class Notifications
    {
        private const float ShowSeconds = 3f;

        private static TextMesh text;
        private static float hideAt;

        public static void Send(string message)
        {
            Plugin.Log(message);
            if (!Inputs.Ready) return;

            if (text == null)
            {
                text = Draw.Text(Inputs.Head, "", Vector3.zero, 0.006f, FontStyle.Bold);
                // Head space: +Z forward, +Y up. Put it slightly below the centre of view.
                text.transform.localPosition = new Vector3(0f, -0.12f, 0.6f);
                text.transform.localRotation = Quaternion.identity;
            }

            text.text = message;
            text.gameObject.SetActive(true);
            hideAt = Time.time + ShowSeconds;
        }

        public static void Update()
        {
            if (text != null && text.gameObject.activeSelf && Time.time >= hideAt)
                text.gameObject.SetActive(false);
        }
    }
}
