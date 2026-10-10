using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Scatters the new coral, grass, and rock models on the sand so the dive looks lived-in.
    /// </summary>
    public sealed class NewItemDressRuntime : MonoBehaviour
    {
        const string CoralA = "Assets/new_item/lowpoly_coral.glb";
        const string CoralB = "Assets/new_item/lowpoly_coral_2.glb";
        const string GroundPack = "Assets/new_item/handpainted-stylized-grass-and-rocks/source/upload.fbx";
        const string TexRoot = "Assets/new_item/handpainted-stylized-grass-and-rocks/textures/";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<NewItemDressRuntime>() != null)
                return;

            var host = new GameObject("NewItemDressRuntime");
            host.AddComponent<NewItemDressRuntime>();
        }

        IEnumerator Start()
        {
            var plantSpots = CollectAndRemoveFakeLeaves();
            if (GameObject.Find("NewReefDress") != null)
                yield break;

            GameObject coralA = null;
            GameObject coralB = null;
            GameObject ground = null;
            for (var i = 0; i < 8 && (coralA == null || coralB == null || ground == null); i++)
            {
                coralA = LoadModel(CoralA);
                coralB = LoadModel(CoralB);
                ground = LoadModel(GroundPack);
                if (coralA == null || coralB == null || ground == null)
                    yield return null;
            }

            var root = new GameObject("NewReefDress");
            if (ground != null)
                PlaceRealGrass(ground, root.transform, plantSpots);
            if (coralA != null || coralB != null)
                ReplaceFakeCorals(coralA != null ? coralA : coralB, coralB != null ? coralB : coralA, root.transform);
            if (coralA != null)
                PlaceCorals(coralA, root.transform, 11);
            if (coralB != null)
                PlaceCorals(coralB, root.transform, 29);
        }

        void PlaceRealGrass(GameObject prefab, Transform root, System.Collections.Generic.List<Vector3> spots)
        {
            var template = Instantiate(prefab, root);
            template.name = "RealGrassTemplate";
            foreach (var renderer in template.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null &&
                    renderer.gameObject.name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.gameObject.SetActive(false);
            }

            StripColliders(template);
            PaintGround(template);
            DropGiantSheets(template);
            template.SetActive(false);

            var rng = new System.Random(7);
            for (var n = 0; n < 28; n++)
            {
                var extra = new Vector3(
                    (float)(rng.NextDouble() * 24.0 - 12.0),
                    0f,
                    (float)(rng.NextDouble() * 18.0 + 3.2));
                if (OpenSand(extra))
                    spots.Add(extra);
            }

            for (var i = 0; i < spots.Count; i++)
            {
                var tuft = Instantiate(template, root);
                tuft.name = "RealGrass_" + i;
                tuft.SetActive(true);
                tuft.transform.position = spots[i];
                tuft.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                var size = 1.05f + (float)(rng.NextDouble() * 0.55);
                if (!FitAndSit(tuft, size, 0.02f))
                    tuft.transform.localScale = Vector3.one * 0.4f;
            }
        }

        static System.Collections.Generic.List<Vector3> CollectAndRemoveFakeLeaves()
        {
            var spots = new System.Collections.Generic.List<Vector3>();
            foreach (var tr in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr == null)
                    continue;
                var name = tr.name;
                if (!name.StartsWith("FakePlant") && !name.StartsWith("CarpetPlant") &&
                    !name.StartsWith("DensePlant") && !name.StartsWith("FillPlant"))
                    continue;
                if (tr.parent != null &&
                    (tr.parent.name.StartsWith("FakePlant") || tr.parent.name.StartsWith("CarpetPlant") ||
                     tr.parent.name.StartsWith("DensePlant") || tr.parent.name.StartsWith("FillPlant")))
                    continue;
                var spot = tr.position;
                spot.y = 0f;
                spots.Add(spot);
                Destroy(tr.gameObject);
            }

            return spots;
        }

        void ReplaceFakeCorals(GameObject coralA, GameObject coralB, Transform root)
        {
            var fakes = new System.Collections.Generic.List<GameObject>();
            foreach (var tr in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tr == null || !tr.name.StartsWith("FakeCoral"))
                    continue;
                if (tr.parent != null && tr.parent.name.StartsWith("FakeCoral"))
                    continue;
                fakes.Add(tr.gameObject);
            }

            for (var i = 0; i < fakes.Count; i++)
            {
                var fake = fakes[i];
                if (fake == null)
                    continue;
                var spot = fake.transform.position;
                spot.y = 0f;
                var yaw = fake.transform.eulerAngles.y;
                Destroy(fake);
                SpawnCoral(i % 2 == 0 ? coralA : coralB, root, spot, yaw, 0.75f + (i % 4) * 0.12f, "RealCoral_" + i);
            }
        }

        void PlaceCorals(GameObject prefab, Transform root, int seed)
        {
            var rng = new System.Random(seed);
            var anchors = new[]
            {
                new Vector3(-6.5f, 0f, 11f),
                new Vector3(0f, 0f, 15.5f),
                new Vector3(6.5f, 0f, 11.5f),
                new Vector3(-10f, 0f, 6f),
            };

            var placed = 0;
            var guard = 0;
            while (placed < 8 && guard < 40)
            {
                guard++;
                var anchor = anchors[rng.Next(anchors.Length)];
                var spot = anchor + new Vector3(
                    (float)(rng.NextDouble() * 8.0 - 4.0),
                    0f,
                    (float)(rng.NextDouble() * 8.0 - 4.0));
                if (!OpenSand(spot))
                    continue;

                SpawnCoral(prefab, root, spot, (float)(rng.NextDouble() * 360.0),
                    0.85f + (float)rng.NextDouble() * 0.7f, "NewCoral_" + seed + "_" + placed);
                placed++;
            }
        }

        static void SpawnCoral(GameObject prefab, Transform root, Vector3 spot, float yaw, float size, string objectName)
        {
            if (prefab == null)
                return;
            var coral = Instantiate(prefab, root);
            coral.name = objectName;
            coral.transform.position = spot;
            coral.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * coral.transform.rotation;
            StripColliders(coral);
            if (!FitAndSit(coral, size, 0.04f))
                coral.transform.localScale = Vector3.one * 0.4f;
        }

        static bool OpenSand(Vector3 p)
        {
            if (p.x < -12f || p.x > 12f || p.z < 3.2f || p.z > 21f)
                return false;
            if (p.x > -4.2f && p.x < 9.6f && p.z < 3.2f)
                return false;
            return true;
        }

        static void PaintGround(GameObject go)
        {
            var cube = LoadTexture(TexRoot + "Cube_Color.png");
            var grass = LoadTexture(TexRoot + "Plane_Color.png");
            var grassA = LoadTexture(TexRoot + "Plane.001_Color.png");
            var grassB = LoadTexture(TexRoot + "Plane.002_Color.png");
            var grassC = LoadTexture(TexRoot + "Plane.025_Color.png");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                var mat = renderer.sharedMaterial;
                var missing = mat == null || mat.shader == null || mat.mainTexture == null ||
                              mat.shader.name.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (!missing)
                    continue;

                var name = renderer.gameObject.name;
                Texture2D tex = grass;
                if (name.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    tex = cube;
                else if (name.IndexOf("025", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    tex = grassC;
                else if (name.IndexOf("002", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    tex = grassB;
                else if (name.IndexOf("001", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    tex = grassA;

                if (shader == null || tex == null)
                    continue;
                var painted = new Material(shader);
                if (painted.HasProperty("_BaseMap"))
                    painted.SetTexture("_BaseMap", tex);
                painted.mainTexture = tex;
                if (painted.HasProperty("_BaseColor"))
                    painted.SetColor("_BaseColor", Color.white);
                painted.color = Color.white;
                renderer.sharedMaterial = painted;
            }
        }

        static void DropGiantSheets(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length < 3)
                return;

            var sizes = new float[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
                sizes[i] = Mathf.Max(renderers[i].bounds.size.x, renderers[i].bounds.size.y, renderers[i].bounds.size.z);
            System.Array.Sort(sizes);
            var median = sizes[sizes.Length / 2];
            if (median < 0.001f)
                return;

            foreach (var renderer in renderers)
            {
                var size = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                if (size > median * 4f)
                    renderer.gameObject.SetActive(false);
            }
        }

        static bool FitAndSit(GameObject go, float targetSize, float groundY)
        {
            go.transform.localScale = Vector3.one;
            if (!TryBounds(go, out var before))
                return false;

            var current = Mathf.Max(before.size.x, before.size.y, before.size.z);
            if (current < 0.0001f)
                return false;

            go.transform.localScale = Vector3.one * Mathf.Clamp(targetSize / current, 0.0001f, 40f);
            if (!TryBounds(go, out var after))
                return false;

            var p = go.transform.position;
            p.y += groundY - after.min.y;
            go.transform.position = p;
            return true;
        }

        static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();
            var found = false;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
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

        static void StripColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
                Destroy(col);
            }
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
    }
}
