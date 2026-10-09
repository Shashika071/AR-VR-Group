using ReefExplorer.Audio;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Hooks Asset Store packs after import:
    /// - Space Shuttle (Amint3D) as dive vehicle
    /// - Ambient Video Game Music - Underwater Worlds (Phat Phrog) as ambience
    /// Retries a few times so late Package Manager imports still work.
    /// </summary>
    public sealed class AssetStorePacksRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<AssetStorePacksRuntime>() != null)
                return;

            var host = new GameObject("AssetStorePacksRuntime");
            host.AddComponent<AssetStorePacksRuntime>();
        }

        void Start() => StartCoroutine(ApplyShuttleWhenReady());

        System.Collections.IEnumerator ApplyShuttleWhenReady()
        {
            for (var i = 0; i < 12; i++)
            {
                if (TryAttachShuttle())
                {
                    Debug.Log("[ReefExplorer] Space Shuttle ready as dive vehicle.");
                    yield break;
                }

                yield return new WaitForSeconds(0.75f);
            }
            // Silent if not imported — no console spam.
        }

        static bool TryAttachShuttle()
        {
            var desktopEarly = GameObject.Find("DesktopPlayer");
            var stuckEarly = desktopEarly != null ? desktopEarly.transform.Find("DiveVehicle") : null;
            if (stuckEarly != null)
                Object.Destroy(stuckEarly.gameObject);

            if (GameObject.Find("StationDiveCraft") != null)
                return true;

            var prefab = FindAsset<GameObject>(
                new[] { "Space Shuttle", "SpaceShuttle", "Shuttle" },
                new[] { ".prefab", ".fbx", ".FBX" },
                mustContain: "Shuttle");
            if (prefab == null)
                return false;

            var desktop = GameObject.Find("DesktopPlayer");
            var stuck = desktop != null ? desktop.transform.Find("DiveVehicle") : null;
            if (stuck != null)
                Object.Destroy(stuck.gameObject);

            var parkedGo = GameObject.Find("StationDiveCraft");
            if (parkedGo == null)
            {
                parkedGo = Object.Instantiate(prefab);
                parkedGo.name = "StationDiveCraft";
                parkedGo.transform.position = new Vector3(2.8f, 0.95f, -0.3f);
                parkedGo.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
                StripColliders(parkedGo);
            }

            FitScale(parkedGo, 3.1f);
            return true;
        }

        static bool TryApplyUnderwaterMusic()
        {
            var hub = Object.FindAnyObjectByType<GameAudioHub>();
            if (hub == null)
                return false;

            // Prefer clips whose path/name suggests underwater ambient music.
            var clip = FindAudioClip(
                nameHints: new[]
                {
                    "Underwater", "Ambient", "Ocean", "Deep", "Water", "Worlds"
                },
                pathHints: new[]
                {
                    "Underwater", "Ambient Video Game Music", "Phat Phrog", "PhatPhrog"
                });

            if (clip == null)
                return false;

            hub.SetAmbienceClip(clip, 0.5f);
            return true;
        }

        static AudioClip FindAudioClip(string[] nameHints, string[] pathHints)
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:AudioClip");
            AudioClip fallback = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.IndexOf("ReefExplorer/Audio", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue; // skip our procedural tones as "found store pack"
                if (path.IndexOf("Samples/", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var pathMatch = false;
                foreach (var h in pathHints)
                {
                    if (path.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pathMatch = true;
                        break;
                    }
                }

                var file = System.IO.Path.GetFileNameWithoutExtension(path);
                var nameMatch = false;
                foreach (var h in nameHints)
                {
                    if (file.IndexOf(h, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        nameMatch = true;
                        break;
                    }
                }

                if (!pathMatch && !nameMatch)
                    continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                    continue;

                // Strong preference: underwater in name or path.
                if (path.IndexOf("Underwater", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    file.IndexOf("Underwater", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip;

                fallback ??= clip;
            }

            return fallback;
#else
            return null;
#endif
        }

        static T FindAsset<T>(string[] searchTerms, string[] extensions, string mustContain)
            where T : Object
        {
#if UNITY_EDITOR
            foreach (var term in searchTerms)
            {
                var guids = AssetDatabase.FindAssets(term);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path))
                        continue;
                    if (path.IndexOf("Sample", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (!string.IsNullOrEmpty(mustContain) &&
                        path.IndexOf(mustContain, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var okExt = extensions == null || extensions.Length == 0;
                    if (!okExt)
                    {
                        foreach (var ext in extensions)
                        {
                            if (path.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
                            {
                                okExt = true;
                                break;
                            }
                        }
                    }

                    if (!okExt && !path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                        return asset;
                }
            }
#endif
            return null;
        }

        static void StripColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Object.Destroy(col);
            }
        }

        static void FitScale(GameObject go, float targetSize)
        {
            go.transform.localScale = Vector3.one;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
                return;
            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            var current = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (current < 0.01f || current > 500f)
            {
                go.transform.localScale = Vector3.one * 0.5f;
                return;
            }

            go.transform.localScale = Vector3.one * Mathf.Clamp(targetSize / current, 0.01f, 5f);
        }

        static void HideNearCameraMeshes(GameObject vehicle, Transform player)
        {
            var cam = player.GetComponentInChildren<Camera>(true);
            if (cam == null)
                return;

            var camPos = cam.transform.position;
            var camFwd = cam.transform.forward;
            foreach (var r in vehicle.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                var to = r.bounds.center - camPos;
                if (to.sqrMagnitude < 2.5f * 2.5f && Vector3.Dot(camFwd, to.normalized) > 0.35f)
                    r.enabled = false;
            }
        }
    }
}
