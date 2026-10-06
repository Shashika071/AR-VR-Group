using System.IO;
using ReefExplorer.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ReefExplorer.EditorTools
{
    /// <summary>
    /// Upgrades ReefExplorer into a compact tropical reef using imported models + textured materials.
    /// Menu: Reef Rescue → Build Underwater Environment
    /// </summary>
    public static class UnderwaterEnvironmentBuilder
    {
        const string ScenePath = "Assets/ReefExplorer/Scenes/ReefExplorer.unity";
        const string MarkerName = "UnderwaterEnvironment_v1";
        const string MaterialsFolder = "Assets/ReefExplorer/Materials";
        const string TexturesFolder = "Assets/ReefExplorer/Textures";

        static readonly Vector3 CoralGarden = new(-6.5f, 0f, 11f);
        static readonly Vector3 SeagrassCrossing = new(0f, 0f, 15.5f);
        static readonly Vector3 SandyPassage = new(6.5f, 0f, 11.5f);
        static readonly Vector3 SamplePoint = new(3.5f, 0.6f, 13.5f);
        static readonly Vector3 BuoyPoint = new(0f, 0f, 7f);

        // Light coral for most placements. Heavy OBJs are hero-only (see PlaceHeroCorals).
        static readonly string[] CoralPaths =
        {
            "Assets/Corals/Coral 25.fbx",
            "Assets/Corals/Coral 25.fbx",
            "Assets/Corals/Coral 25.fbx",
        };

        static readonly string[] HeavyCoralPaths =
        {
            "Assets/Corals/Yellow_Coral.obj",
            "Assets/Corals/Sun_Coral.obj",
        };

        static readonly string[] StonePaths =
        {
            "Assets/Stone/Limestone6.FBX",
            "Assets/Stone/beachstones p1.FBX",
        };

        static readonly string[] PlantPaths =
        {
            "Assets/Plants/Aquarium_Anacharis_Plant.obj",
        };

        static readonly string[] FishPaths =
        {
            "Assets/New_fish/Angelfish.obj",
            "Assets/New_fish/Betta_Fish.obj",
            "Assets/New_fish/Undualte_Triggerfish.FBX",
            "Assets/New_fish/Protomelas Spilonotus.FBX",
            "Assets/New_fish/Aligator Gar.FBX",
        };

        [MenuItem("Reef Rescue/Build Underwater Environment")]
        [MenuItem("Reef Explorer/7. Build Underwater Environment")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play Mode",
                    "Exit Play Mode, then run Reef Rescue → Build Underwater Environment.", "OK");
                return;
            }

            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Missing Scene",
                    "Open or build ReefExplorer first (Reef Explorer → 2. Build / Refresh).", "OK");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (GameObject.Find("ResearchStation") == null)
            {
                EditorUtility.DisplayDialog("Wrong Scene",
                    "ResearchStation not found. Run Reef Explorer → 2 first.", "OK");
                return;
            }

            BuildIntoOpenScene(showDialog: !Application.isBatchMode);
        }

        public static void BuildBatch()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[ReefRescue] Scene missing: " + ScenePath);
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildIntoOpenScene(showDialog: false);
            EditorApplication.Exit(0);
        }

        public static void BuildIntoOpenScene(bool showDialog)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || GameObject.Find("ResearchStation") == null)
            {
                Debug.LogWarning("[ReefRescue] Cannot build environment — ResearchStation missing.");
                return;
            }

            Random.InitState(42); // Stable intentional layout on reruns.

            var mats = LoadOrCreateMaterials();
            ClearPreviousEnvironmentArt();
            var root = GetOrCreateRoot();

            ApplyUnderwaterRenderSettings();
            EnsureSunLight();
            BuildSeabed(root.transform, mats);
            BuildWaterHorizon(root.transform);
            BuildWaterSurface(root.transform);
            BuildRouteClusters(root.transform, mats);
            // Dense light carpet across the whole sand (fake props — real feeling, still VR-friendly).
            FillSpacedCarpet(root.transform, mats);
            FillDenseArenaPass(root.transform, mats);
            PlaceSpacedLife(root.transform, mats);
            FillCentreLightProps(root.transform, mats); // buoy / mid area — fake only, no heavy FBX
            PlaceRockHolesAndArches(root.transform, mats); // walk-through sea rock holes
            PlaceGuideLights(root.transform);
            RelocateMissionAreas();
            DressMonitoringBuoy(mats);
            HideFakePads();
            EnsureParticles();
            EnsureAtmosphereHost();
            PlaceBubbleProps(root.transform);
            SaveReusablePrefabs(mats);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[ReefRescue] Underwater environment built (textured imports preferred). Marker: UnderwaterEnvironment_v1");
            if (!showDialog)
                return;

            EditorUtility.DisplayDialog(
                "Underwater Environment",
                "Built textured compact reef:\n" +
                "Station → Buoy → Coral Garden / Seagrass / Sandy Passage.\n\n" +
                "1) Press Play\n" +
                "2) Desktop → Start Dive\n" +
                "3) Capture Game view from the station toward Coral Garden\n\n" +
                "Imported coral/stone/plant textures are preserved.",
                "OK");
        }

        static GameObject GetOrCreateRoot()
        {
            var root = GameObject.Find(MarkerName);
            if (root == null)
                root = new GameObject(MarkerName);
            return root;
        }

        static void ClearPreviousEnvironmentArt()
        {
            DestroyNamed("SeabedCarpet");
            DestroyNamed("EnvatoReefDecor");
            DestroyNamed("EnvatoFishSchool");
            DestroyNamed("WaterBubblesRoot");
            DestroyNamed("ReefEnvironment");
            DestroyNamed("Seabed");
            DestroyNamed("SafetyFloor");
            DestroyNamed("WaterSurface_Below");
            DestroyNamed("WaterHorizonCurtain");
            DestroyNamed("RuntimeReefFill");
            DestroyNamed("RuntimeExtraLife");
            DestroyNamed("ReefDensityRuntime");

            var marker = GameObject.Find(MarkerName);
            if (marker != null)
            {
                for (var i = marker.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(marker.transform.GetChild(i).gameObject);
            }

            DestroyByPrefix("Rock_");
            DestroyByPrefix("Plant_");
            DestroyByPrefix("CoralBranch_");
            DestroyByPrefix("CoralRound_");
            DestroyByPrefix("CoralFan_");
            DestroyByPrefix("SandMound_");
            DestroyByPrefix("ScenicFish_");
            DestroyByPrefix("FakeRock_");
            DestroyByPrefix("FakePlant_");
            DestroyByPrefix("FakeCoral");
            DestroyByPrefix("FakeFish_");
            DestroyByPrefix("RealCoral_");
            DestroyByPrefix("RealStone_");
            DestroyByPrefix("RealPlant_");
            DestroyByPrefix("FillRock_");
            DestroyByPrefix("FillPlant_");
            DestroyByPrefix("EnvRock_");
            DestroyByPrefix("EnvCoral");
            DestroyByPrefix("EnvGrass_");
            DestroyByPrefix("AmbientFish_");
            DestroyByPrefix("CentreRock_");
            DestroyByPrefix("CentrePlant_");
            DestroyByPrefix("CentreFish_");
            DestroyByPrefix("MidRock_");
            DestroyByPrefix("MidPlant_");
            DestroyByPrefix("ArenaPlant_");
            DestroyByPrefix("CarpetRock_");
            DestroyByPrefix("CarpetPlant");
            DestroyByPrefix("DenseRock_");
            DestroyByPrefix("DensePlant_");
            DestroyByPrefix("RockArch_");
            DestroyByPrefix("RockTunnel_");
            DestroyByPrefix("MovingBubble_");
            DestroyByPrefix("SoftBubble_");
            DestroyByPrefix("PathGuide_");
            DestroyByPrefix("GuideGlow_");
            DestroyByPrefix("SandRipple_");
        }

        static void ApplyUnderwaterRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            // Scuba-diver murk — short visibility, green-blue haze.
            RenderSettings.fogColor = new Color(0.04f, 0.20f, 0.26f);
            RenderSettings.fogDensity = 0.13f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.11f, 0.15f);
            RenderSettings.subtractiveShadowColor = new Color(0.02f, 0.08f, 0.12f);
        }

        static void EnsureSunLight()
        {
            var light = Object.FindAnyObjectByType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                var go = new GameObject("Sun_Underwater");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.name = "Sun_Underwater";
            light.color = new Color(0.42f, 0.68f, 0.78f);
            light.intensity = 0.55f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.transform.rotation = Quaternion.Euler(68f, -35f, 0f);
            if (light.GetComponent<CausticLightPulse>() == null)
                light.gameObject.AddComponent<CausticLightPulse>();

            var fill = GameObject.Find("Fill_Underwater");
            if (fill == null)
            {
                fill = new GameObject("Fill_Underwater");
                var fl = fill.AddComponent<Light>();
                fl.type = LightType.Directional;
                fl.color = new Color(0.12f, 0.32f, 0.42f);
                fl.intensity = 0.2f;
                fl.shadows = LightShadows.None;
                fill.transform.rotation = Quaternion.Euler(15f, 145f, 0f);
            }
            else
            {
                var fl = fill.GetComponent<Light>();
                if (fl != null)
                {
                    fl.color = new Color(0.12f, 0.32f, 0.42f);
                    fl.intensity = 0.2f;
                }
            }
        }

        static void BuildSeabed(Transform parent, MatBag mats)
        {
            // Large enough that walking the full reef never drops through the world.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Seabed";
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, 0f, 10f);
            ground.transform.localScale = new Vector3(3.2f, 1f, 3.4f); // fits tighter horizon ring
            ground.GetComponent<Renderer>().sharedMaterial = mats.sand;
            ground.isStatic = true;

            // Invisible safety floor — if the player walks past sand edge, they land instead of falling to start.
            var safety = GameObject.CreatePrimitive(PrimitiveType.Plane);
            safety.name = "SafetyFloor";
            safety.transform.SetParent(parent);
            safety.transform.position = new Vector3(0f, -0.05f, 10f);
            safety.transform.localScale = new Vector3(4.2f, 1f, 4.2f);
            Object.DestroyImmediate(safety.GetComponent<Renderer>());
            safety.isStatic = true;

            // Dunes inside the tighter horizon ring (less empty outer sand).
            for (var i = 0; i < 40; i++)
            {
                var p = new Vector3(
                    Random.Range(-12f, 12f),
                    0f,
                    Random.Range(1f, 20f));
                if (Mathf.Abs(p.x) < 1.5f && p.z < 4f)
                    continue;
                ProceduralReefMeshes.CreateSandMound($"SandRipple_{i}", parent, p,
                    Random.Range(1.6f, 3.0f), mats.sand);
            }

            // Edge rock frames hug the closer round barrier.
            PlaceRockCluster(parent, new Vector3(-11f, 0f, 8f), 5, 1.8f, mats);
            PlaceRockCluster(parent, new Vector3(11f, 0f, 8f), 5, 1.8f, mats);
            PlaceRockCluster(parent, new Vector3(-10f, 0f, 16f), 4, 1.7f, mats);
            PlaceRockCluster(parent, new Vector3(10f, 0f, 16f), 4, 1.7f, mats);
            PlaceRockCluster(parent, new Vector3(0f, 0f, 19f), 5, 2.0f, mats);
            PlaceRockCluster(parent, new Vector3(-9f, 0f, 3.5f), 4, 1.5f, mats);
            PlaceRockCluster(parent, new Vector3(9f, 0f, 3.5f), 4, 1.5f, mats);
            PlaceRockCluster(parent, new Vector3(-12f, 0f, 13f), 4, 1.9f, mats);
            PlaceRockCluster(parent, new Vector3(12f, 0f, 13f), 4, 1.9f, mats);
        }

        static void BuildWaterHorizon(Transform parent)
        {
            // Wall ring only (no cylinder top-cap — that looked like a huge empty disc).
            var root = new GameObject("WaterHorizonCurtain");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(0f, 0f, 10f);
            var mat = EnsureMat("Mat_Horizon", new Color(0.02f, 0.14f, 0.22f));
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.05f);

            const int walls = 18;
            // Pull ring in — large radius left empty water around the reef.
            const float radius = 15.5f;
            // Unlit fog colour so panels do not show lit seams.
            var unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                mat = new Material(unlit);
                var fog = new Color(0.04f, 0.20f, 0.26f, 1f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", fog);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", fog);
                mat.color = fog;
            }

            for (var i = 0; i < walls; i++)
            {
                var a = (i / (float)walls) * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wall.name = $"HorizonWall_{i}";
                wall.transform.SetParent(root.transform, false);
                wall.transform.position = new Vector3(outward.x * radius, 3.5f, 10f + outward.z * radius);
                // Face inward so the player sees a solid water wall (no backface gaps).
                wall.transform.rotation = Quaternion.LookRotation(-outward);
                wall.transform.localScale = new Vector3(6.2f, 10f, 1f);
                Object.DestroyImmediate(wall.GetComponent<Collider>());
                var r = wall.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static void BuildWaterSurface(Transform parent)
        {
            var surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
            surface.name = "WaterSurface_Below";
            surface.transform.SetParent(parent);
            surface.transform.position = new Vector3(0f, 8f, 12f);
            surface.transform.localScale = new Vector3(5.5f, 1f, 5.5f);
            Object.DestroyImmediate(surface.GetComponent<Collider>());

            var mat = EnsureMat("Mat_WaterSurface", new Color(0.05f, 0.25f, 0.35f, 0.4f));
            ConfigureTransparent(mat, new Color(0.06f, 0.28f, 0.38f, 0.38f));
            surface.GetComponent<Renderer>().sharedMaterial = mat;
            surface.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>
        /// Evenly spaced props across the whole sand — not piled in the centre.
        /// Larger step = faster Play and clearer spacing.
        /// </summary>
        static void FillSpacedCarpet(Transform parent, MatBag mats)
        {
            // Fill almost every cell — empty sand was the main complaint.
            const float step = 2.15f;
            var n = 0;
            for (var x = -12f; x <= 12f; x += step)
            {
                for (var z = 1.8f; z <= 20f; z += step)
                {
                    if (z < 3.5f && Mathf.Abs(x) < 2.0f)
                        continue;
                    if (Mathf.Abs(x) < 1.05f && z > 3.5f && z < 7.0f)
                        continue;

                    var p = new Vector3(
                        x + Random.Range(-0.5f, 0.5f),
                        0f,
                        z + Random.Range(-0.5f, 0.5f));

                    var kind = n++ % 4;
                    var far = Mathf.Abs(x) > 8.5f || z > 16f;
                    if (kind == 0)
                    {
                        var rock = ProceduralReefMeshes.CreateRock(
                            $"CarpetRock_{n}", parent, p, far ? Random.Range(1.0f, 1.8f) : Random.Range(0.65f, 1.2f), mats.rock);
                        rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        SetCheapRender(rock);
                    }
                    else if (kind == 1)
                    {
                        PlaceOneCoral(parent, p, mats, preferReal: false);
                    }
                    else if (kind == 2)
                    {
                        var plant = ProceduralReefMeshes.CreateSeaPlant($"CarpetPlant_{n}", parent, p, mats.plant);
                        if (plant.GetComponent<SeaPlantSway>() == null)
                            plant.AddComponent<SeaPlantSway>();
                        SetCheapRender(plant);
                    }
                    else
                    {
                        // Fewer real Anacharis (was too dense).
                        PlaceOnePlant(parent, p, mats, preferReal: true);
                    }
                }
            }
        }

        /// <summary>Second pass — more plants/rocks in gaps so no big empty patches remain.</summary>
        static void FillDenseArenaPass(Transform parent, MatBag mats)
        {
            const float step = 3.0f;
            var n = 0;
            for (var x = -11.5f; x <= 11.5f; x += step)
            {
                for (var z = 3f; z <= 19f; z += step)
                {
                    // Offset grid so it fills between the first carpet cells.
                    var p = new Vector3(
                        x + step * 0.5f + Random.Range(-0.35f, 0.35f),
                        0f,
                        z + step * 0.5f + Random.Range(-0.35f, 0.35f));

                    if (p.z < 3.5f && Mathf.Abs(p.x) < 2.0f)
                        continue;
                    if (Mathf.Abs(p.x) < 1.05f && p.z > 3.5f && p.z < 7.0f)
                        continue;

                    var kind = n++ % 3;
                    if (kind == 0)
                    {
                        var rock = ProceduralReefMeshes.CreateRock(
                            $"DenseRock_{n}", parent, p, Random.Range(0.75f, 1.4f), mats.rock);
                        rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        SetCheapRender(rock);
                    }
                    else if (kind == 1)
                    {
                        PlaceOnePlant(parent, p, mats, preferReal: true);
                    }
                    else
                    {
                        var plant = ProceduralReefMeshes.CreateSeaPlant($"DensePlant_{n}", parent, p, mats.plant);
                        if (plant.GetComponent<SeaPlantSway>() == null)
                            plant.AddComponent<SeaPlantSway>();
                        SetCheapRender(plant);
                    }
                }
            }

            // A few New_fish schools around the dense carpet.
            for (var i = 0; i < 6; i++)
            {
                var a = (i / 6f) * Mathf.PI * 2f;
                var d = 5f + (i % 3) * 3.5f;
                var c = new Vector3(Mathf.Cos(a) * d, 1.15f, 10f + Mathf.Sin(a) * d * 0.9f);
                PlaceLightFishSchool(parent, c, 2);
            }
        }

        /// <summary>Fish, plants, bubbles spaced around the map — not stacked in the middle.</summary>
        static void PlaceSpacedLife(Transform parent, MatBag mats)
        {
            var plantSpots = new[]
            {
                new Vector3(-10f, 0f, 5.5f), new Vector3(10f, 0f, 5.5f),
                new Vector3(-11f, 0f, 12f), new Vector3(11f, 0f, 12f),
                new Vector3(-9f, 0f, 16.5f), new Vector3(9f, 0f, 16.5f),
                new Vector3(-5f, 0f, 18.5f), new Vector3(5f, 0f, 18.5f),
                new Vector3(0f, 0f, 19f), new Vector3(-11.5f, 0f, 9f),
                new Vector3(11.5f, 0f, 9f), new Vector3(-5f, 0f, 9f),
                new Vector3(5f, 0f, 10f),
            };
            foreach (var c in plantSpots)
                PlaceSeagrassBand(parent, c, mats, count: 5);

            // Light ring of upright Anacharis (not crowded).
            for (var i = 0; i < 8; i++)
            {
                var a = (i / 8f) * Mathf.PI * 2f;
                var d = 7.5f + (i % 2) * 2.0f;
                var p = new Vector3(Mathf.Cos(a) * d, 0f, 10f + Mathf.Sin(a) * d * 0.85f);
                if (p.z < 3.5f && Mathf.Abs(p.x) < 2f)
                    continue;
                PlaceOnePlant(parent, p, mats, preferReal: true);
            }

            var fishSpots = new[]
            {
                new Vector3(-10f, 1.2f, 8f), new Vector3(10f, 1.15f, 9f),
                new Vector3(-7f, 1.2f, 16f), new Vector3(7f, 1.15f, 16f),
                new Vector3(0f, 1.25f, 19f),
            };
            foreach (var c in fishSpots)
                PlaceLightFishSchool(parent, c, 2);
        }

        /// <summary>
        /// Extra rocks / plants / fish around the buoy + mid path.
        /// Procedural (fake) only — never Yellow/Sun coral or heavy stone FBX.
        /// </summary>
        static void FillCentreLightProps(Transform parent, MatBag mats)
        {
            // Thick ring around Reef Buoy Seven (keep socket reach clear).
            for (var i = 0; i < 22; i++)
            {
                var a = (i / 22f) * Mathf.PI * 2f;
                var d = Random.Range(1.8f, 3.8f);
                var p = BuoyPoint + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                if (p.x > 0.3f && p.x < 1.6f && Mathf.Abs(p.z - BuoyPoint.z) < 1.2f)
                    continue;

                var kind = i % 3;
                if (kind == 0)
                {
                    var rock = ProceduralReefMeshes.CreateRock(
                        $"CentreRock_{i}", parent, p, Random.Range(0.9f, 1.55f), mats.rock);
                    rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    SetCheapRender(rock);
                }
                else if (kind == 1)
                {
                    PlaceOneCoral(parent, p, mats, preferReal: false);
                }
                else
                {
                    // Only every other centre plant is real Anacharis.
                    PlaceOnePlant(parent, p, mats, preferReal: i % 2 == 0);
                }
            }

            // Mid path clumps between station → buoy → zones (fake only).
            var clumps = new[]
            {
                new Vector3(-2.4f, 0f, 5.5f), new Vector3(2.5f, 0f, 5.6f),
                new Vector3(-2.8f, 0f, 6.8f), new Vector3(2.9f, 0f, 7.0f),
                new Vector3(-3.2f, 0f, 8.5f), new Vector3(3.3f, 0f, 8.8f),
                new Vector3(-2.0f, 0f, 10.0f), new Vector3(2.2f, 0f, 10.2f),
                new Vector3(-3.5f, 0f, 11.5f), new Vector3(3.6f, 0f, 11.8f),
                new Vector3(-4.0f, 0f, 12.5f), new Vector3(4.2f, 0f, 12.8f),
                new Vector3(-2.5f, 0f, 14.0f), new Vector3(2.6f, 0f, 14.2f),
                new Vector3(-1.8f, 0f, 15.5f), new Vector3(2.0f, 0f, 15.8f),
                new Vector3(-3.0f, 0f, 17.0f), new Vector3(3.2f, 0f, 17.2f),
            };
            for (var i = 0; i < clumps.Length; i++)
            {
                var c = clumps[i];
                for (var j = 0; j < 4; j++)
                {
                    var p = c + new Vector3(Random.Range(-0.85f, 0.85f), 0f, Random.Range(-0.7f, 0.7f));
                    if (Mathf.Abs(p.x) < 1.05f && p.z > 3.5f && p.z < 7.0f)
                        continue;

                    if (j == 0)
                    {
                        var rock = ProceduralReefMeshes.CreateRock(
                            $"MidRock_{i}_{j}", parent, p, Random.Range(0.75f, 1.35f), mats.rock);
                        rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        SetCheapRender(rock);
                    }
                    else
                    {
                        PlaceOnePlant(parent, p, mats, preferReal: j == 1);
                    }
                }
            }

            // New_fish models around buoy + mid path.
            PlaceLightFishSchool(parent, BuoyPoint + new Vector3(-1.5f, 1.15f, 0.8f), 2);
            PlaceLightFishSchool(parent, BuoyPoint + new Vector3(1.8f, 1.2f, 1.2f), 2);
            PlaceLightFishSchool(parent, new Vector3(0f, 1.2f, 13.5f), 2);
        }

        static void PlaceLightFishSchool(Transform parent, Vector3 center, int count)
        {
            PlaceFishSchool(parent, center, count);
        }

        /// <summary>
        /// Sea rock arches / tunnels the player can walk through (open hole, solid sides).
        /// </summary>
        static void PlaceRockHolesAndArches(Transform parent, MatBag mats)
        {
            // Main tunnel at Sandy Passage — face toward station so route leads through it.
            var tunnel = ProceduralReefMeshes.CreateRockTunnel(
                "RockTunnel_Sandy", parent, SandyPassage + new Vector3(0f, 0f, -0.2f), mats.rock, 3.6f);
            tunnel.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            SetCheapRender(tunnel);

            // Arch near Coral Garden approach
            var archA = ProceduralReefMeshes.CreateRockArch(
                "RockArch_Coral", parent, CoralGarden + new Vector3(2.2f, 0f, -2.5f), mats.rock,
                width: 2.7f, height: 2.7f, thickness: 1.0f);
            archA.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            SetCheapRender(archA);

            // Arch on path past buoy toward seagrass
            var archB = ProceduralReefMeshes.CreateRockArch(
                "RockArch_Buoy", parent, BuoyPoint + new Vector3(-2.8f, 0f, 2.8f), mats.rock,
                width: 2.6f, height: 2.65f, thickness: 1.0f);
            archB.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            SetCheapRender(archB);

            // Second arch toward seagrass crossing
            var archC = ProceduralReefMeshes.CreateRockArch(
                "RockArch_Grass", parent, SeagrassCrossing + new Vector3(2.6f, 0f, -2.2f), mats.rock,
                width: 2.7f, height: 2.7f, thickness: 1.0f);
            archC.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
            SetCheapRender(archC);

            // Far scenic arch
            var archD = ProceduralReefMeshes.CreateRockArch(
                "RockArch_Far", parent, new Vector3(-8.5f, 0f, 16.5f), mats.rock,
                width: 2.8f, height: 2.8f, thickness: 1.1f);
            archD.transform.rotation = Quaternion.Euler(0f, 55f, 0f);
            SetCheapRender(archD);

            // Soft plants framing tunnel mouth (does not block opening)
            PlaceSeagrassBand(parent, SandyPassage + new Vector3(-2.6f, 0f, -1.8f), mats, count: 4);
            PlaceSeagrassBand(parent, SandyPassage + new Vector3(2.6f, 0f, -1.8f), mats, count: 4);
            PlaceLightFishSchool(parent, SandyPassage + new Vector3(0f, 1.3f, 0.5f), 2);
        }

        static void BuildRouteClusters(Transform parent, MatBag mats)
        {
            // Soft path stones toward the buoy (mix real + fake).
            for (var i = 0; i < 6; i++)
            {
                var z = 3.2f + i * 1.35f;
                PlaceOneRock(parent, new Vector3(Random.Range(-0.7f, 0.7f), -0.05f, z), 0.45f, mats, mixFake: true);
            }

            // 1) Coral Garden — real + fake coral together.
            PlaceRockCluster(parent, CoralGarden + new Vector3(-2.4f, 0f, 0.2f), 4, 1.0f, mats);
            PlaceCoralCluster(parent, CoralGarden, mats, heavy: true);
            PlaceHeroCorals(parent, CoralGarden); // 1–2 heavy OBJ only
            PlaceFishSchool(parent, CoralGarden + new Vector3(0.2f, 1.15f, 0.3f), 2);

            // Near-station welcome
            PlaceCoralCluster(parent, new Vector3(-3.2f, 0f, 5.2f), mats, heavy: false);
            PlaceRockCluster(parent, new Vector3(-4.5f, 0f, 4.5f), 3, 1.0f, mats);
            PlaceRockCluster(parent, new Vector3(4.2f, 0f, 4.8f), 2, 0.95f, mats);
            PlaceSeagrassBand(parent, new Vector3(3.5f, 0f, 5.5f), mats, count: 7);

            // 2) Seagrass Crossing
            PlaceSeagrassBand(parent, SeagrassCrossing, mats, count: 10);
            PlaceRockCluster(parent, SeagrassCrossing + new Vector3(-3.6f, 0f, 1f), 3, 1.1f, mats);
            PlaceRockCluster(parent, SeagrassCrossing + new Vector3(3.6f, 0f, -0.5f), 3, 1.1f, mats);
            PlaceFishSchool(parent, SeagrassCrossing + new Vector3(0f, 1.15f, 0f), 2);

            // 3) Sandy Passage canyon (+ arches placed in PlaceRockHolesAndArches)
            PlaceRockCluster(parent, SandyPassage + new Vector3(-2.8f, 0f, 0.4f), 3, 1.35f, mats);
            PlaceRockCluster(parent, SandyPassage + new Vector3(2.9f, 0f, -0.3f), 3, 1.35f, mats);
            PlaceCoralCluster(parent, SandyPassage + new Vector3(0.4f, 0f, 1.6f), mats, heavy: false);
            PlaceSeagrassBand(parent, SandyPassage + new Vector3(0f, 0f, -1.2f), mats, count: 6);

            // Buoy flanks
            PlaceRockCluster(parent, BuoyPoint + new Vector3(-2.4f, 0f, 0.5f), 2, 1.15f, mats);
            PlaceRockCluster(parent, BuoyPoint + new Vector3(2.4f, 0f, -0.3f), 2, 1.15f, mats);
            PlaceSeagrassBand(parent, BuoyPoint + new Vector3(1.8f, 0f, 1.5f), mats, count: 5);
        }

        static void PlaceRockCluster(Transform parent, Vector3 center, int count, float size, MatBag mats)
        {
            for (var i = 0; i < count; i++)
            {
                var offset = new Vector3(Random.Range(-1.6f, 1.6f), -0.12f, Random.Range(-1.3f, 1.3f));
                // Boost size so rocks read as real formations, not pebbles.
                PlaceOneRock(parent, center + offset, size * Random.Range(1.05f, 1.45f), mats, mixFake: true);
            }
        }

        static void PlaceOneRock(Transform parent, Vector3 pos, float size, MatBag mats, bool mixFake, bool preferFake = false)
        {
            size = Mathf.Max(size, 0.55f);
            var useReal = !preferFake && (!mixFake || Random.value < 0.45f);
            if (useReal)
            {
                var path = StonePaths[Random.Range(0, StonePaths.Length)];
                if (TrySpawnModel(parent, path, pos, size, mats.rock, forceMaterial: false, bury: 0.18f))
                    return;
            }

            var go = ProceduralReefMeshes.CreateRock($"FakeRock_{pos.x:0}_{pos.z:0}", parent, pos, size, mats.rock);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            SetCheapRender(go);
        }

        static void PlaceHeroCorals(Transform parent, Vector3 center)
        {
            // Only a couple of heavy high-poly corals — these were the main hitch source.
            TrySpawnModel(parent, HeavyCoralPaths[0], center + new Vector3(0.4f, 0f, -0.3f), 1.1f, null, false);
            TrySpawnModel(parent, HeavyCoralPaths[1], center + new Vector3(-1.1f, 0f, 0.8f), 0.9f, null, false);
        }

        static void PlaceCoralCluster(Transform parent, Vector3 center, MatBag mats, bool heavy)
        {
            var n = heavy ? 8 : 5;
            for (var i = 0; i < n; i++)
            {
                var a = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                var d = Random.Range(0.35f, heavy ? 2.4f : 1.6f);
                var p = center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                // Mostly fake + light Coral 25 — not Yellow/Sun every time.
                PlaceOneCoral(parent, p, mats, preferReal: i % 3 == 0);
            }

            var fakeExtra = heavy ? 4 : 2;
            for (var i = 0; i < fakeExtra; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var d = Random.Range(0.5f, heavy ? 2.0f : 1.3f);
                var p = center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                PlaceOneCoral(parent, p, mats, preferReal: false);
            }
        }

        static void PlaceOneCoral(Transform parent, Vector3 p, MatBag mats, bool preferReal)
        {
            if (preferReal)
            {
                var path = CoralPaths[Random.Range(0, CoralPaths.Length)];
                var size = path.Contains("Yellow") ? Random.Range(0.85f, 1.35f)
                    : path.Contains("Sun") ? Random.Range(0.7f, 1.15f)
                    : Random.Range(0.55f, 0.95f);
                if (TrySpawnModel(parent, path, p, size, null, forceMaterial: false))
                    return;
            }

            GameObject go;
            var kind = Random.Range(0, 4);
            if (kind == 0)
                go = ProceduralReefMeshes.CreateBranchingCoral($"FakeCoralB_{p.x:0}_{p.z:0}", parent, p, mats.coral);
            else if (kind == 1)
                go = ProceduralReefMeshes.CreateRoundedCoral($"FakeCoralR_{p.x:0}_{p.z:0}", parent, p, mats.accent);
            else if (kind == 2)
                go = ProceduralReefMeshes.CreateFanCoral($"FakeCoralF_{p.x:0}_{p.z:0}", parent, p, mats.clown);
            else
                go = ProceduralReefMeshes.CreateTubeCoral($"FakeCoralT_{p.x:0}_{p.z:0}", parent, p, mats.accent);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.transform.localScale *= Random.Range(0.85f, 1.25f);
            SetCheapRender(go);
        }

        static void PlaceSeagrassBand(Transform parent, Vector3 center, MatBag mats, int count = 10)
        {
            for (var i = 0; i < count; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var p = center + new Vector3(
                    side * Random.Range(1.1f, 3.0f),
                    0f,
                    Random.Range(-2.4f, 2.4f));
                PlaceOnePlant(parent, p, mats, preferReal: true);
            }
        }

        static void PlaceOnePlant(Transform parent, Vector3 p, MatBag mats, bool preferReal)
        {
            GameObject plant = null;
            if (preferReal && PlantPaths.Length > 0)
            {
                var path = PlantPaths[Random.Range(0, PlantPaths.Length)];
                if (TrySpawnModel(parent, path, p, Random.Range(0.5f, 0.8f), mats.plant, forceMaterial: false))
                {
                    plant = parent.GetChild(parent.childCount - 1).gameObject;
                    OrientPlantUpright(plant, p);
                }
            }

            if (plant == null)
                plant = ProceduralReefMeshes.CreateSeaPlant($"FakePlant_{p.x:0}_{p.z:0}", parent, p, mats.plant);

            if (plant != null && plant.GetComponent<SeaPlantSway>() == null)
                plant.AddComponent<SeaPlantSway>();
        }

        /// <summary>Anacharis OBJ lies flat — rotate so fronds grow UP from the sand.</summary>
        static void OrientPlantUpright(GameObject plant, Vector3 groundPos)
        {
            if (plant == null)
                return;

            var yaw = Random.Range(0f, 360f);
            var rends = plant.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                plant.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
                plant.transform.position = groundPos;
                return;
            }

            // Pick the pitch where most of the mesh sits ABOVE the root (not hanging down).
            float BestScore(Quaternion rot)
            {
                plant.transform.rotation = rot;
                var b = rends[0].bounds;
                for (var i = 1; i < rends.Length; i++)
                    b.Encapsulate(rends[i].bounds);
                // Prefer tall plants with centre above the ground point.
                return (b.center.y - groundPos.y) + b.size.y * 0.35f;
            }

            var candidates = new[]
            {
                Quaternion.Euler(90f, yaw, 0f),
                Quaternion.Euler(-90f, yaw, 0f),
                Quaternion.Euler(0f, yaw, 90f),
                Quaternion.Euler(0f, yaw, -90f),
            };

            var best = candidates[0];
            var bestScore = float.NegativeInfinity;
            foreach (var rot in candidates)
            {
                var score = BestScore(rot);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = rot;
                }
            }

            plant.transform.rotation = best;
            plant.transform.position = groundPos;
            var bounds = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);
            plant.transform.position = groundPos + Vector3.up * (-bounds.min.y + 0.02f);
        }

        static void PlaceFishSchool(Transform parent, Vector3 center, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var p = center + new Vector3(
                    Random.Range(-1.6f, 1.6f),
                    Random.Range(0.7f, 1.45f),
                    Random.Range(-1.6f, 1.6f));

                GameObject fish = null;
                // Try a couple of New_fish models if one fails to import.
                for (var attempt = 0; attempt < FishPaths.Length && fish == null; attempt++)
                {
                    var path = FishPaths[(i + attempt) % FishPaths.Length];
                    if (!TrySpawnModel(parent, path, p, Random.Range(0.32f, 0.48f), null, forceMaterial: false))
                        continue;
                    fish = parent.GetChild(parent.childCount - 1).gameObject;
                }

                if (fish == null)
                    continue;

                fish.name = $"AmbientFish_{center.x:0}_{i}";
                fish.isStatic = false;
                // New_fish OBJs/FBXs are authored nose-down (-Y). Level them for swim.
                fish.transform.rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
                foreach (var r in fish.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = ShadowCastingMode.Off;

                var wander = fish.GetComponent<AnimalWander>() ?? fish.AddComponent<AnimalWander>();
                var so = new SerializedObject(wander);
                so.FindProperty("center").vector3Value = p;
                so.FindProperty("extents").vector3Value = new Vector3(2.2f, 0.35f, 2.2f);
                so.FindProperty("speed").floatValue = Random.Range(0.22f, 0.38f);
                so.FindProperty("meshEulerOffset").vector3Value = new Vector3(-90f, 0f, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void PlaceGuideLights(Transform parent)
        {
            var spots = new[]
            {
                new Vector3(0f, 0.35f, 4.8f),
                new Vector3(-2.8f, 0.35f, 8.5f),
                new Vector3(2.8f, 0.35f, 8.5f),
                new Vector3(0f, 0.35f, 12.5f),
            };
            for (var i = 0; i < spots.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"GuideGlow_{i}";
                go.transform.SetParent(parent);
                go.transform.position = spots[i];
                go.transform.localScale = Vector3.one * 0.1f;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var mat = EnsureMat("Mat_GuideGlow", new Color(0.35f, 0.9f, 1f));
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 0.9f) * 1.4f);
                }

                go.GetComponent<Renderer>().sharedMaterial = mat;
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.45f, 0.9f, 1f);
                light.intensity = 0.7f;
                light.range = 4f;
                light.shadows = LightShadows.None;
            }
        }

        static void RelocateMissionAreas()
        {
            Move("Zone_Coral", CoralGarden);
            Move("Zone_Turtle", SeagrassCrossing);
            Move("Zone_Ray", SandyPassage);
            Move("SampleZone", SamplePoint);
            Move("MonitoringBuoy", BuoyPoint);

            foreach (var wander in Object.FindObjectsByType<AnimalWander>())
            {
                if (wander == null)
                    continue;
                // Ambient fish keep their own centres; only retarget mission animals.
                if (wander.name.StartsWith("AmbientFish_") || wander.name.StartsWith("SchoolFish_"))
                    continue;
                var so = new SerializedObject(wander);
                so.FindProperty("center").vector3Value = wander.transform.position;
                so.FindProperty("extents").vector3Value = new Vector3(2.2f, 0.45f, 2.2f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void DressMonitoringBuoy(MatBag mats)
        {
            var buoy = GameObject.Find("MonitoringBuoy");
            if (buoy == null)
                return;

            // Keep socket / scripts — only restyle visual shells.
            var metal = EnsureMat("Mat_BuoyMetal", new Color(0.45f, 0.5f, 0.55f));
            var stripe = EnsureMat("Mat_BuoyStripe", new Color(0.95f, 0.45f, 0.12f));
            var white = EnsureMat("Mat_BuoyWhite", new Color(0.92f, 0.94f, 0.96f));

            SetChildMat(buoy, "BuoyBase", metal);
            SetChildMat(buoy, "BuoyPole", white);
            SetChildMat(buoy, "BuoyHead", stripe);
            SetChildMat(buoy, "BuoyLamp", stripe);
            SetChildMat(buoy, "Antenna", metal);

            if (buoy.transform.Find("BuoyFloatRing") == null)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "BuoyFloatRing";
                ring.transform.SetParent(buoy.transform, false);
                ring.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                ring.transform.localScale = new Vector3(1.25f, 0.12f, 1.25f);
                ring.GetComponent<Renderer>().sharedMaterial = stripe;
                Object.DestroyImmediate(ring.GetComponent<Collider>());
            }

            if (buoy.transform.Find("BuoyPanel") == null)
            {
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = "BuoyPanel";
                panel.transform.SetParent(buoy.transform, false);
                panel.transform.localPosition = new Vector3(-0.35f, 1.55f, 0.12f);
                panel.transform.localScale = new Vector3(0.08f, 0.55f, 0.35f);
                panel.GetComponent<Renderer>().sharedMaterial = metal;
                Object.DestroyImmediate(panel.GetComponent<Collider>());
            }

            // Ensure battery socket stays readable (slightly brighter).
            var socket = buoy.transform.Find("BuoyPowerSocket");
            if (socket != null)
            {
                var r = socket.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = EnsureMat("Mat_BuoySocket", new Color(0.2f, 0.75f, 0.85f));
            }
        }

        static void SetChildMat(GameObject root, string child, Material mat)
        {
            var t = root.transform.Find(child);
            if (t == null)
                return;
            var r = t.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = mat;
        }

        static void HideFakePads()
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null)
                    continue;
                if (t.name != "ZonePad" && t.name != "SampleMarker" && !t.name.StartsWith("PathMarker"))
                    continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            }
        }

        static void EnsureParticles()
        {
            if (GameObject.Find("UnderwaterParticles") != null)
                return;

            var go = new GameObject("UnderwaterParticles");
            go.transform.position = new Vector3(0f, 2f, 12f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            // Marine snow — the haze divers see floating in the water column.
            main.startLifetime = 9f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startColor = new Color(0.75f, 0.9f, 0.95f, 0.45f);
            main.maxParticles = 140;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 16f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 4f, 18f);
            go.AddComponent<UnderwaterParticlesFix>();
        }

        static void EnsureAtmosphereHost()
        {
            var atmo = Object.FindAnyObjectByType<UnderwaterAtmosphere>();
            if (atmo == null)
            {
                var go = new GameObject("UnderwaterAtmosphere");
                atmo = go.AddComponent<UnderwaterAtmosphere>();
            }

            // Force scuba-diver murk onto the scene component (overrides clearer saved values).
            var so = new SerializedObject(atmo);
            so.FindProperty("fogColor").colorValue = new Color(0.04f, 0.20f, 0.26f);
            so.FindProperty("fogDensity").floatValue = 0.13f;
            so.FindProperty("ambient").colorValue = new Color(0.03f, 0.11f, 0.15f);
            so.ApplyModifiedPropertiesWithoutUndo();
            atmo.Apply();

            if (Object.FindAnyObjectByType<UnderwaterFeelRuntime>() == null)
            {
                var go = new GameObject("UnderwaterFeelRuntime");
                go.AddComponent<UnderwaterFeelRuntime>();
            }
        }

        static void PlaceBubbleProps(Transform parent)
        {
            var spots = new[]
            {
                new Vector3(-10f, 0.25f, 7f),
                new Vector3(10f, 0.25f, 8f),
                new Vector3(-6f, 0.2f, 15f),
                new Vector3(6f, 0.25f, 16f),
                new Vector3(0f, 0.25f, 21f),
                new Vector3(-1.5f, 0.2f, 5.5f),
                new Vector3(-12f, 0.25f, 12f),
                new Vector3(12f, 0.25f, 13f),
                // Around buoy / centre arena
                new Vector3(-2.2f, 0.2f, 7.2f),
                new Vector3(2.4f, 0.25f, 7.5f),
                new Vector3(0.3f, 0.3f, 9.5f),
                new Vector3(-3.5f, 0.2f, 11f),
                new Vector3(3.8f, 0.25f, 12f),
            };
            for (var i = 0; i < spots.Length; i++)
            {
                var before = parent.childCount;
                if (!TrySpawnModel(parent, "Assets/Water_bubles/Prop_09_Bubbles_Size_01_StemCell.fbx",
                        spots[i], Random.Range(0.35f, 0.55f), null, forceMaterial: false))
                {
                    // Lightweight fallback bubble if FBX missing.
                    var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fallback.name = $"SoftBubble_{i}";
                    fallback.transform.SetParent(parent);
                    fallback.transform.position = spots[i];
                    fallback.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
                    Object.DestroyImmediate(fallback.GetComponent<Collider>());
                    fallback.GetComponent<Renderer>().sharedMaterial =
                        EnsureMat("Mat_BubbleSoft", new Color(0.75f, 0.95f, 1f, 0.45f));
                    SetCheapRender(fallback);
                }

                if (parent.childCount > before)
                {
                    var bubble = parent.GetChild(parent.childCount - 1).gameObject;
                    bubble.name = $"MovingBubble_{i}";
                    var drift = bubble.GetComponent<BubbleDrift>() ?? bubble.AddComponent<BubbleDrift>();
                    drift.speed = Random.Range(0.14f, 0.32f);
                    drift.wobble = Random.Range(0.2f, 0.45f);
                    drift.resetY = Random.Range(3.5f, 5.5f);
                    drift.basePos = bubble.transform.position;
                }
            }
        }

        static void SaveReusablePrefabs(MatBag mats)
        {
            const string folder = "Assets/ReefExplorer/Prefabs/Environment";
            Directory.CreateDirectory(folder);

            SaveOnePrefab(ProceduralReefMeshes.CreateRock("PF_Rock", null, Vector3.zero, 1f, mats.rock),
                $"{folder}/PF_Rock.prefab");
            SaveOnePrefab(ProceduralReefMeshes.CreateBranchingCoral("PF_CoralBranch", null, Vector3.zero, mats.coral),
                $"{folder}/PF_CoralBranch.prefab");
            SaveOnePrefab(ProceduralReefMeshes.CreateRoundedCoral("PF_CoralRound", null, Vector3.zero, mats.accent),
                $"{folder}/PF_CoralRound.prefab");
            SaveOnePrefab(ProceduralReefMeshes.CreateFanCoral("PF_CoralFan", null, Vector3.zero, mats.clown),
                $"{folder}/PF_CoralFan.prefab");
            SaveOnePrefab(ProceduralReefMeshes.CreateTubeCoral("PF_CoralTube", null, Vector3.zero, mats.accent),
                $"{folder}/PF_CoralTube.prefab");
            var grass = ProceduralReefMeshes.CreateSeaPlant("PF_Seagrass", null, Vector3.zero, mats.plant);
            if (grass.GetComponent<SeaPlantSway>() == null)
                grass.AddComponent<SeaPlantSway>();
            SaveOnePrefab(grass, $"{folder}/PF_Seagrass.prefab");
        }

        static void SaveOnePrefab(GameObject temp, string path)
        {
            if (temp == null)
                return;
            PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
        }

        /// <summary>
        /// Instantiate an imported model. Keeps original textures unless forceMaterial is true.
        /// </summary>
        static bool TrySpawnModel(
            Transform parent, string path, Vector3 pos, float size, Material fallbackMat,
            bool forceMaterial, float bury = 0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    path.Replace(".fbx", ".FBX").Replace(".obj", ".OBJ"));
            }

            if (prefab == null)
                return false;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (go == null)
                go = Object.Instantiate(prefab);

            go.name = Path.GetFileNameWithoutExtension(path) + "_Env";
            go.transform.SetParent(parent);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            Fit(go, size);
            go.transform.position = pos + Vector3.down * bury;

            if (forceMaterial && fallbackMat != null)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = fallbackMat;
            }
            else
            {
                RepairMaterials(go, path, fallbackMat);
            }

            SetCheapRender(go);
            // Heavy coral OBJs stay dynamic; light props can be static for batching.
            var isFish = path.IndexOf("fish", System.StringComparison.OrdinalIgnoreCase) >= 0
                         || path.IndexOf("New_fish", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!path.Contains("Yellow_Coral") && !path.Contains("Sun_Coral") && !isFish)
                go.isStatic = true;

            return true;
        }

        static void SetCheapRender(GameObject go)
        {
            if (go == null)
                return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        static void RepairMaterials(GameObject go, string path, Material fallbackMat)
        {
            // Assign known Sun Coral textures when the OBJ arrives untextured / pink.
            if (path.Contains("Sun_Coral"))
            {
                var diff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Corals/Sun_Coral/Coral_diffuse.jpg");
                var bump = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Corals/Sun_Coral/Coral_bump.jpg");
                var mat = EnsureTexturedMat("Mat_SunCoral", new Color(1f, 0.75f, 0.45f), diff, bump);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
                return;
            }

            if (path.Contains("Yellow_Coral") || path.Contains("Coral 25"))
            {
                var mat = EnsureMat(
                    path.Contains("Yellow") ? "Mat_YellowCoral" : "Mat_Coral25",
                    path.Contains("Yellow")
                        ? new Color(0.95f, 0.72f, 0.22f)
                        : new Color(0.88f, 0.38f, 0.55f));
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.32f);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsMissingOrPink(r))
                        r.sharedMaterial = mat;
                }

                return;
            }

            if (path.Contains("06_Pebble") || path.Contains("beachstones") || path.Contains("Limestone"))
            {
                var pebbleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Stone/06_Pebble Stack.png");
                var rockMat = EnsureTexturedMat("Mat_StoneImport", new Color(0.55f, 0.52f, 0.48f), pebbleTex, null);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (IsMissingOrPink(r))
                        r.sharedMaterial = rockMat;
                }

                return;
            }

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsMissingOrPink(r))
                    continue;
                if (fallbackMat != null)
                    r.sharedMaterial = fallbackMat;
            }
        }

        static bool IsMissingOrPink(Renderer r)
        {
            if (r == null || r.sharedMaterial == null)
                return true;
            var m = r.sharedMaterial;
            if (m.shader == null || m.shader.name.Contains("InternalError") || m.shader.name.Contains("Hidden/InternalError"))
                return true;
            // Keep materials that already have an albedo map.
            if (m.mainTexture != null)
                return false;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                return false;
            // Untextured / default materials need a URP-safe replacement for coral & stone.
            return true;
        }

        static void Fit(GameObject go, float target)
        {
            go.transform.localScale = Vector3.one;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0)
            {
                go.transform.localScale = Vector3.one * target;
                return;
            }

            var b = rends[0].bounds;
            for (var i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            var cur = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (cur < 0.01f)
                return;
            go.transform.localScale = Vector3.one * Mathf.Clamp(target / cur, 0.01f, 8f);
        }

        static void Move(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.transform.position = pos;
        }

        static void DestroyNamed(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.DestroyImmediate(go);
        }

        static void DestroyByPrefix(string prefix)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (var i = all.Length - 1; i >= 0; i--)
            {
                var t = all[i];
                if (t == null || !t.name.StartsWith(prefix))
                    continue;
                if (t.parent != null && t.parent.name.StartsWith(prefix))
                    continue;
                if (t.name.StartsWith("Animal_") || t.name is "Scanner" or "SampleBottle" or "PowerCell")
                    continue;
                Object.DestroyImmediate(t.gameObject);
            }
        }

        static MatBag LoadOrCreateMaterials()
        {
            Directory.CreateDirectory(MaterialsFolder);
            Directory.CreateDirectory(TexturesFolder);

            var sandTex = EnsureSandTexture();
            // Warm sand muted by underwater blue (not bright beach sand).
            var sand = EnsureTexturedMat("Mat_Sand", new Color(0.55f, 0.52f, 0.38f), sandTex, null, tiling: 10f);
            var rockTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Stone/06_Pebble Stack.png");
            var rock = EnsureTexturedMat("Mat_Rock", new Color(0.42f, 0.4f, 0.38f), rockTex, null, tiling: 2.5f);

            return new MatBag
            {
                sand = sand,
                coral = EnsureMat("Mat_Coral", new Color(0.9f, 0.32f, 0.45f)),
                rock = rock,
                plant = EnsureMat("Mat_Plant", new Color(0.12f, 0.62f, 0.28f)),
                accent = EnsureMat("Mat_Accent", new Color(1f, 0.55f, 0.22f)),
                clown = EnsureMat("Mat_Clown", new Color(1f, 0.5f, 0.12f)),
            };
        }

        static Texture2D EnsureSandTexture()
        {
            const string path = TexturesFolder + "/Tex_SandRipple.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
                return existing;

            const int res = 256;
            var tex = new Texture2D(res, res, TextureFormat.RGB24, true);
            for (var y = 0; y < res; y++)
            {
                for (var x = 0; x < res; x++)
                {
                    var nx = x / (float)res;
                    var ny = y / (float)res;
                    var ripple = Mathf.PerlinNoise(nx * 6f, ny * 14f);
                    var grain = Mathf.PerlinNoise(nx * 40f + 3.1f, ny * 40f + 1.7f);
                    var v = Mathf.Lerp(0.72f, 0.95f, ripple * 0.7f + grain * 0.3f);
                    tex.SetPixel(x, y, new Color(v, v * 0.9f, v * 0.62f));
                }
            }

            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true);
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material EnsureMat(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                             ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material EnsureTexturedMat(
            string name, Color color, Texture2D albedo, Texture2D normal, float tiling = 1f)
        {
            var mat = EnsureMat(name, color);
            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", albedo);
                mat.mainTexture = albedo;
                mat.mainTextureScale = new Vector2(tiling, tiling);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            }

            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void ConfigureTransparent(Material mat, Color color)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.8f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
        }

        sealed class MatBag
        {
            public Material sand, coral, rock, plant, accent, clown;
        }
    }
}
