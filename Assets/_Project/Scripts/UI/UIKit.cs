using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DisasterReady
{
    /// <summary>
    /// Kit de interfaz creado por código: lienzos, paneles redondeados, textos,
    /// botones animados con sonido, deslizadores y degradados.
    /// </summary>
    public static class UIKit
    {
        // Paleta de la interfaz
        public static readonly Color Ink = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        public static readonly Color InkSoft = new Color(0.1f, 0.13f, 0.19f, 0.78f);
        public static readonly Color Accent = new Color(1f, 0.56f, 0.12f);
        public static readonly Color Good = new Color(0.25f, 0.85f, 0.45f);
        public static readonly Color Bad = new Color(1f, 0.3f, 0.28f);
        public static readonly Color Warn = new Color(1f, 0.78f, 0.2f);
        public static readonly Color Info = new Color(0.45f, 0.75f, 1f);
        public static readonly Color TextMain = new Color(0.96f, 0.97f, 1f);
        public static readonly Color TextDim = new Color(0.7f, 0.76f, 0.85f);

        static Font font;
        static Sprite rounded, circle, ring, gradientH, gradientV, edgeGlow;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            rounded = null; circle = null; ring = null; gradientH = null; gradientV = null; edgeGlow = null;
        }

        // ---------------------------------------------------------------------
        // Sprites procedurales
        // ---------------------------------------------------------------------

        /// <summary>Rectángulo redondeado (9-slice).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64, radius = 20;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                        float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                    }
                t.SetPixels(px);
                t.Apply();
                rounded = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                                        new Vector4(radius, radius, radius, radius));
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int size = 128;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d));
                    }
                t.SetPixels(px);
                t.Apply();
                circle = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return circle;
            }
        }

        /// <summary>Anillo (mira al enfocar un objeto).</summary>
        public static Sprite Ring
        {
            get
            {
                if (ring != null) return ring;
                const int size = 128;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        float a = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - (r - 12f));
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                t.SetPixels(px);
                t.Apply();
                ring = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return ring;
            }
        }

        /// <summary>Degradado horizontal: opaco a la izquierda, transparente a la derecha.</summary>
        public static Sprite GradientH
        {
            get
            {
                if (gradientH != null) return gradientH;
                const int w = 256;
                var t = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < w; x++)
                {
                    float a = 1f - x / (float)(w - 1);
                    t.SetPixel(x, 0, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                }
                t.Apply();
                gradientH = Sprite.Create(t, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
                return gradientH;
            }
        }

        /// <summary>Degradado vertical: opaco abajo, transparente arriba.</summary>
        public static Sprite GradientV
        {
            get
            {
                if (gradientV != null) return gradientV;
                const int h = 256;
                var t = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < h; y++)
                {
                    float a = 1f - y / (float)(h - 1);
                    t.SetPixel(0, y, new Color(1f, 1f, 1f, a * a));
                }
                t.Apply();
                gradientV = Sprite.Create(t, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
                return gradientV;
            }
        }

        /// <summary>Brillo en los bordes de la pantalla (alertas, daño).</summary>
        public static Sprite EdgeGlow
        {
            get
            {
                if (edgeGlow != null) return edgeGlow;
                const int size = 128;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Abs((x + 0.5f) / size - 0.5f) * 2f;
                        float dy = Mathf.Abs((y + 0.5f) / size - 0.5f) * 2f;
                        float d = Mathf.Max(dx, dy);
                        float a = Mathf.Clamp01((d - 0.55f) / 0.45f);
                        px[y * size + x] = new Color(1f, 1f, 1f, a * a);
                    }
                t.SetPixels(px);
                t.Apply();
                edgeGlow = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return edgeGlow;
            }
        }

        // ---------------------------------------------------------------------
        // Construcción
        // ---------------------------------------------------------------------

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Ubica un elemento con ancla y pivote en el mismo punto (0..1).</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool roundedCorners = true, float cornerScale = 1f)
        {
            var rt = Node(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (roundedCorners)
            {
                image.sprite = Rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.1f, cornerScale);
            }
            return image;
        }

        public static Image Picture(Transform parent, string name, Sprite sprite, Color color)
        {
            var rt = Node(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
                                 TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, bool shadow = true)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.fontStyle = style;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.05f;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.55f);
                s.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        public static Button Button(Transform parent, string name, string text, UnityAction onClick, Color color, Vector2 size, int fontSize = 30)
        {
            var image = Panel(parent, name, color);
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = Label(image.transform, "Texto", text, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Stretch();

            image.gameObject.AddComponent<UIButtonFx>();
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        public static Slider Slider(Transform parent, string label, float value, float min, float max, UnityAction<float> onChange, float width = 560f)
        {
            var row = Node("Opción " + label, parent);
            row.sizeDelta = new Vector2(width, 64f);
            Label(row, "Etiqueta", label, 26, TextMain).rectTransform.Place(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(220f, 50f));

            var track = Panel(row, "Barra", new Color(1f, 1f, 1f, 0.12f), true, 0.5f);
            track.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(width - 250f, 14f));

            var fillArea = Node("Relleno", track.transform);
            fillArea.Stretch();
            var fill = Panel(fillArea, "Color", Accent, true, 0.5f);
            fill.rectTransform.Stretch();

            var handleArea = Node("Área", track.transform);
            handleArea.Stretch();
            var handle = Picture(handleArea, "Perilla", Circle, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(34f, 34f);
            handle.raycastTarget = true;

            var slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>();
            track.raycastTarget = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        /// <summary>Pinta un texto con color usando rich text.</summary>
        public static string Colorize(string text, Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
    }

    /// <summary>Animación y sonido de los botones al pasar el mouse.</summary>
    public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        float target = 1f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            target = 1.06f;
            GameEvents.PlaySound(SoundId.Hover, 0.5f);
        }

        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        public void OnPointerClick(PointerEventData eventData) => GameEvents.PlaySound(SoundId.Click);

        void OnDisable()
        {
            target = 1f;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, Time.unscaledDeltaTime * 14f);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
