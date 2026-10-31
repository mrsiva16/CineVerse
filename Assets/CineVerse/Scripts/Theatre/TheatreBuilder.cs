using System.Collections.Generic;
using UnityEngine;

namespace CineVerse.Theatre
{
    /// <summary>
    /// Builds the standard-cinema auditorium from code using primitives: stepped floor, seats,
    /// walls, ceiling, house lights and the screen. Dimensions come from <see cref="TheatreLayout"/>.
    ///
    /// Real-world proportions are what make the "dynamic screen perspective" work: the screen is a
    /// fixed physical size and each row is a fixed distance away, so a front-row seat automatically
    /// shows a bigger screen and a back-row seat a smaller one. Nothing is faked.
    /// </summary>
    public class TheatreBuilder : MonoBehaviour
    {
        public TheatreLayout layout = new TheatreLayout();

        [Header("Look")]
        public Color seatColor = new Color(0.55f, 0.07f, 0.10f);
        public Color floorColor = new Color(0.12f, 0.10f, 0.10f);
        public Color wallColor = new Color(0.10f, 0.10f, 0.14f);
        public Color ceilingColor = new Color(0.06f, 0.06f, 0.08f);

        [Header("House lights")]
        public float lightIntensity = 1.2f;
        public float lightRange = 14f;

        public Renderer ScreenRenderer { get; private set; }
        public HouseLights Lights { get; private set; }
        public Vector3 SpawnPosition { get; private set; }
        public Quaternion SpawnRotation { get; private set; }
        public readonly List<SeatInfo> Seats = new List<SeatInfo>();

        GameObject root;
        Material seatMat, floorMat, wallMat, ceilingMat, frameMat;

        /// <summary>Builds the hall. Calling it again destroys and rebuilds it.</summary>
        public void Build()
        {
            if (root != null) Destroy(root);
            Seats.Clear();
            root = new GameObject("Theatre");
            root.transform.SetParent(transform, false);

            seatMat = MaterialFactory.Make(seatColor, "Seat");
            floorMat = MaterialFactory.Make(floorColor, "Floor");
            wallMat = MaterialFactory.Make(wallColor, "Wall");
            ceilingMat = MaterialFactory.Make(ceilingColor, "Ceiling");
            frameMat = MaterialFactory.Make(Color.black, "ScreenFrame");

            BuildShell();
            BuildFloor();
            BuildSeats();
            BuildScreen();
            BuildLights();
            ComputeSpawn();
        }

        // ------------------------------------------------------------------ shell
        void BuildShell()
        {
            TheatreLayout L = layout;
            float w = L.RoomWidth;
            float h = L.CeilingHeight;
            float zBack = L.BackWallZ;
            float depth = zBack; // from z = 0 (screen wall) to the back wall
            float zMid = zBack / 2f;
            const float t = 0.3f; // wall thickness

            Box("Wall_Front", new Vector3(0, h / 2f, -t / 2f), new Vector3(w + 2 * t, h + t, t), wallMat, false);
            Box("Wall_Back", new Vector3(0, h / 2f, zBack + t / 2f), new Vector3(w + 2 * t, h + t, t), wallMat, false);
            Box("Wall_Left", new Vector3(-w / 2f - t / 2f, h / 2f, zMid), new Vector3(t, h + t, depth), wallMat, false);
            Box("Wall_Right", new Vector3(w / 2f + t / 2f, h / 2f, zMid), new Vector3(t, h + t, depth), wallMat, false);
            Box("Ceiling", new Vector3(0, h + t / 2f, zMid), new Vector3(w + 2 * t, t, depth), ceilingMat, false);
        }

        // ------------------------------------------------------------------ floor
        void BuildFloor()
        {
            TheatreLayout L = layout;
            float w = L.RoomWidth;

            // Front slab between the screen wall and the first row's tier (top at y = 0).
            float frontEnd = L.RowZ(0) - L.rowDepth / 2f;
            Slab("Floor_Front", 0f, frontEnd, 0f, w);

            // One tier per row. Each tier runs from the floor up to that row's height.
            for (int r = 0; r < L.rows; r++)
                Slab("Floor_Row_" + TheatreLayout.RowLabel(r), L.RowZ(r) - L.rowDepth / 2f, L.RowZ(r) + L.rowDepth / 2f, L.RowFloorY(r), w);

            // Standing platform behind the last row, level with it.
            Slab("Floor_Back", L.RowZ(L.rows - 1) + L.rowDepth / 2f, L.BackWallZ, L.BackFloorY, w);
        }

        void Slab(string name, float z0, float z1, float topY, float width)
        {
            const float below = 0.1f; // sits slightly under y = 0 so there are no gaps
            float height = topY + below;
            Vector3 centre = new Vector3(0, (topY - below) / 2f, (z0 + z1) / 2f);
            GameObject g = Box(name, centre, new Vector3(width, height, z1 - z0), floorMat, false);
            g.AddComponent<WalkableSurface>();
        }

