using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    /// <summary>
    /// Menú principal sobre una vista cinematográfica de la vivienda en Huancayo:
    /// jugar, cómo jugar, opciones, créditos y salir.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        GameObject mainPanel, howPanel, optionsPanel, creditsPanel;
        Camera cam;
        float orbit;
        CanvasGroup titleGroup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.MenuScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<MainMenu>() != null) return;
            new GameObject("Menú principal").AddComponent<MainMenu>();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cam = Camera.main;
            if (cam != null)
            {
                cam.fieldOfView = 45f;
                cam.farClipPlane = 1500f;
            }
            orbit = 0f;
            BuildUI();
        }

        // ---------------------------------------------------------------------
        void BuildUI()
        {
            var canvas = UIKit.CreateCanvas("Menú", 20);
            var root = canvas.transform;

            // Degradado lateral para que el texto se lea sobre la escena 3D
            var shade = UIKit.Picture(root, "Degradado", UIKit.GradientH, new Color(0.03f, 0.05f, 0.09f, 0.92f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;
            var bottom = UIKit.Picture(root, "Degradado inferior", UIKit.GradientV, new Color(0.03f, 0.05f, 0.09f, 0.7f));
            bottom.rectTransform.anchorMin = Vector2.zero;
            bottom.rectTransform.anchorMax = new Vector2(1f, 0.25f);
            bottom.rectTransform.offsetMin = Vector2.zero;
            bottom.rectTransform.offsetMax = Vector2.zero;

            mainPanel = BuildMainPanel(root);
            howPanel = BuildHowPanel(root);
            optionsPanel = BuildOptionsPanel(root);
            creditsPanel = BuildCreditsPanel(root);
            ShowPanel(mainPanel);
        }

        GameObject BuildMainPanel(Transform root)
        {
            var panel = UIKit.Node("Principal", root);
            panel.Stretch();
            titleGroup = panel.gameObject.AddComponent<CanvasGroup>();

            var tag = UIKit.Panel(panel, "Etiqueta", UIKit.Accent, true, 0.5f);
            tag.rectTransform.Place(new Vector2(0f, 1f), new Vector2(120f, -150f), new Vector2(430f, 40f));
            UIKit.Label(tag.transform, "Texto", "SIMULACIÓN EDUCATIVA · HUANCAYO", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false)
                .rectTransform.Stretch();

            UIKit.Label(panel, "Título 1", "DISASTER", 128, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(112f, -200f), new Vector2(1000f, 140f));
            UIKit.Label(panel, "Título 2", "READY", 128, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(112f, -320f), new Vector2(1000f, 140f));
            UIKit.Label(panel, "Lema", "¿Sabes qué hacer cuando la tierra tiembla?\nPrepárate · Protégete · Evacúa", 30, UIKit.TextDim, TextAnchor.UpperLeft)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(120f, -475f), new Vector2(900f, 90f));

            var size = new Vector2(440f, 70f);
            float y = -600f;
            AddMenuButton(panel, "JUGAR", () => SceneBoot.Load(GameConfig.HouseScene), UIKit.Accent, ref y, size);
            AddMenuButton(panel, "CÓMO JUGAR", () => ShowPanel(howPanel), new Color(0.18f, 0.38f, 0.62f), ref y, size);
            AddMenuButton(panel, "OPCIONES", () => ShowPanel(optionsPanel), new Color(0.18f, 0.38f, 0.62f), ref y, size);
            AddMenuButton(panel, "CRÉDITOS", () => ShowPanel(creditsPanel), new Color(0.18f, 0.38f, 0.62f), ref y, size);
            AddMenuButton(panel, "SALIR", Quit, new Color(0.28f, 0.3f, 0.36f), ref y, size);

            // Insignias ODS
            Badge(panel, "ODS 11", "Ciudades y comunidades\nsostenibles · meta 11.5", new Color(0.98f, 0.62f, 0.15f), new Vector2(120f, 40f));
            Badge(panel, "ODS 13", "Acción por el clima\nresiliencia · meta 13.1", new Color(0.25f, 0.5f, 0.25f), new Vector2(470f, 40f));
            UIKit.Label(panel, "Versión", "v5.0  ·  Unity 6  ·  URP", 18, UIKit.TextDim, TextAnchor.LowerRight, FontStyle.Normal, false)
                .rectTransform.Place(new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(400f, 30f));
            return panel.gameObject;
        }

        static void AddMenuButton(Transform parent, string text, UnityEngine.Events.UnityAction action, Color color, ref float y, Vector2 size)
        {
            var b = UIKit.Button(parent, text, text, action, color, size, 30);
            b.GetComponent<RectTransform>().Place(new Vector2(0f, 1f), new Vector2(120f, y), size);
            y -= size.y + 16f;
        }

        static void Badge(Transform parent, string title, string text, Color color, Vector2 position)
        {
            var card = UIKit.Panel(parent, title, new Color(0f, 0f, 0f, 0.35f));
            card.rectTransform.Place(new Vector2(0f, 0f), position, new Vector2(330f, 74f));
            var chip = UIKit.Panel(card.transform, "Chip", color, true, 0.5f);
            chip.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(96f, 54f));
            UIKit.Label(chip.transform, "T", title, 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false).rectTransform.Stretch();
            UIKit.Label(card.transform, "Texto", text, 16, UIKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Normal, false)
                .rectTransform.Stretch(118f, 8f, 4f, 4f);
        }

        GameObject Window(Transform root, string title, Vector2 size, out RectTransform content)
        {
            var overlay = UIKit.Panel(root, title, new Color(0.02f, 0.03f, 0.06f, 0.55f), false);
            overlay.rectTransform.Stretch();
            overlay.raycastTarget = true;
            var card = UIKit.Panel(overlay.transform, "Tarjeta", UIKit.Ink);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UIKit.Label(card.transform, "Título", title, 52, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(50f, -30f), new Vector2(size.x - 100f, 70f));
            var bar = UIKit.Panel(card.transform, "Acento", UIKit.Accent, true, 0.4f);
            bar.rectTransform.Place(new Vector2(0f, 1f), new Vector2(50f, -104f), new Vector2(120f, 6f));
            content = UIKit.Node("Contenido", card.transform);
            content.Stretch(50f, 50f, 130f, 120f);
            var back = UIKit.Button(card.transform, "Volver", "VOLVER", () => ShowPanel(mainPanel), new Color(0.3f, 0.32f, 0.38f), new Vector2(300f, 64f));
            back.GetComponent<RectTransform>().Place(new Vector2(1f, 0f), new Vector2(-50f, 36f), new Vector2(300f, 64f));
            return overlay.gameObject;
        }

        GameObject BuildHowPanel(Transform root)
        {
            var panel = Window(root, "CÓMO JUGAR", new Vector2(1500f, 860f), out var content);
            PhaseCard(content, 0, "1", "PREPARACIÓN", UIKit.Info,
                "Explora los dos pisos y arma tu MOCHILA DE EMERGENCIA (12 espacios).\n\n" +
                "Prioriza agua, botiquín, linterna, documentos, alimentos y radio.\n\n" +
                "Laptop, consola y adornos restan puntos.\n\nRecuerda dónde están las mesas firmes y las señales ZONA SEGURA.");
            PhaseCard(content, 1, "2", "SISMO", UIKit.Bad,
                "Suena la ALERTA SÍSMICA.\n\nAGÁCHATE (C), CÚBRETE debajo de una mesa firme o junto a una columna con señal verde, y SUJÉTATE (mantén H).\n\n" +
                "No corras. Aléjate de ventanas, estantes y roperos: los objetos caen y los vidrios estallan.");
            PhaseCard(content, 2, "3", "EVACUACIÓN", UIKit.Good,
                "Cuando pare el movimiento: cierra el balón de GAS (cocina) y baja la llave de LUZ (tablero del hall).\n\n" +
                "Sigue las señales de SALIDA y la brújula. Algunas rutas pueden quedar bloqueadas.\n\n" +
                "Cuidado con las RÉPLICAS. Llega al PUNTO DE REUNIÓN.");
            var keys = UIKit.Label(content, "Controles",
                "<b>CONTROLES</b>    WASD mover  ·  Mouse mirar  ·  Shift correr  ·  C/Ctrl agacharse  ·  H sujetarse  ·  E recoger/usar  ·  Q sacar de la mochila  ·  F linterna  ·  Esc pausa",
                19, UIKit.TextDim, TextAnchor.MiddleLeft);
            keys.rectTransform.anchorMin = new Vector2(0f, 0f);
            keys.rectTransform.anchorMax = new Vector2(1f, 0f);
            keys.rectTransform.pivot = new Vector2(0.5f, 0f);
            keys.rectTransform.offsetMin = new Vector2(0f, -70f);
            keys.rectTransform.offsetMax = new Vector2(-330f, -10f);
            return panel;
        }

        static void PhaseCard(RectTransform parent, int index, string number, string title, Color color, string body)
        {
            var card = UIKit.Panel(parent, title, new Color(1f, 1f, 1f, 0.05f));
            card.rectTransform.anchorMin = new Vector2(index / 3f, 0f);
            card.rectTransform.anchorMax = new Vector2((index + 1) / 3f, 1f);
            card.rectTransform.offsetMin = new Vector2(index == 0 ? 0f : 12f, 40f);
            card.rectTransform.offsetMax = new Vector2(index == 2 ? 0f : -12f, 0f);

            var circle = UIKit.Picture(card.transform, "Número", UIKit.Circle, color);
            circle.rectTransform.Place(new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(64f, 64f));
            UIKit.Label(circle.transform, "N", number, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false).rectTransform.Stretch();
            UIKit.Label(card.transform, "Título", title, 30, color, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(104f, -30f), new Vector2(330f, 50f));
            UIKit.Label(card.transform, "Texto", body, 21, UIKit.TextMain, TextAnchor.UpperLeft)
                .rectTransform.Stretch(24f, 24f, 110f, 20f);
        }

        GameObject BuildOptionsPanel(Transform root)
        {
            var panel = Window(root, "OPCIONES", new Vector2(900f, 620f), out var content);
            AddSlider(content, "Volumen general", Settings.Master, 0f, 1f, v => Settings.Master = v, 0);
            AddSlider(content, "Música", Settings.Music, 0f, 1f, v => Settings.Music = v, 1);
            AddSlider(content, "Efectos", Settings.Sfx, 0f, 1f, v => Settings.Sfx = v, 2);
            AddSlider(content, "Sensibilidad del mouse", Settings.Sensitivity, 0.02f, 0.3f, v => Settings.Sensitivity = v, 3);
            return panel;
        }

        static void AddSlider(RectTransform parent, string label, float value, float min, float max, UnityEngine.Events.UnityAction<float> change, int row)
        {
            var slider = UIKit.Slider(parent, label, value, min, max, change, 780f);
            ((RectTransform)slider.transform.parent).Place(new Vector2(0f, 1f), new Vector2(0f, -row * 80f), new Vector2(780f, 64f));
        }

        GameObject BuildCreditsPanel(Transform root)
        {
            var panel = Window(root, "CRÉDITOS", new Vector2(1200f, 760f), out var content);
            string[,] team =
            {
                { "Keen", "Director / Game Designer", "Sistema de fases, puntaje e integración" },
                { "Ricardo", "Programador Gameplay", "Movimiento, interacción y linterna" },
                { "Joshua", "Diseñador de niveles", "Vivienda, mobiliario, zonas y efectos del sismo" },
                { "Abraham", "UI/UX y QA", "Interfaces, audio y pruebas" },
            };
            for (int i = 0; i < team.GetLength(0); i++)
            {
                var row = UIKit.Panel(content, team[i, 0], new Color(1f, 1f, 1f, 0.05f));
                row.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, -i * 96f), new Vector2(1100f, 84f));
                var avatar = UIKit.Picture(row.transform, "Avatar", UIKit.Circle, Color.HSVToRGB(i * 0.18f + 0.05f, 0.6f, 0.95f));
                avatar.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(58f, 58f));
                UIKit.Label(avatar.transform, "Inicial", team[i, 0].Substring(0, 1), 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false).rectTransform.Stretch();
                UIKit.Label(row.transform, "Nombre", team[i, 0], 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, false)
                    .rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(92f, 12f), new Vector2(400f, 40f));
                UIKit.Label(row.transform, "Rol", team[i, 1], 20, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold, false)
                    .rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(92f, -18f), new Vector2(400f, 30f));
                UIKit.Label(row.transform, "Tareas", team[i, 2], 20, UIKit.TextDim, TextAnchor.MiddleRight, FontStyle.Normal, false)
                    .rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(560f, 40f));
            }
            UIKit.Label(content, "Herramientas",
                "Hecho con Unity 6 (URP) y C#. Gráficos low-poly y sonidos generados por código.\n" +
                "Inspirado en 60 Seconds!, The Long Dark, Disaster Report 4 y Stop Disasters!",
                19, UIKit.TextDim, TextAnchor.UpperLeft)
                .rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, -400f), new Vector2(1100f, 70f));
            return panel;
        }

        void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            howPanel.SetActive(panel == howPanel);
            optionsPanel.SetActive(panel == optionsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            if (panel != optionsPanel) Settings.Save();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------------
        void Update()
        {
            // Cámara orbitando lentamente la casa (vista de dron al atardecer)
            if (cam != null)
            {
                // Paneo suave de un lado a otro frente a la casa (sin pasar detrás de las casas vecinas)
                orbit += Time.deltaTime;
                float a = (Mathf.Sin(orbit * 0.07f) * 38f - 18f) * Mathf.Deg2Rad;
                Vector3 center = new Vector3(6f, 2.6f, 4f);
                Vector3 pos = center + new Vector3(Mathf.Sin(a) * 26f, 8.5f + Mathf.Sin(Time.time * 0.2f) * 0.8f, -Mathf.Cos(a) * 26f);
                cam.transform.position = pos;
                cam.transform.rotation = Quaternion.LookRotation(center + Vector3.up * 1.5f - pos) * Quaternion.Euler(0f, -12f, 0f);
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !mainPanel.activeSelf)
                ShowPanel(mainPanel);

            if (titleGroup != null) titleGroup.alpha = Mathf.MoveTowards(titleGroup.alpha, 1f, Time.deltaTime * 1.5f);
        }
    }
}
