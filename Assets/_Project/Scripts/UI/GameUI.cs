using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DisasterReady
{
    public sealed class GameUI : MonoBehaviour
    {
        Canvas canvas;
        Font font;
        Text hud, hint, message, reticle;
        GameObject panel;
        float messageUntil;
        GameManager Game => GameManager.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(50, (root, house) =>
            root.gameObject.AddComponent<GameUI>().Build());
        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Interfaz", typeof(RectTransform));
            canvasObject.transform.SetParent(transform);
            canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var events = Object.FindFirstObjectByType<EventSystem>();
            if (events == null)
            {
                var obj = new GameObject("EventSystem"); obj.transform.SetParent(transform);
                events = obj.AddComponent<EventSystem>();
            }
            foreach (BaseInputModule module in events.GetComponents<BaseInputModule>()) module.enabled = false;
            var input = events.GetComponent<InputSystemUIInputModule>();
            if (input == null) input = events.gameObject.AddComponent<InputSystemUIInputModule>();
            input.enabled = true; input.AssignDefaultActions();
            hud = TextAt(canvas.transform, "", 18, new(-210, 235), new(820, 220));
            hint = TextAt(canvas.transform, "", 21, new(0, -200), new(1100, 70), TextAnchor.MiddleCenter);
            message = TextAt(canvas.transform, "", 20, new(0, -300), new(1120, 65), TextAnchor.MiddleCenter);
            reticle = TextAt(canvas.transform, "+", 22, Vector2.zero, new(40, 40), TextAnchor.MiddleCenter);
            GameEvents.OnStateChanged += State;
            GameEvents.OnMessage += Message;
            State(Game.State);
        }
        RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        Text TextAt(Transform parent, string value, int size, Vector2 pos, Vector2 bounds,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var text = Rect(parent, "Texto", pos, bounds).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Color.white;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        void Button(string label, float y, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(panel.transform, label, new(0, y), new(340, 42));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new(.12f, .42f, .46f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action);
            TextAt(rect, label, 20, Vector2.zero, new(330, 40), TextAnchor.MiddleCenter);
        }
        void ClearPanel()
        { if (panel != null) { panel.SetActive(false); Destroy(panel); } }
        void Panel(string title, string body)
        {
            ClearPanel();
            panel = Rect(canvas.transform, "Panel", Vector2.zero, new(920, 650)).gameObject;
            panel.AddComponent<Image>().color = new(.045f, .075f, .11f, .98f);
            TextAt(panel.transform, title, 30, new(0, 270), new(860, 65), TextAnchor.MiddleCenter);
            // Texto desplazable: resultados completos incluso en partidas con muchos errores.
            var viewport = Rect(panel.transform, "Resumen desplazable", new(0, 45), new(840, 350));
            viewport.gameObject.AddComponent<Image>().color = new(0, 0, 0, .12f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var text = TextAt(viewport, body, 20, Vector2.zero, new(810, 350));
            text.raycastTarget = true;
            var content = text.rectTransform; content.anchorMin = content.anchorMax = new(.5f, 1);
            content.pivot = new(.5f, 1); content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new(810, Mathf.Max(350, text.preferredHeight + 20));
            scroll.content = content; scroll.verticalNormalizedPosition = 1;
        }
        void MainMenu()
        {
            Panel("DISASTER READY", "Simulación educativa · Huancayo, Perú\n\nPrepara tu mochila, protégete durante el sismo y evacúa al punto de reunión.\n\nUna vivienda de dos pisos, decisiones bajo presión y una oportunidad para aprender antes de una emergencia real.");
            Button("Jugar", -160, Game.Play); Button("Cómo jugar", -210, Help);
            Button("Créditos", -260, Credits); Button("Salir", -310, Quit);
        }
        void Help()
        {
            Panel("CÓMO JUGAR", "WASD: caminar · Ratón: mirar · Shift: correr\nCtrl: agacharte · H: sujetarte (quieto y agachado)\nE: interactuar · F: linterna recogida · Esc: pausa\n\n1. Recoge suministros durante la preparación. La mochila tiene capacidad limitada.\n2. La alarma pertenece a este simulacro: no predice todos los sismos.\n3. Agáchate bajo la mesa firme y mantén H sin moverte. Aléjate de ventanas y muebles.\n4. Tras el movimiento, sigue el paso central, baja por la escalera este y sal por la puerta sur.\n5. En una réplica, vuelve a protegerte. Llega al punto verde exterior.\n\nLas zonas estructurales del escenario se consideran previamente evaluadas. En la vida real, identifica las zonas con asesoramiento competente. Corta suministros solo si sabes hacerlo, es seguro y no retrasa la evacuación. Ante olor a gas, evita interruptores eléctricos.\n\nLa mochila se prepara con anticipación; el tiempo limitado es una mecánica del simulacro.");
            Button("Volver", -245, MainMenu);
        }
        void Credits()
        {
            Panel("CRÉDITOS", "Keen · Dirección, diseño, fases e integración\nRicardo · Movimiento, interacción y mochila\nJoshua · Casa, objetos, zonas y peligros\nAbraham · UI/UX, audio y QA\n\nInspiraciones: 60 Seconds!, The Long Dark, Disaster Report 4 y Stop Disasters!\n\nODS 11, meta 11.5: preparación para reducir consecuencias de desastres.\nODS 13, meta 13.1: educación para la resiliencia.\nLos sismos son fenómenos geológicos, no causados por el cambio climático.\n\nReferencias educativas: INDECI, Plan Familiar de Emergencia y recomendaciones ante sismos. Prototipo universitario; no evalúa la seguridad real de una vivienda.");
            Button("Volver", -245, MainMenu);
        }
        void State(GameState state)
        {
            ClearPanel();
            if (state == GameState.Menu) MainMenu();
            if (state == GameState.Result)
            {
                string title = Game.Won ? "EVACUACIÓN COMPLETADA · " + Game.Grade : "DERROTA · " + Game.Grade;
                Panel(title, $"Puntaje: {Game.Score} · Salud: {Game.Health:0}\nCalificación: A ≥ {Game.Config.GradeA}; B ≥ {Game.Config.GradeB}; C por debajo.\nLa calificación mide decisiones; una derrota sigue siendo derrota.\n\nACIERTOS, ERRORES Y CONSEJOS\n• " + string.Join("\n• ", Game.Notes) + "\n\nPrepara la mochila antes de la emergencia. Camina con calma, no regreses por objetos y sigue las indicaciones oficiales.");
                Button("Reintentar", -210, Game.Play); Button("Menú", -265, Game.Menu);
            }
        }
        void Message(string text) { message.text = text; messageUntil = Time.unscaledTime + 4; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Game.Playing)
            {
                Game.SetPause(!Game.Paused);
                if (Game.Paused)
                {
                    Panel("PAUSA", "El tiempo y el audio están detenidos.");
                    Button("Continuar", -180, () => { Game.SetPause(false); ClearPanel(); });
                    Button("Reintentar", -235, Game.Play); Button("Menú", -290, Game.Menu);
                }
                else ClearPanel();
            }
            bool show = Game.Playing && !Game.Paused;
            hud.gameObject.SetActive(show); hint.gameObject.SetActive(show); reticle.gameObject.SetActive(show);
            if (Time.unscaledTime > messageUntil) message.text = "";
            if (!show) return;
            var bag = Game.Player?.Backpack;
            string bagText = bag == null ? "Sin módulo de mochila" :
                $"Mochila: {bag.Used}/{bag.Capacity}\n" + string.Join(", ", bag.Items.Select(i => i.Name));
            hud.text = $"{PhaseName()} · {Game.Remaining:0}s\nSalud: {Game.Health:0} · Puntos: {Game.Score}\n{bagText}\n" +
                (Game.Shaking ? Game.Protected ? "PROTEGIDO" : "EXPUESTO · Ctrl + H en zona segura" : "Ctrl: agacharse · E: usar · F: linterna · Esc: pausa");
            hint.text = string.IsNullOrEmpty(Game.InteractionHint) ? Instruction() : Game.InteractionHint;
        }
        string PhaseName() => Game.State switch
        {
            GameState.Preparation => "PREPARACIÓN", GameState.Alarm => "ALARMA DE SIMULACRO",
            GameState.Earthquake => "SISMO", GameState.Evacuation => Game.Shaking ? "RÉPLICA" : "EVACUACIÓN", _ => ""
        };
        string Instruction() => Game.State switch
        {
            GameState.Preparation => "Recoge suministros útiles; encuentra la mesa y la escalera.",
            GameState.Alarm => "¡Alarma del simulacro! Ubícate en tu zona segura.",
            GameState.Earthquake => "Agáchate, cúbrete y sujétate: Ctrl + H, sin moverte.",
            GameState.Evacuation => Game.Shaking ? "¡Réplica! Protégete y espera." : "Camina hacia la salida y el punto de reunión exterior.", _ => ""
        };
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        { GameEvents.OnStateChanged -= State; GameEvents.OnMessage -= Message; }
    }
}
