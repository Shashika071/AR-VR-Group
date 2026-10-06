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

        /// <summary>
        /// Walk-through natural rock arch / hole.
        /// Visual = irregular boulder clumps (not cubes). Collision = invisible boxes on sides/top only.
        /// </summary>
        public static GameObject CreateRockArch(
            string name, Transform parent, Vector3 pos, Material mat,
            float width = 2.6f, float height = 2.7f, float thickness = 1.15f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var half = width * 0.5f;

            // Invisible collision only (keeps hole clear for the player).
            AddHiddenCollider(root.transform, "Col_PillarL",
                new Vector3(-half, height * 0.45f, 0f),
                new Vector3(thickness * 0.9f, height * 0.95f, thickness));
            AddHiddenCollider(root.transform, "Col_PillarR",
                new Vector3(half, height * 0.45f, 0f),
                new Vector3(thickness * 0.9f, height * 0.95f, thickness));
            AddHiddenCollider(root.transform, "Col_Lintel",
                new Vector3(0f, height + thickness * 0.2f, 0f),
                new Vector3(width + thickness, thickness * 0.75f, thickness * 1.1f));

            // Natural boulder stacks for each pillar + curved top.
            BuildBoulderStack(root.transform, "PillarL",
                new Vector3(-half, 0f, 0f), height, thickness, mat, curveInward: 1f);
            BuildBoulderStack(root.transform, "PillarR",
                new Vector3(half, 0f, 0f), height, thickness, mat, curveInward: -1f);
            BuildArchRoof(root.transform, width, height, thickness, mat);

            return root;
        }

        /// <summary>Short rocky tunnel you can walk through.</summary>
        public static GameObject CreateRockTunnel(
            string name, Transform parent, Vector3 pos, Material mat, float length = 3.4f)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            var a = CreateRockArch("MouthA", root.transform, Vector3.zero, mat, 2.8f, 2.8f, 1.15f);
            a.transform.localPosition = new Vector3(0f, 0f, -length * 0.35f);

            var b = CreateRockArch("MouthB", root.transform, Vector3.zero, mat, 2.8f, 2.8f, 1.15f);
            b.transform.localPosition = new Vector3(0f, 0f, length * 0.35f);

            // Invisible side/roof colliders for the tunnel length.
            AddHiddenCollider(root.transform, "Col_WallL",
                new Vector3(-1.55f, 1.35f, 0f),
                new Vector3(0.9f, 2.6f, length * 0.85f));
            AddHiddenCollider(root.transform, "Col_WallR",
                new Vector3(1.55f, 1.35f, 0f),
                new Vector3(0.9f, 2.6f, length * 0.85f));
            AddHiddenCollider(root.transform, "Col_Roof",
                new Vector3(0f, 2.95f, 0f),
                new Vector3(3.3f, 0.7f, length * 0.9f));

            // Rocky side walls (boulder clumps, not flat slabs).
            BuildBoulderWall(root.transform, "WallL",
                new Vector3(-1.55f, 0f, 0f), length, 2.7f, 1.0f, mat);
            BuildBoulderWall(root.transform, "WallR",
                new Vector3(1.55f, 0f, 0f), length, 2.7f, 1.0f, mat);
            BuildBoulderWall(root.transform, "RoofWall",
                new Vector3(0f, 2.5f, 0f), length * 0.9f, 0.9f, 1.4f, mat, horizontal: true);

            return root;
        }

        static void AddHiddenCollider(Transform parent, string name, Vector3 localPos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
                Object.DestroyImmediate(r);
            // BoxCollider remains for gameplay.
        }

        static void BuildBoulderStack(
            Transform parent, string name, Vector3 basePos, float height, float thickness, Material mat, float curveInward)
        {
            var stack = new GameObject(name);
            stack.transform.SetParent(parent, false);
            stack.transform.localPosition = basePos;

            var layers = 6;
            for (var i = 0; i < layers; i++)
            {
                var t = i / (float)(layers - 1);
                var y = 0.35f + t * height;
                // Lean slightly toward the opening for a natural arch silhouette.
                var x = curveInward * Mathf.Lerp(0.05f, 0.35f, t * t) * thickness;

                for (var j = 0; j < 3; j++)
                {
                    var boulder = GameObject.CreatePrimitive(
                        j % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                    boulder.name = $"Boulder_{i}_{j}";
                    boulder.transform.SetParent(stack.transform, false);
                    boulder.transform.localPosition = new Vector3(
                        x + Random.Range(-0.18f, 0.18f) * thickness,
                        y + Random.Range(-0.12f, 0.12f),
                        Random.Range(-0.28f, 0.28f) * thickness);
                    boulder.transform.localRotation = Quaternion.Euler(
                        Random.Range(0f, 35f), Random.Range(0f, 360f), Random.Range(0f, 35f));
                    var s = thickness * Random.Range(0.45f, 0.85f);
                    boulder.transform.localScale = new Vector3(
                        s * Random.Range(0.8f, 1.2f),
                        s * Random.Range(0.55f, 1.0f),
                        s * Random.Range(0.8f, 1.2f));
                    boulder.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(boulder.GetComponent<Collider>());
                }
            }
        }

        static void BuildArchRoof(Transform parent, float width, float height, float thickness, Material mat)
        {
            var roof = new GameObject("ArchRoof");
            roof.transform.SetParent(parent, false);

            // Arc of boulders over the opening (looks like a real sea cave mouth).
            const int segments = 7;
            for (var i = 0; i < segments; i++)
            {
                var u = i / (float)(segments - 1); // 0..1
                var angle = Mathf.Lerp(-70f, 70f, u) * Mathf.Deg2Rad;
                var radius = width * 0.55f;
                var x = Mathf.Sin(angle) * radius;
                var y = height + Mathf.Cos(angle) * (thickness * 0.55f) + thickness * 0.15f;

                for (var j = 0; j < 2; j++)
                {
                    var boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    boulder.name = $"RoofRock_{i}_{j}";
                    boulder.transform.SetParent(roof.transform, false);
                    boulder.transform.localPosition = new Vector3(
                        x + Random.Range(-0.12f, 0.12f),
                        y + Random.Range(-0.08f, 0.12f),
                        Random.Range(-0.3f, 0.3f) * thickness);
                    boulder.transform.localRotation = Quaternion.Euler(
                        Random.Range(0f, 40f), Random.Range(0f, 360f), Random.Range(0f, 40f));
                    var s = thickness * Random.Range(0.5f, 0.9f);
                    boulder.transform.localScale = new Vector3(s * 1.15f, s * 0.75f, s);
                    boulder.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(boulder.GetComponent<Collider>());
                }
            }
        }

        static void BuildBoulderWall(
            Transform parent, string name, Vector3 basePos, float length, float height, float depth,
            Material mat, bool horizontal = false)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = basePos;

            var count = Mathf.Clamp(Mathf.RoundToInt(length * 2.2f), 5, 12);
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5f : i / (float)(count - 1);
                var boulder = GameObject.CreatePrimitive(
                    i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                boulder.name = $"WallRock_{i}";
                boulder.transform.SetParent(wall.transform, false);

                if (horizontal)
                {
                    boulder.transform.localPosition = new Vector3(
                        Random.Range(-depth * 0.4f, depth * 0.4f),
                        Random.Range(0f, height * 0.5f),
                        Mathf.Lerp(-length * 0.45f, length * 0.45f, t));
                }
                else
                {
                    boulder.transform.localPosition = new Vector3(
                        Random.Range(-0.15f, 0.15f),
                        Random.Range(0.35f, height),
                        Mathf.Lerp(-length * 0.45f, length * 0.45f, t) + Random.Range(-0.15f, 0.15f));
                }

                boulder.transform.localRotation = Quaternion.Euler(
                    Random.Range(0f, 40f), Random.Range(0f, 360f), Random.Range(0f, 40f));
                var s = Random.Range(0.55f, 1.05f);
                boulder.transform.localScale = new Vector3(
                    s * Random.Range(0.85f, 1.25f),
                    s * Random.Range(0.55f, 1.0f),
                    s * Random.Range(0.85f, 1.25f));
                boulder.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(boulder.GetComponent<Collider>());
            }
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

            // Flat ribbon blades (not thin upright sticks).
            for (var i = 0; i < 5; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(root.transform, false);
                blade.transform.localPosition = new Vector3(Random.Range(-0.12f, 0.12f), 0.4f, Random.Range(-0.12f, 0.12f));
                blade.transform.localRotation = Quaternion.Euler(
                    Random.Range(-25f, 25f), i * 28f + Random.Range(-8f, 8f), Random.Range(-18f, 18f));
                blade.transform.localScale = new Vector3(
                    Random.Range(0.08f, 0.14f),
                    Random.Range(0.55f, 0.95f),
                    Random.Range(0.01f, 0.02f));
                blade.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }

            return root;
        }

        public static GameObject CreateTubeCoral(string name, Transform parent, Vector3 pos, Material mat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = pos;

            for (var i = 0; i < 5; i++)
            {
                var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tube.name = $"Tube_{i}";
                tube.transform.SetParent(root.transform, false);
                tube.transform.localPosition = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    Random.Range(0.15f, 0.35f),
                    Random.Range(-0.2f, 0.2f));
                tube.transform.localScale = new Vector3(0.08f, Random.Range(0.18f, 0.35f), 0.08f);
                tube.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(tube.GetComponent<Collider>());
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
