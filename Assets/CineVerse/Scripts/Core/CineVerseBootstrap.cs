using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using CineVerse.Cinema;
using CineVerse.Player;
using CineVerse.Theatre;

namespace CineVerse.Core
{
    /// <summary>
    /// Entry point for the Phase 1 scene. Builds the theatre, wires the screen, then spawns either
    /// the VR rig (headset active) or the desktop test rig. Add this to ONE empty GameObject in an
    /// otherwise empty scene (or use the menu CineVerse > Create Phase 1 Scene).
    /// </summary>
    [RequireComponent(typeof(CinemaScreen))]
    public class CineVerseBootstrap : MonoBehaviour
    {
        [Tooltip("Tick to use keyboard and mouse even if a headset is connected.")]
        public bool forceDesktopMode = false;

        TheatreBuilder theatre;
        CinemaScreen cinema;

        IEnumerator Start()
        {
            // Wait one frame so the XR system has finished initialising before we ask about it.
            yield return null;

            theatre = GetComponent<TheatreBuilder>();
            if (theatre == null) theatre = gameObject.AddComponent<TheatreBuilder>();
            cinema = GetComponent<CinemaScreen>();

            theatre.Build();
            cinema.Initialize(theatre.ScreenRenderer, theatre.Lights);

            bool vr = !forceDesktopMode && XRSettings.isDeviceActive;
            if (vr)
            {
                VRRig rig = VRRig.Create(theatre.SpawnPosition, theatre.SpawnRotation);
                VRLocomotion loco = rig.gameObject.AddComponent<VRLocomotion>();
                loco.rig = rig;
            }
            else
            {
                DesktopRig.Create(theatre.SpawnPosition, theatre.SpawnRotation);
            }

            PlaybackControls controls = gameObject.AddComponent<PlaybackControls>();
            controls.cinema = cinema;

            Debug.Log("[CineVerse] Theatre built: " + theatre.Seats.Count + " seats. Mode: " + (vr ? "VR" : "Desktop"));
        }
    }
}
