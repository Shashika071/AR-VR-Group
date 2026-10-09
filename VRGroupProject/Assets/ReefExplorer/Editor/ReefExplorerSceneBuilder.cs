using System.IO;
using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.Diagnostics;
using ReefExplorer.Environment;
using ReefExplorer.Input;
using ReefExplorer.Interaction;
using ReefExplorer.Survey;
using ReefExplorer.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Object = UnityEngine.Object;

namespace ReefExplorer.EditorTools
{
    public static class ReefExplorerSceneBuilder
    {
        const string ScenePath = "Assets/ReefExplorer/Scenes/ReefExplorer.unity";
        const string XrOriginPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string DataFolder = "Assets/ReefExplorer/Data";
        const string MaterialsFolder = "Assets/ReefExplorer/Materials";

        [MenuItem("Reef Explorer/2. Build / Refresh ReefExplorer Scene")]
        public static void BuildOrRefreshScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ReefExplorer] Stop Play Mode before building the scene.");
                return;
            }

            ProceduralAudioFactory.EnsureAllClips();
            EnsureFolders();
            var (species, baseline, sites) = EnsureSpeciesAndBaseline();
            var mats = EnsureMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Underwater atmosphere — no ordinary sky / sharp horizon.
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.13f;
            RenderSettings.fogColor = new Color(0.04f, 0.20f, 0.26f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.11f, 0.15f);

            CreateLight();
            var systems = CreateSystems(species, baseline, sites);
            var station = CreateStation(mats);
            // Marker so runtime spreader does not re-scatter props into old cube style.
            new GameObject("ReefVisuals_v2");
            CreateSeabed(mats);
            CreateZonesAndAnimals(mats, species, sites);
            CreateMonitoringBuoy(mats);
            CreateTools(mats, station);
            CreateTutorialProps(mats);
            CreatePlayers(systems.modeSelector);
            CreateUi(systems.mission, systems.modeSelector, systems.audioHub);
            CreateParticles();
            var atmo = new GameObject("UnderwaterAtmosphere");
            atmo.AddComponent<UnderwaterAtmosphere>();

            Directory.CreateDirectory("Assets/ReefExplorer/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            // Author compact tropical reef art on top of mission systems.
            UnderwaterEnvironmentBuilder.BuildIntoOpenScene(showDialog: false);

            Debug.Log("[ReefExplorer] Scene built at Assets/ReefExplorer/Scenes/ReefExplorer.unity");
        }

        [MenuItem("Reef Explorer/3. Open ReefExplorer Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ReefExplorer] Stop Play Mode before opening scenes from the menu.");
                return;
            }

            if (!File.Exists(ScenePath))
                BuildOrRefreshScene();
            else
                EditorSceneManager.OpenScene(ScenePath);
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(MaterialsFolder);
            Directory.CreateDirectory("Assets/ReefExplorer/Scenes");
            Directory.CreateDirectory("Assets/ReefExplorer/Prefabs");
        }

        static (SpeciesDefinition[], BaselineSurveyData, SiteDefinition[]) EnsureSpeciesAndBaseline()
        {
            var clown = EnsureSpecies("Species_Clownfish", "clownfish", "Clownfish", new Color(1f, 0.55f, 0.1f));
            var turtle = EnsureSpecies("Species_SeaTurtle", "sea_turtle", "Sea Turtle", new Color(0.3f, 0.75f, 0.4f));
            var ray = EnsureSpecies("Species_Ray", "ray", "Starfish", new Color(0.95f, 0.45f, 0.15f));

            var baseline = AssetDatabase.LoadAssetAtPath<BaselineSurveyData>($"{DataFolder}/BaselineSurvey.asset");
            if (baseline == null)
            {
                baseline = ScriptableObject.CreateInstance<BaselineSurveyData>();
                AssetDatabase.CreateAsset(baseline, $"{DataFolder}/BaselineSurvey.asset");
            }

            var bso = new SerializedObject(baseline);
            bso.FindProperty("surveyLabel").stringValue = "Previous simulated survey (educational)";
            bso.FindProperty("disclaimer").stringValue =
                "Simulated educational data only. Do not treat this as a real reef-health assessment.";
            var entries = bso.FindProperty("entries");
            entries.arraySize = 3;
            SetEntry(entries.GetArrayElementAtIndex(0), clown, "zone_coral", 2);
            SetEntry(entries.GetArrayElementAtIndex(1), turtle, "zone_turtle", 1);
            SetEntry(entries.GetArrayElementAtIndex(2), ray, "zone_ray", 1);
            bso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(baseline);

            var coralSite = EnsureSite("Site_Coral", "site_coral", "Coral Garden", "zone_coral", clown, "Stable rock and rubble", "Moderate — some bleaching on table corals", "Stable rock and rubble, minor loose fragments", "Fair — storm damage to branching corals, substrate intact", 3, false, true);
            var seagrassSite = EnsureSite("Site_Seagrass", "site_seagrass", "Seagrass Crossing", "zone_turtle", turtle, "Dense seagrass meadow", "Good — healthy seagrass bed", "Patchy seagrass", "Poor — anchor damage to seagrass bed", 2, true, false, "Toxic barrel", "Anchor damage has destabilized substrate, and toxic barrel is present.");
            var sandSite = EnsureSite("Site_Sand", "site_sand", "Starfish Ledge", "zone_ray", ray, "Sloping rock ledge", "Fair — starfish cover the rock", "Sloping rock ledge", "Fair — starfish cover the rock", 1, false, false, "", "The ledge is steep and already covered by starfish, so a coral trial would disturb them.");

            return (new[] { clown, turtle, ray }, baseline, new[] { coralSite, seagrassSite, sandSite });
        }

        static SiteDefinition EnsureSite(string assetName, string id, string display, string zone, SpeciesDefinition targetAnimal, string baseSub, string baseCor, string curSub, string curCor, int rub, bool hazard, bool suitable, string hazardType = "", string unsuitableReason = "")
        {
            var path = $"{DataFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SiteDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SiteDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("siteId").stringValue = id;
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("zoneId").stringValue = zone;
            so.FindProperty("baselineSubstrate").stringValue = baseSub;
            so.FindProperty("baselineCoralCondition").stringValue = baseCor;
            so.FindProperty("currentSubstrate").stringValue = curSub;
            so.FindProperty("currentCoralCondition").stringValue = curCor;
            so.FindProperty("initialRubbishCount").intValue = rub;
            so.FindProperty("hasHazard").boolValue = hazard;
            so.FindProperty("hazardType").stringValue = hazardType;
            so.FindProperty("suitableForRestoration").boolValue = suitable;
            so.FindProperty("unsuitableReason").stringValue = unsuitableReason;
            so.FindProperty("targetAnimal").objectReferenceValue = targetAnimal;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void SetEntry(SerializedProperty prop, SpeciesDefinition species, string zone, int count)
        {
            prop.FindPropertyRelative("species").objectReferenceValue = species;
            prop.FindPropertyRelative("zoneId").stringValue = zone;
            prop.FindPropertyRelative("count").intValue = count;
        }

        static SpeciesDefinition EnsureSpecies(string assetName, string id, string display, Color color)
        {
            var path = $"{DataFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SpeciesDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("speciesId").stringValue = id;
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("description").stringValue = display switch
            {
                "Clownfish" => "Clownfish live among anemones that protect them from predators.",
                "Sea Turtle" => "Sea turtles migrate long distances and often return to nesting beaches.",
                "Starfish" => "Starfish cling to the rocky ledge and should be left undisturbed.",
                _ => $"{display} used for the educational reef survey."
            };
            so.FindProperty("accentColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static MaterialBag EnsureMaterials()
        {
            return new MaterialBag
            {
                sand = EnsureMaterial("Mat_Sand", new Color(0.82f, 0.72f, 0.48f)),
                coral = EnsureMaterial("Mat_Coral", new Color(0.9f, 0.32f, 0.45f)),
                rock = EnsureMaterial("Mat_Rock", new Color(0.42f, 0.4f, 0.38f)),
                plant = EnsureMaterial("Mat_Plant", new Color(0.18f, 0.62f, 0.38f)),
                metal = EnsureMaterial("Mat_Metal", new Color(0.4f, 0.48f, 0.52f)),
                accent = EnsureMaterial("Mat_Accent", new Color(1f, 0.55f, 0.2f)),
                waterPanel = EnsureMaterial("Mat_Panel", new Color(0.06f, 0.2f, 0.28f)),
                clown = EnsureMaterial("Mat_Clown", new Color(1f, 0.5f, 0.12f)),
                turtle = EnsureMaterial("Mat_Turtle", new Color(0.25f, 0.65f, 0.35f)),
                ray = EnsureMaterial("Mat_Ray", new Color(0.4f, 0.5f, 0.65f)),
                scanner = EnsureMaterial("Mat_Scanner", new Color(0.2f, 0.75f, 0.85f)),
                bottle = EnsureMaterial("Mat_Bottle", new Color(0.55f, 0.8f, 0.9f)),
            };
        }

        static Material EnsureMaterial(string name, Color color)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                mat = new Material(shader) { color = color };
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }

        static void CreateLight()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.45f, 0.75f, 0.9f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(65f, -25f, 0f);

            var fill = new GameObject("Fill Light");
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.2f, 0.45f, 0.55f);
            fillLight.intensity = 0.35f;
            fill.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
        }

        sealed class Systems
        {
            public MissionController mission;
            public PlayerModeSelector modeSelector;
            public GameAudioHub audioHub;
        }

        static Systems CreateSystems(SpeciesDefinition[] species, BaselineSurveyData baseline, SiteDefinition[] sites)
        {
            var root = new GameObject("ReefExplorer_Systems");
            var mission = root.AddComponent<MissionController>();
            var so = new SerializedObject(mission);
            so.FindProperty("baselineSurvey").objectReferenceValue = baseline;
            var req = so.FindProperty("requiredSpecies");
            req.arraySize = species.Length;
            for (var i = 0; i < species.Length; i++)
                req.GetArrayElementAtIndex(i).objectReferenceValue = species[i];
            
            var reqSites = so.FindProperty("sites");
            reqSites.arraySize = sites.Length;
            for (var i = 0; i < sites.Length; i++)
                reqSites.GetArrayElementAtIndex(i).objectReferenceValue = sites[i];
                
            so.ApplyModifiedPropertiesWithoutUndo();

            var mode = root.AddComponent<PlayerModeSelector>();
            var audio = root.AddComponent<GameAudioHub>();
            WireAudio(audio);

            if (Object.FindAnyObjectByType<XRInteractionManager>() == null)
            {
                var mgr = new GameObject("XR Interaction Manager");
                mgr.AddComponent<XRInteractionManager>();
            }

            root.AddComponent<XRGrabDiagnosticsHUD>().Visible = false;

            return new Systems { mission = mission, modeSelector = mode, audioHub = audio };
        }

        static void WireAudio(GameAudioHub audio)
        {
            var so = new SerializedObject(audio);
            so.FindProperty("ambienceLoop").objectReferenceValue = ProceduralAudioFactory.Load("ambience_underwater");
            so.FindProperty("stationHumLoop").objectReferenceValue = ProceduralAudioFactory.Load("ambience_station_hum");
            so.FindProperty("scannerStart").objectReferenceValue = ProceduralAudioFactory.Load("sfx_scanner_start");
            so.FindProperty("scannerProgressTone").objectReferenceValue = ProceduralAudioFactory.Load("sfx_scanner_progress");
            so.FindProperty("scannerSuccess").objectReferenceValue = ProceduralAudioFactory.Load("sfx_scanner_success");
            so.FindProperty("invalid").objectReferenceValue = ProceduralAudioFactory.Load("sfx_invalid");
            so.FindProperty("sampleFill").objectReferenceValue = ProceduralAudioFactory.Load("sfx_sample_fill");
            so.FindProperty("objectiveComplete").objectReferenceValue = ProceduralAudioFactory.Load("sfx_objective");
            so.FindProperty("missionSuccess").objectReferenceValue = ProceduralAudioFactory.Load("sfx_mission_success");

            var ambience = new GameObject("AmbienceSource");
            ambience.transform.SetParent(audio.transform);
            var aSrc = ambience.AddComponent<AudioSource>();
            so.FindProperty("ambienceSource").objectReferenceValue = aSrc;

            var hum = new GameObject("StationHum");
            hum.transform.SetParent(audio.transform);
            hum.transform.position = new Vector3(0f, 1.2f, 0f);
            var hSrc = hum.AddComponent<AudioSource>();
            hSrc.spatialBlend = 1f;
            so.FindProperty("stationHumSource").objectReferenceValue = hSrc;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateStation(MaterialBag mats)
        {
            var station = new GameObject("ResearchStation");
            station.transform.position = Vector3.zero;

            CreateCube("Deck", station.transform, new Vector3(0f, 0.05f, 0f), new Vector3(7f, 0.12f, 7f), mats.metal);
            CreateCube("DeckRim", station.transform, new Vector3(0f, 0.12f, 3.4f), new Vector3(7f, 0.08f, 0.2f), mats.accent);
            CreateCube("BackWall", station.transform, new Vector3(0f, 1.6f, -3.2f), new Vector3(7f, 3.2f, 0.25f), mats.metal);
            CreateCube("LeftWall", station.transform, new Vector3(-3.4f, 1.3f, -0.4f), new Vector3(0.25f, 2.6f, 5.8f), mats.metal);
            CreateCube("RightPost", station.transform, new Vector3(3.2f, 1.2f, -2.6f), new Vector3(0.3f, 2.4f, 0.3f), mats.metal);
            CreateCube("RoofBeam", station.transform, new Vector3(0f, 3.1f, -2.4f), new Vector3(6.5f, 0.15f, 1.2f), mats.metal);
            CreateCube("Console", station.transform, new Vector3(0f, 0.95f, -2f), new Vector3(2.6f, 0.18f, 0.9f), mats.accent);
            CreateCube("EquipmentRack", station.transform, new Vector3(-2.2f, 1.1f, -2.4f), new Vector3(0.8f, 1.6f, 0.35f), mats.metal);
            CreateCube("RackShelf", station.transform, new Vector3(-2.2f, 1.4f, -2.15f), new Vector3(0.7f, 0.06f, 0.4f), mats.accent);

            // Thin dark frame behind the world UI (renderer stays on; runtime clarity fix
            // can hide it if it ever occludes). Canvas sits in front — see CreateUi.
            var board = CreateCube("MissionBoard", station.transform, new Vector3(0f, 1.75f, -3.05f), new Vector3(2.8f, 1.7f, 0.04f), mats.waterPanel);
            board.AddComponent<WorldMissionBoard>();

            var holder = CreateCube("SampleAnalyser", station.transform, new Vector3(1.35f, 1.1f, -1.75f), new Vector3(0.35f, 0.28f, 0.35f), mats.accent);
            holder.AddComponent<BottleSocket>();
            var socketInteractor = holder.AddComponent<XRSocketInteractor>();
            socketInteractor.socketActive = true;
            holder.AddComponent<SampleAnalyser>();
            
            var analyserLabel = CreateWorldText(station.transform, "SAMPLE ANALYSER", 0.06f, TextAnchor.LowerCenter);
            analyserLabel.position = new Vector3(1.35f, 1.3f, -1.75f);
            
            // Marker
            var marker = CreateCube("RecommendationMarker", station.transform, new Vector3(0.8f, 1.15f, -1.75f), new Vector3(0.15f, 0.4f, 0.15f), mats.accent);
            var mBody = marker.AddComponent<Rigidbody>();
            ConfigureGrabBody(mBody);
            mBody.isKinematic = true;
            var mGrab = marker.AddComponent<XRGrabInteractable>();
            mGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            marker.AddComponent<RecommendationMarker>();
            marker.AddComponent<ToolRespawn>();

            var stationZone = CreateTrigger("StationZone", station.transform, new Vector3(0f, 1f, 0f), new Vector3(8f, 3f, 8f));
            var zone = stationZone.AddComponent<ZoneTrigger>();
            var zso = new SerializedObject(zone);
            zso.FindProperty("isStation").boolValue = true;
            zso.ApplyModifiedPropertiesWithoutUndo();

            var sign = CreateWorldText(station.transform, "REEF RESEARCH STATION", 0.16f, TextAnchor.MiddleCenter);
            sign.position = new Vector3(0f, 2.85f, -3.05f);

            return station;
        }

        static void CreateSeabed(MaterialBag mats)
        {
            var reefRoot = new GameObject("ReefEnvironment");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Seabed";
            ground.transform.SetParent(reefRoot.transform);
            // Compact playable sea — shorter travel between objectives.
            ground.transform.position = new Vector3(0f, 0f, 14f);
            ground.transform.localScale = new Vector3(5.5f, 1f, 5.5f);
            ground.GetComponent<Renderer>().sharedMaterial = mats.sand;
            var teleportMask = InteractionLayerMask.GetMask("Teleport");
            var teleport = ground.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
            teleport.interactionLayers = teleportMask;

            var stationDeck = GameObject.Find("Deck");
            if (stationDeck != null)
            {
                var stationTeleport = stationDeck.AddComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea>();
                stationTeleport.interactionLayers = teleportMask;
            }

            // Gentle sandy height variation.
            for (var i = 0; i < 14; i++)
            {
                ProceduralReefMeshes.CreateSandMound($"SandMound_{i}", reefRoot.transform,
                    new Vector3(Random.Range(-18f, 18f), 0f, Random.Range(4f, 28f)),
                    Random.Range(1.8f, 3.5f), mats.sand);
            }

            // Path markers station → buoy → reef.
            for (var i = 1; i <= 4; i++)
            {
                CreateCube($"PathMarker_{i}", reefRoot.transform,
                    new Vector3(0f, 0.06f, 2.5f + i * 1.6f),
                    new Vector3(0.55f, 0.04f, 0.3f), mats.accent);
            }

            PopulateReefPatch(reefRoot.transform, new Vector3(-8f, 0f, 12f), mats, coralHeavy: true, plants: true);
            PopulateReefPatch(reefRoot.transform, new Vector3(0f, 0f, 18f), mats, coralHeavy: true, plants: true);
            PopulateReefPatch(reefRoot.transform, new Vector3(8f, 0f, 13f), mats, coralHeavy: true, plants: false);
            PopulateReefPatch(reefRoot.transform, new Vector3(0f, 0f, 8f), mats, coralHeavy: true, plants: true);
            PopulateReefPatch(reefRoot.transform, new Vector3(-4f, 0f, 15f), mats, coralHeavy: true, plants: true);
            PopulateReefPatch(reefRoot.transform, new Vector3(4f, 0f, 10f), mats, coralHeavy: true, plants: true);

            // Ambient fish from Assets/New_fish (not scan targets). Fake stylized fish removed.
            var newFishPaths = new[]
            {
                "Assets/Fish/[FBX] Undualte_Triggerfish/Undualte_Triggerfish.FBX",
                "Assets/Fish/[FBX] Protomelas taeniolatus/Protomelas taeniolatus.FBX",
                "Assets/Fish/Butterfly/Butterfly.FBX",
                "Assets/Fish/Anthias1/Anthias1.FBX",
                "Assets/New_fish/Angelfish.obj",
                "Assets/New_fish/Betta_Fish.obj",
                "Assets/New_fish/Undualte_Triggerfish.FBX",
                "Assets/New_fish/Protomelas Spilonotus.FBX",
                "Assets/New_fish/Aligator Gar.FBX",
            };
            for (var i = 0; i < 8; i++)
            {
                var path = newFishPaths[i % newFishPaths.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                var pos = new Vector3(Random.Range(-8f, 8f), Random.Range(0.8f, 1.8f), Random.Range(6f, 18f));
                var fish = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (fish == null)
                    fish = Object.Instantiate(prefab);
                var packFish = IsSideNoseFish(path);
                fish.name = packFish ? $"PackFish_{i}" : $"ScenicFish_{i}";
                fish.transform.SetParent(reefRoot.transform);
                fish.transform.position = pos;
                foreach (var col in fish.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);

                // Fit to ~0.4m max axis so huge OBJ/FBX meshes look like reef fish.
                fish.transform.localScale = Vector3.one;
                var rends = fish.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    for (var r = 1; r < rends.Length; r++)
                        b.Encapsulate(rends[r].bounds);
                    var cur = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (cur > 0.01f)
                        fish.transform.localScale = Vector3.one * Mathf.Clamp(0.4f / cur, 0.01f, 8f);
                }

                fish.transform.rotation = packFish
                    ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                    : Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
                var wander = fish.AddComponent<AnimalWander>();
                var wso = new SerializedObject(wander);
                wso.FindProperty("center").vector3Value = pos;
                wso.FindProperty("extents").vector3Value = new Vector3(2.5f, 0.5f, 2.5f);
                wso.FindProperty("speed").floatValue = 0.4f;
                wso.FindProperty("meshEulerOffset").vector3Value = packFish
                    ? PackFishOffset(path)
                    : new Vector3(-90f, 0f, 0f);
                wso.ApplyModifiedPropertiesWithoutUndo();
            }

            CreateBoundaryWall("Bound_North", new Vector3(0f, 3f, 22f), new Vector3(30f, 8f, 1f));
            CreateBoundaryWall("Bound_South", new Vector3(0f, 3f, -4f), new Vector3(30f, 8f, 1f));
            CreateBoundaryWall("Bound_East", new Vector3(13f, 3f, 10f), new Vector3(1f, 8f, 30f));
            CreateBoundaryWall("Bound_West", new Vector3(-13f, 3f, 10f), new Vector3(1f, 8f, 30f));
        }

        static bool IsSideNoseFish(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            return path.IndexOf("[FBX]", System.StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/Butterfly/", System.StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/Anthias1/", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Vector3 PackFishOffset(string path)
        {
            if (path.IndexOf("Butterfly", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector3(-90f, 90f, 0f);
            if (path.IndexOf("Anthias", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector3(0f, 180f, 0f);
            if (path.IndexOf("Protomelas", System.StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("Taeniolatus", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector3(90f, 0f, 0f);
            return new Vector3(0f, -90f, 0f);
        }

        static void PopulateReefPatch(Transform parent, Vector3 center, MaterialBag mats, bool coralHeavy, bool plants)
        {
            for (var i = 0; i < 8; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var d = Random.Range(0.8f, 3.2f);
                ProceduralReefMeshes.CreateRock($"Rock_{center.x:0}_{i}", parent,
                    center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d),
                    Random.Range(0.6f, 1.3f), mats.rock);
            }

            var coralCount = coralHeavy ? 10 : 5;
            for (var i = 0; i < coralCount; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var d = Random.Range(0.6f, 3f);
                var p = center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                var kind = i % 3;
                if (kind == 0)
                    ProceduralReefMeshes.CreateBranchingCoral($"CoralBranch_{center.x:0}_{i}", parent, p, mats.coral);
                else if (kind == 1)
                    ProceduralReefMeshes.CreateRoundedCoral($"CoralRound_{center.x:0}_{i}", parent, p, mats.accent);
                else
                    ProceduralReefMeshes.CreateFanCoral($"CoralFan_{center.x:0}_{i}", parent, p, mats.clown);
            }

            if (!plants)
                return;

            for (var i = 0; i < 12; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var d = Random.Range(0.6f, 3.8f);
                ProceduralReefMeshes.CreateSeaPlant($"Plant_{center.x:0}_{i}", parent,
                    center + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d), mats.plant);
            }
        }

        static void CreateBoundaryWall(string name, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            Object.DestroyImmediate(wall.GetComponent<Renderer>());
        }

        static void CreateZonesAndAnimals(MaterialBag mats, SpeciesDefinition[] species, SiteDefinition[] sites)
        {
            // Compact reef — short walks, zones stay in view of each other.
            CreateSurveyZone("Zone_Coral", "zone_coral", "CORAL GARDEN", new Vector3(-8f, 0f, 12f), mats, species[0], AnimalKind.Clown, sites[0]);
            CreateSurveyZone("Zone_Turtle", "zone_turtle", "SEAGRASS CROSSING", new Vector3(0f, 0f, 18f), mats, species[1], AnimalKind.Turtle, sites[1]);
            CreateSurveyZone("Zone_Ray", "zone_ray", "STARFISH LEDGE", new Vector3(8f, 0f, 13f), mats, species[2], AnimalKind.Ray, sites[2]);
        }

        enum AnimalKind { Clown, Turtle, Ray }

        static void CreateSurveyZone(string name, string zoneId, string label, Vector3 pos, MaterialBag mats, SpeciesDefinition species, AnimalKind kind, SiteDefinition site)
        {
            var zone = new GameObject(name);
            zone.transform.position = pos;
            // Invisible pad (trigger zones still work). Visible coloured tiles looked fake.
            var pad = CreateCube("ZonePad", zone.transform, new Vector3(0f, 0.02f, 0f), new Vector3(5.5f, 0.04f, 5.5f),
                kind == AnimalKind.Clown ? mats.coral : kind == AnimalKind.Turtle ? mats.plant : mats.sand);
            var padR = pad.GetComponent<Renderer>();
            if (padR != null)
                padR.enabled = false;
            var trigger = CreateTrigger("ZoneTrigger", zone.transform, new Vector3(0f, 1.5f, 0f), new Vector3(6.5f, 3f, 6.5f));
            var zt = trigger.AddComponent<ZoneTrigger>();
            var zso = new SerializedObject(zt);
            zso.FindProperty("zoneId").stringValue = zoneId;
            zso.ApplyModifiedPropertiesWithoutUndo();
            var labelTf = CreateWorldText(zone.transform, label, 0.1f, TextAnchor.MiddleCenter);
            labelTf.localPosition = new Vector3(0f, 2.3f, 0f);
            labelTf.rotation = Quaternion.identity;

            GameObject animal;
            if (kind == AnimalKind.Clown)
                animal = ProceduralReefMeshes.CreateStylizedFish($"Animal_{species.DisplayName}", zone.transform, pos + new Vector3(0f, 1f, 0f), mats.clown, 1.1f);
            else if (kind == AnimalKind.Turtle)
                animal = ProceduralReefMeshes.CreateStylizedTurtle($"Animal_{species.DisplayName}", zone.transform, pos + new Vector3(0f, 0.7f, 0f), mats.turtle);
            else
                animal = ProceduralReefMeshes.CreateStylizedRay($"Animal_{species.DisplayName}", zone.transform, pos + new Vector3(0f, 0.55f, 0f), mats.ray);

            // Keep animal as direct zone child for Find() names used by stylized visuals.
            animal.transform.SetParent(zone.transform, true);
            animal.name = $"Animal_{species.DisplayName}";

            var survey = animal.AddComponent<SurveyAnimal>();
            var sso = new SerializedObject(survey);
            sso.FindProperty("animalInstanceId").stringValue = $"{species.SpeciesId}_01";
            sso.FindProperty("species").objectReferenceValue = species;
            sso.FindProperty("zoneId").stringValue = zoneId;
            var renderers = animal.GetComponentsInChildren<Renderer>();
            var rends = sso.FindProperty("tintRenderers");
            rends.arraySize = renderers.Length;
            for (var i = 0; i < renderers.Length; i++)
                rends.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            sso.ApplyModifiedPropertiesWithoutUndo();

            var wander = animal.AddComponent<AnimalWander>();
            var wso = new SerializedObject(wander);
            wso.FindProperty("center").vector3Value = animal.transform.position;
            wso.FindProperty("extents").vector3Value = new Vector3(2.5f, 0.5f, 2.5f);
            wso.FindProperty("speed").floatValue = 0.35f;
            wso.ApplyModifiedPropertiesWithoutUndo();
            
            // Site-specific survey point
            var surveyPoint = CreateCube("CoralSurveyPoint", zone.transform, new Vector3(2f, 0.2f, 0f), new Vector3(0.5f, 0.5f, 0.5f), mats.coral);
            var spCollider = surveyPoint.GetComponent<Collider>();
            spCollider.isTrigger = false;
            var cp = surveyPoint.AddComponent<CoralSurveyPoint>();
            var cpSo = new SerializedObject(cp);
            cpSo.FindProperty("siteId").stringValue = site.SiteId;
            cpSo.FindProperty("tintRenderers").arraySize = 1;
            cpSo.FindProperty("tintRenderers").GetArrayElementAtIndex(0).objectReferenceValue = surveyPoint.GetComponent<Renderer>();
            cpSo.ApplyModifiedPropertiesWithoutUndo();
            
            // Sample Zone per site
            var sample = CreateTrigger("SampleZone", zone.transform, new Vector3(-2f, 0.6f, -1.5f), new Vector3(2.2f, 1.4f, 2.2f));
            var sz = sample.AddComponent<SampleZone>();
            var szSo = new SerializedObject(sz);
            szSo.FindProperty("siteId").stringValue = site.SiteId;
            szSo.ApplyModifiedPropertiesWithoutUndo();
            var sampleMarker = CreateCube("SampleMarker", sample.transform, Vector3.zero, new Vector3(2f, 0.05f, 2f), mats.accent);
            var smR = sampleMarker.GetComponent<Renderer>();
            if (smR != null) smR.enabled = false;
            var sampleLabel = CreateWorldText(sample.transform, "WATER SAMPLE POINT", 0.07f, TextAnchor.LowerCenter);
            sampleLabel.localPosition = new Vector3(0f, 1.2f, 0f);
            
            // Marker Placement Socket per site
            var holder = CreateCube("MarkerHolder", zone.transform, new Vector3(0f, 0.5f, -2.5f), new Vector3(0.3f, 0.3f, 0.3f), mats.metal);
            var mSocket = holder.AddComponent<XRSocketInteractor>();
            mSocket.socketActive = true;
            var mHolder = holder.AddComponent<MarkerHolder>();
            var mhSo = new SerializedObject(mHolder);
            mhSo.FindProperty("siteId").stringValue = site.SiteId;
            mhSo.FindProperty("socket").objectReferenceValue = mSocket;
            mhSo.ApplyModifiedPropertiesWithoutUndo();
            var holderLabel = CreateWorldText(holder.transform, "RESTORATION MARKER", 0.05f, TextAnchor.LowerCenter);
            holderLabel.localPosition = new Vector3(0f, 0.4f, 0f);
            
            // Rubbish
            for (var i = 0; i < site.InitialRubbishCount; i++)
            {
                var rub = CreateCube($"Rubbish_{i}", zone.transform, new Vector3(Random.Range(-2f, 2f), 0.1f, Random.Range(-2f, 2f)), new Vector3(0.15f, 0.15f, 0.25f), mats.bottle);
                var rBody = rub.AddComponent<Rigidbody>();
                ConfigureGrabBody(rBody);
                rBody.isKinematic = true;
                var rGrab = rub.AddComponent<XRGrabInteractable>();
                rGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                var ri = rub.AddComponent<RubbishItem>();
                var riSo = new SerializedObject(ri);
                riSo.FindProperty("siteId").stringValue = site.SiteId;
                riSo.FindProperty("rubbishId").stringValue = $"rubbish_{site.SiteId}_{i}";
                riSo.ApplyModifiedPropertiesWithoutUndo();
            }
            
            // Hazard
            if (site.HasHazard)
            {
                var haz = CreateCube("Hazard", zone.transform, new Vector3(2.5f, 0.3f, 2.5f), new Vector3(0.6f, 0.8f, 0.6f), mats.metal);
                var hCollider = haz.GetComponent<Collider>();
                hCollider.isTrigger = false;
                var hf = haz.AddComponent<HazardFlag>();
                var hfSo = new SerializedObject(hf);
                hfSo.FindProperty("siteId").stringValue = site.SiteId;
                hfSo.FindProperty("hazardId").stringValue = $"hazard_{site.SiteId}";
                hfSo.FindProperty("hazardType").stringValue = site.HazardType;
                hfSo.FindProperty("tintRenderers").arraySize = 1;
                hfSo.FindProperty("tintRenderers").GetArrayElementAtIndex(0).objectReferenceValue = haz.GetComponent<Renderer>();
                hfSo.ApplyModifiedPropertiesWithoutUndo();
                var hazLabel = CreateWorldText(haz.transform, site.HazardType.ToUpper(), 0.05f, TextAnchor.LowerCenter);
                hazLabel.localPosition = new Vector3(0f, 0.6f, 0f);
            }
        }

        static void CreateTools(MaterialBag mats, GameObject station)
        {
            var scanner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scanner.name = "Scanner";
            scanner.transform.position = new Vector3(0.05f, 1.1f, -1.7f);
            scanner.transform.localScale = new Vector3(0.18f, 0.18f, 0.45f);
            scanner.GetComponent<Renderer>().sharedMaterial = mats.scanner;
            var sBody = scanner.AddComponent<Rigidbody>();
            ConfigureGrabBody(sBody);
            sBody.isKinematic = true; // rest on table until grabbed
            var sGrab = scanner.AddComponent<XRGrabInteractable>();
            sGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            var scan = scanner.AddComponent<ScannerTool>();
            var beamGo = new GameObject("Beam");
            beamGo.transform.SetParent(scanner.transform);
            beamGo.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            var lr = beamGo.AddComponent<LineRenderer>();
            lr.enabled = false;
            lr.widthMultiplier = 0.01f;
            lr.material = mats.scanner;
            var sso = new SerializedObject(scan);
            sso.FindProperty("rayOrigin").objectReferenceValue = beamGo.transform;
            sso.FindProperty("beam").objectReferenceValue = lr;
            sso.ApplyModifiedPropertiesWithoutUndo();
            scanner.AddComponent<ToolRespawn>();

            var bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottle.name = "SampleBottle";
            bottle.transform.position = new Vector3(-0.35f, 1.14f, -1.7f);
            bottle.transform.localScale = new Vector3(0.12f, 0.14f, 0.12f);
            bottle.GetComponent<Renderer>().sharedMaterial = mats.bottle;
            var bBody = bottle.AddComponent<Rigidbody>();
            ConfigureGrabBody(bBody);
            bBody.isKinematic = true;
            var bGrab = bottle.AddComponent<XRGrabInteractable>();
            bGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            var sample = bottle.AddComponent<SampleBottle>();
            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Liquid";
            liquid.transform.SetParent(bottle.transform);
            liquid.transform.localPosition = Vector3.zero;
            liquid.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
            Object.DestroyImmediate(liquid.GetComponent<Collider>());
            liquid.GetComponent<Renderer>().sharedMaterial = mats.bottle;
            var bso = new SerializedObject(sample);
            bso.FindProperty("liquidRenderer").objectReferenceValue = liquid.GetComponent<Renderer>();
            bso.ApplyModifiedPropertiesWithoutUndo();
            bottle.AddComponent<ToolRespawn>();
        }

        static void CreateMonitoringBuoy(MaterialBag mats)
        {
            var root = new GameObject("MonitoringBuoy");
            root.transform.position = new Vector3(0f, 0f, 8f);

            CreateCube("BuoyBase", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(1.1f, 0.25f, 1.1f), mats.metal);
            CreateCube("BuoyPole", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.16f, 2.2f, 0.16f), mats.metal);
            var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.name = "BuoyHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            head.transform.localScale = new Vector3(0.95f, 0.35f, 0.95f);
            head.GetComponent<Renderer>().sharedMaterial = mats.accent;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "BuoyLamp";
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 2.95f, 0f);
            lamp.transform.localScale = Vector3.one * 0.35f;
            lamp.GetComponent<Renderer>().sharedMaterial = mats.accent;
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            CreateCube("Antenna", root.transform, new Vector3(0.25f, 3.25f, 0f), new Vector3(0.05f, 0.55f, 0.05f), mats.metal);

            var lightGo = new GameObject("StatusLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.9f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.55f, 0.1f);

            var signalGo = new GameObject("SignalSource");
            signalGo.transform.SetParent(root.transform, false);
            signalGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            var signal = signalGo.AddComponent<AudioSource>();
            signal.spatialBlend = 1f;
            signal.loop = true;
            signal.volume = 0.3f;
            signal.minDistance = 2f;
            signal.maxDistance = 28f;
            signal.clip = ProceduralAudioFactory.Load("sfx_buoy_signal");

            var buoy = root.AddComponent<MonitoringBuoy>();
            var bso = new SerializedObject(buoy);
            bso.FindProperty("statusRenderers").arraySize = 2;
            bso.FindProperty("statusRenderers").GetArrayElementAtIndex(0).objectReferenceValue = head.GetComponent<Renderer>();
            bso.FindProperty("statusRenderers").GetArrayElementAtIndex(1).objectReferenceValue = lamp.GetComponent<Renderer>();
            bso.FindProperty("statusLight").objectReferenceValue = light;
            bso.FindProperty("signalSource").objectReferenceValue = signal;
            bso.ApplyModifiedPropertiesWithoutUndo();

            var socketGo = CreateCube("BuoyPowerSocket", root.transform, new Vector3(0.55f, 1.6f, 0f), new Vector3(0.35f, 0.35f, 0.35f), mats.metal);
            var socketInteractor = socketGo.AddComponent<XRSocketInteractor>();
            socketInteractor.socketActive = true;
            var powerSocket = socketGo.AddComponent<BuoyPowerSocket>();
            var pso = new SerializedObject(powerSocket);
            pso.FindProperty("socket").objectReferenceValue = socketInteractor;
            pso.FindProperty("snapPoint").objectReferenceValue = socketGo.transform;
            pso.FindProperty("buoy").objectReferenceValue = buoy;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var cell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cell.name = "PowerCell";
            cell.transform.position = new Vector3(1.35f, 0.35f, 7.4f);
            cell.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            cell.GetComponent<Renderer>().sharedMaterial = mats.scanner;
            var cellBody = cell.AddComponent<Rigidbody>();
            ConfigureGrabBody(cellBody);
            cellBody.isKinematic = true;
            var cellGrab = cell.AddComponent<XRGrabInteractable>();
            cellGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            cell.AddComponent<PowerCell>();
            cell.AddComponent<ToolRespawn>();

            var buoyLabel = CreateWorldText(root.transform, "REEF BUOY SEVEN\nInsert power cell", 0.08f, TextAnchor.MiddleCenter);
            buoyLabel.localPosition = new Vector3(0f, 3.55f, 0f);
            buoyLabel.rotation = Quaternion.identity;
        }

        static void CreateTutorialProps(MaterialBag mats)
        {
            var buoy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            buoy.name = "PracticeBuoy";
            // In front of the station so the player facing the board can reach it.
            buoy.transform.position = new Vector3(0.55f, 1.08f, -1.7f);
            buoy.transform.localScale = Vector3.one * 0.28f;
            buoy.GetComponent<Renderer>().sharedMaterial = mats.accent;
            var body = buoy.AddComponent<Rigidbody>();
            ConfigureGrabBody(body);
            body.isKinematic = true;
            var grab = buoy.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            buoy.AddComponent<PracticeActivateTool>();
            buoy.AddComponent<ToolRespawn>();

            var moveGate = CreateTrigger("TutorialMoveGate", null, new Vector3(0f, 1f, 3.5f), new Vector3(4f, 2.5f, 1f));
            var tt = moveGate.AddComponent<TutorialTrigger>();
            var so = new SerializedObject(tt);
            so.FindProperty("stepId").stringValue = "move";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreatePlayers(PlayerModeSelector modeSelector)
        {
            var xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrOriginPrefabPath);
            GameObject xr = null;
            if (xrPrefab != null)
            {
                xr = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
                xr.name = "XR Origin (XR Rig)";
                xr.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0.5f), Quaternion.Euler(0f, 180f, 0f));
                xr.SetActive(false);
            }
            else
            {
                Debug.LogError("[ReefExplorer] XR Origin prefab missing. Import Starter Assets sample.");
            }

            var desktop = new GameObject("DesktopPlayer");
            desktop.tag = "Player";
            // Face the research station / mission board (-Z).
            desktop.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0.5f), Quaternion.Euler(0f, 180f, 0f));
            var cc = desktop.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.center = new Vector3(0f, 0.8f, 0f);
            cc.radius = 0.25f;
            var camGo = new GameObject("DesktopCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(desktop.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            var desktopCtrl = desktop.AddComponent<DesktopPlayerController>();
            var dso = new SerializedObject(desktopCtrl);
            dso.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            dso.ApplyModifiedPropertiesWithoutUndo();
            // Stay active so mode-select UI is visible immediately.
            desktop.SetActive(true);

            var mso = new SerializedObject(modeSelector);
            mso.FindProperty("xrOriginRoot").objectReferenceValue = xr;
            mso.FindProperty("desktopPlayerRoot").objectReferenceValue = desktop;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateUi(MissionController mission, PlayerModeSelector modeSelector, GameAudioHub audioHub)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("MissionCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1100f, 720f);
            // Fixed mount on the back wall (do NOT billboard — that clipped into MissionBoard).
            canvasGo.transform.position = new Vector3(0f, 1.75f, -2.88f);
            canvasGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0022f;

            var briefing = CreateUiPanel(canvasGo.transform, "BriefingPanel", new Vector2(1040f, 680f), new Color(0.03f, 0.12f, 0.18f, 0.96f));
            var title = CreateUiText(briefing.transform, "Title", "Reef Rescue — The Silent Signal", 36, TextAnchor.UpperCenter, new Vector2(0f, 290f), new Vector2(960f, 50f));
            var body = CreateUiText(briefing.transform, "Body",
                "Welcome, diver. Restore the buoy, survey wildlife, return a water sample.",
                22, TextAnchor.UpperLeft, new Vector2(0f, 140f), new Vector2(960f, 220f));

            var submitBtn = CreateUiButton(briefing.transform, "Btn_Submit", "Submit Log", new Vector2(320f, -200f));
            submitBtn.gameObject.SetActive(false);

            var resultsPanel = CreateUiPanel(canvasGo.transform, "ResultsPanel", new Vector2(1040f, 680f), new Color(0.03f, 0.14f, 0.16f, 0.97f));
            resultsPanel.SetActive(false);
            var results = CreateUiText(resultsPanel.transform, "Results", "", 20, TextAnchor.UpperLeft, new Vector2(0f, 40f), new Vector2(960f, 560f));

            var compPanel = CreateUiPanel(canvasGo.transform, "ComparisonPanel", new Vector2(1040f, 680f), new Color(0.04f, 0.15f, 0.12f, 0.97f));
            compPanel.SetActive(false);
            var compText = CreateUiText(compPanel.transform, "ComparisonText", "", 18, TextAnchor.UpperLeft, new Vector2(0f, 100f), new Vector2(960f, 440f));
            
            var btnRecCoral = CreateUiButton(compPanel.transform, "Btn_RecCoral", "Recommend Coral Garden", new Vector2(-300f, -260f));
            var btnRecSea = CreateUiButton(compPanel.transform, "Btn_RecSeagrass", "Recommend Seagrass", new Vector2(0f, -260f));
            var btnRecSand = CreateUiButton(compPanel.transform, "Btn_RecSand", "Recommend Sand", new Vector2(300f, -260f));

            var compUi = canvasGo.AddComponent<ComparisonBoardUI>();
            var cso = new SerializedObject(compUi);
            cso.FindProperty("panel").objectReferenceValue = compPanel;
            cso.FindProperty("comparisonText").objectReferenceValue = compText;
            cso.FindProperty("recommendCoralButton").objectReferenceValue = btnRecCoral;
            cso.FindProperty("recommendSeagrassButton").objectReferenceValue = btnRecSea;
            cso.FindProperty("recommendSandButton").objectReferenceValue = btnRecSand;
            cso.ApplyModifiedPropertiesWithoutUndo();

            var board = Object.FindAnyObjectByType<WorldMissionBoard>();
            if (board != null)
            {
                var bso = new SerializedObject(board);
                bso.FindProperty("titleText").objectReferenceValue = title;
                bso.FindProperty("bodyText").objectReferenceValue = body;
                bso.FindProperty("resultsText").objectReferenceValue = results;
                bso.FindProperty("startButton").objectReferenceValue = null;
                bso.FindProperty("xrButton").objectReferenceValue = null;
                bso.FindProperty("desktopButton").objectReferenceValue = null;
                bso.FindProperty("submitButton").objectReferenceValue = submitBtn;
                bso.FindProperty("creditsButton").objectReferenceValue = null;
                bso.FindProperty("restartButton").objectReferenceValue = null;
                bso.FindProperty("quitButton").objectReferenceValue = null;
                bso.FindProperty("modeSelector").objectReferenceValue = modeSelector;
                bso.FindProperty("briefingPanel").objectReferenceValue = briefing;
                bso.FindProperty("resultsPanel").objectReferenceValue = resultsPanel;
                bso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Screen-space HUD only (objectives / progress) — not on the world board.
            var hudCanvas = new GameObject("MissionHudCanvas");
            var hCanvas = hudCanvas.AddComponent<Canvas>();
            hCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hCanvas.sortingOrder = 200;
            hudCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudCanvas.AddComponent<GraphicRaycaster>();
            var hudPanel = CreateUiPanel(hudCanvas.transform, "HudPanel", new Vector2(880f, 96f), new Color(0.02f, 0.08f, 0.12f, 0.88f));
            var hudRt = hudPanel.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0.5f, 1f);
            hudRt.anchorMax = new Vector2(0.5f, 1f);
            hudRt.pivot = new Vector2(0.5f, 1f);
            hudRt.anchoredPosition = new Vector2(0f, -10f);
            var objective = CreateUiText(hudPanel.transform, "Objective", "", 18, TextAnchor.UpperLeft, new Vector2(0f, 28f), new Vector2(840f, 28f));
            var progress = CreateUiText(hudPanel.transform, "Progress", "", 15, TextAnchor.UpperLeft, new Vector2(0f, 2f), new Vector2(840f, 22f));
            var controls = CreateUiText(hudPanel.transform, "Controls", "", 14, TextAnchor.UpperLeft, new Vector2(0f, -20f), new Vector2(840f, 20f));
            var feedback = CreateUiText(hudPanel.transform, "Feedback", "", 14, TextAnchor.UpperLeft, new Vector2(0f, -40f), new Vector2(840f, 20f));

            var hudGo = new GameObject("MissionHud");
            hudGo.transform.SetParent(hudCanvas.transform);
            var hud = hudGo.AddComponent<MissionHud>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("objectiveText").objectReferenceValue = objective;
            hso.FindProperty("feedbackText").objectReferenceValue = feedback;
            hso.FindProperty("progressText").objectReferenceValue = progress;
            hso.FindProperty("controlsText").objectReferenceValue = controls;
            hso.FindProperty("hudRoot").objectReferenceValue = hudPanel;
            hso.ApplyModifiedPropertiesWithoutUndo();
            hudPanel.SetActive(false);

            // Screen-space pause menu
            var pauseCanvas = new GameObject("PauseCanvas");
            var pCanvas = pauseCanvas.AddComponent<Canvas>();
            pCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.AddComponent<CanvasScaler>();
            pauseCanvas.AddComponent<GraphicRaycaster>();
            var pausePanel = CreateUiPanel(pauseCanvas.transform, "PausePanel", new Vector2(420f, 380f), new Color(0.05f, 0.1f, 0.14f, 0.95f));
            CreateUiText(pausePanel.transform, "PauseTitle", "Paused / Settings", 34, TextAnchor.UpperCenter, new Vector2(0f, 150f), new Vector2(360f, 50f));
            var resume = CreateUiButton(pausePanel.transform, "Resume", "Resume", new Vector2(0f, 80f));
            var restart = CreateUiButton(pausePanel.transform, "Restart", "Restart", new Vector2(0f, 10f));
            var quit = CreateUiButton(pausePanel.transform, "Quit", "Quit", new Vector2(0f, -60f));
            CreateUiText(pausePanel.transform, "AmbLabel", "Ambience", 18, TextAnchor.MiddleLeft, new Vector2(-80f, -120f), new Vector2(140f, 28f));
            CreateUiText(pausePanel.transform, "FxLabel", "Effects", 18, TextAnchor.MiddleLeft, new Vector2(-80f, -160f), new Vector2(140f, 28f));
            var ambSlider = CreateUiSlider(pausePanel.transform, "AmbienceSlider", new Vector2(70f, -120f));
            var fxSlider = CreateUiSlider(pausePanel.transform, "EffectsSlider", new Vector2(70f, -160f));
            var muteToggle = CreateUiToggle(pausePanel.transform, "MuteToggle", "Mute", new Vector2(0f, -200f));
            var pause = pauseCanvas.AddComponent<PauseMenuController>();
            var pso = new SerializedObject(pause);
            pso.FindProperty("panel").objectReferenceValue = pausePanel;
            pso.FindProperty("resumeButton").objectReferenceValue = resume;
            pso.FindProperty("restartButton").objectReferenceValue = restart;
            pso.FindProperty("quitButton").objectReferenceValue = quit;
            pso.FindProperty("ambienceSlider").objectReferenceValue = ambSlider;
            pso.FindProperty("effectsSlider").objectReferenceValue = fxSlider;
            pso.FindProperty("muteToggle").objectReferenceValue = muteToggle;
            pso.FindProperty("audioHub").objectReferenceValue = audioHub;
            pso.ApplyModifiedPropertiesWithoutUndo();
            pausePanel.SetActive(false);
        }

        static Toggle CreateUiToggle(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(180f, 28f);
            rt.anchoredPosition = pos;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.2f, 0.25f, 1f);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(go.transform, false);
            var checkRt = (RectTransform)checkGo.transform;
            checkRt.anchorMin = new Vector2(0f, 0.5f);
            checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.pivot = new Vector2(0f, 0.5f);
            checkRt.sizeDelta = new Vector2(22f, 22f);
            checkRt.anchoredPosition = new Vector2(4f, 0f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.2f, 0.85f, 0.55f, 1f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            toggle.isOn = false;

            CreateUiText(go.transform, "Label", label, 18, TextAnchor.MiddleLeft, new Vector2(40f, 0f), new Vector2(120f, 28f));
            return toggle;
        }

        static Slider CreateUiSlider(Transform parent, string name, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(180f, 24f);
            rt.anchoredPosition = pos;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.2f, 0.25f, 1f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.targetGraphic = bg;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = (RectTransform)fillArea.transform;
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6f, 6f);
            fillAreaRt.offsetMax = new Vector2(-6f, -6f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.75f, 0.7f, 1f);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            slider.fillRect = fillRt;
            return slider;
        }

        static void CreateParticles()
        {
            var go = new GameObject("UnderwaterParticles");
            go.transform.position = new Vector3(0f, 2f, 14f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor = new Color(0.45f, 0.85f, 1f, 0.45f);
            main.maxParticles = 180;
            main.gravityModifier = -0.02f;
            var emission = ps.emission;
            emission.rateOverTime = 14f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 5f, 36f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.15f);

            // Avoid pink/magenta missing-material particles in URP.
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                Shader.Find("Sprites/Default");
            if (renderer != null && shader != null)
            {
                var mat = new Material(shader);
                var blue = new Color(0.3f, 0.7f, 1f, 0.6f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", blue);
                if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", blue);
                mat.color = blue;
                var matPath = "Assets/ReefExplorer/Materials/Mat_WaterParticles.mat";
                AssetDatabase.CreateAsset(mat, matPath);
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }

            go.AddComponent<ReefExplorer.Environment.UnderwaterParticlesFix>();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // Input System mouse/keyboard clicks for screen UI (XR module enabled later if needed).
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        static void ConfigureGrabBody(Rigidbody body)
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.linearDamping = 0.4f;
            body.angularDamping = 0.8f;
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null)
                go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            if (parent == null)
                go.transform.position = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject CreateTrigger(string name, Transform parent, Vector3 localPos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null)
                go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            if (parent == null)
                go.transform.position = localPos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Renderer>());
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            return go;
        }

        static Transform CreateWorldText(Transform parent, string content, float size, TextAnchor anchor)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            var text = go.AddComponent<TextMesh>();
            text.text = content;
            text.characterSize = size;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            go.AddComponent<FaceCameraLabel>();
            return go.transform;
        }

        static GameObject CreateUiPanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        static Text CreateUiText(Transform parent, string name, string value, int fontSize, TextAnchor anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = text.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return text;
        }

        static Button CreateUiButton(Transform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.45f, 0.55f, 1f);
            var button = go.AddComponent<Button>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 48f);
            rt.anchoredPosition = pos;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 22;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            return button;
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == path)
                    return;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(path, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        sealed class MaterialBag
        {
            public Material sand, coral, rock, plant, metal, accent, waterPanel, clown, turtle, ray, scanner, bottle;
        }
    }
}
