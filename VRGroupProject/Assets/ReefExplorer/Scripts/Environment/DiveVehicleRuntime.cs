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
            var desktop = GameObject.Find("DesktopPlayer");
            if (desktop != null)
                AttachToPlayer(desktop.transform, prefab, new Vector3(0f, -0.85f, 0.9f), 2.5f);

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
        }

        static void AttachToPlayer(Transform parent, GameObject prefab, Vector3 localPos, float size)
        {
            var existing = parent.Find("DiveVehicle");
            GameObject vehicle;
            if (existing != null)
            {
                vehicle = existing.gameObject;
            }
            else
            {
                vehicle = Instantiate(prefab, parent);
                vehicle.name = "DiveVehicle";
                DisableColliders(vehicle);
            }

            vehicle.transform.localPosition = localPos;
            vehicle.transform.localRotation = Quaternion.identity;
            FitUniformScale(vehicle, size);
            // Keep craft under the camera so it never blacks out the view.
            HideMeshesInFrontOfCamera(vehicle, parent);
        }

        static void HideMeshesInFrontOfCamera(GameObject vehicle, Transform player)
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
                // Hide anything sitting in the near forward cone (blocks Game view).
                if (to.sqrMagnitude < 2.5f * 2.5f && Vector3.Dot(camFwd, to.normalized) > 0.35f)
                    r.enabled = false;
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
