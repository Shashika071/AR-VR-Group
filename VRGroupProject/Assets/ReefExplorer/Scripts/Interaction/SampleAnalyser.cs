using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ReefExplorer.Interaction
{
    public sealed class SampleAnalyser : MonoBehaviour
    {
        [SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

        void Awake()
        {
            if (socket == null)
                socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        }

        void OnEnable()
        {
            if (socket != null)
                socket.selectEntered.AddListener(OnBottleInserted);
        }

        void OnDisable()
        {
            if (socket != null)
                socket.selectEntered.RemoveListener(OnBottleInserted);
        }

        void OnBottleInserted(SelectEnterEventArgs args)
        {
            var bottle = args.interactableObject.transform.GetComponent<SampleBottle>();
            if (bottle == null) return;

            var mc = MissionController.Instance;
            if (mc == null) return;

            // Mark the bottle as returned (legacy compat)
            mc.TryAcceptReturnedBottle(bottle.IsFilled);

            // Analyse all unanalysed samples
            var allAnalysed = true;
            foreach (var sample in mc.DiveLog.perSiteSamples)
            {
                if (sample.collected && !sample.analysed)
                {
                    if (mc.TryAnalyseSample(sample.siteId))
                    {
                        GameAudio.PlayAnalyserAccept(transform.position);
                        allAnalysed = false;
                    }
                }
            }
            
            if (allAnalysed)
            {
                MissionEvents.RaiseFeedback("No new samples to analyse.");
            }
        }
    }
}
