using MonkeMenu.Utilities;
using UnityEngine;

namespace MonkeMenu.Mods
{
    /// <summary>
    /// Example movement mods. These only change YOUR player and are marked ModdedOnly,
    /// so they switch off in public lobbies.
    /// </summary>
    public static class Movement
    {
        public static float FlySpeed = 12f;
        public static float LowGravityStrength = 0.6f; // 0 = normal gravity, 1 = no gravity

        public static void Fly()
        {
            if (!Inputs.RightPrimary) return;
            Inputs.Body.velocity = Inputs.Head.forward * FlySpeed;
        }

        public static void LowGravity()
        {
            // Push up against a fraction of gravity every frame.
            Inputs.Body.AddForce(-Physics.gravity * LowGravityStrength * Time.deltaTime, ForceMode.VelocityChange);
        }

        private static GameObject leftPlatform;
        private static GameObject rightPlatform;

        public static void Platforms()
        {
            UpdatePlatform(ref leftPlatform, Inputs.LeftGrip, Inputs.LeftHand);
            UpdatePlatform(ref rightPlatform, Inputs.RightGrip, Inputs.RightHand);
        }

        public static void ClearPlatforms()
        {
            Draw.Destroy(leftPlatform);
            Draw.Destroy(rightPlatform);
            leftPlatform = rightPlatform = null;
        }

        private static void UpdatePlatform(ref GameObject platform, bool held, Transform hand)
        {
            if (held && platform == null)
            {
                // A solid, only-you-can-see platform just under your hand.
                platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = "MonkeMenu Platform";
                platform.transform.localScale = new Vector3(0.28f, 0.015f, 0.28f);
                platform.transform.position = hand.position - Vector3.up * 0.05f;
                platform.transform.rotation = Quaternion.Euler(0f, hand.eulerAngles.y, 0f);
                Draw.SetColor(platform, new Color(0.2f, 0.2f, 0.25f));
            }
            else if (!held && platform != null)
            {
                Draw.Destroy(platform);
                platform = null;
            }
        }
    }
}
