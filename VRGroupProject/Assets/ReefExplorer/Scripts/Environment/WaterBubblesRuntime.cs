using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Spawns Assets/Water_bubles FBX clusters around the reef and gently floats them upward.
    /// </summary>
    public sealed class WaterBubblesRuntime : MonoBehaviour
    {
        const string FbxPath = "Assets/Water_bubles/Prop_09_Bubbles_Size_01_StemCell.fbx";
        const string AlbedoPath = "Assets/Water_bubles/textures/Bath_Soap_Bubbles_bcolor.png";
        const string NormalPath = "Assets/Water_bubles/textures/Bath_Soap_Bubbles_norm.png";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<WaterBubblesRuntime>() != null)
                return;

            var host = new GameObject("WaterBubblesRuntime");
            host.AddComponent<WaterBubblesRuntime>();
        }

        void Start() => StartCoroutine(SpawnWhenReady());

        System.Collections.IEnumerator SpawnWhenReady()
        {
            for (var i = 0; i < 6; i++)
            {
                if (TrySpawn())
                    yield break;
                yield return new WaitForSeconds(0.3f);
            }

            Debug.LogWarning($"[ReefExplorer] Missing bubble model: {FbxPath}");
        }

        bool TrySpawn()
        {
            if (GameObject.Find("WaterBubblesRoot") != null)
                return true;

            // Editor build already placed spaced bubbles — don't stack more at Play.
            if (GameObject.Find("UnderwaterEnvironment_v1") != null)
            {
                Debug.Log("[ReefExplorer] Authored reef present — runtime WaterBubbles skipped.");
                return true;
            }

            var prefab = LoadModel(FbxPath);
            if (prefab == null)
                return false;

            var mat = BuildBubbleMaterial();
            var root = new GameObject("WaterBubblesRoot");
            root.transform.SetParent(transform);

            var spots = new[]
            {
                new Vector3(-10f, 0.25f, 7f),
                new Vector3(10f, 0.25f, 8f),
                new Vector3(-6f, 0.25f, 15f),
                new Vector3(6f, 0.25f, 16f),
                new Vector3(0f, 0.25f, 21f),
            };

            for (var i = 0; i < spots.Length; i++)
            {
                var go = Instantiate(prefab, root.transform);
                go.name = $"WaterBubbles_{i}";
                go.transform.position = spots[i] + new Vector3(
                    Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                FitScale(go, Random.Range(0.35f, 0.85f));
                StripColliders(go);
                ApplyMaterial(go, mat);

                var drift = go.AddComponent<BubbleDrift>();
                drift.speed = Random.Range(0.12f, 0.28f);
                drift.wobble = Random.Range(0.15f, 0.35f);
                drift.resetY = 4.5f;
                drift.basePos = go.transform.position;
            }

            // Skip heavy extra particle field — mesh bubbles + scene particles are enough.
            Debug.Log("[ReefExplorer] Water_bubles added (lightweight).");
            return true;
        }

        static Material BuildBubbleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var albedo = LoadTexture(AlbedoPath);
            var normal = LoadTexture(NormalPath);

            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
                mat.mainTexture = albedo;
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            var color = new Color(0.75f, 0.95f, 1f, 0.65f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            // Cheap glass-ish look.
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.85f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.05f);

            return mat;
        }

        static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null)
                return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        static GameObject LoadModel(string path)
        {
#if UNITY_EDITOR
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
                return model;
#endif
            return PlayerAssetCatalog.Model(path);
        }

        static Texture2D LoadTexture(string path)
        {
#if UNITY_EDITOR
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;
#endif
            return PlayerAssetCatalog.Texture(path);
        }

        static void StripColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Destroy(col);
            }
        }

        static void FitScale(GameObject go, float targetSize)
        {
            go.transform.localScale = Vector3.one;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                go.transform.localScale = Vector3.one * targetSize;
                return;
            }

            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            var current = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (current < 0.01f || current > 500f)
            {
                go.transform.localScale = Vector3.one * targetSize;
                return;
            }

            go.transform.localScale = Vector3.one * Mathf.Clamp(targetSize / current, 0.01f, 4f);
        }
    }

    /// <summary>Slow rise + reset so bubble clusters loop underwater.</summary>
    public sealed class BubbleDrift : MonoBehaviour
    {
        public float speed = 0.2f;
        public float wobble = 0.25f;
        public float resetY = 4.5f;
        public Vector3 basePos;

        float phase;
        int updatePhase;

        void Start()
        {
            if (basePos == Vector3.zero)
                basePos = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);
            updatePhase = Random.Range(0, 2);
        }

        void Update()
        {
            if ((Time.frameCount + updatePhase) % 2 != 0)
                return;

            var dt = Time.deltaTime * 2f;
            phase += dt;
            var p = transform.position;
            p.y += speed * dt;
            p.x = basePos.x + Mathf.Sin(phase * 1.3f) * wobble;
            p.z = basePos.z + Mathf.Cos(phase * 1.1f) * wobble * 0.7f;
            if (p.y > basePos.y + resetY)
                p.y = basePos.y;
            transform.position = p;
        }
    }
}
