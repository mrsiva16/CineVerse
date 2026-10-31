using UnityEngine;
using UnityEngine.Rendering;

namespace CineVerse.Theatre
{
    /// <summary>The ceiling lights. Dimmed while a movie plays and raised when it stops.</summary>
    public class HouseLights : MonoBehaviour
    {
        public Light[] lights;
        public float fadeSpeed = 0.8f; // level units per second
        public Color ambientAtFullLevel = new Color(0.22f, 0.22f, 0.25f);

        float[] baseIntensity;
        float level = 1f;
        float target = 1f;

        public void Setup(Light[] sceneLights)
        {
            lights = sceneLights;
            baseIntensity = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            Apply();
        }

        /// <summary>0 = dark, 1 = full house lights. Fades smoothly.</summary>
        public void SetLevel(float newTarget)
        {
            target = Mathf.Clamp01(newTarget);
        }

        void Update()
        {
            if (Mathf.Approximately(level, target)) return;
            level = Mathf.MoveTowards(level, target, fadeSpeed * Time.deltaTime);
            Apply();
        }

        void Apply()
        {
            if (lights == null) return;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = baseIntensity[i] * level;
            RenderSettings.ambientLight = ambientAtFullLevel * level;
        }
    }
}
