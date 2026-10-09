using ReefExplorer.Audio;
using ReefExplorer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReefExplorer.Interaction
{
    [RequireComponent(typeof(Collider))]
    public sealed class SampleZone : MonoBehaviour
    {
        const float FillSeconds = 2.4f;

        [SerializeField] string siteId = "site_coral";

        public string SiteId => siteId;
        [SerializeField] Key desktopFillKey = Key.E;

        SampleBottle bottleInZone;
        SampleBottle fillingBottle;
        float fill;
        bool filling;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var bottle = other.GetComponentInParent<SampleBottle>();
            if (bottle != null)
                bottleInZone = bottle;
        }

        void OnTriggerExit(Collider other)
        {
            var bottle = other.GetComponentInParent<SampleBottle>();
            if (bottle != null && bottle == bottleInZone)
                bottleInZone = null;
        }

        void Update()
        {
            if (filling)
            {
                if (fillingBottle == null || Vector3.Distance(fillingBottle.transform.position, transform.position) > 3.5f)
                {
                    CancelFill();
                    return;
                }

                fill += Time.deltaTime / FillSeconds;
                SampleLoadBar.Show(Mathf.Clamp01(fill));
                if (fill < 1f)
                    return;

                CompleteFill();
                return;
            }

            if (bottleInZone == null || MissionController.Instance == null)
                return;
            var pressed = Keyboard.current != null && Keyboard.current[desktopFillKey].wasPressedThisFrame;
            if (pressed)
                BeginSample(bottleInZone);
        }

        public void BeginSample(SampleBottle bottle)
        {
            if (bottle == null || filling)
                return;

            fillingBottle = bottle;
            bottleInZone = bottle;
            fill = 0f;
            filling = true;
            SampleLoadBar.Show(0f);
            MissionEvents.RaiseFeedback("Taking sample.");
        }

        public bool TryFill()
        {
            if (bottleInZone != null)
                BeginSample(bottleInZone);
            return filling;
        }

        void CancelFill()
        {
            filling = false;
            fill = 0f;
            fillingBottle = null;
            SampleLoadBar.Hide();
        }

        void CompleteFill()
        {
            filling = false;
            SampleLoadBar.Hide();
            var bottle = fillingBottle;
            fillingBottle = null;

            var ok = MissionController.Instance != null &&
                     MissionController.Instance.TryCollectSample(siteId, true, true);
            if (!ok)
            {
                GameAudio.PlayInvalid(transform.position);
                return;
            }

            bottle?.MarkFilled();
            PlaySuu(transform.position);
        }

        static void PlaySuu(Vector3 position)
        {
            var go = new GameObject("SampleSuu");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0.4f;
            source.clip = SuuClip();
            source.Play();
            Destroy(go, source.clip.length + 0.15f);
        }

        static AudioClip suuClip;

        static AudioClip SuuClip()
        {
            if (suuClip != null)
                return suuClip;

            const int rate = 22050;
            const float seconds = 0.75f;
            var count = (int)(rate * seconds);
            var clip = AudioClip.Create("SampleSuu", count, 1, rate, false);
            var data = new float[count];
            var rng = new System.Random(11);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)rate;
                var env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));
                var freq = Mathf.Lerp(980f, 160f, t / seconds);
                var tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                var noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = (tone * 0.3f + noise * 0.5f) * env * 0.55f;
            }

            clip.SetData(data, 0);
            suuClip = clip;
            return clip;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }

    /// <summary>Short bar shown only while a water sample is being drawn.</summary>
    sealed class SampleLoadBar : MonoBehaviour
    {
        static SampleLoadBar instance;
        Image fill;

        public static void Show(float amount)
        {
            Ensure();
            instance.gameObject.SetActive(true);
            instance.fill.fillAmount = Mathf.Clamp01(amount);
        }

        public static void Hide()
        {
            if (instance != null)
                instance.gameObject.SetActive(false);
        }

        static void Ensure()
        {
            if (instance != null)
                return;

            var canvasGo = new GameObject("SampleLoadCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var root = new GameObject("SampleLoadBar");
            root.transform.SetParent(canvasGo.transform, false);
            instance = root.AddComponent<SampleLoadBar>();
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -40f);
            rect.sizeDelta = new Vector2(280f, 22f);

            var back = new GameObject("Track");
            back.transform.SetParent(root.transform, false);
            var backRect = back.AddComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = backRect.offsetMax = Vector2.zero;
            var sprite = WhiteSprite();
            var backImage = back.AddComponent<Image>();
            backImage.sprite = sprite;
            backImage.color = new Color(0.02f, 0.08f, 0.1f, 0.9f);

            var front = new GameObject("Fill");
            front.transform.SetParent(back.transform, false);
            var frontRect = front.AddComponent<RectTransform>();
            frontRect.anchorMin = Vector2.zero;
            frontRect.anchorMax = Vector2.one;
            frontRect.offsetMin = new Vector2(3f, 3f);
            frontRect.offsetMax = new Vector2(-3f, -3f);
            instance.fill = front.AddComponent<Image>();
            instance.fill.sprite = sprite;
            instance.fill.color = new Color(0.15f, 0.75f, 1f, 1f);
            instance.fill.type = Image.Type.Filled;
            instance.fill.fillMethod = Image.FillMethod.Horizontal;
            instance.fill.fillAmount = 0f;
        }

        static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
