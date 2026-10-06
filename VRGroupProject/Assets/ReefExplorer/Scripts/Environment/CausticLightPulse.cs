using UnityEngine;

namespace ReefExplorer.Environment
{
    /// <summary>
    /// Subtle moving light intensity to suggest caustic patterns on the seabed.
    /// Not a true projected caustic map — safe for desktop + XR.
    /// </summary>
    public sealed class CausticLightPulse : MonoBehaviour
    {
        [SerializeField] float baseIntensity = 0.85f;
        [SerializeField] float amplitude = 0.08f;
        [SerializeField] float speed = 0.7f;

        Light lit;
        float phase;

        void Awake()
        {
            lit = GetComponent<Light>();
            if (lit != null)
                baseIntensity = lit.intensity;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            if (lit == null)
                return;
            if ((Time.frameCount & 1) != 0)
                return;

            phase += Time.deltaTime * 2f * speed;
            var a = Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 1.7f + 1.3f) * 0.4f;
            lit.intensity = baseIntensity + a * amplitude;
        }
    }
}
