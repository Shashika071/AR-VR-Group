using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Parks the Explorer submarine at the station as the dive craft.
    /// </summary>
    public sealed class DiveVehicleRuntime : MonoBehaviour
    {
        const string SubmarinePath = "Assets/submarinespaceship_nautilus-31.glb";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<DiveVehicleRuntime>() != null)
                return;

            var host = new GameObject("DiveVehicleRuntime");
            host.AddComponent<DiveVehicleRuntime>();
        }

        IEnumerator Start()
        {
            var desktop = GameObject.Find("DesktopPlayer");
            var stuck = desktop != null ? desktop.transform.Find("DiveVehicle") : null;
            if (stuck != null)
                Destroy(stuck.gameObject);

            GameObject prefab = null;
            for (var i = 0; i < 8 && prefab == null; i++)
            {
                prefab = FindSubmarinePrefab();
                if (prefab == null)
                    yield return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning("[ReefExplorer] Explorer submarine was not found at " + SubmarinePath);
                yield break;
            }

            var old = GameObject.Find("StationDiveCraft");
            if (old != null)
            {
                old.name = "OldDiveCraft";
                Destroy(old);
            }

            var parkedGo = Instantiate(prefab);
            parkedGo.name = "StationDiveCraft";
            var park = new Vector3(6.2f, 0.2f, -0.6f);
            parkedGo.transform.position = park;
            parkedGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            DisableColliders(parkedGo);
            var slot = new GameObject("VehicleBatterySlot_1");
            slot.transform.SetParent(parkedGo.transform, false);
            slot.transform.localPosition = Vector3.zero;

            for (var i = 0; i < 8; i++)
            {
                if (FitWholeCraft(parkedGo, 4.8f, park))
                {
                    PlaceBatteryOnStation();
                    parkedGo.AddComponent<SubmarineDrive>().PrepareCockpit();
                    yield break;
                }

                yield return null;
            }

            parkedGo.transform.localScale = Vector3.one * 0.35f;
            SitOnSand(parkedGo);
            PlaceBatteryOnStation();
            parkedGo.AddComponent<SubmarineDrive>().PrepareCockpit();
        }

        static bool FitWholeCraft(GameObject go, float targetSize, Vector3 park)
        {
            go.transform.localScale = Vector3.one;
            go.transform.position = park;
            if (!TryBounds(go, out var before))
                return false;

            var current = Mathf.Max(before.size.x, before.size.y, before.size.z);
            if (current < 0.0001f)
                return false;

            go.transform.localScale = Vector3.one * Mathf.Clamp(targetSize / current, 0.0001f, 80f);
            if (!TryBounds(go, out var after))
                return false;

            var p = go.transform.position;
            p.x += park.x - after.center.x;
            p.z += park.z - after.center.z;
            p.y += 0.12f - after.min.y;
            go.transform.position = p;
            return true;
        }

        static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();
            var found = false;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found && bounds.size.sqrMagnitude > 0.000001f;
        }

        static void PlaceBatteryOnStation()
        {
            var pack = GameObject.Find("VehiclePowerPack_1");
            if (pack == null || pack.transform.parent != null)
                return;

            pack.transform.SetPositionAndRotation(new Vector3(-1.62f, 1.46f, -2.42f), Quaternion.identity);
        }

        static void SitOnSand(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
                return;
            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            var p = go.transform.position;
            p.y += 0.05f - b.min.y;
            go.transform.position = p;
        }

        static GameObject FindSubmarinePrefab()
        {
#if UNITY_EDITOR
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(SubmarinePath);
            if (go != null)
                return go;

            foreach (var guid in AssetDatabase.FindAssets("nautilus"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.IndexOf(".glb", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                    return go;
            }
#endif
            return PlayerAssetCatalog.Model(SubmarinePath);
        }

        static void DisableColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Destroy(col);
            }
        }

    }
}
