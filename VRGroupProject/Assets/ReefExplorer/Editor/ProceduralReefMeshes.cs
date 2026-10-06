using UnityEngine;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Builds recognizable stylized reef meshes (not recoloured cubes).
    /// </summary>
    public static class ProceduralReefMeshes
    {
        public static GameObject CreateRock(string name, Transform parent, Vector3 pos, float size, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            // Irregular cluster of spheres / capsules
            for (var i = 0; i < 4; i++)
            {
                var part = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                part.name = $"RockPart_{i}";
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = new Vector3(
                    Random.Range(-0.35f, 0.35f) * size,
                    Random.Range(0.15f, 0.55f) * size,
                    Random.Range(-0.35f, 0.35f) * size);
                part.transform.localScale = new Vector3(
                    size * Random.Range(0.55f, 1.1f),
                    size * Random.Range(0.4f, 0.9f),
                    size * Random.Range(0.55f, 1.1f));
                part.transform.localRotation = Quaternion.Euler(Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f));
                part.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(part.GetComponent<Collider>());
            }

            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, size * 0.35f, 0f);
            col.radius = size * 0.7f;
            return root;
        }

        public static GameObject CreateBranchingCoral(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            trunk.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            trunk.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            for (var i = 0; i < 5; i++)
            {
                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = $"Branch_{i}";
                branch.transform.SetParent(root.transform, false);
                var yaw = i * 72f + Random.Range(-10f, 10f);
                branch.transform.localRotation = Quaternion.Euler(Random.Range(25f, 55f), yaw, 0f);
                branch.transform.localPosition = new Vector3(0f, 0.55f, 0f) + branch.transform.up * 0.25f;
                branch.transform.localScale = new Vector3(0.07f, 0.28f, 0.07f);
                branch.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(branch.GetComponent<Collider>());

                var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.name = $"Tip_{i}";
                tip.transform.SetParent(branch.transform, false);
                tip.transform.localPosition = new Vector3(0f, 1f, 0f);
                tip.transform.localScale = new Vector3(1.4f, 0.5f, 1.4f);
                tip.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(tip.GetComponent<Collider>());
            }

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 1.2f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.5f, 0f);
            return root;
        }

        public static GameObject CreateRoundedCoral(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            for (var i = 0; i < 3; i++)
            {
                var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lobe.name = $"Lobe_{i}";
                lobe.transform.SetParent(root.transform, false);
                lobe.transform.localPosition = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    0.25f + i * 0.12f,
                    Random.Range(-0.2f, 0.2f));
                var s = Random.Range(0.35f, 0.55f);
                lobe.transform.localScale = new Vector3(s, s * 0.85f, s);
                lobe.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(lobe.GetComponent<Collider>());
            }

            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.35f, 0f);
            col.radius = 0.45f;
            return root;
        }

        public static GameObject CreateFanCoral(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var baseStem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseStem.name = "Stem";
            baseStem.transform.SetParent(root.transform, false);
            baseStem.transform.localScale = new Vector3(0.06f, 0.2f, 0.06f);
            baseStem.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            baseStem.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(baseStem.GetComponent<Collider>());

            for (var i = 0; i < 6; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(root.transform, false);
                var t = (i / 5f) - 0.5f;
                blade.transform.localPosition = new Vector3(t * 0.55f, 0.55f + Mathf.Abs(t) * 0.1f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, t * -35f);
                blade.transform.localScale = new Vector3(0.08f, 0.55f - Mathf.Abs(t) * 0.15f, 0.04f);
                blade.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(0.8f, 1f, 0.25f);
            return root;
        }

        public static GameObject CreateSeaPlant(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            for (var i = 0; i < 3; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3(Random.Range(-0.08f, 0.08f), 0.45f, Random.Range(-0.08f, 0.08f));
                blade.transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), i * 40f, Random.Range(-8f, 8f));
                blade.transform.localScale = new Vector3(0.06f, Random.Range(0.4f, 0.7f), 0.04f);
                blade.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }

            return root;
        }

        public static GameObject CreateSandMound(string name, Transform parent, Vector3 pos, float size, Material mat)
        {
            var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.name = name;
            mound.transform.SetParent(parent);
            mound.transform.position = pos + Vector3.up * (size * 0.15f);
            mound.transform.localScale = new Vector3(size, size * 0.35f, size * 0.85f);
            mound.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(mound.GetComponent<Collider>());
            return mound;
        }

        public static GameObject CreateStylizedFish(string name, Transform parent, Vector3 pos, Material mat, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            body.transform.localScale = new Vector3(0.22f, 0.35f, 0.18f) * scale;
            body.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(-0.28f, 0f, 0f) * scale;
            tail.transform.localScale = new Vector3(0.12f, 0.22f, 0.04f) * scale;
            tail.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(tail.GetComponent<Collider>());

            var fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fin.name = "Dorsal";
            fin.transform.SetParent(root.transform, false);
            fin.transform.localPosition = new Vector3(0f, 0.14f, 0f) * scale;
            fin.transform.localScale = new Vector3(0.18f, 0.1f, 0.03f) * scale;
            fin.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(fin.GetComponent<Collider>());

            var col = root.AddComponent<CapsuleCollider>();
            col.radius = 0.12f * scale;
            col.height = 0.5f * scale;
            col.direction = 0;
            return root;
        }

        public static GameObject CreateStylizedTurtle(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.name = "Shell";
            shell.transform.SetParent(root.transform, false);
            shell.transform.localScale = new Vector3(0.9f, 0.4f, 0.7f);
            shell.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(shell.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
            head.transform.localScale = new Vector3(0.28f, 0.22f, 0.28f);
            head.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            for (var i = 0; i < 4; i++)
            {
                var flipper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                flipper.name = $"Flipper_{i}";
                flipper.transform.SetParent(root.transform, false);
                var side = i < 2 ? -1f : 1f;
                var front = i % 2 == 0 ? 0.2f : -0.25f;
                flipper.transform.localPosition = new Vector3(side * 0.45f, -0.05f, front);
                flipper.transform.localRotation = Quaternion.Euler(0f, 0f, side * 70f);
                flipper.transform.localScale = new Vector3(0.08f, 0.28f, 0.16f);
                flipper.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(flipper.GetComponent<Collider>());
            }

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.1f, 0.5f, 1f);
            return root;
        }

        public static GameObject CreateStylizedRay(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var wing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wing.name = "Wing";
            wing.transform.SetParent(root.transform, false);
            wing.transform.localScale = new Vector3(1.4f, 0.12f, 0.9f);
            wing.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(wing.GetComponent<Collider>());

            var tail = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tail.name = "Tail";
            tail.transform.SetParent(root.transform, false);
            tail.transform.localPosition = new Vector3(0f, 0f, -0.7f);
            tail.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tail.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
            tail.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(tail.GetComponent<Collider>());

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 0.2f, 1.2f);
            return root;
        }
    }
}
