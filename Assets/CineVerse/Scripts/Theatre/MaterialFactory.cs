using UnityEngine;

namespace CineVerse.Theatre
{
    /// <summary>
    /// Makes simple coloured materials WITHOUT calling Shader.Find (which can be stripped from
    /// player builds, giving pink materials on the headset). It clones the render pipeline's own
    /// default material from a throw-away primitive, so it works in Built-in, URP and HDRP.
    /// </summary>
    public static class MaterialFactory
    {
        static Material template;

        static Material Template()
        {
            if (template != null) return template;
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template = probe.GetComponent<Renderer>().sharedMaterial;
            if (Application.isPlaying) Object.Destroy(probe); else Object.DestroyImmediate(probe);
            return template;
        }

        public static Material Make(Color color, string name)
        {
            Material m = new Material(Template());
            m.name = name;
            SetColor(m, color);
            return m;
        }

        public static void SetColor(Material m, Color color)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color); // URP / HDRP
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);         // Built-in
        }

        public static void SetMainTexture(Material m, Texture texture)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture); // URP
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", texture); // Built-in
        }

        /// <summary>Best-effort emissive glow so the screen stays bright in a dark hall.</summary>
        public static void SetEmission(Material m, Texture texture)
        {
            if (!m.HasProperty("_EmissionColor")) return;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", Color.white);
            if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", texture);
        }
    }
}
