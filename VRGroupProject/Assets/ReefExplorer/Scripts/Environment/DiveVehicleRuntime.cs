using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Optional Space Shuttle dive craft. Silent if the Asset Store pack is not imported yet.
    /// Player craft uses see-through glass so the reef stays visible.
    /// </summary>
    public sealed class DiveVehicleRuntime : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;

            var prefab = FindShuttlePrefab();
            if (prefab == null)
                return; // Shuttle not imported — do nothing (no warnings, no stuck state).

            if (FindAnyObjectByType<DiveVehicleRuntime>() != null)
                return;

            var host = new GameObject("DiveVehicleRuntime");
            var runtime = host.AddComponent<DiveVehicleRuntime>();
            runtime.Attach(prefab);
        }

        void Attach(GameObject prefab)
        {
            // Keep the shuttle parked at the station. Do not parent it to the player,
            // or the hull sits on their feet and the feet show inside the craft.
            var desktop = GameObject.Find("DesktopPlayer");
            var stuck = desktop != null ? desktop.transform.Find("DiveVehicle") : null;
            if (stuck != null)
                Destroy(stuck.gameObject);

            var parkedGo = GameObject.Find("StationDiveCraft");
            if (parkedGo == null)
            {
                parkedGo = Instantiate(prefab);
                parkedGo.name = "StationDiveCraft";
                parkedGo.transform.position = new Vector3(2.8f, 0.95f, -0.3f);
                parkedGo.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
                DisableColliders(parkedGo);
            }

            FitUniformScale(parkedGo, 3.1f);
            HideBodyParts(parkedGo);
        }

        static void HideBodyParts(GameObject vehicle)
        {
            if (vehicle == null)
                return;

            foreach (var t in vehicle.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == vehicle.transform)
                    continue;
                var n = t.name;
                if (n.IndexOf("foot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("feet", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("leg", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("shoe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("boot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("toe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("pilot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("human", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("person", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    t.gameObject.SetActive(false);
            }
        }

        static GameObject FindShuttlePrefab()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("Space Shuttle t:Prefab t:Model");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Sample", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (path.IndexOf("Shuttle", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                    return go;
            }
#endif
            return Resources.Load<GameObject>("ReefModels/SpaceShuttle");
        }

        static void DisableColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Destroy(col);
            }
        }

        static void FitUniformScale(GameObject go, float targetSize)
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

            var s = Mathf.Clamp(targetSize / current, 0.01f, 5f);
            go.transform.localScale = Vector3.one * s;
        }
    }
}
