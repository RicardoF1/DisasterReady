using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady
{
    /// <summary>
    /// Interfaz durante la partida: fase y tiempo, objetivo, puntaje, salud, mochila,
    /// mira con texto de interacción, estado de protección, brújula de evacuación,
    /// notificaciones animadas y carteles de cada fase.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        RectTransform root;

        // Superior izquierda
        Image phaseChip;
        Text phaseText, timerText;
        RectTransform phaseBar;

        // Objetivo
        Text objectiveText;

        // Superior derecha
        Text scoreText, healthText;
        RectTransform healthBar;
        Image healthFill;
        float shownScore;

        // Mochila
        Text backpackTitle, backpackList;
        readonly List<Image> slots = new List<Image>();

        // Centro
        Image crosshair, crosshairRing;
        Image promptBg;
        Text promptText;
        Image statusBg;
        Text statusText;

        // Brújula
        RectTransform compass;
        RectTransform compassArrow;
        Text compassText;

        // Banner
        CanvasGroup banner;
        Text bannerTitle, bannerSubtitle;
        float bannerTime = -10f;

        // Controles
        CanvasGroup controls;
        float startTime;

        // Notificaciones
        RectTransform feed;
        readonly List<Toast> toasts = new List<Toast>();

        class Toast
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public float Born;
            public float Y;
        }

        CanvasGroup hudGroup;

        // ---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<HUD>() != null) return;
            new GameObject("HUD").AddComponent<HUD>();
        }

        void Awake()
        {
            Build();
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.Message += OnMessage;
            GameEvents.ScoreChanged += OnScoreChanged;
            GameEvents.InventoryChanged += RefreshBackpack;
            GameEvents.TaskCompleted += OnTaskCompleted;
            startTime = Time.time;
        }

        void OnDestroy()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.Message -= OnMessage;
            GameEvents.ScoreChanged -= OnScoreChanged;
            GameEvents.InventoryChanged -= RefreshBackpack;
            GameEvents.TaskCompleted -= OnTaskCompleted;
        }

        // ---------------------------------------------------------------------
        // Construcción
        // ---------------------------------------------------------------------
        void Build()
        {
            var canvas = UIKit.CreateCanvas("HUD", 10);
            root = (RectTransform)canvas.transform;
            hudGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;

            BuildPhase();
            BuildObjective();
            BuildStats();
            BuildBackpack();
            BuildCenter();
            BuildCompass();
            BuildBanner();
            BuildControls();

            feed = UIKit.Node("Notificaciones", root);
            feed.Place(new Vector2(0f, 0.5f), new Vector2(40f, 40f), new Vector2(620f, 400f));
            RefreshBackpack();
        }

        void BuildPhase()
        {
            var card = UIKit.Panel(root, "Fase", UIKit.Ink);
            card.rectTransform.Place(new Vector2(0f, 1f), new Vector2(36f, -32f), new Vector2(400f, 132f));

            phaseChip = UIKit.Panel(card.transform, "Chip", UIKit.Info, true, 0.6f);
            phaseChip.rectTransform.Place(new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(230f, 40f));
            phaseText = UIKit.Label(phaseChip.transform, "Texto", "PREPARACIÓN", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            phaseText.rectTransform.Stretch();

            timerText = UIKit.Label(card.transform, "Tiempo", "01:30", 52, UIKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            timerText.rectTransform.Place(new Vector2(0f, 0f), new Vector2(22f, 18f), new Vector2(300f, 64f));

            var barBg = UIKit.Panel(card.transform, "Barra", new Color(1f, 1f, 1f, 0.1f), true, 0.4f);
            barBg.rectTransform.Place(new Vector2(0f, 0f), new Vector2(20f, 12f), new Vector2(360f, 8f));
            var fill = UIKit.Panel(barBg.transform, "Progreso", UIKit.Info, true, 0.4f);
            phaseBar = fill.rectTransform;
            phaseBar.anchorMin = Vector2.zero;
            phaseBar.anchorMax = new Vector2(1f, 1f);
            phaseBar.offsetMin = Vector2.zero;
            phaseBar.offsetMax = Vector2.zero;
        }

        void BuildObjective()
        {
            var card = UIKit.Panel(root, "Objetivo", UIKit.InkSoft);
            card.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(900f, 92f));
            var tag = UIKit.Label(card.transform, "Etiqueta", "OBJETIVO", 16, UIKit.Accent, TextAnchor.UpperLeft, FontStyle.Bold, false);
            tag.rectTransform.Place(new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(200f, 22f));
            objectiveText = UIKit.Label(card.transform, "Texto", "", 23, UIKit.TextMain, TextAnchor.MiddleLeft);
            objectiveText.rectTransform.Stretch(24f, 24f, 28f, 8f);
        }

        void BuildStats()
        {
            var card = UIKit.Panel(root, "Estado", UIKit.Ink);
            card.rectTransform.Place(new Vector2(1f, 1f), new Vector2(-36f, -32f), new Vector2(360f, 132f));

            UIKit.Label(card.transform, "Etq puntos", "PUNTAJE", 16, UIKit.TextDim, TextAnchor.UpperLeft, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(150f, 22f));
            scoreText = UIKit.Label(card.transform, "Puntos", "0", 46, UIKit.Accent, TextAnchor.UpperRight, FontStyle.Bold);
            scoreText.rectTransform.Place(new Vector2(1f, 1f), new Vector2(-22f, -6f), new Vector2(200f, 56f));

            UIKit.Label(card.transform, "Etq salud", "SALUD", 16, UIKit.TextDim, TextAnchor.UpperLeft, FontStyle.Bold, false)
                .rectTransform.Place(new Vector2(0f, 0f), new Vector2(22f, 40f), new Vector2(150f, 22f));
            healthText = UIKit.Label(card.transform, "Salud", "100%", 18, UIKit.TextMain, TextAnchor.UpperRight, FontStyle.Bold, false);
            healthText.rectTransform.Place(new Vector2(1f, 0f), new Vector2(-22f, 40f), new Vector2(100f, 22f));
            var bg = UIKit.Panel(card.transform, "Barra salud", new Color(1f, 1f, 1f, 0.1f), true, 0.4f);
            bg.rectTransform.Place(new Vector2(0f, 0f), new Vector2(22f, 20f), new Vector2(316f, 14f));
            healthBar = bg.rectTransform;
            healthFill = UIKit.Panel(bg.transform, "Relleno", UIKit.Good, true, 0.4f);
            healthFill.rectTransform.anchorMin = Vector2.zero;
            healthFill.rectTransform.anchorMax = Vector2.one;
            healthFill.rectTransform.offsetMin = Vector2.zero;
            healthFill.rectTransform.offsetMax = Vector2.zero;
        }

        void BuildBackpack()
        {
            var card = UIKit.Panel(root, "Mochila", UIKit.InkSoft);
            card.rectTransform.Place(new Vector2(1f, 1f), new Vector2(-36f, -180f), new Vector2(360f, 330f));
            backpackTitle = UIKit.Label(card.transform, "Título", "MOCHILA  0/12", 20, UIKit.TextMain, TextAnchor.UpperLeft, FontStyle.Bold, false);
            backpackTitle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(320f, 28f));

            // 12 espacios
            for (int i = 0; i < GameConfig.BackpackCapacity; i++)
            {
                var slot = UIKit.Panel(card.transform, "Espacio", new Color(1f, 1f, 1f, 0.08f), true, 0.3f);
                slot.rectTransform.Place(new Vector2(0f, 1f), new Vector2(22f + i * 26.5f, -52f), new Vector2(22f, 22f));
                slots.Add(slot);
            }

            backpackList = UIKit.Label(card.transform, "Lista", "", 19, UIKit.TextMain, TextAnchor.UpperLeft);
            backpackList.rectTransform.Stretch(22f, 16f, 88f, 40f);

            var hint = UIKit.Label(card.transform, "Ayuda", "E recoger  ·  Q sacar el último", 15, UIKit.TextDim, TextAnchor.LowerLeft, FontStyle.Normal, false);
            hint.rectTransform.Stretch(22f, 16f, 0f, 12f);
        }

        void BuildCenter()
        {
            crosshairRing = UIKit.Picture(root, "Anillo", UIKit.Ring, new Color(1f, 0.85f, 0.4f, 0f));
            crosshairRing.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            crosshair = UIKit.Picture(root, "Mira", UIKit.Circle, new Color(1f, 1f, 1f, 0.85f));
            crosshair.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));

            promptBg = UIKit.Panel(root, "Interacción", UIKit.Ink, true, 0.7f);
            promptBg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(620f, 54f));
            promptText = UIKit.Label(promptBg.transform, "Texto", "", 24, UIKit.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            promptText.rectTransform.Stretch(16f, 16f, 0f, 0f);

            statusBg = UIKit.Panel(root, "Protección", UIKit.Ink, true, 0.8f);
            statusBg.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(640f, 74f));
            statusText = UIKit.Label(statusBg.transform, "Texto", "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            statusText.rectTransform.Stretch(12f, 12f, 0f, 0f);
        }

        void BuildCompass()
        {
            compass = UIKit.Node("Brújula", root);
            compass.Place(new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(380f, 90f));
            var bg = UIKit.Panel(compass, "Fondo", UIKit.InkSoft, true, 0.8f);
            bg.rectTransform.Stretch();

            compassArrow = UIKit.Node("Flecha", compass);
            compassArrow.Place(new Vector2(0f, 0.5f), new Vector2(46f, 0f), new Vector2(60f, 60f));
            compassArrow.pivot = new Vector2(0.5f, 0.5f);
            var shaft = UIKit.Panel(compassArrow, "Asta", UIKit.Good, true, 0.3f);
            shaft.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(12f, 34f));
            shaft.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var head = UIKit.Panel(compassArrow, "Punta", UIKit.Good, true, 0.3f);
            head.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(26f, 26f));
            head.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            head.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            compassText = UIKit.Label(compass, "Texto", "", 20, UIKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            compassText.rectTransform.Stretch(96f, 12f, 0f, 0f);
            compass.gameObject.SetActive(false);
        }

        void BuildBanner()
        {
            var rt = UIKit.Node("Banner", root);
            rt.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1920f, 200f));
            banner = rt.gameObject.AddComponent<CanvasGroup>();
            banner.alpha = 0f;
            var band = UIKit.Panel(rt, "Franja", new Color(0f, 0f, 0f, 0.55f), false);
            band.rectTransform.Stretch(0f, 0f, 30f, 30f);
            bannerTitle = UIKit.Label(rt, "Título", "", 66, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerTitle.rectTransform.Stretch(0f, 0f, 30f, 70f);
            bannerSubtitle = UIKit.Label(rt, "Subtítulo", "", 26, UIKit.TextDim, TextAnchor.MiddleCenter);
            bannerSubtitle.rectTransform.Stretch(0f, 0f, 120f, 30f);
        }

        void BuildControls()
        {
            var card = UIKit.Panel(root, "Controles", UIKit.InkSoft);
            card.rectTransform.Place(new Vector2(0f, 0f), new Vector2(36f, 32f), new Vector2(520f, 196f));
            controls = card.gameObject.AddComponent<CanvasGroup>();
            string text =
                "<b>CONTROLES</b>\n" +
                "WASD  mover   ·   Mouse  mirar   ·   Shift  correr\n" +
                "C / Ctrl  agacharse   ·   H  sujetarse\n" +
                "E  recoger / usar   ·   Q  sacar de la mochila\n" +
                "F  linterna   ·   Esc  pausa";
            var label = UIKit.Label(card.transform, "Texto", text, 19, UIKit.TextMain, TextAnchor.MiddleLeft);
            label.rectTransform.Stretch(22f, 16f, 10f, 10f);
        }

        // ---------------------------------------------------------------------
        // Eventos
        // ---------------------------------------------------------------------
        void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Preparacion:
                    ShowBanner("HUANCAYO · 4:35 P.M.", "Estás solo en casa. Recuerdas el simulacro del colegio: es momento de prepararte.");
                    Invoke(nameof(PhaseOneBanner), 3.4f);
                    break;
                case GameState.Alerta:
                    ShowBanner("¡ALERTA SÍSMICA!", "Busca una zona segura AHORA");
                    break;
                case GameState.Sismo:
                    ShowBanner("FASE 2 · ¡SISMO!", "Agáchate · Cúbrete · Sujétate");
                    break;
                case GameState.Evacuacion:
                    ShowBanner("FASE 3 · EVACUACIÓN", "Corta el gas y la luz y sal con calma al punto de reunión");
                    break;
                case GameState.Resultado:
                    hudGroup.alpha = 0f;
                    break;
            }
            if (state != GameState.Resultado) GameEvents.PlaySound(SoundId.Whoosh, 0.7f);
            RefreshBackpack();
        }

        void PhaseOneBanner()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Preparacion)
            {
                ShowBanner("FASE 1 · PREPARACIÓN", "Arma tu mochila de emergencia antes de que llegue el sismo");
                GameEvents.PlaySound(SoundId.Whoosh, 0.7f);
            }
        }

        void ShowBanner(string title, string subtitle)
        {
            bannerTitle.text = title;
            bannerSubtitle.text = subtitle;
            bannerTime = Time.time;
        }

        void OnMessage(string text, MessageKind kind)
        {
            Color c;
            switch (kind)
            {
                case MessageKind.Good: c = UIKit.Good; break;
                case MessageKind.Bad: c = UIKit.Bad; break;
                case MessageKind.Warning: c = UIKit.Warn; break;
                default: c = UIKit.Info; break;
            }
            AddToast(text, c, null);
        }

        void OnScoreChanged(int total, int delta, string reason)
        {
            if (string.IsNullOrEmpty(reason)) return;
            string sign = delta > 0 ? "+" + delta : delta.ToString();
            AddToast(reason, delta > 0 ? UIKit.Good : UIKit.Bad, sign);
        }

        void OnTaskCompleted(string task) => RefreshBackpack();

        void AddToast(string text, Color accent, string badge)
        {
            var rt = UIKit.Node("Aviso", feed);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(600f, 58f);
            var group = rt.gameObject.AddComponent<CanvasGroup>();

            var bg = UIKit.Panel(rt, "Fondo", UIKit.Ink, true, 0.7f);
            bg.rectTransform.Stretch();
            var bar = UIKit.Panel(rt, "Color", accent, true, 0.3f);
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.offsetMin = new Vector2(6f, 10f);
            bar.rectTransform.offsetMax = new Vector2(12f, -10f);

            float left = 24f;
            if (!string.IsNullOrEmpty(badge))
            {
                var b = UIKit.Label(rt, "Puntos", badge, 26, accent, TextAnchor.MiddleLeft, FontStyle.Bold, false);
                b.rectTransform.Stretch(left, 0f, 0f, 0f);
                left += 70f;
            }
            var label = UIKit.Label(rt, "Texto", text, 20, UIKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Normal, false);
            label.rectTransform.Stretch(left, 14f, 4f, 4f);

            // Altura según el largo del texto
            float lines = Mathf.Ceil(text.Length / 48f);
            rt.sizeDelta = new Vector2(600f, Mathf.Max(52f, 18f + lines * 26f));

            toasts.Insert(0, new Toast { Rect = rt, Group = group, Born = Time.time, Y = 60f });
            while (toasts.Count > 6)
            {
                Destroy(toasts[toasts.Count - 1].Rect.gameObject);
                toasts.RemoveAt(toasts.Count - 1);
            }
        }

        void RefreshBackpack()
        {
            var gm = GameManager.Instance;
            if (gm == null || backpackTitle == null) return;
            var inv = gm.Backpack;
            backpackTitle.text = $"MOCHILA  {inv.Used}/{inv.Capacity}";

            int slot = 0;
            var lines = new System.Text.StringBuilder();
            foreach (var item in inv.Items)
            {
                for (int i = 0; i < item.Size && slot < slots.Count; i++, slot++)
                    slots[slot].color = item.Color;
                lines.Append(UIKit.Colorize("■ ", item.Color)).Append(item.Name);
                if (!item.Useful) lines.Append(UIKit.Colorize("  (no esencial)", UIKit.Bad));
                lines.Append('\n');
            }
            for (; slot < slots.Count; slot++) slots[slot].color = new Color(1f, 1f, 1f, 0.08f);

            if (inv.Items.Count == 0) lines.Append(UIKit.Colorize("Vacía: busca suministros por la casa", UIKit.TextDim));
            if (gm.State != GameState.Preparacion)
            {
                lines.Append('\n');
                lines.Append(gm.IsTaskDone(GameConfig.TaskGas) ? UIKit.Colorize("● Gas cerrado\n", UIKit.Good) : UIKit.Colorize("○ Cerrar el gas\n", UIKit.TextDim));
                lines.Append(gm.IsTaskDone(GameConfig.TaskPower) ? UIKit.Colorize("● Luz cortada", UIKit.Good) : UIKit.Colorize("○ Cortar la luz", UIKit.TextDim));
            }
            backpackList.text = lines.ToString();
        }

        // ---------------------------------------------------------------------
        // Actualización
        // ---------------------------------------------------------------------
        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Resultado)
            {
                hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, 0f, Time.unscaledDeltaTime * 3f);
                return;
            }
            hudGroup.alpha = gm.Paused ? 0.25f : 1f;

            UpdatePhase(gm);
            objectiveText.text = gm.Objective;

            shownScore = Mathf.Lerp(shownScore, gm.Score, Time.deltaTime * 8f);
            scoreText.text = Mathf.RoundToInt(shownScore).ToString();

            float health = gm.Health / (float)GameConfig.MaxHealth;
            healthText.text = gm.Health + "%";
            var fill = healthFill.rectTransform;
            fill.anchorMax = new Vector2(Mathf.Lerp(fill.anchorMax.x, health, Time.deltaTime * 6f), 1f);
            healthFill.color = health > 0.6f ? UIKit.Good : health > 0.3f ? UIKit.Warn : UIKit.Bad;

            UpdateCenter(gm);
            UpdateCompass(gm);
            UpdateBanner();
            UpdateToasts();

            // Controles visibles al inicio de la preparación
            float since = Time.time - startTime;
            float controlsTarget = gm.State == GameState.Preparacion && since < 25f ? 1f : 0f;
            controls.alpha = Mathf.MoveTowards(controls.alpha, controlsTarget, Time.deltaTime * 2f);
        }

        void UpdatePhase(GameManager gm)
        {
            Color c;
            string name;
            switch (gm.State)
            {
                case GameState.Preparacion: c = UIKit.Info; name = "PREPARACIÓN"; break;
                case GameState.Alerta: c = UIKit.Bad; name = "ALERTA SÍSMICA"; break;
                case GameState.Sismo: c = UIKit.Bad; name = "SISMO"; break;
                default: c = gm.InAftershock ? UIKit.Bad : UIKit.Good; name = gm.InAftershock ? "RÉPLICA" : "EVACUACIÓN"; break;
            }
            if (gm.State == GameState.Alerta || gm.State == GameState.Sismo || gm.InAftershock)
                c = Color.Lerp(c, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 10f));
            phaseChip.color = c;
            phaseText.text = name;

            int seconds = Mathf.CeilToInt(gm.TimeLeft);
            timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            timerText.color = gm.TimeLeft < 10f && gm.State != GameState.Sismo ? Color.Lerp(UIKit.Bad, Color.white, Mathf.PingPong(Time.time * 2f, 1f)) : UIKit.TextMain;

            float progress = gm.PhaseDuration > 0f ? gm.TimeLeft / gm.PhaseDuration : 0f;
            phaseBar.anchorMax = new Vector2(progress, 1f);
            phaseBar.GetComponent<Image>().color = c;
        }

        void UpdateCenter(GameManager gm)
        {
            var focused = PlayerInfo.Focused;
            bool hasFocus = focused != null && !gm.Paused;
            string prompt = hasFocus ? focused.Prompt : "";
            bool show = !string.IsNullOrEmpty(prompt);

            if (show)
            {
                bool can = focused.CanInteract;
                promptText.text = can ? UIKit.Colorize("[E]  ", UIKit.Accent) + prompt : UIKit.Colorize(prompt, UIKit.TextDim);
                float width = Mathf.Clamp(prompt.Length * 12.5f + 90f, 240f, 900f);
                promptBg.rectTransform.sizeDelta = new Vector2(width, 54f);
            }
            promptBg.gameObject.SetActive(show);

            float ringAlpha = hasFocus ? (focused.CanInteract ? 0.9f : 0.35f) : 0f;
            var rc = crosshairRing.color;
            rc.a = Mathf.MoveTowards(rc.a, ringAlpha, Time.deltaTime * 6f);
            crosshairRing.color = rc;
            float pulse = hasFocus ? 1f + 0.08f * Mathf.Sin(Time.time * 8f) : 0.7f;
            crosshairRing.rectTransform.localScale = Vector3.one * pulse;

            // Estado de protección mientras tiembla o en la alerta
            bool danger = gm.IsShaking || gm.State == GameState.Alerta;
            if (danger)
            {
                string text;
                Color bg;
                if (gm.PlayerHolding) { text = "SUJETÁNDOTE  ·  ¡PROTEGIDO!"; bg = new Color(0.05f, 0.5f, 0.3f, 0.9f); }
                else if (gm.PlayerProtected) { text = "PROTEGIDO  ·  mantén H para sujetarte"; bg = new Color(0.1f, 0.45f, 0.25f, 0.88f); }
                else if (gm.PlayerExposed) { text = "¡PELIGRO! LEJOS DE LA VENTANA"; bg = new Color(0.65f, 0.08f, 0.08f, 0.9f); }
                else if (PlayerInfo.IsCrouching) { text = "Agachado: busca una mesa o columna"; bg = new Color(0.55f, 0.4f, 0.05f, 0.9f); }
                else { text = "¡EXPUESTO!  Agáchate (C) bajo una mesa"; bg = new Color(0.65f, 0.1f, 0.1f, 0.9f); }
                statusText.text = text;
                statusBg.color = bg;
                float s = 1f + 0.03f * Mathf.Sin(Time.time * 12f);
                statusBg.rectTransform.localScale = new Vector3(s, s, 1f);
            }
            statusBg.gameObject.SetActive(danger);
        }

        void UpdateCompass(GameManager gm)
        {
            bool show = gm.State == GameState.Evacuacion && !gm.InAftershock && PlayerInfo.HasPlayer && PlayerInfo.Camera != null;
            compass.gameObject.SetActive(show);
            if (!show) return;
            if (!EvacuationRoute.TryGetNext(PlayerInfo.Feet, out Vector3 next)) return;

            Vector3 to = next - PlayerInfo.Feet;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 fwd = PlayerInfo.Camera.transform.forward;
            fwd.y = 0f;
            float angle = Vector3.SignedAngle(fwd.normalized, flat.normalized, Vector3.up);
            compassArrow.localRotation = Quaternion.Euler(0f, 0f, -angle);

            var points = EvacuationRoute.Points;
            float toGoal = Vector3.Distance(PlayerInfo.Feet, points[points.Count - 1]);
            string where;
            if (to.y < -1.5f) where = "Baja por la escalera";
            else if (PlayerInfo.Feet.z < 0f) where = "Al punto de reunión";
            else where = "Sigue las señales de SALIDA";
            compassText.text = $"{where}\n<size=16>{toGoal:0} m al punto de reunión</size>";
        }

        void UpdateBanner()
        {
            float t = Time.time - bannerTime;
            float a = t < 0.3f ? t / 0.3f : t < 2.6f ? 1f : Mathf.Clamp01(1f - (t - 2.6f) / 0.6f);
            banner.alpha = a;
            float s = 1f + Mathf.Max(0f, 0.3f - t) * 0.6f;
            bannerTitle.rectTransform.localScale = new Vector3(s, s, 1f);
            var gm = GameManager.Instance;
            if (gm != null && gm.State == GameState.Alerta)
            {
                banner.alpha = 1f;
                bannerTitle.color = Color.Lerp(UIKit.Bad, Color.white, Mathf.PingPong(Time.time * 4f, 1f));
            }
            else bannerTitle.color = Color.white;
        }

        void UpdateToasts()
        {
            float y = 0f;
            for (int i = 0; i < toasts.Count; i++)
            {
                var toast = toasts[i];
                float age = Time.time - toast.Born;
                float life = 5.5f;
                toast.Y = Mathf.Lerp(toast.Y, -y, Time.deltaTime * 12f);
                float slide = Mathf.Clamp01(age / 0.25f);
                toast.Rect.anchoredPosition = new Vector2(Mathf.Lerp(-60f, 0f, slide), toast.Y);
                toast.Group.alpha = Mathf.Clamp01(slide) * Mathf.Clamp01((life - age) / 0.6f);
                y += toast.Rect.sizeDelta.y + 8f;
            }
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (Time.time - toasts[i].Born > 5.5f)
                {
                    Destroy(toasts[i].Rect.gameObject);
                    toasts.RemoveAt(i);
                }
            }
        }
    }
}