        // ------------------------------------------------------------------ seats
        void BuildSeats()
        {
            TheatreLayout L = layout;
            GameObject seatsRoot = new GameObject("Seats");
            seatsRoot.transform.SetParent(root.transform, false);

            for (int r = 0; r < L.rows; r++)
            {
                string label = TheatreLayout.RowLabel(r);
                for (int i = 0; i < L.seatsPerRow; i++)
                {
                    Vector3 pos = new Vector3(L.SeatX(i), L.RowFloorY(r), L.RowZ(r));
                    GameObject seat = new GameObject("Seat_" + label + (i + 1));
                    seat.transform.SetParent(seatsRoot.transform, false);
                    seat.transform.localPosition = pos;

                    float pitch = L.seatPitch;
                    float sw = pitch * 0.85f; // seat width, leaves a small gap between neighbours
                    Part(seat, "Pedestal", new Vector3(0, 0.2f, 0), new Vector3(sw * 0.6f, 0.4f, 0.4f));
                    Part(seat, "Cushion", new Vector3(0, L.seatSurfaceHeight - 0.05f, 0), new Vector3(sw, 0.1f, 0.5f));
                    // Backrest on the +z side: the audience faces -z toward the screen.
                    Part(seat, "Backrest", new Vector3(0, L.seatSurfaceHeight + 0.3f, 0.23f), new Vector3(sw, 0.65f, 0.08f));

                    SeatInfo info = seat.AddComponent<SeatInfo>();
                    info.rowIndex = r;
                    info.rowLabel = label;
                    info.number = i + 1;
                    info.eyePosition = new Vector3(pos.x, L.SeatedEyeY(r), pos.z);
                    Seats.Add(info);
                }
            }
        }

        void Part(GameObject seat, string name, Vector3 localPos, Vector3 size)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = name;
            p.transform.SetParent(seat.transform, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = size;
            p.GetComponent<Renderer>().sharedMaterial = seatMat;
        }

        // ------------------------------------------------------------------ screen
        void BuildScreen()
        {
            TheatreLayout L = layout;
            float sw = L.screenWidth, sh = L.ScreenHeight;
            float centreY = L.screenBottomHeight + sh / 2f;

            Box("Screen_Frame", new Vector3(0, centreY, 0.025f), new Vector3(sw + 0.4f, sh + 0.4f, 0.05f), frameMat, true);

            // A Unity Quad is visible from its -z side, so rotate it 180 degrees to face the audience (+z).
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Screen";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0, centreY, 0.06f);
            quad.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            quad.transform.localScale = new Vector3(sw, sh, 1f);
            Collider c = quad.GetComponent<Collider>();
            if (c != null) Destroy(c);
            ScreenRenderer = quad.GetComponent<Renderer>();
            ScreenRenderer.sharedMaterial = MaterialFactory.Make(Color.black, "ScreenPlaceholder");
        }

        // ------------------------------------------------------------------ lights
        void BuildLights()
        {
            TheatreLayout L = layout;
            GameObject lightsRoot = new GameObject("HouseLights");
            lightsRoot.transform.SetParent(root.transform, false);

            float y = L.CeilingHeight - 0.6f;
            float xs = L.RoomWidth / 4f;
            float z1 = L.BackWallZ * 0.25f, z2 = L.BackWallZ * 0.75f;
            Vector3[] spots =
            {
                new Vector3(-xs, y, z1), new Vector3(xs, y, z1),
                new Vector3(-xs, y, z2), new Vector3(xs, y, z2),
            };

            Light[] made = new Light[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject g = new GameObject("Light_" + i);
                g.transform.SetParent(lightsRoot.transform, false);
                g.transform.localPosition = spots[i];
                Light l = g.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = lightRange;
                l.intensity = lightIntensity;
                l.color = new Color(1f, 0.92f, 0.8f);
                l.shadows = LightShadows.None; // realtime shadows are too costly on a standalone headset
                made[i] = l;
            }

            Lights = lightsRoot.AddComponent<HouseLights>();
            Lights.Setup(made);
        }

        // ------------------------------------------------------------------ spawn
        void ComputeSpawn()
        {
            TheatreLayout L = layout;
            // Stand in the aisle at the back of the hall, facing the screen.
            SpawnPosition = new Vector3(L.AisleCenterX, L.BackFloorY, L.BackWallZ - 1.0f);
            SpawnRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
        }

        // ------------------------------------------------------------------ helpers
        GameObject Box(string name, Vector3 centre, Vector3 size, Material mat, bool decorative)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(root.transform, false);
            g.transform.localPosition = centre;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            // Floors and walls keep their collider (teleport rays and the desktop player need them).
            // Purely decorative parts, such as the screen frame, drop it.
            if (decorative) Destroy(g.GetComponent<Collider>());
            return g;
        }
    }
}
