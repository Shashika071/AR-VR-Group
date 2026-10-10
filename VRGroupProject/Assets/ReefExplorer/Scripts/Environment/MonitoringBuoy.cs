using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Reef Buoy Seven — amber while silent, green after power cell restore.
    /// </summary>
    public sealed class MonitoringBuoy : MonoBehaviour
    {
        [SerializeField] Renderer[] statusRenderers;
        [SerializeField] Light statusLight;
        [SerializeField] AudioSource signalSource;
        [SerializeField] Color brokenColor = new(1f, 0.55f, 0.1f);
        [SerializeField] Color restoredColor = new(0.2f, 0.9f, 0.55f);
        [SerializeField] bool restored;

        public bool IsRestored => restored;

        void OnEnable()
        {
            MissionEvents.MissionRestarted += OnRestart;
            ApplyVisual();
            UpdateSignal();
        }

        void OnDisable()
        {
            MissionEvents.MissionRestarted -= OnRestart;
        }

        public void SetRestored(bool value)
        {
            restored = value;
            ApplyVisual();
            UpdateSignal();
            if (restored)
                PlayPowerOn();
        }

        void OnRestart()
        {
            restored = false;
            ApplyVisual();
            UpdateSignal();
        }

        void ApplyVisual()
        {
            var color = restored ? restoredColor : brokenColor;
            if (statusRenderers != null)
            {
                foreach (var r in statusRenderers)
                {
                    if (r == null)
                        continue;
                    foreach (var mat in r.materials)
                    {
                        if (mat != null && mat.HasProperty("_BaseColor"))
                            mat.SetColor("_BaseColor", color);
                        else if (mat != null)
                            mat.color = color;
                    }
                }
            }

            if (statusLight != null)
            {
                statusLight.color = color;
                statusLight.intensity = restored ? 2.2f : 1.2f;
            }
        }

        void UpdateSignal()
        {
            EnsureSource();
            if (!restored)
            {
                signalSource.Stop();
                return;
            }

            if (signalSource.clip == null)
                signalSource.clip = PingClip();
            signalSource.loop = true;
            signalSource.volume = 0.7f;
            if (!signalSource.isPlaying)
                signalSource.Play();
        }

        void PlayPowerOn()
        {
            EnsureSource();
            signalSource.PlayOneShot(PowerOnClip(), 1f);
            GameAudio.PlayObjectiveComplete(transform.position);
        }

        void EnsureSource()
        {
            if (signalSource == null)
                signalSource = GetComponentInChildren<AudioSource>();
            if (signalSource != null)
                return;

            var go = new GameObject("SignalSource");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            signalSource = go.AddComponent<AudioSource>();
            signalSource.playOnAwake = false;
            signalSource.spatialBlend = 0.35f;
            signalSource.minDistance = 4f;
            signalSource.maxDistance = 36f;
            signalSource.loop = true;
        }

        static AudioClip pingClip;
        static AudioClip powerOnClip;

        static AudioClip PingClip()
        {
            if (pingClip != null)
                return pingClip;

            const int rate = 44100;
            const float seconds = 0.9f;
            var count = (int)(rate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)rate;
                var blip = t < 0.16f ? Mathf.Sin(2f * Mathf.PI * 520f * t) * (1f - t / 0.16f) : 0f;
                data[i] = blip * 0.55f;
            }

            pingClip = AudioClip.Create("BuoyPing", count, 1, rate, false);
            pingClip.SetData(data, 0);
            return pingClip;
        }

        static AudioClip PowerOnClip()
        {
            if (powerOnClip != null)
                return powerOnClip;

            const int rate = 44100;
            const float seconds = 0.45f;
            var count = (int)(rate * seconds);
            var data = new float[count];
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)rate;
                var freq = t < 0.18f ? 520f : 780f;
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            }

            powerOnClip = AudioClip.Create("BuoyPowerOn", count, 1, rate, false);
            powerOnClip.SetData(data, 0);
            return powerOnClip;
        }
    }
}
