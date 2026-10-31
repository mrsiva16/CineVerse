using UnityEngine;

namespace CineVerse.Theatre
{
    /// <summary>
    /// Identity of one seat. Phase 2 (seat selection) and Phase 3 (shared seating) build on this.
    /// </summary>
    public class SeatInfo : MonoBehaviour
    {
        public int rowIndex;
        public string rowLabel;
        /// <summary>1-based seat number within the row, counted from the audience's left to right as they face the screen.</summary>
        public int number;
        /// <summary>World position of a seated person's eyes (approximate).</summary>
        public Vector3 eyePosition;

        public string Id { get { return rowLabel + number; } }
    }
}
