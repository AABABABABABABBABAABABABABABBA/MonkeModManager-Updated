using UnityEngine;

namespace MonkeMenu.Utilities
{
    /// <summary>
    /// One place for everything that talks to the game's player and controllers.
    /// If a Gorilla Tag update renames something, this is the only file you should need to fix.
    /// </summary>
    public static class Inputs
    {
        public static bool Ready => GorillaTagger.Instance != null && ControllerInputPoller.instance != null;

        // Buttons (Quest layout: left X/Y, right A/B)
        public static bool LeftPrimary => ControllerInputPoller.instance.leftControllerPrimaryButton;     // X
        public static bool LeftSecondary => ControllerInputPoller.instance.leftControllerSecondaryButton; // Y
        public static bool RightPrimary => ControllerInputPoller.instance.rightControllerPrimaryButton;   // A
        public static bool RightSecondary => ControllerInputPoller.instance.rightControllerSecondaryButton; // B
        public static bool LeftGrip => ControllerInputPoller.instance.leftGrab;
        public static bool RightGrip => ControllerInputPoller.instance.rightGrab;
        public static float LeftTrigger => ControllerInputPoller.instance.leftControllerIndexFloat;
        public static float RightTrigger => ControllerInputPoller.instance.rightControllerIndexFloat;

        // Body
        public static Transform LeftHand => GorillaTagger.Instance.leftHandTransform;
        public static Transform RightHand => GorillaTagger.Instance.rightHandTransform;
        public static Transform Head => GorillaTagger.Instance.headCollider.transform;
        public static Rigidbody Body => GorillaTagger.Instance.rigidbody;

        public static void Vibrate(bool right, float strength, float duration)
        {
            GorillaTagger.Instance.StartVibration(!right, strength, duration);
        }
    }
}
