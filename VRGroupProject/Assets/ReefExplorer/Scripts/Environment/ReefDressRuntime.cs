using System.Collections;
using ReefExplorer.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Dresses Coral Garden with the imported coral models, turns the ray site into a starfish,
    /// and places the craft battery on the station console.
    /// </summary>
    public sealed class ReefDressRuntime : MonoBehaviour
    {
        static readonly string[] CoralPaths =
        {
            "Assets/Corals/Coral 25.fbx",
            "Assets/Corals/Yellow_Coral.obj",
            "Assets/Corals/Sun_Coral/Sun_Coral.obj",
        };

        const string StarfishPath = "Assets/Sea_Star/Sea_Star.obj";
        const string CraftBatteryPath = "Assets/Car Battery.glb";
        const string BuoyBatteryPath = "Assets/[FBX] AAA Battery 03/AAA Battery 03.FBX";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (FindAnyObjectByType<ReefDressRuntime>() != null)
                return;

            var host = new GameObject("ReefDressRuntime");
            host.AddComponent<ReefDressRuntime>();
        }

        IEnumerator Start()
        {
            yield return null;
            yield return null;
            DressCoralGarden();
            DressStarfish();
            PlacePowerPacks();
            DressBuoyBattery();
        }

        void DressCoralGarden()
        {
            if (GameObject.Find("GardenCoral_0") != null)
                return;

            var zone = GameObject.Find("Zone_Coral");
            var origin = zone != null ? zone.transform.position : new Vector3(-6.5f, 0f, 11f);
            var spots = new[]
            {
                new Vector3(-2.4f, 0f, 1.6f),
                new Vector3(-1.5f, 0f, 2.4f),
                new Vector3(-0.4f, 0f, 1.8f),
                new Vector3(0.7f, 0f, 2.5f),
                new Vector3(1.8f, 0f, 1.7f),
                new Vector3(2.6f, 0f, 0.6f),
                new Vector3(-2.8f, 0f, 0.2f),
                new Vector3(-1.8f, 0f, -0.6f),
                new Vector3(-0.6f, 0f, 0.4f),
                new Vector3(0.5f, 0f, -0.2f),
                new Vector3(1.6f, 0f, 0.3f),
                new Vector3(2.4f, 0f, -0.8f),
                new Vector3(-2.2f, 0f, -1.6f),
                new Vector3(-1.1f, 0f, -2.2f),
                new Vector3(0.2f, 0f, -1.8f),
                new Vector3(1.2f, 0f, -2.4f),
                new Vector3(2.2f, 0f, -1.7f),
                new Vector3(-3.1f, 0f, -0.9f),
                new Vector3(3.0f, 0f, 1.2f),
                new Vector3(0.9f, 0f, 1.1f),
                new Vector3(-0.8f, 0f, -1.1f),
                new Vector3(1.9f, 0f, -0.2f),
            };

            for (var i = 0; i < spots.Length; i++)
            {
                var path = CoralPaths[i % CoralPaths.Length];
                var prefab = LoadModel(path);
                if (prefab == null)
                    continue;

                var coral = Instantiate(prefab);
                coral.name = "GardenCoral_" + i;
                coral.transform.position = origin + spots[i];
                coral.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                DisableColliders(coral);
                var size = 0.55f + (i % 5) * 0.16f;
                Fit(coral, size);
                StartCoroutine(Refit(coral, size));
            }
        }

        void DressStarfish()
        {
            var animal = GameObject.Find("Animal_Starfish") ?? GameObject.Find("Animal_Ray");
            if (animal == null || animal.transform.Find("StarfishModel") != null)
                return;

            var wander = animal.GetComponent<AnimalWander>();
            if (wander != null)
                wander.enabled = false;

            var zone = GameObject.Find("Zone_Ray");
            var origin = zone != null ? zone.transform.position : animal.transform.position;
            animal.transform.position = origin + new Vector3(0f, 0.12f, 0f);
            animal.transform.rotation = Quaternion.Euler(-90f, 20f, 0f);

            foreach (var renderer in animal.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            SpawnStarfish(animal.transform, Vector3.zero, 0.72f, "StarfishModel");

            var extras = new[]
            {
                new Vector3(1.1f, 0.08f, 0.6f),
                new Vector3(-1.2f, 0.08f, 0.4f),
                new Vector3(0.4f, 0.08f, -1.3f),
                new Vector3(-0.7f, 0.08f, -0.9f),
                new Vector3(1.5f, 0.08f, -0.5f),
            };
            for (var i = 0; i < extras.Length; i++)
            {
                var holder = new GameObject("LedgeStarfish_" + i);
                holder.transform.position = origin + extras[i];
                holder.transform.rotation = Quaternion.Euler(-90f, 40f + i * 55f, 0f);
                SpawnStarfish(holder.transform, Vector3.zero, 0.42f + (i % 3) * 0.12f, "Mesh");
            }
        }

        void SpawnStarfish(Transform parent, Vector3 localPos, float size, string name)
        {
            var prefab = LoadModel(StarfishPath);
            if (prefab == null)
                return;

            var star = Instantiate(prefab, parent);
            star.name = name;
            star.transform.localPosition = localPos;
            star.transform.localRotation = Quaternion.identity;
            DisableColliders(star);
            Fit(star, size);
            StartCoroutine(Refit(star, size));
        }

        void PlacePowerPacks()
        {
            if (GameObject.Find("VehiclePowerPack_1") != null)
                return;

            var craft = GameObject.Find("StationDiveCraft");
            if (craft != null)
                MakeSlot(craft.transform, "VehicleBatterySlot_1", craft.transform.position);

            SpawnPack("VehiclePowerPack_1", new Vector3(-1.62f, 1.46f, -2.42f), null);
        }

        static Transform MakeSlot(Transform craft, string name, Vector3 worldPos)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(craft, true);
            slot.transform.position = worldPos;
            slot.transform.rotation = Quaternion.identity;
            return slot.transform;
        }

        void SpawnPack(string name, Vector3 position, Transform parent)
        {
            var pack = new GameObject(name);
            pack.transform.SetParent(parent, true);
            pack.transform.position = position;
            pack.transform.rotation = Quaternion.identity;
            var box = pack.AddComponent<BoxCollider>();
            box.size = new Vector3(0.36f, 0.22f, 0.24f);
            box.center = new Vector3(0f, 0.11f, 0f);

            var body = pack.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var grab = pack.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.colliders.Clear();
            grab.colliders.Add(box);
            pack.AddComponent<VehiclePowerPack>();

            var prefab = LoadModel(CraftBatteryPath) ?? LoadModel("Assets/Car Battery.obj");
            if (prefab == null)
                return;

            var mesh = Instantiate(prefab, pack.transform);
            mesh.name = "Mesh";
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            DisableColliders(mesh);
            Fit(mesh, 0.42f);
            StartCoroutine(Refit(mesh, 0.42f));
        }

        void DressBuoyBattery()
        {
            var cell = GameObject.Find("PowerCell");
            if (cell == null || cell.transform.Find("BuoyBatteryMesh") != null)
                return;

            cell.transform.localScale = Vector3.one;
            var rootRenderer = cell.GetComponent<Renderer>();
            if (rootRenderer != null)
                rootRenderer.enabled = false;

            var old = cell.GetComponent<Collider>();
            if (old != null)
                Destroy(old);
            var box = cell.AddComponent<BoxCollider>();
            box.size = new Vector3(0.4f, 0.55f, 0.4f);
            box.center = Vector3.zero;

            var grab = cell.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.colliders.Clear();
                grab.colliders.Add(box);
            }

            var prefab = LoadModel(BuoyBatteryPath);
            if (prefab == null)
                return;

            var mesh = Instantiate(prefab, cell.transform);
            mesh.name = "BuoyBatteryMesh";
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            DisableColliders(mesh);
            Fit(mesh, 0.5f);
            StartCoroutine(Refit(mesh, 0.5f));
            PlaceBuoyBatteryInOpen(cell.transform);
        }

        static void PlaceBuoyBatteryInOpen(Transform cell)
        {
            var station = GameObject.Find("ResearchStation");
            var spot = station != null
                ? station.transform.TransformPoint(new Vector3(-0.9f, 1.28f, -1.78f))
                : new Vector3(-0.9f, 1.28f, -1.78f);
            cell.SetPositionAndRotation(spot, Quaternion.identity);

            var anchor = GameObject.Find("PowerCell_RespawnAnchor");
            if (anchor != null)
                anchor.transform.SetPositionAndRotation(cell.position, cell.rotation);

            var cover = GameObject.Find("BuoyBatteryRock");
            if (cover != null)
                Destroy(cover);
        }

        static IEnumerator Refit(GameObject go, float size)
        {
            yield return null;
            Fit(go, size);
        }

        static void Fit(GameObject go, float targetSize)
        {
            if (go == null)
                return;
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            var current = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (current < 0.001f)
                return;
            go.transform.localScale = Vector3.one * (targetSize / current);
        }

        static void DisableColliders(GameObject go)
        {
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Destroy(col);
        }

        static GameObject LoadModel(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }
    }
}
