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

            if (ambienceSource != null && ambienceLoop != null)
            {
                ambienceSource.clip = ambienceLoop;
                ambienceSource.loop = true;
                ambienceSource.spatialBlend = 0f;
                if (ambienceGroup != null)
                    ambienceSource.outputAudioMixerGroup = ambienceGroup;
                ambienceSource.Play();
            }

            if (stationHumSource != null && stationHumLoop != null)
            {
                stationHumSource.clip = stationHumLoop;
                stationHumSource.loop = true;
                stationHumSource.spatialBlend = 1f;
                if (effectsGroup != null)
                    stationHumSource.outputAudioMixerGroup = effectsGroup;
                stationHumSource.Play();
            }
        }

        public void SetAmbienceVolume(float linear01)
        {
            if (mixer != null)
                mixer.SetFloat("AmbienceVolume", LinearToDb(linear01));
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
}
