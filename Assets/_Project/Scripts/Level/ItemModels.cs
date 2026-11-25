using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Modelos low-poly de cada suministro, hechos con primitivas.
    /// El origen de cada modelo es la base (apoyado sobre la superficie).
    /// Para reemplazarlos por modelos de Blender, basta con cambiar este archivo.
    /// </summary>
    public static class ItemModels
    {
        const PrimitiveType Cube = PrimitiveType.Cube;
        const PrimitiveType Cyl = PrimitiveType.Cylinder;
        const PrimitiveType Ball = PrimitiveType.Sphere;

        public static void Build(string id, Transform t)
        {
            switch (id)
            {
                case "agua": Water(t); break;
                case "botiquin": FirstAid(t); break;
                case "linterna": Flashlight(t); break;
                case "documentos": Documents(t); break;
                case "alimentos": Food(t); break;
                case "radio": Radio(t); break;
                case "silbato": Whistle(t); break;
                case "pilas": Batteries(t); break;
                case "abrigo": Coat(t); break;
                case "laptop": Laptop(t); break;
                case "consola": Console(t); break;
                case "adorno": Trophy(t); break;
                default: P(t, Cube, new Vector3(0f, 0.1f, 0f), Vector3.one * 0.2f, Color.magenta); break;
            }
        }

        static GameObject P(Transform t, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, float smooth = 0.35f, Vector3? euler = null)
            => DisasterReady.Build.Part(t, type, pos, scale, Palette.Solid(color, smooth), false, euler);

        static GameObject G(Transform t, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, float glow, Vector3? euler = null)
            => DisasterReady.Build.Part(t, type, pos, scale, Palette.Glow(color, glow), false, euler);

        // Bidón de agua azul con asa
        static void Water(Transform t)
        {
            var blue = new Color(0.35f, 0.65f, 0.95f);
            P(t, Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.24f, 0.38f, 0.17f), blue, 0.8f);
            P(t, Cyl, new Vector3(0.05f, 0.42f, 0f), new Vector3(0.07f, 0.03f, 0.07f), new Color(0.1f, 0.3f, 0.75f));
            P(t, Cube, new Vector3(-0.04f, 0.41f, 0f), new Vector3(0.1f, 0.03f, 0.05f), new Color(0.1f, 0.3f, 0.75f));
            P(t, Cube, new Vector3(0f, 0.2f, -0.087f), new Vector3(0.18f, 0.14f, 0.005f), Color.white);
            P(t, Cube, new Vector3(0f, 0.2f, -0.09f), new Vector3(0.1f, 0.03f, 0.004f), blue);
        }

        // Maletín rojo con cruz blanca
        static void FirstAid(Transform t)
        {
            var red = new Color(0.88f, 0.15f, 0.15f);
            P(t, Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0.34f, 0.22f, 0.12f), red, 0.5f);
            foreach (float z in new[] { -0.062f, 0.062f })
            {
                P(t, Cube, new Vector3(0f, 0.11f, z), new Vector3(0.05f, 0.14f, 0.004f), Color.white);
                P(t, Cube, new Vector3(0f, 0.11f, z), new Vector3(0.14f, 0.05f, 0.004f), Color.white);
            }
            P(t, Cube, new Vector3(0f, 0.245f, 0f), new Vector3(0.12f, 0.025f, 0.03f), new Color(0.2f, 0.2f, 0.2f));
        }

        // Linterna amarilla acostada con lente luminoso
        static void Flashlight(Transform t)
        {
            var yellow = new Color(1f, 0.78f, 0.1f);
            P(t, Cyl, new Vector3(0f, 0.035f, 0f), new Vector3(0.06f, 0.1f, 0.06f), new Color(0.12f, 0.12f, 0.12f), 0.6f, new Vector3(0f, 0f, 90f));
            P(t, Cyl, new Vector3(0.12f, 0.045f, 0f), new Vector3(0.09f, 0.035f, 0.09f), yellow, 0.6f, new Vector3(0f, 0f, 90f));
            G(t, Cyl, new Vector3(0.155f, 0.045f, 0f), new Vector3(0.07f, 0.004f, 0.07f), new Color(1f, 0.95f, 0.7f), 2.5f, new Vector3(0f, 0f, 90f));
            P(t, Cube, new Vector3(-0.02f, 0.07f, 0f), new Vector3(0.03f, 0.015f, 0.02f), yellow);
        }

        // Folder dentro de una bolsa plástica
        static void Documents(Transform t)
        {
            P(t, Cube, new Vector3(0f, 0.012f, 0f), new Vector3(0.32f, 0.024f, 0.24f), new Color(0.55f, 0.75f, 1f), 0.9f);
            P(t, Cube, new Vector3(0f, 0.026f, 0f), new Vector3(0.26f, 0.006f, 0.19f), new Color(0.98f, 0.98f, 0.95f));
            P(t, Cube, new Vector3(-0.05f, 0.03f, 0.03f), new Vector3(0.1f, 0.004f, 0.07f), new Color(0.75f, 0.2f, 0.25f));
            P(t, Cube, new Vector3(0f, 0.03f, -0.105f), new Vector3(0.32f, 0.008f, 0.02f), new Color(0.2f, 0.4f, 0.9f));
        }

        // Tres latas de conserva y un paquete de galletas
        static void Food(Transform t)
        {
            Color[] labels = { new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.6f, 0.3f), new Color(0.95f, 0.7f, 0.1f) };
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(-0.1f + i * 0.1f, 0.055f, 0f);
                P(t, Cyl, p, new Vector3(0.08f, 0.055f, 0.08f), Palette.Steel, 0.8f);
                P(t, Cyl, p, new Vector3(0.083f, 0.035f, 0.083f), labels[i], 0.3f);
            }
            P(t, Cube, new Vector3(0f, 0.03f, 0.11f), new Vector3(0.26f, 0.06f, 0.08f), new Color(0.95f, 0.85f, 0.55f));
            P(t, Cube, new Vector3(0f, 0.03f, 0.069f), new Vector3(0.18f, 0.03f, 0.004f), new Color(0.75f, 0.35f, 0.1f));
        }

        // Radio portátil con parlante y antena
        static void Radio(Transform t)
        {
            var body = new Color(0.4f, 0.3f, 0.22f);
            P(t, Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.28f, 0.16f, 0.09f), body, 0.4f);
            P(t, Cyl, new Vector3(-0.06f, 0.08f, -0.046f), new Vector3(0.11f, 0.004f, 0.11f), new Color(0.15f, 0.15f, 0.15f), 0.2f, new Vector3(90f, 0f, 0f));
            G(t, Cube, new Vector3(0.07f, 0.11f, -0.046f), new Vector3(0.1f, 0.035f, 0.004f), new Color(0.6f, 1f, 0.7f), 1.2f);
            P(t, Cyl, new Vector3(0.07f, 0.05f, -0.047f), new Vector3(0.03f, 0.005f, 0.03f), Palette.Steel, 0.6f, new Vector3(90f, 0f, 0f));
            P(t, Cyl, new Vector3(0.11f, 0.3f, 0.02f), new Vector3(0.008f, 0.14f, 0.008f), Palette.Steel, 0.8f, new Vector3(0f, 0f, -12f));
            P(t, Cube, new Vector3(0f, 0.175f, 0f), new Vector3(0.2f, 0.02f, 0.02f), new Color(0.15f, 0.15f, 0.15f));
        }

        // Silbato naranja con cordón
        static void Whistle(Transform t)
        {
            var orange = new Color(1f, 0.45f, 0.05f);
            P(t, Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.09f, 0.04f, 0.035f), orange, 0.7f);
            P(t, Cyl, new Vector3(0.035f, 0.035f, 0f), new Vector3(0.05f, 0.017f, 0.05f), orange, 0.7f, new Vector3(90f, 0f, 0f));
            P(t, Cyl, new Vector3(-0.08f, 0.005f, 0.02f), new Vector3(0.11f, 0.004f, 0.11f), new Color(0.1f, 0.25f, 0.6f));
        }

        // Paquete de pilas
        static void Batteries(Transform t)
        {
            P(t, Cube, new Vector3(0f, 0.004f, 0f), new Vector3(0.14f, 0.008f, 0.16f), new Color(0.95f, 0.85f, 0.2f));
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(-0.045f + i * 0.03f, 0.02f, 0f);
                P(t, Cyl, p, new Vector3(0.026f, 0.05f, 0.026f), new Color(0.75f, 0.45f, 0.2f), 0.7f, new Vector3(90f, 0f, 0f));
                P(t, Cyl, p + new Vector3(0f, 0f, 0.035f), new Vector3(0.027f, 0.016f, 0.027f), Palette.Charcoal, 0.7f, new Vector3(90f, 0f, 0f));
            }
        }

        // Casaca doblada
        static void Coat(Transform t)
        {
            var green = new Color(0.25f, 0.45f, 0.3f);
            P(t, Cube, new Vector3(0f, 0.04f, 0f), new Vector3(0.46f, 0.08f, 0.34f), green, 0.15f);
            P(t, Cube, new Vector3(0f, 0.09f, 0.02f), new Vector3(0.4f, 0.05f, 0.28f), green * 0.9f, 0.15f);
            P(t, Cube, new Vector3(0f, 0.118f, -0.06f), new Vector3(0.16f, 0.008f, 0.08f), new Color(0.6f, 0.4f, 0.2f));
            P(t, Cube, new Vector3(0f, 0.12f, 0.03f), new Vector3(0.012f, 0.004f, 0.25f), Palette.Steel, 0.8f);
        }

        // Laptop abierta con pantalla encendida
        static void Laptop(Transform t)
        {
            var gray = new Color(0.55f, 0.56f, 0.6f);
            P(t, Cube, new Vector3(0f, 0.01f, 0f), new Vector3(0.34f, 0.02f, 0.24f), gray, 0.7f);
            P(t, Cube, new Vector3(0f, 0.021f, 0.02f), new Vector3(0.3f, 0.003f, 0.12f), new Color(0.15f, 0.15f, 0.17f));
            P(t, Cube, new Vector3(0f, 0.13f, 0.13f), new Vector3(0.34f, 0.23f, 0.012f), gray, 0.7f, new Vector3(-15f, 0f, 0f));
            G(t, Cube, new Vector3(0f, 0.13f, 0.122f), new Vector3(0.3f, 0.19f, 0.004f), new Color(0.35f, 0.6f, 1f), 1.4f, new Vector3(-15f, 0f, 0f));
        }

        // Consola con mando
        static void Console(Transform t)
        {
            var black = new Color(0.1f, 0.1f, 0.12f);
            P(t, Cube, new Vector3(0f, 0.035f, 0f), new Vector3(0.3f, 0.07f, 0.22f), black, 0.8f);
            G(t, Cube, new Vector3(0.1f, 0.05f, -0.111f), new Vector3(0.05f, 0.006f, 0.004f), new Color(0.2f, 0.6f, 1f), 3f);
            P(t, Cube, new Vector3(0.05f, 0.08f, -0.03f), new Vector3(0.14f, 0.03f, 0.08f), new Color(0.85f, 0.85f, 0.9f), 0.6f);
            P(t, Ball, new Vector3(0.0f, 0.08f, -0.05f), new Vector3(0.05f, 0.03f, 0.05f), new Color(0.85f, 0.85f, 0.9f), 0.6f);
            P(t, Ball, new Vector3(0.1f, 0.08f, -0.05f), new Vector3(0.05f, 0.03f, 0.05f), new Color(0.85f, 0.85f, 0.9f), 0.6f);
        }

        // Trofeo dorado
        static void Trophy(Transform t)
        {
            var gold = new Color(1f, 0.78f, 0.25f);
            P(t, Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.06f, 0.12f), new Color(0.2f, 0.15f, 0.12f), 0.6f);
            P(t, Cyl, new Vector3(0f, 0.1f, 0f), new Vector3(0.025f, 0.04f, 0.025f), gold, 0.9f);
            P(t, Cyl, new Vector3(0f, 0.2f, 0f), new Vector3(0.12f, 0.06f, 0.12f), gold, 0.9f);
            P(t, Ball, new Vector3(-0.07f, 0.21f, 0f), new Vector3(0.04f, 0.07f, 0.02f), gold, 0.9f);
            P(t, Ball, new Vector3(0.07f, 0.21f, 0f), new Vector3(0.04f, 0.07f, 0.02f), gold, 0.9f);
        }
    }
}
