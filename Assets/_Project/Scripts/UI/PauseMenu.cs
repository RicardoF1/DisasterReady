using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    /// <summary>Pausa con Esc: continuar, reiniciar, opciones de volumen y sensibilidad, volver al menú.</summary>
    public class PauseMenu : MonoBehaviour
    {
        GameObject panel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<PauseMenu>() != null) return;
            new GameObject("Menú de pausa").AddComponent<PauseMenu>();
        }

        void Start() => Build();

        void Build()
        {
            var canvas = UIKit.CreateCanvas("Pausa", 60);
            var overlay = UIKit.Panel(canvas.transform, "Oscurecer", new Color(0.02f, 0.03f, 0.06f, 0.6f), false);
            overlay.rectTransform.Stretch();
            overlay.raycastTarget = true;
            panel = overlay.gameObject;

            var card = UIKit.Panel(overlay.transform, "Tarjeta", UIKit.Ink);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 760f));

            UIKit.Label(card.transform, "Título", "PAUSA", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 80f));

            var buttons = UIKit.Node("Botones", card.transform);
            buttons.Place(new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(560f, 260f));
            Vector2 size = new Vector2(560f, 72f);
            UIKit.Button(buttons, "Continuar", "CONTINUAR", Resume, UIKit.Accent, size).GetComponent<RectTransform>()
                .Place(new Vector2(0.5f, 1f), Vector2.zero, size);
            UIKit.Button(buttons, "Reiniciar", "REINICIAR SIMULACIÓN", () => GameManager.Instance?.Restart(), new Color(0.2f, 0.42f, 0.7f), size)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0f, -90f), size);
            UIKit.Button(buttons, "Menú", "MENÚ PRINCIPAL", () => GameManager.Instance?.GoToMenu(), new Color(0.3f, 0.32f, 0.38f), size)
                .GetComponent<RectTransform>().Place(new Vector2(0.5f, 1f), new Vector2(0f, -180f), size);

            UIKit.Label(card.transform, "Opciones", "OPCIONES", 22, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(-10f, 300f), new Vector2(580f, 30f));
            var options = UIKit.Node("Deslizadores", card.transform);
            options.Place(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(580f, 250f));
            AddSlider(options, "Volumen general", Settings.Master, 0f, 1f, v => Settings.Master = v, 0);
            AddSlider(options, "Música", Settings.Music, 0f, 1f, v => Settings.Music = v, 1);
            AddSlider(options, "Efectos", Settings.Sfx, 0f, 1f, v => Settings.Sfx = v, 2);
            AddSlider(options, "Sensibilidad", Settings.Sensitivity, 0.02f, 0.3f, v => Settings.Sensitivity = v, 3);

            panel.SetActive(false);
        }

        static void AddSlider(RectTransform parent, string label, float value, float min, float max, UnityEngine.Events.UnityAction<float> change, int row)
        {
            var slider = UIKit.Slider(parent, label, value, min, max, change, 580f);
            ((RectTransform)slider.transform.parent).Place(new Vector2(0.5f, 1f), new Vector2(0f, -row * 62f), new Vector2(580f, 60f));
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var keyboard = Keyboard.current;
            if (gm == null || keyboard == null || panel == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame && gm.Playing)
            {
                if (gm.Paused) Resume();
                else Pause();
            }
            if (!gm.Playing && panel.activeSelf) panel.SetActive(false);
        }

        void Pause()
        {
            GameManager.Instance.SetPaused(true);
            panel.SetActive(true);
            GameEvents.PlaySound(SoundId.Click);
        }

        void Resume()
        {
            Settings.Save();
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            panel.SetActive(false);
        }
    }
}
