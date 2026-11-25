using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Exterior: barrio de Huancayo con casas de ladrillo, eucaliptos, calle, punto de reunión
    /// y cordillera de fondo (con nevados). También ajusta el sol, la luz ambiental y la niebla.
    /// </summary>
    public static class EnvironmentBuilder
    {
        public static void Build(Transform root, bool gameScene)
        {
            var env = DisasterReady.Build.Group("Exterior", Vector3.zero, root);
            Lighting();
            Ground(env);
            MeetingPoint(env);
            Street(env);
            Neighbors(env);
            Trees(env);
            Mountains(env);
        }

        // ---------------------------------------------------------------------
        static void Lighting()
        {
            Light sun = null;
            foreach (var rootObject in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var light in rootObject.GetComponentsInChildren<Light>())
                    if (light.type == LightType.Directional) { sun = light; break; }
                if (sun != null) break;
            }
            if (sun == null)
            {
                sun = new GameObject("Sol").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            // Tarde soleada: el sol desde el suroeste ilumina la fachada principal
            sun.transform.rotation = Quaternion.Euler(38f, 32f, 0f);
            sun.color = new Color(1f, 0.9f, 0.76f);
            sun.intensity = 1.7f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.17f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0045f;
            RenderSettings.fogColor = new Color(0.74f, 0.8f, 0.88f);
        }

        static void Ground(Transform t)
        {
            DisasterReady.Build.Block("Terreno", new Vector3(-80f, -0.4f, -80f), new Vector3(92f, -0.05f, 90f),
                Palette.Textured("grass", Color.white, new Vector2(40f, 40f), 0.05f), t);

            // Bordes invisibles del área jugable
            Invisible(t, new Vector3(-22f, -1f, -30f), new Vector3(-21f, 6f, 30f));
            Invisible(t, new Vector3(33f, -1f, -30f), new Vector3(34f, 6f, 30f));
            Invisible(t, new Vector3(-22f, -1f, 29f), new Vector3(34f, 6f, 30f));
            Invisible(t, new Vector3(-22f, -1f, -31f), new Vector3(34f, 6f, -30f));

            // Camino de ingreso desde la puerta
            var concrete = Palette.Textured("concrete", Color.white, new Vector2(1f, 6f), 0.1f);
            DisasterReady.Build.Block("Camino", new Vector3(3.1f, -0.06f, -9.6f), new Vector3(4.5f, -0.03f, -0.9f), concrete, t);
            // Jardinera frente a la casa
            var flowers = new[] { new Color(0.95f, 0.3f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.6f, 0.35f, 0.85f), Color.white };
            for (int i = 0; i < 18; i++)
            {
                float x = i < 9 ? 0.4f + i * 0.28f : 5.0f + (i - 9) * 0.75f;
                var c = flowers[i % flowers.Length];
                DisasterReady.Build.Sphere("Flor", new Vector3(x, 0.12f, -0.6f - (i % 2) * 0.25f), Vector3.one * 0.22f, Palette.Solid(c, 0.3f), t, false);
            }
            DisasterReady.Build.Block("Jardinera", new Vector3(0f, -0.06f, -1.0f), new Vector3(2.9f, 0.08f, -0.2f), Palette.Solid(new Color(0.35f, 0.24f, 0.16f)), t, false);
            DisasterReady.Build.Block("Jardinera 2", new Vector3(4.8f, -0.06f, -1.0f), new Vector3(12f, 0.08f, -0.2f), Palette.Solid(new Color(0.35f, 0.24f, 0.16f)), t, false);

            // Muro bajo del frente (cerco) con portón abierto
            var brick = Palette.Textured("brick", Color.white, new Vector2(6f, 1f), 0.1f);
            DisasterReady.Build.Block("Cerco oeste", new Vector3(-2f, -0.05f, -16.1f), new Vector3(1.5f, 0.9f, -15.9f), brick, t);
            DisasterReady.Build.Block("Cerco este", new Vector3(8.5f, -0.05f, -16.1f), new Vector3(14f, 0.9f, -15.9f), brick, t);
            DisasterReady.Build.Block("Cerco lateral O", new Vector3(-2.1f, -0.05f, -16.1f), new Vector3(-1.9f, 0.9f, 0f), brick, t);
            DisasterReady.Build.Block("Cerco lateral E", new Vector3(13.9f, -0.05f, -16.1f), new Vector3(14.1f, 0.9f, 0f), brick, t);
        }

        static void Invisible(Transform t, Vector3 min, Vector3 max)
        {
            var go = DisasterReady.Build.Block("Límite", min, max, Palette.Solid(Color.white), t);
            go.GetComponent<Renderer>().enabled = false;
        }

        static void MeetingPoint(Transform t)
        {
            Vector3 c = HouseLayout.MeetingPoint;
            Vector3 s = HouseLayout.MeetingPointSize;
            DisasterReady.Build.Box("Punto de reunión", new Vector3(c.x, -0.035f, c.z), new Vector3(s.x, 0.02f, s.z),
                Palette.Solid(new Color(0.1f, 0.68f, 0.35f), 0.2f), t, false);
            // Borde blanco
            var white = Palette.Solid(Color.white, 0.2f);
            DisasterReady.Build.Box("Borde", new Vector3(c.x, -0.03f, c.z - s.z * 0.5f), new Vector3(s.x, 0.02f, 0.12f), white, t, false);
            DisasterReady.Build.Box("Borde", new Vector3(c.x, -0.03f, c.z + s.z * 0.5f), new Vector3(s.x, 0.02f, 0.12f), white, t, false);
            DisasterReady.Build.Box("Borde", new Vector3(c.x - s.x * 0.5f, -0.03f, c.z), new Vector3(0.12f, 0.02f, s.z), white, t, false);
            DisasterReady.Build.Box("Borde", new Vector3(c.x + s.x * 0.5f, -0.03f, c.z), new Vector3(0.12f, 0.02f, s.z), white, t, false);
            // Flechas pintadas que apuntan al centro (símbolo de punto de reunión)
            for (int i = 0; i < 4; i++)
            {
                var arrow = DisasterReady.Build.Box("Flecha", new Vector3(c.x, -0.025f, c.z), new Vector3(0.25f, 0.02f, 1.2f), white, t, false);
                arrow.transform.rotation = Quaternion.Euler(0f, 45f + i * 90f, 0f);
                arrow.transform.position += arrow.transform.forward * 1.1f;
            }
            if (SafeZoneEnabled) SafeZone.Create(ZoneType.PuntoReunion, new Vector3(c.x, 1f, c.z), s, t);

            // Letrero sobre poste
            Vector3 post = new Vector3(c.x + s.x * 0.5f - 0.3f, 0f, c.z + s.z * 0.5f - 0.3f);
            DisasterReady.Build.Cylinder("Poste", post + Vector3.up * 1.4f, 0.09f, 2.8f, Palette.Solid(Palette.Steel, 0.5f, 0.7f), t);
            DisasterReady.Build.MeetingPointSign(post + new Vector3(0f, 2.3f, 0.07f), Vector3.forward, t);

            // Bancas
            var wood = Palette.Solid(Palette.Oak, 0.3f);
            foreach (float x in new[] { c.x - 2.5f, c.x + 2.5f })
            {
                DisasterReady.Build.Box("Banca", new Vector3(x, 0.45f, c.z - s.z * 0.5f - 0.7f), new Vector3(1.6f, 0.08f, 0.45f), wood, t);
                DisasterReady.Build.Box("Pata", new Vector3(x - 0.65f, 0.2f, c.z - s.z * 0.5f - 0.7f), new Vector3(0.08f, 0.45f, 0.4f), Palette.Solid(Palette.Charcoal), t, false);
                DisasterReady.Build.Box("Pata", new Vector3(x + 0.65f, 0.2f, c.z - s.z * 0.5f - 0.7f), new Vector3(0.08f, 0.45f, 0.4f), Palette.Solid(Palette.Charcoal), t, false);
            }
        }

        /// <summary>Las zonas solo se crean en la escena de juego.</summary>
        static bool SafeZoneEnabled => HouseBuilder.Instance != null && HouseBuilder.Instance.IsGameScene;

        static void Street(Transform t)
        {
            var concrete = Palette.Textured("concrete", Color.white, new Vector2(30f, 1f), 0.1f);
            var asphalt = Palette.Textured("asphalt", Color.white, new Vector2(30f, 3f), 0.15f);
            DisasterReady.Build.Block("Vereda", new Vector3(-80f, -0.06f, -18.5f), new Vector3(92f, 0.08f, -16.2f), concrete, t);
            DisasterReady.Build.Block("Sardinel", new Vector3(-80f, -0.06f, -18.65f), new Vector3(92f, 0.1f, -18.5f), Palette.Solid(new Color(0.9f, 0.85f, 0.3f)), t, false);
            DisasterReady.Build.Block("Pista", new Vector3(-80f, -0.1f, -26f), new Vector3(92f, -0.04f, -18.5f), asphalt, t);
            for (float x = -78f; x < 90f; x += 5f)
                DisasterReady.Build.Box("Línea", new Vector3(x, -0.035f, -22.25f), new Vector3(2.5f, 0.01f, 0.15f), Palette.Solid(Color.white), t, false);
            DisasterReady.Build.Block("Vereda frente", new Vector3(-80f, -0.06f, -28.3f), new Vector3(92f, 0.08f, -26f), concrete, t);

            // Postes de alumbrado
            foreach (float x in new[] { -12f, 6f, 24f })
            {
                DisasterReady.Build.Cylinder("Poste luz", new Vector3(x, 3f, -17.2f), 0.14f, 6f, Palette.Solid(new Color(0.55f, 0.55f, 0.55f), 0.3f), t);
                DisasterReady.Build.Box("Brazo", new Vector3(x, 5.9f, -17.9f), new Vector3(0.1f, 0.1f, 1.4f), Palette.Solid(new Color(0.55f, 0.55f, 0.55f)), t, false);
                DisasterReady.Build.Box("Luminaria", new Vector3(x, 5.82f, -18.5f), new Vector3(0.35f, 0.1f, 0.6f), Palette.Glow(new Color(1f, 0.85f, 0.6f), 1.5f), t, false);
            }

            // Postes de concreto con cables (típicos de las calles de Huancayo)
            var pole = Palette.Textured("concrete", new Color(0.85f, 0.84f, 0.8f), Vector2.one, 0.1f);
            var cable = Palette.Solid(new Color(0.08f, 0.08f, 0.08f), 0.3f);
            float[] poles = { -20f, -5f, 10f, 25f };
            foreach (float x in poles)
            {
                DisasterReady.Build.Cylinder("Poste de luz", new Vector3(x, 4.5f, -25.6f), 0.28f, 9f, pole, t);
                DisasterReady.Build.Box("Cruceta", new Vector3(x, 8.4f, -25.6f), new Vector3(0.12f, 0.12f, 1.6f), pole, t, false);
            }
            for (int i = 0; i < poles.Length - 1; i++)
                foreach (float dz in new[] { -0.7f, 0f, 0.7f })
                {
                    // Cable con catenaria aproximada en 4 tramos
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k / 4f, b = (k + 1) / 4f;
                        float sagA = Mathf.Sin(a * Mathf.PI) * 0.6f, sagB = Mathf.Sin(b * Mathf.PI) * 0.6f;
                        Vector3 pa = new Vector3(Mathf.Lerp(poles[i], poles[i + 1], a), 8.5f - sagA, -25.6f + dz);
                        Vector3 pb = new Vector3(Mathf.Lerp(poles[i], poles[i + 1], b), 8.5f - sagB, -25.6f + dz);
                        var seg = DisasterReady.Build.Box("Cable", (pa + pb) * 0.5f, new Vector3((pb - pa).magnitude, 0.025f, 0.025f), cable, t, false);
                        seg.transform.rotation = Quaternion.FromToRotation(Vector3.right, (pb - pa).normalized);
                    }
                }

            // Auto estacionado
            Car(t, new Vector3(-6f, 0f, -20f), new Color(0.85f, 0.15f, 0.15f));
            Car(t, new Vector3(18f, 0f, -24.3f), new Color(0.2f, 0.45f, 0.8f));
        }

        static void Car(Transform t, Vector3 p, Color color)
        {
            var body = Palette.Solid(color, 0.7f, 0.3f);
            var glass = Palette.Solid(new Color(0.15f, 0.2f, 0.28f), 0.9f);
            DisasterReady.Build.Box("Auto", p + new Vector3(0f, 0.6f, 0f), new Vector3(4f, 0.7f, 1.8f), body, t);
            DisasterReady.Build.Box("Cabina", p + new Vector3(-0.2f, 1.2f, 0f), new Vector3(2.2f, 0.6f, 1.6f), glass, t, false);
            DisasterReady.Build.Box("Techo auto", p + new Vector3(-0.2f, 1.52f, 0f), new Vector3(2.0f, 0.06f, 1.62f), body, t, false);
            foreach (var w in new[] { new Vector3(-1.3f, 0.32f, 0.85f), new Vector3(1.3f, 0.32f, 0.85f), new Vector3(-1.3f, 0.32f, -0.85f), new Vector3(1.3f, 0.32f, -0.85f) })
            {
                var wheel = DisasterReady.Build.Cylinder("Rueda", p + w, 0.64f, 0.25f, Palette.Solid(new Color(0.08f, 0.08f, 0.08f)), t, false);
                wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        /// <summary>Casas vecinas: ladrillo expuesto o fachadas de colores, azotea con fierros.</summary>
        static void Neighbors(Transform t)
        {
            NeighborHouse(t, new Vector3(-15f, 0f, -1f), new Vector3(11f, 6.2f, 12f), null, 1);
            NeighborHouse(t, new Vector3(16f, 0f, 0f), new Vector3(10f, 9.3f, 11f), new Color(0.55f, 0.75f, 0.9f), 2);
            NeighborHouse(t, new Vector3(-3f, 0f, 14f), new Vector3(9f, 6.2f, 10f), new Color(0.95f, 0.7f, 0.75f), 3);
            NeighborHouse(t, new Vector3(9f, 0f, 14f), new Vector3(12f, 6.2f, 10f), null, 4);
            NeighborHouse(t, new Vector3(-16f, 0f, -40f), new Vector3(12f, 6.2f, 10f), new Color(0.6f, 0.85f, 0.6f), 5);
            NeighborHouse(t, new Vector3(-2f, 0f, -40f), new Vector3(10f, 9.3f, 10f), null, 6);
            NeighborHouse(t, new Vector3(10f, 0f, -40f), new Vector3(11f, 6.2f, 10f), new Color(0.98f, 0.85f, 0.5f), 7);
            NeighborHouse(t, new Vector3(23f, 0f, -40f), new Vector3(10f, 6.2f, 10f), null, 8);
        }

        static void NeighborHouse(Transform t, Vector3 min, Vector3 size, Color? paint, int seed)
        {
            var wall = paint.HasValue
                ? Palette.Textured("plaster", paint.Value, new Vector2(3f, 2f), 0.1f)
                : Palette.Textured("brick", Color.white, new Vector2(size.x / 1.2f, size.y / 0.9f), 0.05f);
            Vector3 max = min + size;
            DisasterReady.Build.Block("Casa vecina", min, max, wall, t);
            DisasterReady.Build.Block("Losa", new Vector3(min.x - 0.1f, max.y, min.z - 0.1f), new Vector3(max.x + 0.1f, max.y + 0.2f, max.z + 0.1f),
                Palette.Textured("concrete", Color.white, Vector2.one), t, false);

            // Ventanas oscuras en las fachadas largas
            var glass = Palette.Solid(new Color(0.18f, 0.24f, 0.3f), 0.9f);
            var frame = Palette.Solid(Palette.WarmWhite);
            int floors = Mathf.Max(1, Mathf.RoundToInt(size.y / 3.1f));
            var rng = new System.Random(seed);
            for (int f = 0; f < floors; f++)
                for (float x = min.x + 1.5f; x < max.x - 1f; x += 3f)
                {
                    if (rng.NextDouble() < 0.2) continue;
                    float y = f * 3.1f + 1.6f;
                    foreach (float z in new[] { min.z - 0.02f, max.z + 0.02f })
                    {
                        DisasterReady.Build.Box("Ventana", new Vector3(x, y, z), new Vector3(1.4f, 1.1f, 0.06f), glass, t, false);
                        DisasterReady.Build.Box("Marco", new Vector3(x, y - 0.6f, z), new Vector3(1.6f, 0.08f, 0.1f), frame, t, false);
                    }
                }

            // Fierros en la azotea (casa "en construcción")
            var rebar = Palette.Solid(new Color(0.35f, 0.22f, 0.15f), 0.3f, 0.6f);
            foreach (var cx in new[] { min.x + 0.2f, max.x - 0.2f })
                foreach (var cz in new[] { min.z + 0.2f, max.z - 0.2f })
                    for (int i = 0; i < 4; i++)
                        DisasterReady.Build.Cylinder("Fierro", new Vector3(cx + (i % 2) * 0.12f, max.y + 0.8f, cz + (i / 2) * 0.12f), 0.025f, 1.2f, rebar, t, false);

            // Tanque de agua
            if (rng.NextDouble() < 0.7)
                DisasterReady.Build.Cylinder("Tanque", new Vector3(min.x + size.x * 0.7f, max.y + 0.95f, min.z + size.z * 0.6f), 1.2f, 1.4f,
                    Palette.Solid(new Color(0.08f, 0.08f, 0.09f), 0.6f), t, false);
        }

        static void Trees(Transform t)
        {
            var positions = new[]
            {
                new Vector3(-4f, 0f, -6f), new Vector3(16f, 0f, -8f), new Vector3(-8f, 0f, -12f), new Vector3(13f, 0f, -12f),
                new Vector3(-2.2f, 0f, 12f), new Vector3(28f, 0f, 6f), new Vector3(-24f, 0f, -8f), new Vector3(30f, 0f, -12f),
                new Vector3(5f, 0f, 28f), new Vector3(-12f, 0f, 30f), new Vector3(24f, 0f, 28f), new Vector3(-30f, 0f, 16f),
            };
            for (int i = 0; i < positions.Length; i++) Eucalyptus(t, positions[i], i);
        }

        /// <summary>Eucalipto: árbol muy común en el valle del Mantaro.</summary>
        static void Eucalyptus(Transform t, Vector3 p, int seed)
        {
            float h = 6f + (seed % 3) * 1.5f;
            DisasterReady.Build.Cylinder("Tronco", p + Vector3.up * (h * 0.5f), 0.32f, h, Palette.Solid(new Color(0.75f, 0.7f, 0.62f), 0.2f), t);
            var leaves = Palette.Solid(new Color(0.32f + (seed % 4) * 0.03f, 0.48f, 0.36f), 0.1f);
            for (int i = 0; i < 3; i++)
            {
                float y = h * (0.62f + i * 0.17f);
                float s = 3.0f - i * 0.7f;
                DisasterReady.Build.Sphere("Copa", p + new Vector3((i - 1) * 0.4f, y, (i % 2) * 0.4f), new Vector3(s, s * 1.15f, s), leaves, t, false);
            }
        }

        /// <summary>Cordillera de fondo con nevados (como el Huaytapallana).</summary>
        static void Mountains(Transform t)
        {
            var rock = Palette.Solid(new Color(0.42f, 0.4f, 0.36f), 0.05f);
            var green = Palette.Solid(new Color(0.36f, 0.45f, 0.3f), 0.05f);
            var snow = Palette.Solid(new Color(0.96f, 0.97f, 1f), 0.3f);
            var rng = new System.Random(42);
            for (int i = 0; i < 26; i++)
            {
                float angle = i * Mathf.PI * 2f / 26f + (float)rng.NextDouble() * 0.15f;
                float distance = 170f + (float)rng.NextDouble() * 90f;
                Vector3 p = new Vector3(6f + Mathf.Cos(angle) * distance, -2f, 5f + Mathf.Sin(angle) * distance);
                bool high = Mathf.Sin(angle) > 0.2f && rng.NextDouble() < 0.7; // nevados hacia el norte
                float height = high ? 75f + (float)rng.NextDouble() * 45f : 35f + (float)rng.NextDouble() * 30f;
                float radius = height * (1.1f + (float)rng.NextDouble() * 0.5f);
                DisasterReady.Build.Cone("Montaña", p, radius, height, 7, high ? rock : green, t, 0.25f, i);
                if (high)
                {
                    float capRatio = 0.3f;
                    DisasterReady.Build.Cone("Nevado", p + Vector3.up * height * (1f - capRatio), radius * capRatio * 1.05f, height * capRatio + 0.5f, 7, snow, t, 0.2f, i);
                }
            }
        }
    }
}
