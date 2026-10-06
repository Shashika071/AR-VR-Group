using System.Linq;
using ReefExplorer.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

namespace ReefExplorer.EditorTools
{
    public static class VRTestSceneGrabFixer
    {
        const string ScenePath = "Assets/VRTestScene.unity";

        [MenuItem("Reef Explorer/1. Fix VRTestScene Grab Setup")]
        public static void FixGrabSetup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ReefExplorer] Stop Play Mode before fixing VRTestScene.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var fixedCount = 0;

            // Remove duplicate XR Origins — keep the first active one.
            var origins = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include)
                .OrderBy(o => o.transform.GetSiblingIndex())
                .ToArray();

            if (origins.Length > 1)
            {
                for (var i = 1; i < origins.Length; i++)
                {
                    Debug.LogWarning($"[ReefExplorer] Removing duplicate XR Origin: {origins[i].name}");
                    Object.DestroyImmediate(origins[i].gameObject);
                    fixedCount++;
                }
            }

            if (Object.FindAnyObjectByType<XRInteractionManager>() == null)
            {
                var managerGo = new GameObject("XR Interaction Manager");
                managerGo.AddComponent<XRInteractionManager>();
                fixedCount++;
            }

            var cube = GameObject.Find("Cube");
            if (cube != null)
            {
                var grab = cube.GetComponent<XRGrabInteractable>();
                if (grab == null)
                    grab = cube.AddComponent<XRGrabInteractable>();

                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
                grab.interactionLayers = 1;

                var body = cube.GetComponent<Rigidbody>();
                if (body == null)
                    body = cube.AddComponent<Rigidbody>();

                body.useGravity = true;
                body.isKinematic = false;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                body.linearDamping = 0.2f;
                body.angularDamping = 0.5f;

                if (cube.GetComponent<Collider>() == null)
                    cube.AddComponent<BoxCollider>();

                // Place cube at a comfortable grab height in front of the player.
                cube.transform.position = new Vector3(0.6f, 1.1f, 1.1f);
                cube.transform.localScale = Vector3.one * 0.25f;
                fixedCount++;
            }
            else
            {
                Debug.LogError("[ReefExplorer] Cube not found in VRTestScene.");
            }

            if (Object.FindAnyObjectByType<XRGrabDiagnosticsHUD>() == null)
            {
                var hud = new GameObject("XR Grab Diagnostics HUD");
                hud.AddComponent<XRGrabDiagnosticsHUD>();
                fixedCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ReefExplorer] VRTestScene grab fix complete. Changes: {fixedCount}. " +
                      "Play mode tip: hold Space, aim at Cube, press G to grab (not left click).");
        }
    }
}
