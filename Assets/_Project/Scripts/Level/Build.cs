using UnityEngine;
using UnityEngine.UI;

namespace DisasterReady
{
    /// <summary>
    /// Ayudas para construir el escenario con primitivas, mallas low-poly y letreros.
    /// Todas las posiciones son de mundo (el padre siempre está en el origen).
    /// </summary>
    public static class Build
    {
        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ---------------------------------------------------------------------
        // Primitivas
        // ---------------------------------------------------------------------

        public static GameObject Box(string name, Vector3 center, Vector3 size, Material material, Transform parent, bool collider = true)
            => Primitive(PrimitiveType.Cube, name, center, size, material, parent, collider);

        public static GameObject Box(string name, Vector3 center, Vector3 size, Color color, Transform parent, bool collider = true)
            => Primitive(PrimitiveType.Cube, name, center, size, Palette.Solid(color), parent, collider);

        /// <summary>Caja definida por sus esquinas mínima y máxima (más cómodo para muros y pisos).</summary>
        public static GameObject Block(string name, Vector3 min, Vector3 max, Material material, Transform parent, bool collider = true)
            => Box(name, (min + max) * 0.5f, max - min, material, parent, collider);

        public static GameObject Cylinder(string name, Vector3 center, float diameter, float height, Material material, Transform parent, bool collider = true)
            => Primitive(PrimitiveType.Cylinder, name, center, new Vector3(diameter, height * 0.5f, diameter), material, parent, collider);

        public static GameObject Sphere(string name, Vector3 center, Vector3 size, Material material, Transform parent, bool collider = true)
            => Primitive(PrimitiveType.Sphere, name, center, size, material, parent, collider);

        public static GameObject Primitive(PrimitiveType type, string name, Vector3 center, Vector3 size, Material material,
                                           Transform parent, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (!collider) DestroyCollider(go);
            return go;
        }

