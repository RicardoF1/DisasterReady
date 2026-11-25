using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Materiales y texturas procedurales del estilo low-poly: madera, cerámica,
    /// manta andina, ladrillo, teja, césped. Todo se genera por código (sin assets externos),
    /// y luego se puede reemplazar por modelos y texturas de Blender o del Asset Store.
    /// </summary>
    public static class Palette
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly List<Material> powered = new List<Material>();   // se apagan con el corte de luz
        static readonly Dictionary<Material, Color> poweredColors = new Dictionary<Material, Color>();
        static Shader lit;
        static Shader unlit;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        static readonly int Metallic = Shader.PropertyToID("_Metallic");

        // ---------- Colores base del estilo ----------
        public static readonly Color Cream = new Color(0.94f, 0.9f, 0.82f);
        public static readonly Color WarmWhite = new Color(0.97f, 0.95f, 0.9f);
        public static readonly Color Terracotta = new Color(0.8f, 0.45f, 0.32f);
        public static readonly Color Sage = new Color(0.62f, 0.72f, 0.6f);
        public static readonly Color SkyBlue = new Color(0.55f, 0.72f, 0.85f);
        public static readonly Color Oak = new Color(0.62f, 0.44f, 0.28f);
        public static readonly Color DarkWood = new Color(0.36f, 0.24f, 0.16f);
        public static readonly Color Charcoal = new Color(0.16f, 0.17f, 0.19f);
        public static readonly Color Steel = new Color(0.7f, 0.72f, 0.75f);
        public static readonly Color Teal = new Color(0.12f, 0.45f, 0.48f);
        public static readonly Color Mustard = new Color(0.9f, 0.68f, 0.2f);
        public static readonly Color SafeGreen = new Color(0.05f, 0.62f, 0.3f);
        public static readonly Color Danger = new Color(0.85f, 0.15f, 0.12f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            textures.Clear();
            powered.Clear();
            poweredColors.Clear();
            lit = null;
            unlit = null;
        }

        static Shader Lit
        {
            get
            {
                if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = FromVariantMaterial("M_Lit_Emision");   // respaldo para builds (Web)
                if (lit == null) lit = Shader.Find("Standard");
                return lit;
            }
        }

        /// <summary>
        /// Los materiales de Art/Resources/ShaderVariants obligan a Unity a incluir en el build
        /// las variantes de shader que este juego usa (emisión, transparencia, partículas).
        /// </summary>
        static Shader FromVariantMaterial(string name)
        {
            var m = Resources.Load<Material>("ShaderVariants/" + name);
            return m != null ? m.shader : null;
        }

        // ---------------------------------------------------------------------
        // Materiales
        // ---------------------------------------------------------------------

        /// <summary>Material opaco de color liso (compartido y cacheado).</summary>
        public static Material Solid(Color color, float smoothness = 0.2f, float metallic = 0f)
        {
            string key = $"s{color}{smoothness}{metallic}";
            if (cache.TryGetValue(key, out var m)) return m;
            m = NewLit(color, smoothness, metallic);
            cache[key] = m;
            return m;
        }

        /// <summary>Material con textura procedural repetida.</summary>
        public static Material Textured(string texture, Color tint, Vector2 tiling, float smoothness = 0.25f)
        {
            string key = $"t{texture}{tint}{tiling}{smoothness}";
            if (cache.TryGetValue(key, out var m)) return m;
            m = NewLit(tint, smoothness, 0f);
            m.SetTexture(BaseMap, GetTexture(texture));
            m.SetTextureScale(BaseMap, tiling);
            m.mainTexture = GetTexture(texture);
            m.mainTextureScale = tiling;
            cache[key] = m;
            return m;
        }

        /// <summary>Material que brilla (focos, letreros, pantallas). Si powered, se apaga con el corte de luz.</summary>
        public static Material Glow(Color color, float intensity, bool powered = false)
        {
            string key = $"g{color}{intensity}{powered}";
            if (cache.TryGetValue(key, out var m)) return m;
            m = NewLit(color, 0.5f, 0f);
            Color emission = color * intensity;
            m.SetColor(Emission, emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (powered)
            {
                Palette.powered.Add(m);
                poweredColors[m] = emission;
            }
            cache[key] = m;
            return m;
        }

        /// <summary>Vidrio semitransparente.</summary>
        public static Material Glass(Color color, float alpha = 0.3f)
        {
            string key = $"glass{color}{alpha}";
            if (cache.TryGetValue(key, out var m)) return m;
            var c = color;
            c.a = alpha;
            m = NewLit(c, 0.95f, 0f);
            MakeTransparent(m);
            cache[key] = m;
            return m;
        }

        static Material NewLit(Color color, float smoothness, float metallic)
        {
            var m = new Material(Lit);
            m.SetColor(BaseColor, color);
            m.color = color;
            m.SetFloat(Smoothness, smoothness);
            m.SetFloat(Metallic, metallic);
            // Emisión activada (negra) para poder resaltar objetos interactuables.
            m.EnableKeyword("_EMISSION");
            m.SetColor(Emission, Color.black);
            return m;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>Material sin iluminación (partículas de polvo).</summary>
        public static Material Particle(Color color)
        {
            string key = $"p{color}";
            if (cache.TryGetValue(key, out var m)) return m;
            if (unlit == null) unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (unlit == null) unlit = FromVariantMaterial("M_Particulas");
            if (unlit == null) unlit = Shader.Find("Sprites/Default");
            m = new Material(unlit);
            m.SetTexture(BaseMap, GetTexture("soft_dot"));
            m.mainTexture = GetTexture("soft_dot");
            m.SetColor(BaseColor, color);
            m.color = color;
            MakeTransparent(m);
            cache[key] = m;
            return m;
        }

        /// <summary>Enciende o apaga todos los materiales que dependen de la electricidad.</summary>
        public static void SetPower(bool on, float flicker = 1f)
        {
            foreach (var m in powered)
            {
                if (m == null) continue;
                m.SetColor(Emission, on ? poweredColors[m] * flicker : Color.black);
            }
        }

        // ---------------------------------------------------------------------
        // Texturas procedurales
        // ---------------------------------------------------------------------

        public static Texture2D GetTexture(string name)
        {
            if (textures.TryGetValue(name, out var t) && t != null) return t;
            switch (name)
            {
                case "wood": t = WoodPlanks(); break;
                case "tiles": t = Tiles(new Color(0.93f, 0.92f, 0.88f), new Color(0.75f, 0.73f, 0.7f)); break;
                case "tiles_blue": t = Tiles(new Color(0.55f, 0.75f, 0.85f), new Color(0.9f, 0.92f, 0.93f)); break;
                case "manta": t = Manta(); break;
                case "brick": t = Brick(); break;
                case "roof": t = RoofTiles(); break;
                case "grass": t = Noise(new Color(0.36f, 0.58f, 0.27f), new Color(0.45f, 0.68f, 0.3f), 0.09f); break;
                case "asphalt": t = Noise(new Color(0.22f, 0.22f, 0.24f), new Color(0.3f, 0.3f, 0.32f), 0.35f); break;
                case "plaster": t = Noise(new Color(0.93f, 0.93f, 0.93f), new Color(1f, 1f, 1f), 0.12f); break;
                case "concrete": t = Noise(new Color(0.62f, 0.62f, 0.6f), new Color(0.74f, 0.74f, 0.72f), 0.2f); break;
                case "soft_dot": t = SoftDot(); break;
                default: t = Noise(Color.gray, Color.white, 0.1f); break;
            }
            t.name = "Procedural_" + name;
            textures[name] = t;
            return t;
        }

        static Texture2D NewTexture(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 4;
            return t;
        }

        static Texture2D WoodPlanks()
        {
            const int size = 256;
            var t = NewTexture(size);
            var px = new Color[size * size];
            const int planks = 4;
            for (int y = 0; y < size; y++)
            {
                int plank = y * planks / size;
                float plankShift = plank * 71.3f;
                float tone = 0.85f + 0.15f * Mathf.PerlinNoise(plank * 3.7f, 0.5f);
                for (int x = 0; x < size; x++)
                {
                    int localY = y % (size / planks);
                    float grain = Mathf.PerlinNoise((x + plankShift) * 0.02f, y * 0.35f) * 0.25f;
                    float ring = Mathf.Sin((x + plankShift) * 0.08f + Mathf.PerlinNoise(x * 0.01f, y * 0.1f) * 6f) * 0.04f;
                    float v = tone * (0.78f + grain + ring);
                    // Junta entre tablas y corte en una posición distinta por tabla
                    int joint = (int)(plankShift * 2.3f) % size;
                    if (localY < 2 || Mathf.Abs(x - joint) < 2) v *= 0.55f;
                    px[y * size + x] = new Color(0.78f * v, 0.56f * v, 0.36f * v);
                }
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Texture2D Tiles(Color tile, Color grout)
        {
            const int size = 128;
            var t = NewTexture(size);
            var px = new Color[size * size];
            const int count = 4;
            int cell = size / count;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool line = x % cell < 2 || y % cell < 2;
                    float n = 0.95f + 0.05f * Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    px[y * size + x] = line ? grout : tile * n;
                }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        /// <summary>Patrón inspirado en las mantas andinas: franjas vivas con rombos.</summary>
        static Texture2D Manta()
        {
            const int size = 256;
            var t = NewTexture(size);
            var px = new Color[size * size];
            Color[] stripes =
            {
                new Color(0.85f, 0.1f, 0.25f), new Color(1f, 0.75f, 0.1f), new Color(0.1f, 0.55f, 0.35f),
                new Color(0.95f, 0.4f, 0.6f), new Color(0.15f, 0.3f, 0.65f), new Color(1f, 0.45f, 0.1f),
                new Color(0.55f, 0.1f, 0.45f), new Color(0.98f, 0.92f, 0.8f)
            };
            int band = size / stripes.Length;
            for (int y = 0; y < size; y++)
            {
                int i = y / band;
                Color c = stripes[i % stripes.Length];
                int local = y % band;
                for (int x = 0; x < size; x++)
                {
                    Color p = c;
                    // Rombos centrados en bandas alternas
                    if (i % 2 == 0)
                    {
                        int period = band;
                        int dx = Mathf.Abs((x % period) - period / 2);
                        int dy = Mathf.Abs(local - band / 2);
                        if (dx + dy < band / 3) p = stripes[(i + 3) % stripes.Length];
                        if (dx + dy < band / 7) p = stripes[(i + 5) % stripes.Length];
                    }
                    else if (local < 3 || local > band - 4) p = stripes[(i + 2) % stripes.Length];
                    // Textura del tejido
                    float weave = ((x + y) % 4 < 2) ? 0.93f : 1f;
                    px[y * size + x] = p * weave;
                }
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Texture2D Brick()
        {
            const int size = 128;
            var t = NewTexture(size);
            var px = new Color[size * size];
            const int rows = 8, cols = 4;
            int bh = size / rows, bw = size / cols;
            var mortar = new Color(0.72f, 0.68f, 0.6f);
            for (int y = 0; y < size; y++)
            {
                int row = y / bh;
                int offset = row % 2 == 0 ? 0 : bw / 2;
                for (int x = 0; x < size; x++)
                {
                    int bx = (x + offset) % bw;
                    int brick = ((x + offset) / bw) * 7 + row * 13;
                    bool line = y % bh < 2 || bx < 2;
                    float n = 0.8f + 0.2f * Mathf.PerlinNoise(brick * 1.7f, 0.3f) + 0.06f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    px[y * size + x] = line ? mortar : new Color(0.72f * n, 0.36f * n, 0.24f * n);
                }
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Texture2D RoofTiles()
        {
            const int size = 128;
            var t = NewTexture(size);
            var px = new Color[size * size];
            const int rows = 8;
            int rh = size / rows;
            for (int y = 0; y < size; y++)
            {
                float shade = 0.7f + 0.3f * (y % rh) / (float)rh;
                for (int x = 0; x < size; x++)
                {
                    float wave = 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(x * Mathf.PI / 16f));
                    float n = 0.9f + 0.1f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                    float v = shade * wave * n;
                    px[y * size + x] = new Color(0.72f * v, 0.3f * v, 0.2f * v);
                }
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Texture2D Noise(Color a, Color b, float scale)
        {
            const int size = 128;
            var t = NewTexture(size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * scale, y * scale) * 0.7f + Mathf.PerlinNoise(x * scale * 4f, y * scale * 4f) * 0.3f;
                    px[y * size + x] = Color.Lerp(a, b, n);
                }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        static Texture2D SoftDot()
        {
            const int size = 32;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f, dy = (y + 0.5f) / size - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    px[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
