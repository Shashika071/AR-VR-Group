using ReefExplorer.Interaction;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Loads Cute Fish Pack FBX models onto survey animals during Editor Play.
    /// Why they "didn't load" before: models only appeared after running the menu
    /// Reef Explorer → 4. Apply Cute Fish Pack Models. This auto-applies on Play.
    /// </summary>
    public sealed class CuteFishRuntimeLoader : MonoBehaviour
    {
        const string FbxFolder = "Assets/Cute Fish Pack - Feb 2020/FBX/";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("Animal_Clownfish") == null)
                return;
            if (FindAnyObjectByType<CuteFishRuntimeLoader>() != null)
                return;

            var host = new GameObject("CuteFishRuntimeLoader");
            host.AddComponent<CuteFishRuntimeLoader>();
        }

        void Start()
        {
            // Wait one frame so stylized placeholders can spawn, then replace them.
            Invoke(nameof(Apply), 0.05f);
        }

        void Apply()
        {
#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder("Assets/Cute Fish Pack - Feb 2020"))
            {
                Debug.LogWarning("[ReefExplorer] Cute Fish Pack folder not found under Assets.");
                return;
            }

            var clownOk = ApplyToAnimal("Animal_Clownfish", "Clownfish.fbx", 0.55f, new Vector3(0f, 0.05f, 0f));
            var rayOk = ApplyToAnimal("Animal_Ray", "Flatfish.fbx", 0.7f, new Vector3(0f, 0.02f, 0f));
            SpawnScenic("BlueTang.fbx", new Vector3(-26f, 1.4f, 28f), 0.4f);
            SpawnScenic("YellowTang.fbx", new Vector3(28f, 1.5f, 26f), 0.4f);
            SpawnScenic("ParrotFish.fbx", new Vector3(8f, 1.3f, 46f), 0.5f);
            SpawnScenic("ButterflyFish.fbx", new Vector3(-8f, 1.6f, 36f), 0.35f);
            SpawnScenic("MoorishIdol.fbx", new Vector3(22f, 1.4f, 42f), 0.35f);

            Debug.Log($"[ReefExplorer] Cute Fish Pack loaded. Clownfish={clownOk}, Ray/Flatfish={rayOk}. " +
                      "Turtle uses Loggerhead FBX via OceanFbxRuntimeLoader.");
#else
            Debug.LogWarning("[ReefExplorer] Cute Fish auto-load runs in Editor Play. For builds, run menu " +
                             "'Reef Explorer / 4. Apply Cute Fish Pack Models' and save the scene first.");
#endif
        }

#if UNITY_EDITOR
        static bool ApplyToAnimal(string animalName, string fbxFile, float scale, Vector3 localPos)
        {
            var animal = GameObject.Find(animalName);
            if (animal == null)
            {
                Debug.LogWarning($"[ReefExplorer] Missing {animalName}");
                return false;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxFolder + fbxFile);
            if (model == null)
            {
                Debug.LogWarning($"[ReefExplorer] Could not load {FbxFolder}{fbxFile}. Select the FBX in Project and check Import settings.");
                return false;
            }

            // Remove placeholder visuals / previous fish
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
                capsule.radius = 0.4f;
            }

            var instance = Instantiate(model, animal.transform);
            instance.name = "CuteFishModel";
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            instance.transform.localScale = Vector3.one * scale;

            foreach (var col in instance.GetComponentsInChildren<Collider>())
                Destroy(col);

            // Make sure materials render in URP
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == null)
                        continue;
                    if (mat.shader != null && mat.shader.name.Contains("Standard"))
                    {
                        var urp = Shader.Find("Universal Render Pipeline/Lit");
                        if (urp != null)
                            mat.shader = urp;
                    }
                }
            }

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

        static void SpawnScenic(string fbxFile, Vector3 worldPos, float scale)
        {
            var name = "Scenic_" + System.IO.Path.GetFileNameWithoutExtension(fbxFile);
            var existing = GameObject.Find(name);
            if (existing != null)
                Destroy(existing);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxFolder + fbxFile);
            if (model == null)
                return;

            var instance = Instantiate(model);
            instance.name = name;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one * scale;
            foreach (var col in instance.GetComponentsInChildren<Collider>())
                Destroy(col);

            var wander = instance.AddComponent<AnimalWander>();
            var wField = typeof(AnimalWander).GetField("center",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eField = typeof(AnimalWander).GetField("extents",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            wField?.SetValue(wander, worldPos);
            eField?.SetValue(wander, new Vector3(5f, 0.8f, 5f));
        }
#endif
    }
}
