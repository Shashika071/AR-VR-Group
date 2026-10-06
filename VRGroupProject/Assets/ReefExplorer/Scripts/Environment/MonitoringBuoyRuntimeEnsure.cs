using ReefExplorer.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReefExplorer.Environment
{
    /// <summary>
    /// If the scene was built before the buoy feature, spawn buoy + power cell on Play.
    /// </summary>
    public sealed class MonitoringBuoyRuntimeEnsure : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (GameObject.Find("ResearchStation") == null)
                return;
            if (GameObject.Find("MonitoringBuoy") != null)
                return;
            if (FindAnyObjectByType<MonitoringBuoyRuntimeEnsure>() != null)
                return;

            var host = new GameObject("MonitoringBuoyRuntimeEnsure");
            host.AddComponent<MonitoringBuoyRuntimeEnsure>();
        }

        void Start() => Spawn();

        void Spawn()
        {
            if (GameObject.Find("MonitoringBuoy") != null)
                return;
            // Rebuilt polished scenes already include the buoy from the scene builder.
            if (GameObject.Find("ReefVisuals_v2") != null)
                return;

            var root = new GameObject("MonitoringBuoy");
            root.transform.position = new Vector3(0f, 0f, 8f);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pole.name = "BuoyPole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            pole.transform.localScale = new Vector3(0.18f, 2.2f, 0.18f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "BuoyHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            head.transform.localScale = new Vector3(0.85f, 0.55f, 0.85f);
            Tint(head, new Color(1f, 0.55f, 0.1f));

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = "BuoyLamp";
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            lamp.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            Tint(lamp, new Color(1f, 0.55f, 0.1f));

            var lightGo = new GameObject("StatusLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.9f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.color = new Color(1f, 0.55f, 0.1f);

            var signalGo = new GameObject("SignalSource");
            signalGo.transform.SetParent(root.transform, false);
            var signal = signalGo.AddComponent<AudioSource>();
            signal.spatialBlend = 1f;
            signal.loop = true;
            signal.volume = 0.3f;
#if UNITY_EDITOR
            signal.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ReefExplorer/Audio/sfx_buoy_signal.wav");
#endif

            var buoy = root.AddComponent<MonitoringBuoy>();
            var fieldR = typeof(MonitoringBuoy).GetField("statusRenderers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldR?.SetValue(buoy, new Renderer[]
            {
                head.GetComponent<Renderer>(),
                lamp.GetComponent<Renderer>()
            });
            var fieldL = typeof(MonitoringBuoy).GetField("statusLight",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldL?.SetValue(buoy, light);
            var fieldS = typeof(MonitoringBuoy).GetField("signalSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldS?.SetValue(buoy, signal);

            var socketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socketGo.name = "BuoyPowerSocket";
            socketGo.transform.SetParent(root.transform, false);
            socketGo.transform.localPosition = new Vector3(0.55f, 1.6f, 0f);
            socketGo.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var socketInteractor = socketGo.AddComponent<XRSocketInteractor>();
            socketInteractor.socketActive = true;
            var powerSocket = socketGo.AddComponent<BuoyPowerSocket>();
            var fSock = typeof(BuoyPowerSocket).GetField("socket",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fSnap = typeof(BuoyPowerSocket).GetField("snapPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fBuoy = typeof(BuoyPowerSocket).GetField("buoy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fSock?.SetValue(powerSocket, socketInteractor);
            fSnap?.SetValue(powerSocket, socketGo.transform);
            fBuoy?.SetValue(powerSocket, buoy);

            var cell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cell.name = "PowerCell";
            cell.transform.position = new Vector3(1.4f, 0.35f, 13.2f);
            cell.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
            Tint(cell, new Color(0.3f, 0.75f, 1f));
            var rb = cell.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var grab = cell.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            cell.AddComponent<PowerCell>();
            cell.AddComponent<ToolRespawn>();

            for (var i = 1; i <= 3; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"BuoyPathMarker_{i}";
                marker.transform.position = new Vector3(0f, 0.08f, 3f + i * 2.5f);
                marker.transform.localScale = new Vector3(0.7f, 0.05f, 0.35f);
                Tint(marker, new Color(1f, 0.55f, 0.1f));
                Destroy(marker.GetComponent<Collider>());
            }

            Debug.Log("[ReefRescue] Spawned MonitoringBuoy + PowerCell (runtime ensure).");
        }

        static void Tint(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            r.sharedMaterial = mat;
        }
    }
}
