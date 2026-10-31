using System.IO;
using UnityEngine;
using UnityEngine.Video;
using CineVerse.Theatre;

namespace CineVerse.Cinema
{
    /// <summary>
    /// Plays a video onto the theatre's screen. Phase 1: LOCAL playback only.
    /// (Phase 5 will drive this from a shared host state so every viewer stays in sync.)
    ///
    /// Source: either assign a VideoClip, or set <see cref="url"/> to an http(s) URL or to a file
    /// name inside Assets/StreamingAssets. Use public-domain or properly licensed video only.
    /// </summary>
    public class CinemaScreen : MonoBehaviour
    {
        [Header("Video source (use ONE)")]
        public VideoClip clip;
        [Tooltip("http(s) URL, or a file name inside Assets/StreamingAssets, e.g. sample.mp4")]
        public string url = "";

        [Header("Optional")]
        [Tooltip("Assign a URP/Unlit material here for the brightest, most reliable screen. " +
                 "If empty, a clone of the default material with emission is used.")]
        public Material screenMaterial;
        public int textureWidth = 1920;
        public int textureHeight = 1080;
        [Range(0f, 1f)] public float dimmedLightLevel = 0.08f;
        public bool loop = false;

        public bool IsPlaying { get { return player != null && player.isPlaying; } }
        public double PlaybackTime { get { return player != null ? player.time : 0.0; } }

        VideoPlayer player;
        AudioSource audioOut;
        RenderTexture target;
        HouseLights houseLights;
        bool initialised;
        bool playWhenPrepared;

        /// <summary>Called by the bootstrap once the theatre exists.</summary>
        public void Initialize(Renderer screenRenderer, HouseLights lights)
        {
            houseLights = lights;

            target = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            target.name = "CineVerseScreenRT";
            ClearTarget();

            Material m = screenMaterial != null
                ? new Material(screenMaterial)
                : MaterialFactory.Make(Color.white, "ScreenVideo");
            MaterialFactory.SetMainTexture(m, target);
            if (screenMaterial == null) MaterialFactory.SetEmission(m, target);
            screenRenderer.sharedMaterial = m;

            // Audio comes from the screen, as plain stereo for Phase 1 (spatial voice is Phase 4).
            audioOut = screenRenderer.gameObject.AddComponent<AudioSource>();
            audioOut.playOnAwake = false;
            audioOut.spatialBlend = 0f;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioOut);
            player.isLooping = loop;
            player.skipOnDrop = true;
            player.prepareCompleted += OnPrepared;
            player.errorReceived += OnError;
            player.loopPointReached += OnFinished;

            initialised = true;
        }

        public void TogglePlay()
        {
            if (IsPlaying) Pause(); else Play();
        }

        public void Play()
        {
            if (!initialised) return;
            if (!player.isPrepared)
            {
                if (!ConfigureSource()) return;
                playWhenPrepared = true;
                player.Prepare();
                return;
            }
            player.Play();
            if (houseLights != null) houseLights.SetLevel(dimmedLightLevel);
        }

        public void Pause()
        {
            if (!initialised) return;
            player.Pause();
            if (houseLights != null) houseLights.SetLevel(1f);
        }

        public void Seek(double seconds)
        {
            if (initialised && player.isPrepared)
                player.time = Mathf.Clamp((float)seconds, 0f, (float)player.length);
        }

        // ------------------------------------------------------------------ internals
        bool ConfigureSource()
        {
            if (clip != null)
            {
                player.source = VideoSource.VideoClip;
                player.clip = clip;
                return true;
            }
            if (!string.IsNullOrEmpty(url))
            {
                player.source = VideoSource.Url;
                player.url = url.Contains("://") ? url : Path.Combine(Application.streamingAssetsPath, url);
                return true;
            }
            Debug.LogWarning("[CineVerse] No video source. Assign a VideoClip or set 'url' on the CinemaScreen component.");
            return false;
        }

        void OnPrepared(VideoPlayer vp)
        {
            if (!playWhenPrepared) return;
            playWhenPrepared = false;
            Play();
        }

        void OnError(VideoPlayer vp, string message)
        {
            playWhenPrepared = false;
            Debug.LogError("[CineVerse] Video error: " + message);
        }

        void OnFinished(VideoPlayer vp)
        {
            if (houseLights != null) houseLights.SetLevel(1f);
        }

        void ClearTarget()
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;
        }

        void OnDestroy()
        {
            if (target != null) target.Release();
        }
    }
}
