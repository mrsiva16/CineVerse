using UnityEngine;

namespace CineVerse.Player
{
    /// <summary>
    /// Keyboard-and-mouse walker so the theatre can be tested in the Unity editor with no headset.
    /// WASD / arrows move, mouse looks, Esc releases the cursor, click recaptures it.
    /// Uses a CharacterController so it collides with walls and climbs the 0.2 m row steps.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopRig : MonoBehaviour
    {
        public float walkSpeed = 3f;
        public float lookSensitivity = 0.1f; // degrees per pixel
        public float eyeHeight = 1.7f;

        Transform cam;
        float pitch;
        float yVelocity;
        CharacterController controller;

        public static DesktopRig Create(Vector3 position, Quaternion rotation)
        {
            GameObject go = new GameObject("DesktopRig");
            go.transform.SetPositionAndRotation(position, rotation);
            // Configure the CharacterController before it can collide, to avoid spawn-time pushes.
            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.03f;

            DesktopRig rig = go.AddComponent<DesktopRig>();

            GameObject camGo = new GameObject("Head");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0, rig.eyeHeight, 0);
            Camera c = camGo.AddComponent<Camera>();
            c.nearClipPlane = 0.05f;
            c.farClipPlane = 100f;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            rig.cam = camGo.transform;
            return rig;
        }

        void Awake() { controller = GetComponent<CharacterController>(); }

        void Start() { SetCursorLocked(true); }

        void Update()
        {
            if (DesktopInput.EscapePressed()) SetCursorLocked(false);
            else if (Cursor.lockState != CursorLockMode.Locked && DesktopInput.ClickPressed()) SetCursorLocked(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = DesktopInput.Look() * lookSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = DesktopInput.Move();
            Vector3 planar = (transform.right * move.x + transform.forward * move.y);
            if (planar.sqrMagnitude > 1f) planar.Normalize();
            planar *= walkSpeed;

            if (controller.isGrounded) yVelocity = -1f; else yVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((planar + Vector3.up * yVelocity) * Time.deltaTime);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
