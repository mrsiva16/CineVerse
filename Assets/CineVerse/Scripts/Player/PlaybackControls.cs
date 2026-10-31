using UnityEngine;
using UnityEngine.XR;
using CineVerse.Cinema;

namespace CineVerse.Player
{
    /// <summary>
    /// Local play/pause: Space on desktop, the right controller's A button (primary button) in VR.
    /// Phase 1 only. In Phase 5 only the room host will be allowed to control playback.
    /// </summary>
    public class PlaybackControls : MonoBehaviour
    {
        public CinemaScreen cinema;
        bool buttonWasDown;

        void Update()
        {
            if (cinema == null) return;

            if (DesktopInput.PlayPausePressed()) cinema.TogglePlay();

            InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool down;
            if (right.isValid && right.TryGetFeatureValue(CommonUsages.primaryButton, out down))
            {
                if (down && !buttonWasDown) cinema.TogglePlay();
                buttonWasDown = down;
            }
        }
    }
}