        /// <summary>Crea una pieza como hija (coordenadas locales). Útil para objetos compuestos que se mueven juntos.</summary>
        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material,
                                      bool collider = false, Vector3? localEuler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = parent.name + "_pieza";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            if (localEuler.HasValue) go.transform.localRotation = Quaternion.Euler(localEuler.Value);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) DestroyCollider(go);
            return go;
        }

        public static void DestroyCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null) return;
            col.enabled = false;
            Object.Destroy(col);
        }

        /// <summary>Objeto vacío que agrupa piezas.</summary>
        public static Transform Group(string name, Vector3 position, Transform parent, float yaw = 0f)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return go.transform;
        }

        // ---------------------------------------------------------------------
        // Mallas low-poly
        // ---------------------------------------------------------------------

        /// <summary>Cono facetado (montañas, copas de árbol, conos de tránsito).</summary>
        public static GameObject Cone(string name, Vector3 basePosition, float radius, float height, int sides, Material material,
                                      Transform parent, float jitter = 0f, int seed = 0)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = basePosition;

            var rng = new System.Random(seed);
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float r = radius * (1f + (float)(rng.NextDouble() * 2 - 1) * jitter);
                ring[i] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            }
            Vector3 apex = new Vector3(0f, height, 0f);

            // Vértices duplicados por triángulo = sombreado plano (look low-poly)
            var vertices = new Vector3[sides * 3];
            var triangles = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                vertices[i * 3] = ring[i];
                vertices[i * 3 + 1] = apex;
                vertices[i * 3 + 2] = ring[(i + 1) % sides];
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        // ---------------------------------------------------------------------
        // Letreros (Canvas en el mundo: respetan la profundidad, no se ven a través de paredes)
        // ---------------------------------------------------------------------

        // =====================================================================
        // LETREROS: Canvas en el mundo (respetan la profundidad: no se ven a través de paredes).
        // Alta resolución (1000 px por metro) y texto que se ajusta solo al tamaño del letrero.
        // =====================================================================
        const float PixelsPerMeter = 1000f;
        static Sprite circleSprite, triangleSprite;

        static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null) return circleSprite;
                const int size = 64;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r))));
                t.SetPixels(px);
                t.Apply();
                circleSprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return circleSprite;
            }
        }

        /// <summary>Triángulo que apunta hacia arriba (puntas de flecha).</summary>
        static Sprite TriangleSprite
        {
            get
            {
                if (triangleSprite != null) return triangleSprite;
                const int size = 64;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float v = (y + 0.5f) / size;              // 0 abajo, 1 arriba
                        float half = (1f - v) * 0.5f;             // ancho a esa altura
                        float d = Mathf.Abs((x + 0.5f) / size - 0.5f);
                        float a = Mathf.Clamp01((half - d) * size);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                t.SetPixels(px);
                t.Apply();
                triangleSprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return triangleSprite;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSprites()
        {
            circleSprite = null;
            triangleSprite = null;
            font = null;
        }

        /// <summary>Crea el lienzo de un letrero. "facing" = hacia dónde mira (desde la pared hacia quien lo lee).</summary>
        public static RectTransform SignCanvas(string name, Vector3 position, Vector3 facing, Vector2 sizeMeters, Transform parent)
        {
            var go = new GameObject("Letrero " + name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-facing.normalized, Vector3.up));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 3f;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMeters * PixelsPerMeter;
            rt.localScale = Vector3.one / PixelsPerMeter;
            return rt;
        }

        /// <summary>Rectángulo dentro de un letrero (coordenadas normalizadas 0..1).</summary>
        public static Image SignRect(RectTransform parent, Color color, Vector2 min, Vector2 max, Sprite sprite = null, float rotation = 0f)
        {
            var go = new GameObject("Forma", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Texto que se ajusta al área indicada (coordenadas normalizadas 0..1).</summary>
        public static Text SignText(RectTransform parent, string text, Color color, Vector2 min, Vector2 max, int maxSize = 400,
                                    TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.color = color;
            t.fontStyle = FontStyle.Bold;
            t.alignment = alignment;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 8;
            t.resizeTextMaxSize = maxSize;
            t.fontSize = maxSize;
            t.lineSpacing = 0.9f;
            return t;
        }

        /// <summary>Fondo con borde blanco, como las señales reglamentarias.</summary>
        static void SignPlate(RectTransform rt, Color background, Color border, float borderFraction)
        {
            SignRect(rt, border, Vector2.zero, Vector2.one);
            float bx = borderFraction, by = borderFraction * rt.sizeDelta.x / Mathf.Max(1f, rt.sizeDelta.y);
            SignRect(rt, background, new Vector2(bx, by), new Vector2(1f - bx, 1f - by));
        }

        /// <summary>Marco físico detrás del letrero (le da volumen).</summary>
        static void SignBacking(Vector3 position, Vector3 facing, Vector2 size, Transform parent)
        {
            var box = Box("Placa", position - facing.normalized * 0.012f, new Vector3(size.x + 0.02f, size.y + 0.02f, 0.02f),
                          Palette.Solid(new Color(0.85f, 0.86f, 0.84f), 0.4f), parent, false);
            box.transform.rotation = Quaternion.LookRotation(-facing.normalized, Vector3.up);
        }

        /// <summary>Letrero simple de color con texto (GAS, LUZ, RUTA BLOQUEADA...).</summary>
        public static RectTransform Sign(string text, Vector3 position, Vector3 facing, Vector2 sizeMeters, Color background, Color textColor,
                                         int fontSize, Transform parent)
        {
            bool plate = background.a > 0.01f;
            if (plate) SignBacking(position, facing, sizeMeters, parent);
            var rt = SignCanvas(text.Replace("\n", " "), position + facing.normalized * 0.002f, facing, sizeMeters, parent);
            if (plate) SignPlate(rt, background, Color.white, 0.03f);
            SignText(rt, text, textColor, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), fontSize * 6);
            return rt;
        }

        /// <summary>Flecha dibujada (0° = derecha, 90° = arriba, -90° = abajo, 180° = izquierda).</summary>
        static void Arrow(RectTransform parent, Vector2 center, float size, float angle, Color color)
        {
            var go = new GameObject("Flecha", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = center;
            rt.sizeDelta = new Vector2(size, size);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            SignRect(rt, color, new Vector2(0.38f, 0.05f), new Vector2(0.62f, 0.6f));
            SignRect(rt, color, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 1f), TriangleSprite);
        }

        /// <summary>Pictograma de persona corriendo (señal de evacuación).</summary>
        static void RunningPerson(RectTransform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject("Pictograma", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            void Limb(float cx, float cy, float w, float h, float rot) =>
                SignRect(rt, color, new Vector2(cx - w * 0.5f, cy - h * 0.5f), new Vector2(cx + w * 0.5f, cy + h * 0.5f), null, rot);
            SignRect(rt, color, new Vector2(0.56f, 0.76f), new Vector2(0.76f, 0.96f), CircleSprite);   // cabeza
            Limb(0.52f, 0.55f, 0.13f, 0.36f, -22f);   // torso
            Limb(0.68f, 0.6f, 0.09f, 0.3f, -70f);     // brazo adelante
            Limb(0.34f, 0.6f, 0.09f, 0.28f, 55f);     // brazo atrás
            Limb(0.62f, 0.24f, 0.1f, 0.38f, 30f);     // pierna adelante
            Limb(0.36f, 0.24f, 0.1f, 0.38f, -40f);    // pierna atrás
        }

        static float ArrowAngle(string arrow)
        {
            switch (arrow)
            {
                case "←": return 180f;
                case "↓": return -90f;
                case "↑": return 90f;
                default: return 0f;
            }
        }

        /// <summary>Señal verde de evacuación: pictograma + texto + flecha.</summary>
        public static void ExitSign(Vector3 position, Vector3 facing, string arrow, Transform parent, string text = "SALIDA")
        {
            var size = new Vector2(0.62f, 0.25f);
            SignBacking(position, facing, size, parent);
            var rt = SignCanvas(text, position + facing.normalized * 0.002f, facing, size, parent);
            SignPlate(rt, Palette.SafeGreen, Color.white, 0.025f);
            RunningPerson(rt, new Vector2(0.06f, 0.14f), new Vector2(0.27f, 0.86f), Color.white);
            SignText(rt, text, Color.white, new Vector2(0.28f, 0.18f), new Vector2(0.76f, 0.82f), 160);
            if (!string.IsNullOrEmpty(arrow)) Arrow(rt, new Vector2(0.87f, 0.5f), 130f, ArrowAngle(arrow), Color.white);
        }

        /// <summary>Señal "ZONA SEGURA EN CASO DE SISMOS", como las que se usan en el Perú.</summary>
        public static void SafeSign(Vector3 position, Vector3 facing, Transform parent)
        {
            var size = new Vector2(0.5f, 0.42f);
            SignBacking(position, facing, size, parent);
            var rt = SignCanvas("Zona segura", position + facing.normalized * 0.002f, facing, size, parent);
            SignPlate(rt, Palette.SafeGreen, Color.white, 0.035f);
            SignRect(rt, Color.white, new Vector2(0.36f, 0.5f), new Vector2(0.64f, 0.86f), CircleSprite);
            SignRect(rt, Palette.SafeGreen, new Vector2(0.385f, 0.53f), new Vector2(0.615f, 0.83f), CircleSprite);
            SignText(rt, "S", Color.white, new Vector2(0.36f, 0.52f), new Vector2(0.64f, 0.85f), 200);
            SignText(rt, "ZONA SEGURA", Color.white, new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.47f), 120);
            SignText(rt, "EN CASO DE SISMOS", Color.white, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.25f), 70);
        }

        /// <summary>Señal de PUNTO DE REUNIÓN: cuatro flechas hacia el centro y personas.</summary>
        public static void MeetingPointSign(Vector3 position, Vector3 facing, Transform parent)
        {
            var size = new Vector2(1.1f, 1.1f);
            SignBacking(position, facing, size, parent);
            var rt = SignCanvas("Punto de reunión", position + facing.normalized * 0.002f, facing, size, parent);
            SignPlate(rt, Palette.SafeGreen, Color.white, 0.03f);
            Vector2 c = new Vector2(0.5f, 0.6f);
            Arrow(rt, c + new Vector2(-0.3f, 0f), 140f, 0f, Color.white);
            Arrow(rt, c + new Vector2(0.3f, 0f), 140f, 180f, Color.white);
            Arrow(rt, c + new Vector2(0f, 0.26f), 140f, -90f, Color.white);
            Arrow(rt, c + new Vector2(0f, -0.26f), 140f, 90f, Color.white);
            for (int i = -1; i <= 1; i++)
            {
                float x = c.x + i * 0.07f;
                SignRect(rt, Color.white, new Vector2(x - 0.025f, c.y + 0.02f), new Vector2(x + 0.025f, c.y + 0.07f), CircleSprite);
                SignRect(rt, Color.white, new Vector2(x - 0.028f, c.y - 0.08f), new Vector2(x + 0.028f, c.y + 0.015f));
            }
            SignText(rt, "PUNTO DE REUNIÓN", Color.white, new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.25f), 140);
        }
    }
}
