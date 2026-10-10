using System.Collections;
using System.Collections.Generic;
using ReefExplorer.Audio;
using ReefExplorer.Core;
using ReefExplorer.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ReefExplorer.Interaction
{
    /// <summary>
    /// Handheld water tester. Pick it up, then press E to read the samples in the bottle.
    /// </summary>
    public sealed class SampleAnalyser : MonoBehaviour
    {
        const float TestSeconds = 2.1f;

        bool busy;

        void Awake()
        {
            var socket = GetComponent<XRSocketInteractor>();
            if (socket != null)
                socket.enabled = false;

            var holder = GetComponent<BottleSocket>();
            if (holder != null)
                holder.enabled = false;

            transform.SetParent(null, true);

            var body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var grab = GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = gameObject.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
            }

            grab.colliders.Clear();
            var col = GetComponent<Collider>();
            if (col != null)
                grab.colliders.Add(col);
        }

        public bool TryUse()
        {
            if (busy)
                return true;

            var mc = MissionController.Instance;
            if (mc == null)
                return false;

            if (!mc.HasAllWaterSamples())
            {
                MissionEvents.RaiseFeedback("Collect every water sample with the bottle first.");
                return true;
            }

            var pending = new List<string>();
            foreach (var sample in mc.DiveLog.perSiteSamples)
            {
                if (sample != null && sample.collected && !sample.analysed && !string.IsNullOrEmpty(sample.siteId))
                    pending.Add(sample.siteId);
            }

            if (pending.Count == 0)
            {
                var any = false;
                foreach (var sample in mc.DiveLog.perSiteSamples)
                {
                    if (sample != null && sample.collected)
                    {
                        any = true;
                        break;
                    }
                }

                MissionEvents.RaiseFeedback(any
                    ? "These samples are already tested."
                    : "Collect a water sample with the bottle first.");
                return true;
            }

            StartCoroutine(RunTest(pending));
            return true;
        }

        IEnumerator RunTest(List<string> pending)
        {
            busy = true;
            MissionEvents.RaiseFeedback("Testing water samples.");
            var t = 0f;
            while (t < TestSeconds)
            {
                var player = FindAnyObjectByType<ReefExplorer.Input.DesktopPlayerController>();
                if (player == null || player.HeldObject != transform)
                {
                    SampleLoadBar.Hide();
                    busy = false;
                    yield break;
                }

                t += Time.deltaTime;
                var amount = Mathf.Clamp01(t / TestSeconds);
                SampleLoadBar.Show(amount);
                GameAudio.PlayScannerProgress(transform.position, amount);
                yield return null;
            }

            SampleLoadBar.Hide();
            var mc = MissionController.Instance;
            var tested = 0;
            if (mc != null)
            {
                foreach (var siteId in pending)
                {
                    if (mc.TryAnalyseSample(siteId))
                        tested++;
                }
            }

            if (tested > 0)
            {
                GameAudio.PlayAnalyserAccept(transform.position);
                DiveReadout.ShowWaterTest();
            }

            busy = false;
        }
    }
}
