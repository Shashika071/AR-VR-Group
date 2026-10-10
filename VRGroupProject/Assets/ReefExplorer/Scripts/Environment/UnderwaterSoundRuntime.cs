using ReefExplorer.Audio;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Plays Assets/underwater-sound/Underwater.wav as the dive ambience loop.
    /// </summary>
    public sealed class UnderwaterSoundRuntime : MonoBehaviour
    {
        const string ClipPath = "Assets/underwater-sound/Underwater.wav";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<UnderwaterSoundRuntime>() != null)
                return;

            var host = new GameObject("UnderwaterSoundRuntime");
            host.AddComponent<UnderwaterSoundRuntime>();
        }

        void Start() => StartCoroutine(ApplyWhenReady());

        System.Collections.IEnumerator ApplyWhenReady()
        {
            for (var i = 0; i < 8; i++)
            {
                if (TryPlay())
                    yield break;
                yield return new WaitForSeconds(0.25f);
            }

            Debug.LogWarning($"[ReefExplorer] Missing audio clip: {ClipPath}");
        }

        static bool TryPlay()
        {
            var hub = Object.FindAnyObjectByType<GameAudioHub>();
            if (hub == null)
                return false;

            var clip = LoadClip();
            if (clip == null)
                return false;

            hub.SetAmbienceClip(clip, 1f);
            hub.SetAmbienceVolume(1f);
            Debug.Log("[ReefExplorer] Playing Assets/underwater-sound/Underwater.wav");
            return true;
        }

        static AudioClip LoadClip()
        {
#if UNITY_EDITOR
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip != null)
                return clip;
#endif
            return PlayerAssetCatalog.Clip(ClipPath);
        }
    }
}
