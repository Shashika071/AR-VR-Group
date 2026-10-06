using UnityEngine;
using UnityEngine.Rendering;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Lightweight runtime helpers only. Does NOT re-carpet the reef when
    /// UnderwaterEnvironment_v1 already exists (avoids centre stacking + lag).
    /// </summary>
    public sealed class ReefDensityRuntime : MonoBehaviour
    {
        static readonly Color Fog = new(0.04f, 0.20f, 0.26f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<ReefDensityRuntime>() != null)
                return;

            var host = new GameObject("ReefDensityRuntime");
            host.AddComponent<ReefDensityRuntime>();
        }

        void Start() => StartCoroutine(ApplyLate());

        System.Collections.IEnumerator ApplyLate()
        {
            yield return null;
            ApplyFogAndCameras();
            EnsureHorizonWalls();
            EnsureLargeSeabed();
            EnsureSafetyFloor();

            var authored = GameObject.Find("UnderwaterEnvironment_v1");
            if (authored != null && authored.transform.childCount >= 20)
            {
                // Scene already has spaced carpet from the Editor build — do not spawn more.
                Debug.Log("[ReefExplorer] Authored reef present — runtime fill skipped (faster Play).");
                yield break;
            }

            // Fallback only if the environment was never built.
            BuildSpacedFallback();
        }

        static void ApplyFogAndCameras()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Fog;
            RenderSettings.fogDensity = 0.13f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.11f, 0.15f);
            RenderSettings.skybox = null;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Fog;
                cam.farClipPlane = 26f;
            }
        }

        static void EnsureLargeSeabed()
        {
            var seabed = GameObject.Find("Seabed");
            if (seabed == null)
                return;
            seabed.transform.position = new Vector3(0f, 0f, 10f);
            var s = seabed.transform.localScale;
            // Match tighter horizon ring — do not inflate back to the old large floor.
            if (s.x < 3.0f || s.z < 3.2f || s.x > 3.6f || s.z > 3.8f)
                seabed.transform.localScale = new Vector3(3.2f, 1f, 3.4f);
        }

        static void EnsureSafetyFloor()
        {
            var existing = GameObject.Find("SafetyFloor");
            if (existing != null)
            {
                existing.transform.position = new Vector3(0f, -0.05f, 10f);
                existing.transform.localScale = new Vector3(4.2f, 1f, 4.2f);
                return;
            }

            var safety = GameObject.CreatePrimitive(PrimitiveType.Plane);
            safety.name = "SafetyFloor";
            safety.transform.position = new Vector3(0f, -0.05f, 10f);
            safety.transform.localScale = new Vector3(4.2f, 1f, 4.2f);
            var r = safety.GetComponent<Renderer>();
            if (r != null)
                Object.Destroy(r);
        }

        static void EnsureHorizonWalls()
        {
            const float radius = 15.5f;
            var old = GameObject.Find("WaterHorizonCurtain");
            if (old != null)
            {
                var existing = old.transform.Find("HorizonWall_0");
                // Rebuild if missing, wrong count, far ring, or still facing outward (seam bug).
                if (existing != null && old.transform.childCount == 18)
                {
                    var flat = existing.position;
                    flat.y = 10f;
                    var dist = Vector3.Distance(flat, new Vector3(0f, 10f, 10f));
                    var facesInward = Vector3.Dot(existing.forward,
                        (new Vector3(0f, existing.position.y, 10f) - existing.position).normalized) > 0.5f;
                    if (dist <= radius + 1.25f && facesInward)
                        return;
                }

                Object.Destroy(old);
            }

            var root = new GameObject("WaterHorizonCurtain");
            root.transform.position = new Vector3(0f, 0f, 10f);
            var mat = MakeUnlitFogMat();
            const int walls = 18;
            for (var i = 0; i < walls; i++)
            {
                var a = (i / (float)walls) * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wall.name = $"HorizonWall_{i}";
                wall.transform.SetParent(root.transform, false);
                wall.transform.position = new Vector3(outward.x * radius, 3.5f, 10f + outward.z * radius);
                wall.transform.rotation = Quaternion.LookRotation(-outward);
                wall.transform.localScale = new Vector3(6.2f, 10f, 1f);
                Object.Destroy(wall.GetComponent<Collider>());
                Cheap(wall, mat);
            }
        }

        static Material MakeUnlitFogMat()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return MakeMat("Runtime_Horizon", Fog, 0.05f);
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Fog);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Fog);
            mat.color = Fog;
            return mat;
        }

        void BuildSpacedFallback()
        {
            if (GameObject.Find("RuntimeReefFill") != null)
                return;

            var root = new GameObject("RuntimeReefFill");
            root.transform.SetParent(transform);
            var rock = MakeMat("Runtime_Rock", new Color(0.32f, 0.34f, 0.36f), 0.15f);
            var plant = MakeMat("Runtime_Plant", new Color(0.12f, 0.55f, 0.28f), 0.1f);
            var coral = MakeMat("Runtime_Coral", new Color(0.9f, 0.35f, 0.4f), 0.25f);

            const float step = 3.6f;
            var n = 0;
            for (var x = -15f; x <= 15f; x += step)
            {
                for (var z = 2f; z <= 23f; z += step)
                {
                    var ix = Mathf.RoundToInt((x + 15f) / step);
                    var iz = Mathf.RoundToInt((z - 2f) / step);
                    if (((ix + iz) & 1) == 0)
                        continue;
                    if (z < 3.6f && Mathf.Abs(x) < 2f)
                        continue;

                    var p = new Vector3(x, 0f, z);
                    var kind = n++ % 3;
                    if (kind == 0) MakeRock(root.transform, p, Random.Range(0.8f, 1.4f), rock);
                    else if (kind == 1) MakePlant(root.transform, p, plant);
                    else MakeCoral(root.transform, p, coral);
                }
            }

            Debug.Log($"[ReefExplorer] Spaced fallback fill ({n} props).");
        }

        static void MakeRock(Transform parent, Vector3 pos, float size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "FillRock";
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.up * (size * 0.25f);
            go.transform.localScale = new Vector3(size, size * 0.7f, size * 0.9f);
            Cheap(go, mat);
        }

        static void MakePlant(Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject("FillPlant");
            root.transform.SetParent(parent);
            root.transform.position = pos;
            for (var i = 0; i < 3; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3(Random.Range(-0.08f, 0.08f), 0.4f, Random.Range(-0.08f, 0.08f));
                blade.transform.localRotation = Quaternion.Euler(Random.Range(-18f, 18f), i * 40f, 0f);
                blade.transform.localScale = new Vector3(0.09f, Random.Range(0.5f, 0.85f), 0.015f);
                Object.Destroy(blade.GetComponent<Collider>());
                Cheap(blade, mat);
            }

            root.AddComponent<SeaPlantSway>();
        }

        static void MakeCoral(Transform parent, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "FillCoral";
            go.transform.SetParent(parent);
            go.transform.position = pos + Vector3.up * 0.25f;
            go.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            Object.Destroy(go.GetComponent<Collider>());
            Cheap(go, mat);
        }

        static void Cheap(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null)
                return;
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static Material MakeMat(string name, Color color, float smooth)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smooth);
            return mat;
        }
    }
}
