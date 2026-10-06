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
                GameAudio.PlayObjectiveComplete(transform.position);
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
            if (signalSource == null)
                return;

            if (restored)
            {
                signalSource.Stop();
                signalSource.loop = false;
            }
            else if (!signalSource.isPlaying && signalSource.clip != null)
            {
                signalSource.loop = true;
                signalSource.spatialBlend = 1f;
                signalSource.volume = 0.35f;
                signalSource.Play();
            }
        }
    }
}
