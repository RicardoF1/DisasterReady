using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DisasterReady
{
    /// <summary>
    /// Post-procesado y efectos de pantalla: tonemapping cinematográfico, bloom, viñeta,
    /// aberración cromática al recibir golpes, grano durante el sismo, desenfoque en pausa
    /// y resultados, brillo rojo en los bordes y fundidos a negro entre escenas.
    /// </summary>
    public class ScreenEffects : MonoBehaviour
    {
        Volume volume;
        VolumeProfile profile;
        Bloom bloom;
        Vignette vignette;
        ColorAdjustments color;
        ChromaticAberration chroma;
        FilmGrain grain;
        DepthOfField dof;

        Image fade;
        Image edge;
        float fadeTarget;
        float hitPulse;
        float edgeAlpha;
        Color edgeColor = Color.red;
        bool menuScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister()
        {
            SceneBoot.Register(GameConfig.HouseScene, () => Spawn(false));
            SceneBoot.Register(GameConfig.MenuScene, () => Spawn(true));
        }

        static void Spawn(bool menu)
        {
            if (FindAnyObjectByType<ScreenEffects>() != null) return;
            var fx = new GameObject("Efectos de pantalla").AddComponent<ScreenEffects>();
            fx.menuScene = menu;
        }

        void Awake()
        {
            GameEvents.PlayerHit += OnHit;
            GameEvents.FadeOutRequested += OnFadeOut;
        }

        void OnDestroy()
        {
            GameEvents.PlayerHit -= OnHit;
            GameEvents.FadeOutRequested -= OnFadeOut;
            if (profile != null) Destroy(profile);
        }

        void Start()
        {
            BuildVolume();
            BuildOverlay();
            EnableCameraEffects();
        }

        void BuildVolume()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.75f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.65f);

            color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.25f);
            color.contrast.Override(14f);
            color.saturation.Override(14f);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);

            chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(0f);

            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0f);

            dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(menuScene ? 40f : 0.5f);
            dof.gaussianEnd.Override(menuScene ? 160f : 3f);
            dof.gaussianMaxRadius.Override(1.2f);
            dof.active = menuScene;

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.profile = profile;
        }

        static void EnableCameraEffects()
        {
            var cam = PlayerInfo.Camera != null ? PlayerInfo.Camera : Camera.main;
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        void BuildOverlay()
        {
            var canvas = UIKit.CreateCanvas("Pantalla", 90);
            edge = UIKit.Picture(canvas.transform, "Bordes", UIKit.EdgeGlow, new Color(1f, 0f, 0f, 0f));
            edge.rectTransform.Stretch();

            var fadeCanvas = UIKit.CreateCanvas("Fundido", 200);
            fade = UIKit.Panel(fadeCanvas.transform, "Negro", Color.black, false);
            fade.rectTransform.Stretch();
            fade.raycastTarget = false;
            fadeTarget = 0f; // aparece desde negro
        }

        void OnHit(int damage) => hitPulse = Mathf.Clamp01(damage / 15f);

        void OnFadeOut() => fadeTarget = 1f;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Fundido
            if (fade != null)
            {
                var c = fade.color;
                c.a = Mathf.MoveTowards(c.a, fadeTarget, dt * 2.2f);
                fade.color = c;
                fade.enabled = c.a > 0.001f;
            }

            var gm = GameManager.Instance;
            float q = QuakeInfo.Intensity;
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, dt * 1.6f);

            float health = gm != null ? gm.Health / (float)GameConfig.MaxHealth : 1f;
            bool alert = gm != null && gm.State == GameState.Alerta;
            bool blur = gm != null && (gm.Paused || gm.State == GameState.Resultado);

            // Bordes rojos: alerta (pulso), golpe, salud baja
            float target = 0f;
            edgeColor = new Color(1f, 0.1f, 0.05f);
            if (alert) target = 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 9f);
            if (health < 0.35f && gm != null && gm.Playing) target = Mathf.Max(target, 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f));
            target = Mathf.Max(target, hitPulse * 0.9f);
            if (gm != null && gm.PlayerProtected && gm.IsShaking) { edgeColor = new Color(0.2f, 1f, 0.5f); target = Mathf.Max(target, 0.18f); }
            edgeAlpha = Mathf.Lerp(edgeAlpha, target, dt * 10f);
            if (edge != null) edge.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, edgeAlpha);

            if (profile == null) return;

            vignette.intensity.value = 0.24f + 0.18f * q + 0.25f * hitPulse;
            vignette.color.value = Color.Lerp(Color.black, new Color(0.45f, 0f, 0f), Mathf.Max(hitPulse, health < 0.35f ? 0.6f : 0f));
            chroma.intensity.value = Mathf.Clamp01(q * 0.35f + hitPulse * 0.8f);
            grain.intensity.value = q * 0.45f;
            float evacuationMood = gm != null && gm.State == GameState.Evacuacion ? -12f : 0f;
            color.saturation.value = 14f + evacuationMood - (1f - health) * 35f;
            color.postExposure.value = QuakeInfo.PowerOn ? 0.25f : 0.45f;
            dof.active = menuScene || blur;
        }
    }
}
