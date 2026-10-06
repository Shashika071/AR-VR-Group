using ReefExplorer.Interaction;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Loads student-added ocean FBX (turtle, jellyfish, seafloor) during Editor Play.
    /// </summary>
    public sealed class OceanFbxRuntimeLoader : MonoBehaviour
    {
        const string TurtlePath = "Assets/[FBX] LoggerheadTurtle/LoggerheadTurtle.FBX";
        const string JellyPath = "Assets/[FBX] Jellyfish_v3_Max_Vray_NC/Jellyfish_v3_Max_Vray_NC.fbx";
        const string MainFloorPath = "Assets/[FBX] SeaFloor01/SeaFloor01.FBX";
        const string SideFloorPath = "Assets/[FBX] SeaFloor02/SeaFloor02.FBX";
        const string ArchelonPath = "Assets/[FBX] archelon_static/archelon_static.FBX";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("Animal_Sea Turtle") == null && GameObject.Find("Animal_Clownfish") == null)
                return;
            if (FindAnyObjectByType<OceanFbxRuntimeLoader>() != null)
                return;

            var host = new GameObject("OceanFbxRuntimeLoader");
            host.AddComponent<OceanFbxRuntimeLoader>();
        }

        void Start()
        {
            Invoke(nameof(Apply), 0.1f);
        }

        void Apply()
        {
#if UNITY_EDITOR
            var turtleOk = ApplyToAnimal("Animal_Sea Turtle", TurtlePath, 1.1f, new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, 180f, 0f));
            SpawnScenic(JellyPath, "Scenic_Jellyfish_A", new Vector3(-28f, 3.2f, 22f), 0.9f, true);
            SpawnScenic(JellyPath, "Scenic_Jellyfish_B", new Vector3(30f, 3.8f, 34f), 0.7f, true);
            SpawnScenic(JellyPath, "Scenic_Jellyfish_C", new Vector3(2f, 2.6f, 48f), 1.0f, true);
            SpawnScenic(ArchelonPath, "Scenic_Archelon", new Vector3(-18f, 0.4f, 44f), 1.6f, false);
            // SeaFloor01 = main open sea; SeaFloor02 = side reef patch.
            SpawnSeaFloor(MainFloorPath, "Scenic_SeaFloor01", new Vector3(0f, -0.05f, 30f), 70f, hideFlatSeabed: true);
            SpawnSeaFloor(SideFloorPath, "Scenic_SeaFloor02", new Vector3(-32f, -0.05f, 22f), 28f, hideFlatSeabed: false);

            Debug.Log($"[ReefExplorer] New ocean FBX loaded. Turtle={turtleOk}, jellyfish x3, archelon, SeaFloor01 (main).");
#else
            Debug.LogWarning("[ReefExplorer] Ocean FBX auto-load runs in Editor Play. For builds, run menu " +
                             "'Reef Explorer / 5. Apply New Ocean FBX Models' and save the scene first.");
#endif
        }

#if UNITY_EDITOR
        static bool ApplyToAnimal(string animalName, string assetPath, float targetSize, Vector3 localPos, Quaternion localRot)
        {
            var animal = GameObject.Find(animalName);
            if (animal == null)
            {
                Debug.LogWarning($"[ReefExplorer] Missing {animalName}");
                return false;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[ReefExplorer] Could not load {assetPath}");
                return false;
            }

            for (var i = animal.transform.childCount - 1; i >= 0; i--)
            {
                var child = animal.transform.GetChild(i);
                if (child.name.StartsWith("Label_"))
                    continue;
                Destroy(child.gameObject);
            }

            var rootRenderer = animal.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            if (animal.GetComponent<Collider>() == null)
            {
                var capsule = animal.AddComponent<CapsuleCollider>();
                capsule.height = 0.9f;
                capsule.radius = 0.45f;
            }

            var instance = Instantiate(model, animal.transform);
            instance.name = "OceanModel";
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetSize);
            StripColliders(instance);
            ConvertMaterialsToUrp(instance);

            var survey = animal.GetComponent<SurveyAnimal>();
            if (survey != null)
            {
                var field = typeof(SurveyAnimal).GetField("tintRenderers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(survey, instance.GetComponentsInChildren<Renderer>());
            }

            return true;
        }

        static void SpawnScenic(string assetPath, string name, Vector3 worldPos, float targetSize, bool jellyDrift)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Destroy(existing);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[ReefExplorer] Could not load {assetPath}");
                return;
            }

            var instance = Instantiate(model);
            instance.name = name;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetSize);
            StripColliders(instance);
            ConvertMaterialsToUrp(instance);

            var wander = instance.AddComponent<AnimalWander>();
            var wField = typeof(AnimalWander).GetField("center",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eField = typeof(AnimalWander).GetField("extents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            wField?.SetValue(wander, worldPos);
            eField?.SetValue(wander, jellyDrift
                ? new Vector3(3f, 2f, 3f)
                : new Vector3(4f, 0.5f, 4f));
        }

        static void SpawnSeaFloor(string assetPath, string name, Vector3 worldPos, float targetWidth, bool hideFlatSeabed)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Destroy(existing);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"[ReefExplorer] Could not load {assetPath}");
                return;
            }

            var instance = Instantiate(model);
            instance.name = name;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetWidth);
            ConvertMaterialsToUrp(instance);

            // Keep walkable plane for teleport; hide its mesh so SeaFloor01 is the visible seabed.
            if (!hideFlatSeabed)
                return;

            var seabed = GameObject.Find("Seabed");
            if (seabed == null)
                return;

            var rend = seabed.GetComponent<Renderer>();
            if (rend != null)
                rend.enabled = false;
        }

        static void FitToSize(GameObject instance, float targetSize)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0 || targetSize <= 0.001f)
                return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (max < 0.0001f)
                return;

            instance.transform.localScale *= targetSize / max;
        }

        static void StripColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>())
                Destroy(col);
        }

        static void ConvertMaterialsToUrp(GameObject root)
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null || mat.shader == null)
                        continue;
                    var n = mat.shader.name;
                    if (n.Contains("Standard") || n.Contains("Autodesk") || n.Contains("Legacy") || n == "Hidden/InternalErrorShader")
                        mat.shader = urp;
                }
            }
        }
#endif
    }
}
