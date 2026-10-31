using UnityEngine;
using UnityEngine.XR;

namespace CineVerse.Player
{
    /// <summary>
    /// A minimal headset rig built entirely in code, with no XR Interaction Toolkit dependency.
    /// It creates a head camera and two simple hand markers, and copies the tracked poses from
    /// UnityEngine.XR.InputDevices every frame. Needs XR Plugin Management + OpenXR enabled for
    /// the target platform (see README).
    ///
    /// Hierarchy: VRRig (floor-level origin) -> Head (Camera, AudioListener), LeftHand, RightHand.
    /// </summary>
    public class VRRig : MonoBehaviour
    {
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        public static VRRig Create(Vector3 position, Quaternion rotation)
        {
            GameObject go = new GameObject("VRRig");
            go.transform.SetPositionAndRotation(position, rotation);
            VRRig rig = go.AddComponent<VRRig>();

            GameObject headGo = new GameObject("Head");
            headGo.tag = "MainCamera";
            headGo.transform.SetParent(go.transform, false);
            Camera cam = headGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            headGo.AddComponent<AudioListener>();
            rig.head = headGo.transform;

            rig.leftHand = MakeHand("LeftHand", go.transform);
            rig.rightHand = MakeHand("RightHand", go.transform);
            return rig;
        }

        static Transform MakeHand(string name, Transform parent)
        {
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = name;
            hand.transform.SetParent(parent, false);
            hand.transform.localScale = Vector3.one * 0.07f;
            Destroy(hand.GetComponent<Collider>());
            return hand.transform;
        }

        void Awake()
        {
            // Ask the runtime for a floor-level origin so y = 0 is the real floor.
            var subsystems = new System.Collections.Generic.List<XRInputSubsystem>();
            SubsystemManager.GetInstances(subsystems);
            foreach (XRInputSubsystem s in subsystems)
                s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }

        void OnEnable() { Application.onBeforeRender += UpdatePoses; }
        void OnDisable() { Application.onBeforeRender -= UpdatePoses; }
        void Update() { UpdatePoses(); }

        void UpdatePoses()
        {
            ApplyHead();
            ApplyHand(XRNode.LeftHand, leftHand);
            ApplyHand(XRNode.RightHand, rightHand);
        }

        void ApplyHead()
        {
            InputDevice dev = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!dev.isValid || head == null) return;

            Vector3 pos;
            Quaternion rot;
            bool gotPos = dev.TryGetFeatureValue(CommonUsages.centerEyePosition, out pos)
                          || dev.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            bool gotRot = dev.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rot)
                          || dev.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
            if (gotPos) head.localPosition = pos;
            if (gotRot) head.localRotation = rot;
        }

        static void ApplyHand(XRNode node, Transform target)
        {
            InputDevice dev = InputDevices.GetDeviceAtXRNode(node);
            if (!dev.isValid || target == null) return;

            Vector3 pos;
            Quaternion rot;
            if (dev.TryGetFeatureValue(CommonUsages.devicePosition, out pos)) target.localPosition = pos;
            if (dev.TryGetFeatureValue(CommonUsages.deviceRotation, out rot)) target.localRotation = rot;
        }
    }
}
