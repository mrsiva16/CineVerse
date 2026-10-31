using System;

namespace CineVerse.Theatre
{
    /// <summary>
    /// Pure layout maths for the auditorium (no UnityEngine dependency, so it is easy to unit test
    /// and to reuse in Phase 2 for seat maps and in Phase 3 for server-side seat validation).
    ///
    /// Coordinate system (metres): the screen wall is at z = 0 and the audience sits at +z, looking
    /// toward -z. x = 0 is the centre line. y = 0 is the front floor. Row 0 ("A") is the front row.
    /// </summary>
    [Serializable]
    public class TheatreLayout
    {
        // ---- Seating ----
        public int rows = 10;
        public int seatsPerRow = 14;
        /// <summary>An aisle gap is inserted after this many seats. 0 = no aisle.</summary>
        public int aisleAfterSeat = 7;
        /// <summary>Centre-to-centre seat spacing.</summary>
        public float seatPitch = 0.6f;
        public float rowDepth = 1.1f;
        /// <summary>How much each row is raised above the one in front of it.</summary>
        public float rowRise = 0.2f;
        public float aisleWidth = 1.4f;

        // ---- Screen ----
        public float screenWidth = 12f;
        public float aspect = 16f / 9f;
        /// <summary>Height of the bottom edge of the screen above the front floor.</summary>
        public float screenBottomHeight = 1.5f;
        /// <summary>Distance from the screen wall to the centre of row A.</summary>
        public float screenToFirstRow = 4.5f;

        // ---- Room ----
        public float sideMargin = 2f;
        public float backMargin = 3f;
        public float headroomAboveScreen = 2.5f;

        // ---- Seat geometry used for eye position ----
        public float seatSurfaceHeight = 0.45f;
        /// <summary>Approximate seated eye height above the seat surface.</summary>
        public float seatedEyeHeight = 0.75f;

        public bool HasAisle
        {
            get { return aisleAfterSeat > 0 && aisleAfterSeat < seatsPerRow; }
        }

        public float ScreenHeight { get { return screenWidth / aspect; } }

        public float RowWidth
        {
            get { return seatsPerRow * seatPitch + (HasAisle ? aisleWidth : 0f); }
        }

        public float RoomWidth
        {
            get { return Math.Max(RowWidth + 2f * sideMargin, screenWidth + 2f); }
        }

        public float RowZ(int row) { return screenToFirstRow + row * rowDepth; }

        /// <summary>Floor height of a row (top of its tier).</summary>
        public float RowFloorY(int row) { return row * rowRise; }

        /// <summary>X centre of seat index (0-based) in a row.</summary>
        public float SeatX(int seatIndex)
        {
            float x = -RowWidth / 2f + seatPitch / 2f + seatIndex * seatPitch;
            if (HasAisle && seatIndex >= aisleAfterSeat) x += aisleWidth;
            return x;
        }

        /// <summary>X of the middle of the aisle, or 0 when there is no aisle.</summary>
        public float AisleCenterX
        {
            get
            {
                if (!HasAisle) return 0f;
                return -RowWidth / 2f + aisleAfterSeat * seatPitch + aisleWidth / 2f;
            }
        }

        /// <summary>Z of the back wall's inner face.</summary>
        public float BackWallZ
        {
            get { return RowZ(rows - 1) + rowDepth / 2f + backMargin; }
        }

        public float BackFloorY { get { return RowFloorY(rows - 1); } }

        public float CeilingHeight
        {
            get
            {
                float aboveScreen = screenBottomHeight + ScreenHeight + headroomAboveScreen;
                float aboveBackRows = BackFloorY + 3.5f;
                return Math.Max(aboveScreen, aboveBackRows);
            }
        }

        /// <summary>World-space eye height (y) of a person seated in the given row.</summary>
        public float SeatedEyeY(int row)
        {
            return RowFloorY(row) + seatSurfaceHeight + seatedEyeHeight;
        }

        /// <summary>Row label: 0 = "A", 1 = "B" ... 25 = "Z", then "AA" style for larger halls.</summary>
        public static string RowLabel(int row)
        {
            string s = string.Empty;
            int n = row;
            do
            {
                s = (char)('A' + (n % 26)) + s;
                n = n / 26 - 1;
            } while (n >= 0);
            return s;
        }
    }
}
