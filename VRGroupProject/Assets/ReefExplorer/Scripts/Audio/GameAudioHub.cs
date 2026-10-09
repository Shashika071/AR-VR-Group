using UnityEngine;
using UnityEngine.Audio;

namespace ReefExplorer.Audio
{
    public sealed class GameAudioHub : MonoBehaviour
    {
        public enum Cue
        {
            ScannerStart,
            ScannerSuccess,
            Invalid,
            SampleFill,
            ObjectiveComplete,
            MissionSuccess
        }

        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioMixerGroup ambienceGroup;
        [SerializeField] AudioMixerGroup effectsGroup;
        [SerializeField] AudioSource ambienceSource;
        [SerializeField] AudioSource stationHumSource;
        [SerializeField] AudioClip ambienceLoop;
        [SerializeField] AudioClip stationHumLoop;
        [SerializeField] AudioClip scannerStart;
        [SerializeField] AudioClip scannerSuccess;
        [SerializeField] AudioClip invalid;
        [SerializeField] AudioClip sampleFill;
        [SerializeField] AudioClip objectiveComplete;
        [SerializeField] AudioClip missionSuccess;
        [SerializeField] AudioClip scannerProgressTone;

        float lastProgressPlay;

        void Awake()
        {
            GameAudio.Bind(this);

            // Mute old procedural hum / buoy beep. Underwater.wav is applied at runtime.
            if (stationHumSource != null)
            {
                stationHumSource.Stop();
                stationHumSource.enabled = false;
            }

            if (ambienceSource != null)
                ambienceSource.Stop();
        }

        public void SetAmbienceVolume(float linear01)
        {
            var level = Mathf.Clamp01(linear01);
            if (ambienceSource != null)
                ambienceSource.volume = level;
            if (mixer != null)
                mixer.SetFloat("AmbienceVolume", LinearToDb(Mathf.Max(level, 0.0001f)));
        }

        public void SetEffectsVolume(float linear01)
        {
            if (mixer != null)
                mixer.SetFloat("EffectsVolume", LinearToDb(linear01));
        }

        public void SetMuted(bool muted)
        {
            AudioListener.volume = muted ? 0f : 1f;
        }

        /// <summary>
        /// Play a looping underwater ambience clip (e.g. Assets/underwater-sound/Underwater.wav).
        /// </summary>
        public void SetAmbienceClip(AudioClip clip, float volume = 0.45f)
        {
            if (clip == null)
                return;

            if (ambienceSource == null)
            {
                var go = new GameObject("AmbienceSource");
                go.transform.SetParent(transform, false);
                ambienceSource = go.AddComponent<AudioSource>();
            }

            ambienceLoop = clip;
            ambienceSource.enabled = true;
            ambienceSource.Stop();
            ambienceSource.clip = clip;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = Mathf.Clamp01(volume);
            ambienceSource.outputAudioMixerGroup = null;
            if (ambienceSource.GetComponent<SpeakerBoost>() == null)
                ambienceSource.gameObject.AddComponent<SpeakerBoost>();
            ambienceSource.Play();
        }

        public void StopLoopingAudio()
        {
            if (ambienceSource != null)
                ambienceSource.Stop();
            if (stationHumSource != null)
            {
                stationHumSource.Stop();
                stationHumSource.enabled = false;
            }
        }

        public void Play(Cue cue, Vector3 position, bool spatial = true)
        {
            var clip = Resolve(cue);
            if (clip == null)
                return;

            if (spatial)
            {
                // PlayClipAtPoint creates a temporary 3D AudioSource at the world position.
                // Limitation: cannot route temporary sources through mixer groups easily.
                AudioSource.PlayClipAtPoint(clip, position, 0.9f);
            }
            else if (ambienceSource != null)
            {
                ambienceSource.PlayOneShot(clip);
            }
        }

        public void PlayProgress(Vector3 position, float t01)
        {
            if (scannerProgressTone == null)
                return;
            if (Time.unscaledTime - lastProgressPlay < 0.2f)
                return;

            lastProgressPlay = Time.unscaledTime;
            AudioSource.PlayClipAtPoint(scannerProgressTone, position, 0.25f + 0.5f * Mathf.Clamp01(t01));
        }

        AudioClip Resolve(Cue cue) => cue switch
        {
            Cue.ScannerStart => scannerStart,
            Cue.ScannerSuccess => scannerSuccess,
            Cue.Invalid => invalid,
            Cue.SampleFill => sampleFill,
            Cue.ObjectiveComplete => objectiveComplete,
            Cue.MissionSuccess => missionSuccess,
            _ => null
        };

        static float LinearToDb(float linear)
        {
            return Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        }
    }

    /// <summary>
    /// Raises a quiet clip so laptop speakers can hear it at full volume.
    /// </summary>
    sealed class SpeakerBoost : MonoBehaviour
    {
        const float Gain = 2.8f;

        void OnAudioFilterRead(float[] data, int channels)
        {
            for (var i = 0; i < data.Length; i++)
                data[i] = Mathf.Clamp(data[i] * Gain, -1f, 1f);
        }
    }
}
