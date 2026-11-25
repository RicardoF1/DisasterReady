using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Mobiliario de cada ambiente: muebles, zonas seguras y peligrosas, suministros
    /// (en lugares lógicos: el agua en la cocina, el botiquín en el baño...) y objetos
    /// que pueden caer durante el sismo.
    /// </summary>
    public static class Furnishing
    {
        static Transform root;
        static bool game;

        // Materiales frecuentes
        static Material oak, darkWood, white, fabricBlue, fabricMustard, metal, black, manta;

        public static void Build(HouseBuilder house)
        {
            root = DisasterReady.Build.Group("Mobiliario", Vector3.zero, house.transform);
            game = house.IsGameScene;

            oak = Palette.Solid(Palette.Oak, 0.35f);
            darkWood = Palette.Solid(Palette.DarkWood, 0.35f);
            white = Palette.Solid(Palette.WarmWhite, 0.3f);
            fabricBlue = Palette.Solid(new Color(0.22f, 0.36f, 0.55f), 0.1f);
            fabricMustard = Palette.Solid(Palette.Mustard, 0.1f);
            metal = Palette.Solid(Palette.Steel, 0.7f, 0.8f);
            black = Palette.Solid(Palette.Charcoal, 0.5f);
            manta = Palette.Textured("manta", Color.white, new Vector2(1f, 1f), 0.05f);

            LivingRoom();
            Kitchen();
            Hall();
            Study();
            Bathroom(0, new Vector3(0f, 0f, 6f));
            ParentsRoom();
            TeenRoom();
            Corridor();
            Bathroom(1, new Vector3(0f, HouseLayout.FloorY(1), 7f));
            WindowZones();
        }

        // =====================================================================
        // PRIMER PISO
        // =====================================================================

        static void LivingRoom()
        {
            // Alfombra andina, sofá y mesa de centro
            Rug(new Vector3(2.2f, 0f, 2.7f), new Vector2(2.6f, 2.0f));
            Sofa(new Vector3(0.6f, 0f, 3.0f), 90f, 2.6f, fabricBlue);
            Box("Mesa de centro", new Vector3(2.2f, 0.4f, 2.9f), new Vector3(0.95f, 0.05f, 0.55f), oak);
            foreach (var o in new[] { new Vector2(-0.4f, -0.22f), new Vector2(0.4f, -0.22f), new Vector2(-0.4f, 0.22f), new Vector2(0.4f, 0.22f) })
                Box("Pata", new Vector3(2.2f + o.x, 0.19f, 2.9f + o.y), new Vector3(0.05f, 0.38f, 0.05f), darkWood, false);
            Plant(new Vector3(2.2f, 0.425f, 2.9f), 0.5f);

            // Mueble de TV, televisor (puede caer) y la consola (objeto prescindible)
            Box("Mueble TV", new Vector3(6.67f, 0.25f, 1.15f), new Vector3(0.45f, 0.5f, 1.5f), darkWood);
            var tv = Fallable("Televisor", new Vector3(6.75f, 0.84f, 1.25f), 15, false, Vector3.left, 12f);
            Part(tv, PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.62f, 1.05f), black, true);
            Part(tv, PrimitiveType.Cube, new Vector3(-0.031f, 0f, 0f), new Vector3(0.004f, 0.56f, 0.98f), Palette.Glow(new Color(0.25f, 0.5f, 0.9f), 0.9f, true));
            Part(tv, PrimitiveType.Cube, new Vector3(0.02f, -0.34f, 0f), new Vector3(0.18f, 0.04f, 0.3f), black, true);
            FinishFallable(tv);
            Item("consola", new Vector3(6.6f, 0.5f, 0.55f), 90f);

            // Comedor (ZONA SEGURA bajo la mesa) con lámpara colgante encima
            Table("Mesa de comedor", new Vector3(3.8f, 0f, 4.5f), new Vector2(1.8f, 1.0f), oak, true);
            Chair(new Vector3(2.6f, 0f, 4.5f), 90f);
            Chair(new Vector3(5.0f, 0f, 4.5f), -90f);
            HangingLamp(new Vector3(3.8f, 2.25f, 4.5f), Palette.Terracotta);

            // Estante con libros (se vuelca hacia adelante)
            Bookshelf(new Vector3(1.7f, 0f, 6.0f), Vector3.back, 1.4f, 1.9f, 4);
            Frame(new Vector3(3.8f, 1.8f, 5.89f), Vector3.back, new Vector2(0.9f, 0.6f), 0);
            Frame(new Vector3(6.89f, 1.75f, 1.25f), Vector3.left, new Vector2(0.7f, 0.5f), 1);

            Plant(new Vector3(0.45f, 0f, 5.5f), 1f);
            Plant(new Vector3(6.5f, 0f, 3.8f), 0.9f);
            Clock(new Vector3(6.2f, 2.0f, 5.88f), Vector3.back);

            // Columna con señal ZONA SEGURA
            Column(new Vector3(6.72f, 0f, 5.72f), Vector3.left, new Vector3(6.3f, 1f, 5.3f));
        }

        static void Kitchen()
        {
            // Refrigeradora (pesada: puede volcarse)
            var fridge = Fallable("Refrigeradora", new Vector3(7.5f, 0.95f, 0.5f), 30, true, Vector3.right, 70f);
            Part(fridge, PrimitiveType.Cube, Vector3.zero, new Vector3(0.7f, 1.9f, 0.7f), Palette.Solid(new Color(0.92f, 0.93f, 0.95f), 0.7f), true);
            Part(fridge, PrimitiveType.Cube, new Vector3(0.36f, 0.35f, 0.25f), new Vector3(0.02f, 0.4f, 0.04f), metal);
            Part(fridge, PrimitiveType.Cube, new Vector3(0.36f, -0.4f, 0.25f), new Vector3(0.02f, 0.3f, 0.04f), metal);
            Part(fridge, PrimitiveType.Cube, new Vector3(0.352f, 0.1f, 0f), new Vector3(0.004f, 0.02f, 0.68f), Palette.Solid(new Color(0.7f, 0.7f, 0.72f)));
            FinishFallable(fridge);
            Item("agua", new Vector3(7.55f, 0f, 1.3f), 0f);

            // Lavadero bajo la ventana sur
            var counterTop = Palette.Solid(new Color(0.35f, 0.36f, 0.38f), 0.7f);
            Box("Mueble lavadero", new Vector3(9.5f, 0.43f, 0.4f), new Vector3(2.2f, 0.86f, 0.6f), white);
            Box("Tablero", new Vector3(9.5f, 0.88f, 0.4f), new Vector3(2.24f, 0.04f, 0.64f), counterTop);
            Box("Poza", new Vector3(9.5f, 0.89f, 0.42f), new Vector3(0.6f, 0.02f, 0.4f), metal, false);
            Box("Caño", new Vector3(9.5f, 1.05f, 0.18f), new Vector3(0.04f, 0.3f, 0.04f), metal, false);

            // Mesón con cocina y balón de gas
            Box("Mesón", new Vector3(11.6f, 0.43f, 1.25f), new Vector3(0.6f, 0.86f, 1.3f), white);
            Box("Tablero mesón", new Vector3(11.6f, 0.88f, 1.25f), new Vector3(0.64f, 0.04f, 1.34f), counterTop);
            Box("Cocina", new Vector3(11.6f, 0.91f, 1.0f), new Vector3(0.55f, 0.03f, 0.6f), black, false);
            for (int i = 0; i < 4; i++)
                Cyl("Hornilla", new Vector3(11.48f + (i % 2) * 0.24f, 0.93f, 0.85f + (i / 2) * 0.3f), 0.14f, 0.01f, Palette.Solid(new Color(0.3f, 0.3f, 0.3f), 0.6f), false);
            var pot = Fallable("Olla", new Vector3(11.48f, 1.02f, 0.85f), 8, false, Vector3.zero, 1.5f);
            Part(pot, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.24f, 0.08f, 0.24f), metal, true);
            FinishFallable(pot);

            GasCylinder(new Vector3(11.55f, 0f, 2.25f));

            // Mesón del fondo, repisa con platos y los alimentos
            Box("Mesón 2", new Vector3(11.6f, 0.43f, 5.0f), new Vector3(0.6f, 0.86f, 1.8f), white);
            Box("Tablero mesón 2", new Vector3(11.6f, 0.88f, 5.0f), new Vector3(0.64f, 0.04f, 1.84f), counterTop);
            Box("Repisa", new Vector3(11.75f, 1.7f, 5.0f), new Vector3(0.3f, 0.04f, 1.6f), oak);
            for (int i = 0; i < 6; i++)
            {
                var plate = Fallable("Plato", new Vector3(11.75f, 1.84f, 4.35f + i * 0.26f), 6, false, Vector3.left, 0.4f);
                Part(plate, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.01f, 0.22f), Palette.Solid(i % 2 == 0 ? Color.white : new Color(0.55f, 0.75f, 0.85f), 0.8f), true, new Vector3(0f, 0f, 90f));
                FinishFallable(plate);
            }
            Item("alimentos", new Vector3(11.55f, 0.9f, 5.0f), -90f);
            Calendar(new Vector3(7.12f, 1.65f, 4.6f), Vector3.right);
            Clock(new Vector3(7.12f, 2.25f, 1.2f), Vector3.right);

            // Mesa de cocina (ZONA SEGURA)
            Table("Mesa de cocina", new Vector3(9.3f, 0f, 3.4f), new Vector2(1.6f, 1.0f), Palette.Solid(new Color(0.85f, 0.8f, 0.7f), 0.4f), true);
            Chair(new Vector3(8.25f, 0f, 3.4f), 90f);
            Chair(new Vector3(10.35f, 0f, 3.4f), -90f);
        }

        static void Hall()
        {
            // Tablero eléctrico
            var panel = Box("Tablero eléctrico", new Vector3(11.86f, 1.5f, 6.9f), new Vector3(0.08f, 0.5f, 0.38f), Palette.Solid(new Color(0.75f, 0.76f, 0.78f), 0.5f));
            var lever = DisasterReady.Build.Group("Palanca", new Vector3(11.8f, 1.5f, 6.9f), root);
            DisasterReady.Build.Part(lever, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(0.05f, 0.16f, 0.05f), Palette.Solid(Palette.Danger, 0.5f));
            DisasterReady.Build.Sign("LUZ", new Vector3(11.81f, 1.85f, 6.9f), Vector3.left, new Vector2(0.3f, 0.12f), Palette.Mustard, Palette.Charcoal, 30, root);
            if (game)
                UtilityInteractable.Create(panel, GameConfig.TaskPower, "Bajar la llave general de LUZ",
                    "Cortaste la electricidad: evitas cortocircuitos", SoundId.Switch, lever, new Vector3(0f, 0f, 160f));

            Plant(new Vector3(7.45f, 0f, 6.4f), 1.1f);
            Frame(new Vector3(9.0f, 1.7f, 6.11f), Vector3.forward, new Vector2(0.8f, 0.55f), 2);
        }

        static void Study()
        {
            // Escritorio (ZONA SEGURA) con laptop y documentos
            Table("Escritorio", new Vector3(5.9f, 0f, 9.3f), new Vector2(1.6f, 1.0f), darkWood, true);
            Chair(new Vector3(4.6f, 0f, 8.6f), 40f);
            Item("laptop", new Vector3(5.55f, TableTop, 9.45f), 180f);
            Item("documentos", new Vector3(6.3f, TableTop, 9.25f), 15f);
            DeskLamp(new Vector3(6.55f, TableTop, 9.65f));

            // Librero con la radio y las pilas
            var shelfLevels = Bookshelf(new Vector3(3.0f, 0f, 7.0f), Vector3.right, 1.4f, 1.8f, 4, 2);
            Item("radio", new Vector3(3.3f, shelfLevels[2], 6.75f), 90f);
            Item("pilas", new Vector3(3.3f, shelfLevels[1], 7.3f), 90f);

            Box("Archivador", new Vector3(3.45f, 0.55f, 9.5f), new Vector3(0.5f, 1.1f, 0.6f), Palette.Solid(new Color(0.45f, 0.5f, 0.55f), 0.5f));
            Frame(new Vector3(6.89f, 1.7f, 8.8f), Vector3.left, new Vector2(0.7f, 0.5f), 3);
            Rug(new Vector3(5.0f, 0f, 8.0f), new Vector2(2.0f, 1.4f));
        }

        static void Bathroom(int floor, Vector3 origin)
        {
            float y = origin.y;
            var porcelain = Palette.Solid(new Color(0.97f, 0.97f, 0.98f), 0.85f);
            float zTop = origin.z + (floor == 0 ? 3.9f : 2.9f);
            // Inodoro
            Cyl("Inodoro", new Vector3(0.5f, y + 0.2f, zTop - 0.45f), 0.42f, 0.4f, porcelain);
            Box("Tanque inodoro", new Vector3(0.5f, y + 0.6f, zTop - 0.1f), new Vector3(0.45f, 0.4f, 0.18f), porcelain);
            // Lavatorio y espejo
            Box("Lavatorio", new Vector3(1.7f, y + 0.82f, zTop - 0.25f), new Vector3(0.55f, 0.12f, 0.42f), porcelain);
            Box("Pie lavatorio", new Vector3(1.7f, y + 0.4f, zTop - 0.2f), new Vector3(0.18f, 0.8f, 0.18f), porcelain, false);
            Box("Espejo", new Vector3(1.7f, y + 1.55f, zTop - 0.02f), new Vector3(0.6f, 0.75f, 0.02f), Palette.Solid(new Color(0.75f, 0.85f, 0.9f), 0.98f, 0.9f), false);

            if (floor == 0)
            {
                // Ducha con cortina y el botiquín sobre un mueble
                Box("Plato ducha", new Vector3(0.7f, 0.03f, 6.75f), new Vector3(1.1f, 0.06f, 1.1f), porcelain);
                Box("Cortina", new Vector3(1.27f, 1.2f, 6.75f), new Vector3(0.02f, 1.8f, 1.1f), Palette.Solid(new Color(0.55f, 0.8f, 0.85f), 0.1f), false);
                Box("Mueble baño", new Vector3(2.62f, 0.4f, 6.45f), new Vector3(0.45f, 0.8f, 0.6f), white);
                Item("botiquin", new Vector3(2.62f, 0.8f, 6.45f), 90f);
            }
        }

        static void GasCylinder(Vector3 p)
        {
            // Balón de gas típico (amarillo) con válvula roja
            Cyl("Balón de gas", p + Vector3.up * 0.35f, 0.32f, 0.6f, Palette.Solid(new Color(0.98f, 0.72f, 0.1f), 0.6f));
            Cyl("Cuello", p + Vector3.up * 0.7f, 0.12f, 0.1f, Palette.Solid(new Color(0.98f, 0.72f, 0.1f), 0.6f), false);
            Cyl("Manguera", p + new Vector3(0f, 0.85f, -0.5f), 0.025f, 0.8f, black, false).transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            var valve = Box("Válvula de gas", p + Vector3.up * 0.8f, new Vector3(0.14f, 0.08f, 0.14f), Palette.Solid(new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.8f));
            var handle = DisasterReady.Build.Group("Perilla", p + Vector3.up * 0.86f, root);
            DisasterReady.Build.Part(handle, PrimitiveType.Cube, Vector3.zero, new Vector3(0.18f, 0.03f, 0.04f), Palette.Solid(Palette.Danger, 0.5f));
            DisasterReady.Build.Sign("GAS", p + new Vector3(-0.17f, 0.45f, 0f), Vector3.left, new Vector2(0.24f, 0.11f), Palette.Danger, Color.white, 30, root);
            if (game)
                UtilityInteractable.Create(valve, GameConfig.TaskGas, "Cerrar la válvula del balón de GAS",
                    "Cerraste el gas: evitas fugas e incendios", SoundId.Valve, handle, new Vector3(0f, 90f, 0f));
        }

        // =====================================================================
        // SEGUNDO PISO
        // =====================================================================

        static void ParentsRoom()
        {
            float y = HouseLayout.FloorY(1);
            Bed(new Vector3(4.9f, y, 2.2f), -90f, 1.6f, 2.0f, manta);
            Nightstand(new Vector3(5.65f, y, 1.05f));
            Nightstand(new Vector3(5.65f, y, 3.35f));
            Wardrobe(new Vector3(1.1f, y, 4.62f), Vector3.back, new Vector3(1.4f, 2.0f, 0.55f));
            Box("Cómoda", new Vector3(4.6f, y + 0.45f, 0.38f), new Vector3(1.2f, 0.9f, 0.5f), oak);
            Box("Espejo cómoda", new Vector3(4.6f, y + 1.45f, 0.13f), new Vector3(0.8f, 0.9f, 0.02f), Palette.Solid(new Color(0.75f, 0.85f, 0.9f), 0.98f, 0.9f), false);
            Frame(new Vector3(5.89f, y + 1.65f, 2.2f), Vector3.left, new Vector2(1.0f, 0.55f), 4);
            Rug(new Vector3(3.2f, y, 2.2f), new Vector2(1.2f, 2.2f));
            Item("abrigo", new Vector3(4.4f, y + 0.54f, 2.5f), -80f);
        }

        static void TeenRoom()
        {
            float y = HouseLayout.FloorY(1);
            Bed(new Vector3(7.15f, y, 3.0f), 90f, 1.0f, 2.0f, Palette.Textured("manta", new Color(0.9f, 0.95f, 1f), new Vector2(1f, 1f)));
            Nightstand(new Vector3(6.38f, y, 2.1f), false);
            Item("linterna", new Vector3(6.38f, y + 0.5f, 2.1f), 30f);

            // Repisa con libros sobre la cama
            Box("Repisa cama", new Vector3(6.22f, y + 1.55f, 3.0f), new Vector3(0.25f, 0.03f, 0.9f), oak);
            for (int i = 0; i < 6; i++) Book(new Vector3(6.22f, y + 1.57f, 2.65f + i * 0.13f), Vector3.right, i);

            // Escritorio (ZONA SEGURA) con silbato y trofeo
            Table("Escritorio joven", new Vector3(10.7f, y, 4.35f), new Vector2(1.6f, 1.0f), Palette.Solid(Palette.WarmWhite, 0.4f), true);
            Chair(new Vector3(9.55f, y, 3.7f), 60f);
            Item("silbato", new Vector3(10.25f, y + TableTop, 4.45f), 20f);
            Item("adorno", new Vector3(11.25f, y + TableTop, 4.6f), 0f);
            DeskLamp(new Vector3(11.3f, y + TableTop, 4.05f));
            Frame(new Vector3(10.7f, y + 1.75f, 4.89f), Vector3.back, new Vector2(0.9f, 0.6f), 5);

            Wardrobe(new Vector3(11.62f, y, 0.95f), Vector3.left, new Vector3(0.55f, 2.0f, 1.3f));
            Rug(new Vector3(9.2f, y, 2.2f), new Vector2(1.8f, 1.3f));
            Plant(new Vector3(8.0f, y, 0.45f), 0.8f);
        }

        static void Corridor()
        {
            float y = HouseLayout.FloorY(1);
            Rug(new Vector3(7.0f, y, 6.6f), new Vector2(5.0f, 1.0f));
            Column(new Vector3(11.72f, y, 5.28f), Vector3.left, new Vector3(11.3f, y + 1f, 5.75f));
            Frame(new Vector3(9.7f, y + 1.7f, 5.11f), Vector3.forward, new Vector2(0.6f, 0.45f), 6);
            Frame(new Vector3(10.5f, y + 1.6f, 5.11f), Vector3.forward, new Vector2(0.45f, 0.6f), 7);
            Plant(new Vector3(3.45f, y, 5.45f), 1f);
            Box("Mueble pasillo", new Vector3(5.2f, y + 0.4f, 9.7f), new Vector3(1.2f, 0.8f, 0.35f), oak);
            var vase = Fallable("Jarrón", new Vector3(5.2f, y + 0.95f, 9.7f), 8, false, Vector3.back, 1f);
            Part(vase, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.18f, 0.15f, 0.18f), Palette.Solid(Palette.Teal, 0.7f), true);
            FinishFallable(vase);
        }

        // =====================================================================
        // ZONAS
        // =====================================================================

        static void WindowZones()
        {
            if (!game) return;
            foreach (var o in HouseLayout.Openings)
            {
                if (!o.IsWindow || Mathf.Abs(o.Inward) < 0.5f) continue;
                Vector3 c = o.Center;
                Vector3 center = new Vector3(c.x, HouseLayout.FloorY(o.Floor) + 1.2f, c.z) + o.InwardDirection * 0.85f;
                Vector3 size = o.AlongX ? new Vector3(o.Width + 0.6f, 2.4f, 1.7f) : new Vector3(1.7f, 2.4f, o.Width + 0.6f);
                SafeZone.Create(ZoneType.Ventana, center, size, root);
            }
        }

        // =====================================================================
        // MUEBLES (ayudas)
        // =====================================================================

        static GameObject Box(string name, Vector3 center, Vector3 size, Material m, bool collider = true)
            => DisasterReady.Build.Box(name, center, size, m, root, collider);

        static GameObject Cyl(string name, Vector3 center, float diameter, float height, Material m, bool collider = true)
            => DisasterReady.Build.Cylinder(name, center, diameter, height, m, root, collider);

        static GameObject Part(Transform parent, PrimitiveType type, Vector3 local, Vector3 scale, Material m, bool collider = false, Vector3? euler = null)
            => DisasterReady.Build.Part(parent, type, local, scale, m, collider, euler);

        static void Item(string id, Vector3 position, float yaw)
        {
            if (game) PickupItem.Create(id, position, yaw, root);
            else
            {
                // En el menú solo se ve el modelo
                var g = DisasterReady.Build.Group("Decoración " + id, position, root, yaw);
                ItemModels.Build(id, g);
            }
        }

        /// <summary>Crea el padre de un objeto que puede caer. Añadir piezas y luego llamar a FinishFallable.</summary>
        static Transform Fallable(string name, Vector3 position, int damage, bool heavy, Vector3 tip, float mass, float yaw = 0f)
        {
            var t = DisasterReady.Build.Group(name, position, root, yaw);
            t.gameObject.AddComponent<FallableSetup>().Configure(damage, heavy, tip, mass);
            return t;
        }

        static void FinishFallable(Transform t)
        {
            var setup = t.GetComponent<FallableSetup>();
            var body = t.gameObject.AddComponent<Rigidbody>();
            body.mass = setup.Mass;
            body.isKinematic = true;
            var f = t.gameObject.AddComponent<DisasterReady.Fallable>();
            f.Damage = setup.Damage;
            f.Heavy = setup.Heavy;
            f.TipDirection = setup.Tip;
            Object.Destroy(setup);
        }

        // Altura de las mesas: la parte inferior del tablero queda a ~0.80 m, el jugador agachado mide ~0.64 m.
        const float TableHeight = 0.8f;
        const float TableTop = TableHeight + 0.045f;

        /// <summary>Mesa con 4 patas. Si safe, crea una ZONA SEGURA debajo.</summary>
        static void Table(string name, Vector3 floorCenter, Vector2 size, Material top, bool safe)
        {
            const float h = TableHeight;
            Box(name, floorCenter + Vector3.up * (h + 0.02f), new Vector3(size.x, 0.05f, size.y), top);
            float lx = size.x * 0.5f - 0.06f, lz = size.y * 0.5f - 0.06f;
            foreach (var o in new[] { new Vector2(-lx, -lz), new Vector2(lx, -lz), new Vector2(-lx, lz), new Vector2(lx, lz) })
                Box("Pata", floorCenter + new Vector3(o.x, h * 0.5f, o.y), new Vector3(0.06f, h, 0.06f), darkWood);

            if (safe && game)
                SafeZone.Create(ZoneType.Mesa, floorCenter + Vector3.up * 0.42f, new Vector3(size.x + 0.3f, 0.84f, size.y + 0.3f), root);
        }

        static void Chair(Vector3 floor, float yaw)
        {
            var g = DisasterReady.Build.Group("Silla", floor, root, yaw);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.42f, 0.05f, 0.42f), oak, true);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.75f, -0.19f), new Vector3(0.42f, 0.55f, 0.04f), oak, true);
            foreach (var o in new[] { new Vector2(-0.18f, -0.18f), new Vector2(0.18f, -0.18f), new Vector2(-0.18f, 0.18f), new Vector2(0.18f, 0.18f) })
                Part(g, PrimitiveType.Cube, new Vector3(o.x, 0.22f, o.y), new Vector3(0.04f, 0.44f, 0.04f), darkWood);
        }

        static void Sofa(Vector3 floor, float yaw, float length, Material fabric)
        {
            var g = DisasterReady.Build.Group("Sofá", floor, root, yaw);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(length, 0.44f, 0.85f), fabric, true);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.36f), new Vector3(length, 0.6f, 0.18f), fabric, true);
            Part(g, PrimitiveType.Cube, new Vector3(-length * 0.5f + 0.1f, 0.55f, 0f), new Vector3(0.2f, 0.3f, 0.85f), fabric, true);
            Part(g, PrimitiveType.Cube, new Vector3(length * 0.5f - 0.1f, 0.55f, 0f), new Vector3(0.2f, 0.3f, 0.85f), fabric, true);
            for (int i = 0; i < 3; i++)
                Part(g, PrimitiveType.Cube, new Vector3(-length * 0.3f + i * length * 0.3f, 0.5f, 0.05f), new Vector3(length * 0.28f, 0.12f, 0.6f), fabricMustard);
            Part(g, PrimitiveType.Cube, new Vector3(length * 0.2f, 0.92f, -0.36f), new Vector3(0.8f, 0.03f, 0.22f), manta);
        }

        static void Bed(Vector3 floor, float yaw, float width, float length, Material blanket)
        {
            var g = DisasterReady.Build.Group("Cama", floor, root, yaw);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(width, 0.3f, length), darkWood, true);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), new Vector3(width - 0.05f, 0.16f, length - 0.05f), white, true);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.51f, 0.2f), new Vector3(width, 0.04f, length * 0.75f), blanket);
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.6f, -length * 0.5f - 0.03f), new Vector3(width + 0.1f, 1.0f, 0.06f), darkWood, true);
            float pillows = width > 1.2f ? 2 : 1;
            for (int i = 0; i < pillows; i++)
            {
                float x = pillows == 1 ? 0f : (i == 0 ? -width * 0.25f : width * 0.25f);
                Part(g, PrimitiveType.Cube, new Vector3(x, 0.56f, -length * 0.5f + 0.25f), new Vector3(width / pillows - 0.12f, 0.12f, 0.3f), Palette.Solid(Color.white, 0.1f));
            }
        }

        static void Nightstand(Vector3 floor, bool lamp = true)
        {
            Box("Velador", floor + Vector3.up * 0.25f, new Vector3(0.45f, 0.5f, 0.42f), oak);
            if (!lamp) return;
            var l = Fallable("Lámpara de noche", floor + Vector3.up * 0.5f, 6, false, Vector3.zero, 1f);
            Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.14f), black, true);
            Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.03f, 0.12f, 0.03f), black);
            Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.34f, 0f), new Vector3(0.24f, 0.08f, 0.24f), Palette.Glow(new Color(1f, 0.85f, 0.6f), 1.2f, true), true);
            FinishFallable(l);
        }

        static void DeskLamp(Vector3 p)
        {
            var l = Fallable("Lámpara de escritorio", p, 5, false, Vector3.zero, 0.8f);
            Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0f), new Vector3(0.14f, 0.015f, 0.14f), black, true);
            Part(l, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(0.025f, 0.38f, 0.025f), black);
            Part(l, PrimitiveType.Cube, new Vector3(0f, 0.39f, -0.07f), new Vector3(0.12f, 0.08f, 0.16f), Palette.Solid(Palette.Mustard, 0.5f), true);
            FinishFallable(l);
        }

        static void Wardrobe(Vector3 floor, Vector3 front, Vector3 size)
        {
            float yaw = Quaternion.LookRotation(front).eulerAngles.y;
            var w = Fallable("Ropero", floor + Vector3.up * (size.y * 0.5f), 30, true, front, 55f, yaw);
            // En local: ancho X, profundidad Z, frente hacia +Z
            float width = Mathf.Abs(front.x) > 0.5f ? size.z : size.x;
            float depth = Mathf.Abs(front.x) > 0.5f ? size.x : size.z;
            Part(w, PrimitiveType.Cube, Vector3.zero, new Vector3(width, size.y, depth), oak, true);
            Part(w, PrimitiveType.Cube, new Vector3(0f, 0f, depth * 0.5f + 0.005f), new Vector3(0.01f, size.y - 0.1f, 0.01f), darkWood);
            Part(w, PrimitiveType.Cube, new Vector3(-0.08f, 0.05f, depth * 0.5f + 0.02f), new Vector3(0.03f, 0.2f, 0.03f), metal);
            Part(w, PrimitiveType.Cube, new Vector3(0.08f, 0.05f, depth * 0.5f + 0.02f), new Vector3(0.03f, 0.2f, 0.03f), metal);
            Part(w, PrimitiveType.Cube, new Vector3(0f, size.y * 0.5f + 0.15f, 0f), new Vector3(width * 0.6f, 0.3f, depth * 0.8f), Palette.Solid(new Color(0.75f, 0.6f, 0.4f)), true);
            FinishFallable(w);
        }

        /// <summary>Estante con libros. Devuelve la altura de cada repisa (para poner objetos encima).</summary>
        static float[] Bookshelf(Vector3 wallBase, Vector3 front, float width, float height, int levels, int skipBooksOnLevel = -1)
        {
            const float depth = 0.34f;
            float yaw = Quaternion.LookRotation(front).eulerAngles.y;
            Vector3 center = wallBase + front * (depth * 0.5f + 0.1f) + Vector3.up * (height * 0.5f);
            var shelf = Fallable("Estante", center, 25, true, front, 40f, yaw);

            Part(shelf, PrimitiveType.Cube, new Vector3(0f, 0f, -depth * 0.5f + 0.01f), new Vector3(width, height, 0.02f), darkWood, true);
            Part(shelf, PrimitiveType.Cube, new Vector3(-width * 0.5f + 0.015f, 0f, 0f), new Vector3(0.03f, height, depth), oak, true);
            Part(shelf, PrimitiveType.Cube, new Vector3(width * 0.5f - 0.015f, 0f, 0f), new Vector3(0.03f, height, depth), oak, true);

            var levelHeights = new float[levels + 1];
            for (int i = 0; i <= levels; i++)
            {
                float ly = -height * 0.5f + 0.02f + i * (height - 0.04f) / levels;
                Part(shelf, PrimitiveType.Cube, new Vector3(0f, ly, 0f), new Vector3(width - 0.03f, 0.03f, depth), oak, true);
                levelHeights[i] = wallBase.y + height * 0.5f + ly + 0.015f;
            }
            FinishFallable(shelf);

            // Libros (cada uno cae por separado)
            for (int level = 0; level < levels; level++)
            {
                if (level == skipBooksOnLevel || level == skipBooksOnLevel - 1) continue;
                float x = -width * 0.5f + 0.08f;
                int i = level * 7;
                while (x < width * 0.5f - 0.15f)
                {
                    float thickness = 0.04f + (i % 3) * 0.015f;
                    if (i % 5 == 4) x += 0.12f; // huecos
                    Vector3 local = new Vector3(x + thickness * 0.5f, 0f, 0.02f);
                    Vector3 world = shelf.TransformPoint(local);
                    world.y = levelHeights[level];
                    Book(world, front, i, thickness, yaw);
                    x += thickness + 0.005f;
                    i++;
                }
            }
            return levelHeights;
        }

        static readonly Color[] BookColors =
        {
            new Color(0.75f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.7f), new Color(0.9f, 0.7f, 0.2f), new Color(0.2f, 0.55f, 0.35f),
            new Color(0.5f, 0.3f, 0.6f), new Color(0.9f, 0.9f, 0.85f), new Color(0.3f, 0.3f, 0.35f), new Color(0.9f, 0.45f, 0.2f)
        };

        static void Book(Vector3 bottom, Vector3 front, int index, float thickness = 0.05f, float yaw = -1f)
        {
            if (yaw < 0f) yaw = Quaternion.LookRotation(front).eulerAngles.y;
            float h = 0.2f + (index % 4) * 0.025f;
            var b = Fallable("Libro", bottom + Vector3.up * (h * 0.5f), 4, false, front, 0.5f, yaw);
            Part(b, PrimitiveType.Cube, Vector3.zero, new Vector3(thickness, h, 0.16f), Palette.Solid(BookColors[index % BookColors.Length], 0.3f), true);
            FinishFallable(b);
        }

        /// <summary>Cuadro colgado: cae al piso durante el sismo.</summary>
        static void Frame(Vector3 wallPoint, Vector3 facing, Vector2 size, int art)
        {
            float yaw = Quaternion.LookRotation(facing).eulerAngles.y;
            var f = Fallable("Cuadro", wallPoint + facing * 0.03f, 8, false, facing, 2f, yaw);
            Part(f, PrimitiveType.Cube, Vector3.zero, new Vector3(size.x, size.y, 0.04f), darkWood, true);
            Material canvas;
            switch (art % 4)
            {
                case 0: canvas = Palette.Textured("manta", Color.white, new Vector2(0.5f, 0.5f)); break;
                case 1: canvas = Palette.Solid(new Color(0.45f, 0.65f, 0.85f)); break;
                case 2: canvas = Palette.Solid(new Color(0.95f, 0.8f, 0.55f)); break;
                default: canvas = Palette.Solid(new Color(0.55f, 0.75f, 0.5f)); break;
            }
            Part(f, PrimitiveType.Cube, new Vector3(0f, 0f, 0.021f), new Vector3(size.x - 0.08f, size.y - 0.08f, 0.005f), canvas);
            if (art % 4 != 0)
            {
                // Paisaje andino simple: montaña nevada
                Part(f, PrimitiveType.Cube, new Vector3(-0.05f, -size.y * 0.12f, 0.025f), new Vector3(size.y * 0.45f, size.y * 0.45f, 0.004f), Palette.Solid(new Color(0.35f, 0.45f, 0.4f)), false, new Vector3(0f, 0f, 45f));
                Part(f, PrimitiveType.Cube, new Vector3(-0.05f, size.y * 0.05f, 0.027f), new Vector3(size.y * 0.15f, size.y * 0.15f, 0.004f), Palette.Solid(Color.white), false, new Vector3(0f, 0f, 45f));
            }
            FinishFallable(f);
        }

        static void HangingLamp(Vector3 p, Color shade)
        {
            Cyl("Cable", p + Vector3.up * 0.42f, 0.015f, 0.7f, black, false);
            var l = Fallable("Lámpara colgante", p, 15, false, Vector3.zero, 3f);
            Part(l, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.12f, 0.5f), Palette.Solid(shade, 0.4f), true);
            Part(l, PrimitiveType.Sphere, new Vector3(0f, -0.1f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Palette.Glow(new Color(1f, 0.85f, 0.6f), 3f, true));
            FinishFallable(l);
        }

        static void Plant(Vector3 floor, float scale)
        {
            Cyl("Maceta", floor + Vector3.up * 0.18f * scale, 0.32f * scale, 0.36f * scale, Palette.Solid(Palette.Terracotta, 0.2f));
            var leaf = Palette.Solid(new Color(0.25f, 0.55f, 0.3f), 0.2f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var s = DisasterReady.Build.Sphere("Hoja", floor + new Vector3(Mathf.Cos(a) * 0.1f, 0.5f + (i % 2) * 0.12f, Mathf.Sin(a) * 0.1f) * scale,
                    new Vector3(0.18f, 0.4f, 0.18f) * scale, leaf, root, false);
                s.transform.rotation = Quaternion.Euler(Mathf.Sin(a) * 25f, 0f, Mathf.Cos(a) * 25f);
            }
        }

        /// <summary>Reloj de pared (marca la hora del sismo).</summary>
        static void Clock(Vector3 wallPoint, Vector3 facing)
        {
            var g = DisasterReady.Build.Group("Reloj", wallPoint + facing * 0.03f, root, Quaternion.LookRotation(facing).eulerAngles.y);
            Part(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.36f, 0.025f, 0.36f), black, false, new Vector3(90f, 0f, 0f));
            Part(g, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.012f), new Vector3(0.32f, 0.02f, 0.32f), Palette.Solid(new Color(0.98f, 0.97f, 0.92f), 0.6f), false, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Part(g, PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.13f, Mathf.Cos(a) * 0.13f, 0.024f), new Vector3(0.012f, 0.03f, 0.004f), black, false, new Vector3(0f, 0f, -i * 30f));
            }
            // 4:35
            Part(g, PrimitiveType.Cube, new Vector3(0.035f, -0.02f, 0.03f), new Vector3(0.014f, 0.09f, 0.004f), black, false, new Vector3(0f, 0f, -127f));
            Part(g, PrimitiveType.Cube, new Vector3(-0.02f, -0.05f, 0.032f), new Vector3(0.01f, 0.12f, 0.004f), black, false, new Vector3(0f, 0f, 150f));
        }

        static void Calendar(Vector3 wallPoint, Vector3 facing)
        {
            var g = DisasterReady.Build.Group("Calendario", wallPoint + facing * 0.012f, root, Quaternion.LookRotation(facing).eulerAngles.y);
            Part(g, PrimitiveType.Cube, Vector3.zero, new Vector3(0.32f, 0.45f, 0.006f), Palette.Solid(Color.white, 0.2f));
            Part(g, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.004f), new Vector3(0.28f, 0.16f, 0.004f), manta);
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 7; c++)
                    Part(g, PrimitiveType.Cube, new Vector3(-0.12f + c * 0.04f, -0.02f - r * 0.045f, 0.004f), new Vector3(0.025f, 0.025f, 0.003f),
                         Palette.Solid(r == 2 && c == 3 ? Palette.Danger : new Color(0.75f, 0.75f, 0.75f)));
        }

        static void Rug(Vector3 floor, Vector2 size)
        {
            Box("Alfombra", floor + Vector3.up * 0.017f, new Vector3(size.x, 0.008f, size.y), manta, false);
        }

        /// <summary>Columna estructural con la señal ZONA SEGURA y su zona.</summary>
        static void Column(Vector3 floor, Vector3 signFacing, Vector3 zoneCenter)
        {
            float h = floor.y < 1f ? HouseLayout.WallHeight(0) : HouseLayout.WallHeight(1);
            Box("Columna", floor + Vector3.up * (h * 0.5f), new Vector3(0.38f, h, 0.38f), Palette.Solid(Palette.WarmWhite, 0.2f));
            DisasterReady.Build.SafeSign(floor + Vector3.up * 1.65f + signFacing * 0.2f, signFacing, root);
            if (game) SafeZone.Create(ZoneType.Columna, zoneCenter, new Vector3(1.2f, 2f, 1.2f), root);
        }
    }

    /// <summary>Datos temporales mientras se arma un objeto que puede caer.</summary>
    public class FallableSetup : MonoBehaviour
    {
        public int Damage;
        public bool Heavy;
        public Vector3 Tip;
        public float Mass;

        public void Configure(int damage, bool heavy, Vector3 tip, float mass)
        {
            Damage = damage;
            Heavy = heavy;
            Tip = tip;
            Mass = mass;
        }
    }
}
