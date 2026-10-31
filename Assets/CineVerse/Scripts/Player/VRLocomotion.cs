using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using CineVerse.Theatre;

namespace CineVerse.Player
{
    /// <summary>
    /// Comfort-friendly VR movement (no smooth walking, which causes motion sickness):
    ///  - RIGHT thumbstick: push forward to aim a teleport arc, release to teleport.
    ///    The arc is green on a walkable surface and red otherwise.
    ///  - LEFT thumbstick: flick left or right to snap-turn.
    /// Only surfaces tagged with <see cref="WalkableSurface"/> are valid teleport targets.
    /// </summary>
    public class VRLocomotion : MonoBehaviour
    {
        public VRRig rig;

        [Header("Teleport")]
        public float aimThreshold = 0.7f;
        public float arcSpeed = 9f;
        public int arcSteps = 40;
        public float arcStepTime = 0.05f;
        [Tooltip("Tilts the aim ray up/down (degrees) if controller 'forward' feels off.")]
        public float aimPitchOffset = 0f;

        [Header("Snap turn")]
        public float snapAngle = 30f;
        public float snapThreshold = 0.7f;
        public float snapReset = 0.3f;

        LineRenderer line;
        GameObject reticle;
        Material validMat, invalidMat;
        bool aiming;
        bool targetValid;
        Vector3 target;
        bool snapReady = true;
        readonly List<Vector3> points = new List<Vector3>();

        void Start()
        {
            validMat = MaterialFactory.Make(new Color(0.2f, 1f, 0.4f), "TeleportValid");
            invalidMat = MaterialFactory.Make(new Color(1f, 0.25f, 0.25f), "TeleportInvalid");

            GameObject lineGo = new GameObject("TeleportArc");
            lineGo.transform.SetParent(transform, false);
            line = lineGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = 0.02f;
            line.enabled = false;

            reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reticle.name = "TeleportReticle";
            reticle.transform.localScale = new Vector3(0.4f, 0.01f, 0.4f);
            Destroy(reticle.GetComponent<Collider>());
            reticle.SetActive(false);
        }

        void Update()
        {
            HandleSnapTurn(InputDevices.GetDeviceAtXRNode(XRNode.LeftHand));
            HandleTeleport(InputDevices.GetDeviceAtXRNode(XRNode.RightHand));
        }

        // ------------------------------------------------------------------ snap turn
        void HandleSnapTurn(InputDevice left)
        {
            if (!left.isValid) return;
            Vector2 axis;
            if (!left.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis)) return;

            if (snapReady && Mathf.Abs(axis.x) > snapThreshold)
            {
                float dir = Mathf.Sign(axis.x);
                // Rotate around the head so the user does not swing sideways while turning.
                rig.transform.RotateAround(rig.head.position, Vector3.up, dir * snapAngle);
                snapReady = false;
            }
            else if (Mathf.Abs(axis.x) < snapReset)
            {
                snapReady = true;
            }
        }

        // ------------------------------------------------------------------ teleport
        void HandleTeleport(InputDevice right)
        {
            if (!right.isValid) return;
            Vector2 axis;
            if (!right.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis)) return;

            if (axis.y > aimThreshold)
            {
                aiming = true;
                UpdateAim();
            }
            else if (aiming)
            {
                aiming = false;
                if (targetValid) TeleportTo(target);
                line.enabled = false;
                reticle.SetActive(false);
            }
        }

        void UpdateAim()
        {
            Transform hand = rig.rightHand;
            Vector3 dir = Quaternion.AngleAxis(aimPitchOffset, hand.right) * hand.forward;
            Vector3 p = hand.position;
            Vector3 v = dir * arcSpeed;
            Vector3 g = Physics.gravity;

            points.Clear();
            points.Add(p);
            targetValid = false;

            for (int i = 0; i < arcSteps; i++)
            {
                float dt = arcStepTime;
                Vector3 next = p + v * dt + 0.5f * g * dt * dt;
                v += g * dt;

                RaycastHit hit;
                if (Physics.Linecast(p, next, out hit, ~0, QueryTriggerInteraction.Ignore))
                {
                    points.Add(hit.point);
                    targetValid = hit.normal.y > 0.7f && hit.collider.GetComponentInParent<WalkableSurface>() != null;
                    target = hit.point;
                    break;
                }
                points.Add(next);
                p = next;
            }

            line.sharedMaterial = targetValid ? validMat : invalidMat;
            line.positionCount = points.Count;
            line.SetPositions(points.ToArray());
            line.enabled = true;

            reticle.SetActive(targetValid);
            if (targetValid) reticle.transform.position = target + Vector3.up * 0.01f;
        }

        void TeleportTo(Vector3 destination)
        {
            // Move the rig so the HEAD (not the rig origin) lands on the target.
            Vector3 headOffset = rig.head.position - rig.transform.position;
            headOffset.y = 0f;
            rig.transform.position = new Vector3(destination.x - headOffset.x, destination.y, destination.z - headOffset.z);
        }
    }
}
