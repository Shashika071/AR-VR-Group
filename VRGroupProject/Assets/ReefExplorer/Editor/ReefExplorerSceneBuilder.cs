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
            var species = EnsureSpeciesAndBaseline(out var baseline);
            var mats = EnsureMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.028f;
            RenderSettings.fogColor = new Color(0.04f, 0.26f, 0.36f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.26f, 0.34f);

            CreateLight();
            var systems = CreateSystems(species, baseline);
            var station = CreateStation(mats);
            CreateSeabed(mats);
            CreateZonesAndAnimals(mats, species);
            CreateTools(mats, station);
            CreateTutorialProps(mats);
            CreatePlayers(systems.modeSelector);
            CreateUi(systems.mission, systems.modeSelector, systems.audioHub);
            CreateParticles();

            Directory.CreateDirectory("Assets/ReefExplorer/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
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

        static SpeciesDefinition[] EnsureSpeciesAndBaseline(out BaselineSurveyData baseline)
        {
            var clown = EnsureSpecies("Species_Clownfish", "clownfish", "Clownfish", new Color(1f, 0.55f, 0.1f));
            var turtle = EnsureSpecies("Species_SeaTurtle", "sea_turtle", "Sea Turtle", new Color(0.3f, 0.75f, 0.4f));
            var ray = EnsureSpecies("Species_Ray", "ray", "Ray", new Color(0.45f, 0.55f, 0.7f));

            baseline = AssetDatabase.LoadAssetAtPath<BaselineSurveyData>($"{DataFolder}/BaselineSurvey.asset");
            if (baseline == null)
            {
                baseline = ScriptableObject.CreateInstance<BaselineSurveyData>();
                AssetDatabase.CreateAsset(baseline, $"{DataFolder}/BaselineSurvey.asset");
            }

            var so = new SerializedObject(baseline);
            so.FindProperty("surveyLabel").stringValue = "Previous simulated survey (educational)";
            so.FindProperty("disclaimer").stringValue =
                "Simulated educational data only. Do not treat this as a real reef-health assessment.";
            var entries = so.FindProperty("entries");
            entries.arraySize = 3;
            SetEntry(entries.GetArrayElementAtIndex(0), clown, "zone_coral", 2);
            SetEntry(entries.GetArrayElementAtIndex(1), turtle, "zone_turtle", 1);
            SetEntry(entries.GetArrayElementAtIndex(2), ray, "zone_ray", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(baseline);

            return new[] { clown, turtle, ray };
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
            so.FindProperty("description").stringValue = $"{display} used for the educational reef survey.";
            so.FindProperty("accentColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static MaterialBag EnsureMaterials()
        {
            return new MaterialBag
            {
                sand = EnsureMaterial("Mat_Sand", new Color(0.76f, 0.68f, 0.45f)),
                coral = EnsureMaterial("Mat_Coral", new Color(0.85f, 0.35f, 0.4f)),
                rock = EnsureMaterial("Mat_Rock", new Color(0.35f, 0.4f, 0.42f)),
                plant = EnsureMaterial("Mat_Plant", new Color(0.15f, 0.55f, 0.35f)),
                metal = EnsureMaterial("Mat_Metal", new Color(0.35f, 0.45f, 0.5f)),
                accent = EnsureMaterial("Mat_Accent", new Color(1f, 0.62f, 0.25f)),
                waterPanel = EnsureMaterial("Mat_Panel", new Color(0.08f, 0.22f, 0.3f)),
                clown = EnsureMaterial("Mat_Clown", new Color(1f, 0.55f, 0.15f)),
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
            light.color = new Color(0.55f, 0.8f, 0.95f);
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        sealed class Systems
        {
            public MissionController mission;
            public PlayerModeSelector modeSelector;
            public GameAudioHub audioHub;
        }

        static Systems CreateSystems(SpeciesDefinition[] species, BaselineSurveyData baseline)
        {
            var root = new GameObject("ReefExplorer_Systems");
            var mission = root.AddComponent<MissionController>();
            var so = new SerializedObject(mission);
            so.FindProperty("baselineSurvey").objectReferenceValue = baseline;
            var req = so.FindProperty("requiredSpecies");
            req.arraySize = species.Length;
            for (var i = 0; i < species.Length; i++)
                req.GetArrayElementAtIndex(i).objectReferenceValue = species[i];
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

            CreateCube("Deck", station.transform, new Vector3(0f, 0.05f, 0f), new Vector3(6f, 0.1f, 6f), mats.metal);
            CreateCube("BackWall", station.transform, new Vector3(0f, 1.5f, -2.8f), new Vector3(6f, 3f, 0.2f), mats.metal);
            CreateCube("LeftWall", station.transform, new Vector3(-2.9f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 5.5f), mats.metal);
            CreateCube("Console", station.transform, new Vector3(0f, 0.9f, -1.8f), new Vector3(2.2f, 0.15f, 0.8f), mats.accent);

            var board = CreateCube("MissionBoard", station.transform, new Vector3(0f, 1.7f, -2.6f), new Vector3(2.4f, 1.4f, 0.08f), mats.waterPanel);
            board.AddComponent<WorldMissionBoard>();

            var holder = CreateCube("BottleHolder", station.transform, new Vector3(1.2f, 1.05f, -1.6f), new Vector3(0.25f, 0.25f, 0.25f), mats.accent);
            var socket = holder.AddComponent<BottleSocket>();
            var socketInteractor = holder.AddComponent<XRSocketInteractor>();
            socketInteractor.socketActive = true;

            var stationZone = CreateTrigger("StationZone", station.transform, new Vector3(0f, 1f, 0f), new Vector3(7f, 3f, 7f));
            var zone = stationZone.AddComponent<ZoneTrigger>();
            var zso = new SerializedObject(zone);
            zso.FindProperty("isStation").boolValue = true;
            zso.ApplyModifiedPropertiesWithoutUndo();

            CreateWorldText(station.transform, "REEF RESEARCH STATION", 0.18f, TextAnchor.MiddleCenter)
                .position = new Vector3(0f, 2.6f, -2.7f);

            return station;
        }

        static void CreateSeabed(MaterialBag mats)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Seabed";
            // Large open sea floor — station at origin, reef stretches forward (+Z).
            ground.transform.position = new Vector3(0f, 0f, 28f);
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
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

            // Cluster props into reef patches with open water between (not one pile).
            Vector3[] patches =
            {
                new(0f, 0f, 10f),
                new(-30f, 0f, 24f),
                new(4f, 0f, 52f),
                new(34f, 0f, 30f),
                new(-12f, 0f, 38f),
                new(20f, 0f, 18f),
            };

            var rockId = 0;
            var plantId = 0;
            var coralId = 0;
            foreach (var patch in patches)
            {
                for (var i = 0; i < 5; i++)
                {
                    var a = Random.Range(0f, Mathf.PI * 2f);
                    var d = Random.Range(1.5f, 5f);
                    CreateCube($"Rock_{rockId++}", null,
                        new Vector3(patch.x + Mathf.Cos(a) * d, 0.25f, patch.z + Mathf.Sin(a) * d),
                        new Vector3(Random.Range(0.4f, 1.6f), Random.Range(0.3f, 1.2f), Random.Range(0.4f, 1.6f)),
                        mats.rock);
                }

                for (var i = 0; i < 6; i++)
                {
                    var a = Random.Range(0f, Mathf.PI * 2f);
                    var d = Random.Range(1f, 4.5f);
                    CreateCube($"Plant_{plantId++}", null,
                        new Vector3(patch.x + Mathf.Cos(a) * d, 0.45f, patch.z + Mathf.Sin(a) * d),
                        new Vector3(0.12f, Random.Range(0.5f, 1.3f), 0.12f),
                        mats.plant);
                }

                for (var i = 0; i < 3; i++)
                {
                    var a = Random.Range(0f, Mathf.PI * 2f);
                    var d = Random.Range(0.8f, 3.5f);
                    CreateCube($"Coral_{coralId++}", null,
                        new Vector3(patch.x + Mathf.Cos(a) * d, 0.35f, patch.z + Mathf.Sin(a) * d),
                        new Vector3(Random.Range(0.4f, 1.1f), Random.Range(0.4f, 1.1f), Random.Range(0.4f, 1.1f)),
                        mats.coral);
                }
            }

            // Invisible walls around the larger sea area.
            CreateBoundaryWall("Bound_North", new Vector3(0f, 3f, 72f), new Vector3(100f, 8f, 1f));
            CreateBoundaryWall("Bound_South", new Vector3(0f, 3f, -12f), new Vector3(100f, 8f, 1f));
            CreateBoundaryWall("Bound_East", new Vector3(52f, 3f, 28f), new Vector3(1f, 8f, 90f));
            CreateBoundaryWall("Bound_West", new Vector3(-52f, 3f, 28f), new Vector3(1f, 8f, 90f));
        }

        static void CreateBoundaryWall(string name, Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            Object.DestroyImmediate(wall.GetComponent<Renderer>());
        }

        static void CreateZonesAndAnimals(MaterialBag mats, SpeciesDefinition[] species)
        {
            // Far apart so the player swims / walks through open water between objectives.
            CreateSurveyZone("Zone_Coral", "zone_coral", new Vector3(-30f, 0f, 24f), mats.coral, species[0], mats.clown, PrimitiveType.Capsule, new Vector3(0.25f, 0.2f, 0.25f));
            CreateSurveyZone("Zone_Turtle", "zone_turtle", new Vector3(4f, 0f, 52f), mats.plant, species[1], mats.turtle, PrimitiveType.Cube, new Vector3(0.9f, 0.35f, 0.55f));
            CreateSurveyZone("Zone_Ray", "zone_ray", new Vector3(34f, 0f, 30f), mats.rock, species[2], mats.ray, PrimitiveType.Cube, new Vector3(1.1f, 0.12f, 0.7f));

            var sample = CreateTrigger("SampleZone", null, new Vector3(16f, 0.6f, 40f), new Vector3(2.2f, 1.4f, 2.2f));
            sample.AddComponent<SampleZone>();
            var marker = CreateCube("SampleMarker", sample.transform, Vector3.zero, new Vector3(2f, 0.05f, 2f), mats.accent);
            CreateWorldText(sample.transform, "WATER SAMPLE POINT\nHold bottle + press E / Trigger", 0.08f, TextAnchor.LowerCenter)
                .localPosition = new Vector3(0f, 1.2f, 0f);
        }

        static void CreateSurveyZone(string name, string zoneId, Vector3 pos, Material zoneMat, SpeciesDefinition species, Material animalMat, PrimitiveType shape, Vector3 animalScale)
        {
            var zone = new GameObject(name);
            zone.transform.position = pos;
            var floor = CreateCube("ZonePad", zone.transform, new Vector3(0f, 0.02f, 0f), new Vector3(6f, 0.05f, 6f), zoneMat);
            var trigger = CreateTrigger("ZoneTrigger", zone.transform, new Vector3(0f, 1.5f, 0f), new Vector3(7f, 3f, 7f));
            var zt = trigger.AddComponent<ZoneTrigger>();
            var zso = new SerializedObject(zt);
            zso.FindProperty("zoneId").stringValue = zoneId;
            zso.ApplyModifiedPropertiesWithoutUndo();
            CreateWorldText(zone.transform, zoneId.ToUpperInvariant(), 0.12f, TextAnchor.MiddleCenter).localPosition = new Vector3(0f, 2.4f, 0f);

            var animal = shape == PrimitiveType.Capsule
                ? GameObject.CreatePrimitive(PrimitiveType.Capsule)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            animal.name = $"Animal_{species.DisplayName}";
            animal.transform.SetParent(zone.transform);
            animal.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            animal.transform.localScale = animalScale;
            animal.GetComponent<Renderer>().sharedMaterial = animalMat;
            var survey = animal.AddComponent<SurveyAnimal>();
            var sso = new SerializedObject(survey);
            sso.FindProperty("animalInstanceId").stringValue = $"{species.SpeciesId}_01";
            sso.FindProperty("species").objectReferenceValue = species;
            sso.FindProperty("zoneId").stringValue = zoneId;
            var rends = sso.FindProperty("tintRenderers");
            rends.arraySize = 1;
            rends.GetArrayElementAtIndex(0).objectReferenceValue = animal.GetComponent<Renderer>();
            sso.ApplyModifiedPropertiesWithoutUndo();
            animal.AddComponent<AnimalWander>();
            var wander = new SerializedObject(animal.GetComponent<AnimalWander>());
            wander.FindProperty("center").vector3Value = animal.transform.position;
            wander.FindProperty("extents").vector3Value = new Vector3(4.5f, 0.7f, 4.5f);
            wander.ApplyModifiedPropertiesWithoutUndo();
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
            rt.sizeDelta = new Vector2(1200f, 800f);
            // Face the player at the station (player looks toward -Z).
            canvasGo.transform.SetPositionAndRotation(
                new Vector3(0f, 1.7f, -2.55f),
                Quaternion.Euler(0f, 180f, 0f));
            canvasGo.transform.localScale = Vector3.one * 0.002f;

            var panel = CreateUiPanel(canvasGo.transform, "Panel", new Vector2(1100f, 700f), new Color(0.05f, 0.14f, 0.2f, 0.92f));
            var title = CreateUiText(panel.transform, "Title", "Reef Explorer — The Missing Survey", 40, TextAnchor.UpperCenter, new Vector2(0f, 280f), new Vector2(1000f, 60f));
            var body = CreateUiText(panel.transform, "Body",
                "Choose a control mode, then Start Dive.\nSurvey three zones, scan three animals, fill and return the bottle, submit the log.",
                24, TextAnchor.UpperLeft, new Vector2(0f, 160f), new Vector2(1000f, 140f));
            var objective = CreateUiText(panel.transform, "Objective", "Choose VR or Desktop.", 26, TextAnchor.UpperLeft, new Vector2(0f, 40f), new Vector2(1000f, 60f));
            var progress = CreateUiText(panel.transform, "Progress", "", 22, TextAnchor.UpperLeft, new Vector2(0f, -20f), new Vector2(1000f, 40f));
            var controls = CreateUiText(panel.transform, "Controls", "", 20, TextAnchor.UpperLeft, new Vector2(0f, -70f), new Vector2(1000f, 50f));
            var feedback = CreateUiText(panel.transform, "Feedback", "", 22, TextAnchor.UpperLeft, new Vector2(0f, -120f), new Vector2(1000f, 40f));
            var results = CreateUiText(panel.transform, "Results", "", 20, TextAnchor.UpperLeft, new Vector2(0f, -220f), new Vector2(1000f, 160f));

            var xrBtn = CreateUiButton(panel.transform, "Btn_XR", "VR / Simulator", new Vector2(-360f, -300f));
            var deskBtn = CreateUiButton(panel.transform, "Btn_Desktop", "Desktop", new Vector2(-120f, -300f));
            var startBtn = CreateUiButton(panel.transform, "Btn_Start", "Start Dive", new Vector2(120f, -300f));
            var submitBtn = CreateUiButton(panel.transform, "Btn_Submit", "Submit Log", new Vector2(360f, -300f));
            var creditsBtn = CreateUiButton(panel.transform, "Btn_Credits", "Credits", new Vector2(-360f, -370f));
            var restartBtn = CreateUiButton(panel.transform, "Btn_Restart", "Restart", new Vector2(-120f, -370f));
            var quitBtn = CreateUiButton(panel.transform, "Btn_Quit", "Quit", new Vector2(120f, -370f));

            var board = Object.FindAnyObjectByType<WorldMissionBoard>();
            if (board != null)
            {
                var bso = new SerializedObject(board);
                bso.FindProperty("titleText").objectReferenceValue = title;
                bso.FindProperty("bodyText").objectReferenceValue = body;
                bso.FindProperty("resultsText").objectReferenceValue = results;
                bso.FindProperty("startButton").objectReferenceValue = startBtn;
                bso.FindProperty("xrButton").objectReferenceValue = xrBtn;
                bso.FindProperty("desktopButton").objectReferenceValue = deskBtn;
                bso.FindProperty("submitButton").objectReferenceValue = submitBtn;
                bso.FindProperty("creditsButton").objectReferenceValue = creditsBtn;
                bso.FindProperty("restartButton").objectReferenceValue = restartBtn;
                bso.FindProperty("quitButton").objectReferenceValue = quitBtn;
                bso.FindProperty("modeSelector").objectReferenceValue = modeSelector;
                bso.ApplyModifiedPropertiesWithoutUndo();
            }

            var hudGo = new GameObject("MissionHud");
            hudGo.transform.SetParent(canvasGo.transform);
            var hud = hudGo.AddComponent<MissionHud>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("objectiveText").objectReferenceValue = objective;
            hso.FindProperty("feedbackText").objectReferenceValue = feedback;
            hso.FindProperty("progressText").objectReferenceValue = progress;
            hso.FindProperty("controlsText").objectReferenceValue = controls;
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Screen-space pause menu
            var pauseCanvas = new GameObject("PauseCanvas");
            var pCanvas = pauseCanvas.AddComponent<Canvas>();
            pCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.AddComponent<CanvasScaler>();
            pauseCanvas.AddComponent<GraphicRaycaster>();
            var pausePanel = CreateUiPanel(pauseCanvas.transform, "PausePanel", new Vector2(420f, 320f), new Color(0.05f, 0.1f, 0.14f, 0.95f));
            CreateUiText(pausePanel.transform, "PauseTitle", "Paused", 34, TextAnchor.UpperCenter, new Vector2(0f, 120f), new Vector2(360f, 50f));
            var resume = CreateUiButton(pausePanel.transform, "Resume", "Resume", new Vector2(0f, 40f));
            var restart = CreateUiButton(pausePanel.transform, "Restart", "Restart", new Vector2(0f, -30f));
            var quit = CreateUiButton(pausePanel.transform, "Quit", "Quit", new Vector2(0f, -100f));
            var pause = pauseCanvas.AddComponent<PauseMenuController>();
            var pso = new SerializedObject(pause);
            pso.FindProperty("panel").objectReferenceValue = pausePanel;
            pso.FindProperty("resumeButton").objectReferenceValue = resume;
            pso.FindProperty("restartButton").objectReferenceValue = restart;
            pso.FindProperty("quitButton").objectReferenceValue = quit;
            pso.FindProperty("audioHub").objectReferenceValue = audioHub;
            pso.ApplyModifiedPropertiesWithoutUndo();
            pausePanel.SetActive(false);
        }

        static void CreateParticles()
        {
            var go = new GameObject("UnderwaterParticles");
            go.transform.position = new Vector3(0f, 2f, 30f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 10f;
            main.startSize = 0.04f;
            main.startColor = new Color(0.35f, 0.75f, 1f, 0.55f); // blue water drops
            main.maxParticles = 220;
            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 6f, 70f);

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
