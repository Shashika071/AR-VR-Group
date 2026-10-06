using ReefExplorer.Interaction;
using UnityEditor;
using UnityEngine;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Places newly imported ocean FBX models into the ReefExplorer scene.
    /// Menu: Reef Explorer / 5. Apply New Ocean FBX Models
    /// </summary>
    public static class OceanFbxInstaller
    {
        const string TurtlePath = "Assets/[FBX] LoggerheadTurtle/LoggerheadTurtle.FBX";
        const string JellyPath = "Assets/[FBX] Jellyfish_v3_Max_Vray_NC/Jellyfish_v3_Max_Vray_NC.fbx";
        const string MainFloorPath = "Assets/[FBX] SeaFloor01/SeaFloor01.FBX";
        const string SideFloorPath = "Assets/[FBX] SeaFloor02/SeaFloor02.FBX";
        const string ArchelonPath = "Assets/[FBX] archelon_static/archelon_static.FBX";

        [MenuItem("Reef Explorer/5. Apply New Ocean FBX Models")]
        public static void ApplyModels()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first, then run this menu.", "OK");
                return;
            }

            var turtle = ApplyToAnimal("Animal_Sea Turtle", TurtlePath, 1.1f, new Vector3(0f, 0.05f, 0f), Quaternion.Euler(0f, 180f, 0f));
            SpawnScenic(JellyPath, "Scenic_Jellyfish_A", new Vector3(-28f, 3.2f, 22f), 0.9f, true);
            SpawnScenic(JellyPath, "Scenic_Jellyfish_B", new Vector3(30f, 3.8f, 34f), 0.7f, true);
            SpawnScenic(JellyPath, "Scenic_Jellyfish_C", new Vector3(2f, 2.6f, 48f), 1.0f, true);
            SpawnScenic(ArchelonPath, "Scenic_Archelon", new Vector3(-18f, 0.4f, 44f), 1.6f, false);
            var mainFloor = SpawnSeaFloor(MainFloorPath, "Scenic_SeaFloor01", new Vector3(0f, -0.05f, 30f), 70f, hideFlatSeabed: true);
            var sideFloor = SpawnSeaFloor(SideFloorPath, "Scenic_SeaFloor02", new Vector3(-32f, -0.05f, 22f), 28f, hideFlatSeabed: false);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Ocean FBX applied",
                "Applied:\n" +
                $"• Loggerhead turtle → Animal_Sea Turtle {(turtle ? "OK" : "(open ReefExplorer scene)")}\n" +
                "• Jellyfish x3 (scenic)\n" +
                "• Archelon (scenic)\n" +
                $"• SeaFloor01 MAIN environment {(mainFloor ? "OK" : "missing")}\n" +
                $"• SeaFloor02 side patch {(sideFloor ? "OK" : "missing")}\n\n" +
                "Save the scene (Ctrl+S). Or just Press Play — auto-load also runs.",
                "OK");
        }

        static bool ApplyToAnimal(string animalName, string assetPath, float targetSize, Vector3 localPos, Quaternion localRot)
        {
            var animal = GameObject.Find(animalName);
            if (animal == null)
            {
                Debug.LogWarning($"[ReefExplorer] {animalName} not found. Open ReefExplorer scene first.");
                return false;
            }

            var model = LoadModel(assetPath);
            if (model == null)
                return false;

            for (var i = animal.transform.childCount - 1; i >= 0; i--)
            {
                var child = animal.transform.GetChild(i);
                if (child.name.StartsWith("Label_"))
                    continue;
                Object.DestroyImmediate(child.gameObject);
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

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = Object.Instantiate(model);

            instance.name = "OceanModel";
            instance.transform.SetParent(animal.transform, false);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetSize);
            StripColliders(instance);
            ConvertMaterialsToUrp(instance);

            var survey = animal.GetComponent<SurveyAnimal>();
            if (survey != null)
            {
                var so = new SerializedObject(survey);
                var rends = so.FindProperty("tintRenderers");
                var renderers = instance.GetComponentsInChildren<Renderer>();
                rends.arraySize = renderers.Length;
                for (var i = 0; i < renderers.Length; i++)
                    rends.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(animal);
            return true;
        }

        static void SpawnScenic(string assetPath, string name, Vector3 worldPos, float targetSize, bool jellyDrift)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var model = LoadModel(assetPath);
            if (model == null)
                return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = Object.Instantiate(model);

            instance.name = name;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetSize);
            StripColliders(instance);
            ConvertMaterialsToUrp(instance);

            var wander = instance.AddComponent<ReefExplorer.Environment.AnimalWander>();
            var wso = new SerializedObject(wander);
            wso.FindProperty("center").vector3Value = worldPos;
            wso.FindProperty("extents").vector3Value = jellyDrift
                ? new Vector3(3f, 2f, 3f)
                : new Vector3(4f, 0.5f, 4f);
            wso.FindProperty("speed").floatValue = jellyDrift ? 0.25f : 0.35f;
            wso.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool SpawnSeaFloor(string assetPath, string name, Vector3 worldPos, float targetWidth, bool hideFlatSeabed)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var model = LoadModel(assetPath);
            if (model == null)
                return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (instance == null)
                instance = Object.Instantiate(model);

            instance.name = name;
            instance.transform.position = worldPos;
            instance.transform.localScale = Vector3.one;
            FitToSize(instance, targetWidth);
            ConvertMaterialsToUrp(instance);

            if (hideFlatSeabed)
            {
                var seabed = GameObject.Find("Seabed");
                if (seabed != null)
                {
                    var rend = seabed.GetComponent<Renderer>();
                    if (rend != null)
                        rend.enabled = false;
                    EditorUtility.SetDirty(seabed);
                }
            }

            return true;
        }

        static GameObject LoadModel(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
                Debug.LogError("[ReefExplorer] Missing model: " + path);
            return model;
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
                Object.DestroyImmediate(col);
        }

        static void ConvertMaterialsToUrp(GameObject root)
        {
            var urp = Shader.Find("Universal Render Pipeline/Lit");
            if (urp == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null || mat.shader == null)
                        continue;
                    var n = mat.shader.name;
                    if (n.Contains("Standard") || n.Contains("Autodesk") || n.Contains("Legacy") || n == "Hidden/InternalErrorShader")
                    {
                        mat.shader = urp;
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }
    }
}
