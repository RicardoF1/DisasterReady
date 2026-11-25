using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>Ventana construida (para romper el vidrio durante el sismo).</summary>
    public class WindowInfo
    {
        public GameObject Pane;
        public Opening Opening;
        public bool Broken;
    }

    /// <summary>
    /// Construye la vivienda de dos pisos: losas, muros con puertas y ventanas, escalera,
    /// barandas, azotea con tanque de agua y luces. Luego llama al entorno y al mobiliario.
    /// Se crea en la escena House y también en el menú (como fondo animado).
    /// </summary>
    public class HouseBuilder : MonoBehaviour
    {
        public static HouseBuilder Instance { get; private set; }

        public readonly List<WindowInfo> Windows = new List<WindowInfo>();
        public readonly List<Light> RoomLights = new List<Light>();
        public readonly List<float> RoomLightIntensity = new List<float>();
        public bool IsGameScene { get; private set; }

        const float T = HouseLayout.Thickness;

        Material exteriorWall, interiorWall, slab, frame, band;

        // ---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister()
        {
            SceneBoot.Register(GameConfig.HouseScene, () => Spawn(true));
            SceneBoot.Register(GameConfig.MenuScene, () => Spawn(false));
        }

        static void Spawn(bool gameScene)
        {
            if (Instance != null) return;
            var go = new GameObject("Vivienda");
            var builder = go.AddComponent<HouseBuilder>();
            builder.IsGameScene = gameScene;
            builder.BuildAll();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void BuildAll()
        {
            exteriorWall = Palette.Textured("plaster", new Color(0.96f, 0.84f, 0.6f), new Vector2(2f, 1f), 0.1f);
            interiorWall = Palette.Textured("plaster", Palette.WarmWhite, new Vector2(2f, 1f), 0.1f);
            slab = Palette.Textured("plaster", new Color(0.97f, 0.96f, 0.94f), new Vector2(3f, 3f), 0.05f);
            frame = Palette.Solid(Palette.WarmWhite, 0.4f);
            band = Palette.Solid(Palette.Terracotta, 0.2f);

            BuildFloors();
            BuildWalls();
            RoomFinish.Build(transform);
            BuildStairs();
            BuildRoof();
            BuildLights();
            BuildFrontDoor();
            BuildExitSigns();

            EnvironmentBuilder.Build(transform, IsGameScene);
            Furnishing.Build(this);
            EvacuationRoute.Set(HouseLayout.EvacuationRoute);
        }

        // ---------------------------------------------------------------------
        // Pisos y losas
        // ---------------------------------------------------------------------
        void BuildFloors()
        {
            var t = transform;
            // Losa base del primer piso (colisión)
            Build.Block("Losa base", new Vector3(-0.1f, -0.25f, -0.1f), new Vector3(12.1f, 0f, 10.1f), Palette.Textured("concrete", Color.white, new Vector2(4, 4)), t);

            // Acabados de piso por ambiente (sin colisión, 1 cm sobre la losa)
            var wood = Palette.Textured("wood", Color.white, new Vector2(3f, 3f), 0.35f);
            var tiles = Palette.Textured("tiles", Color.white, new Vector2(5f, 6f), 0.5f);
            var bathTiles = Palette.Textured("tiles_blue", Color.white, new Vector2(3f, 4f), 0.6f);
            FloorFinish("Piso sala", 0, 0f, 0f, 7f, 6f, wood);
            FloorFinish("Piso cocina", 0, 7f, 0f, 12f, 6f, tiles);
            FloorFinish("Piso hall", 0, 7f, 6f, 12f, 10f, wood);
            FloorFinish("Piso estudio", 0, 3f, 6f, 7f, 10f, wood);
            FloorFinish("Piso baño", 0, 0f, 6f, 3f, 10f, bathTiles);

            // Losa entre pisos con el hueco de la escalera
            float y0 = HouseLayout.WallHeight(0), y1 = HouseLayout.FloorY(1);
            Vector2 hMin = HouseLayout.StairHoleMin, hMax = HouseLayout.StairHoleMax;
            Build.Block("Losa 2do piso A", new Vector3(0f, y0, 0f), new Vector3(12f, y1, hMin.y), slab, t);
            Build.Block("Losa 2do piso B", new Vector3(0f, y0, hMin.y), new Vector3(hMin.x, y1, 10f), slab, t);
            Build.Block("Losa 2do piso C", new Vector3(hMax.x, y0, hMin.y), new Vector3(12f, y1, 10f), slab, t);

            FloorFinish("Piso dorm. padres", 1, 0f, 0f, 6f, 5f, wood);
            FloorFinish("Piso dorm. joven", 1, 6f, 0f, 12f, 5f, wood);
            FloorFinish("Piso pasillo A", 1, 3f, 5f, 12f, hMin.y, wood);
            FloorFinish("Piso pasillo B", 1, 3f, hMin.y, hMin.x, 10f, wood);
            FloorFinish("Piso baño 2", 1, 0f, 5f, 3f, 10f, bathTiles);

            // Cornisa exterior entre pisos
            Build.Block("Cornisa sur", new Vector3(-0.15f, y0 - 0.05f, -0.15f), new Vector3(12.15f, y1 + 0.05f, 0.05f), band, t, false);
            Build.Block("Cornisa norte", new Vector3(-0.15f, y0 - 0.05f, 9.95f), new Vector3(12.15f, y1 + 0.05f, 10.15f), band, t, false);
            Build.Block("Cornisa oeste", new Vector3(-0.15f, y0 - 0.05f, -0.15f), new Vector3(0.05f, y1 + 0.05f, 10.15f), band, t, false);
            Build.Block("Cornisa este", new Vector3(11.95f, y0 - 0.05f, -0.15f), new Vector3(12.15f, y1 + 0.05f, 10.15f), band, t, false);
        }

        void FloorFinish(string name, int floor, float x0, float z0, float x1, float z1, Material material)
        {
            float y = HouseLayout.FloorY(floor);
            Build.Block(name, new Vector3(x0, y, z0), new Vector3(x1, y + 0.012f, z1), material, transform, false);
        }

        // ---------------------------------------------------------------------
        // Muros
        // ---------------------------------------------------------------------
        void BuildWalls()
        {
            for (int floor = 0; floor < 2; floor++)
            {
                Wall(floor, true, 0f, -T / 2f, 12f + T / 2f, exteriorWall, interiorWall);
                Wall(floor, true, 10f, -T / 2f, 12f + T / 2f, exteriorWall, interiorWall);
                Wall(floor, false, 0f, T / 2f, 10f - T / 2f, exteriorWall, interiorWall);
                Wall(floor, false, 12f, T / 2f, 10f - T / 2f, exteriorWall, interiorWall);
            }

            // Primer piso
            Wall(0, false, 7f, T / 2f, 10f - T / 2f, interiorWall, null);
            Wall(0, true, 6f, T / 2f, 7f - T / 2f, interiorWall, null);
            Wall(0, true, 6f, 7f + T / 2f, 12f - T / 2f, interiorWall, null);
            Wall(0, false, 3f, 6f + T / 2f, 10f - T / 2f, interiorWall, null);

            // Segundo piso
            Wall(1, true, 5f, T / 2f, 12f - T / 2f, interiorWall, null);
            Wall(1, false, 6f, T / 2f, 5f - T / 2f, interiorWall, null);
            Wall(1, true, 7f, T / 2f, 3f - T / 2f, interiorWall, null);
            Wall(1, false, 3f, 7f + T / 2f, 10f - T / 2f, interiorWall, null);
        }

        /// <summary>Construye un muro recto dejando los huecos de puertas y ventanas.</summary>
        void Wall(int floor, bool alongX, float fixedCoord, float from, float to, Material outer, Material inner)
        {
            var gaps = new List<Opening>();
            foreach (var o in HouseLayout.Openings)
                if (o.Floor == floor && o.AlongX == alongX && Mathf.Abs(o.Fixed - fixedCoord) < 0.01f
                    && o.From >= from - 0.01f && o.To <= to + 0.01f)
                    gaps.Add(o);
            gaps.Sort((a, b) => a.From.CompareTo(b.From));

            float baseY = HouseLayout.FloorY(floor);
            float h = HouseLayout.WallHeight(floor);
            var root = Build.Group($"Muro P{floor + 1} {(alongX ? "z" : "x")}={fixedCoord}", Vector3.zero, transform);

            float cursor = from;
            foreach (var g in gaps)
            {
                if (g.From > cursor) Segment(root, alongX, fixedCoord, cursor, g.From, baseY, baseY + h, outer, inner);
                if (g.Bottom > 0f) Segment(root, alongX, fixedCoord, g.From, g.To, baseY, baseY + g.Bottom, outer, inner);
                if (g.Top < h) Segment(root, alongX, fixedCoord, g.From, g.To, baseY + g.Top, baseY + h, outer, inner);
                if (g.IsWindow) WindowPane(root, g);
                else DoorFrame(root, g);
                cursor = g.To;
            }
            if (cursor < to) Segment(root, alongX, fixedCoord, cursor, to, baseY, baseY + h, outer, inner);
        }

        /// <summary>
        /// Un tramo de muro. Los muros exteriores se hacen con dos capas para pintar
        /// distinto el exterior (amarillo ocre) y el interior (blanco).
        /// </summary>
        void Segment(Transform parent, bool alongX, float fixedCoord, float a, float b, float y0, float y1, Material outer, Material inner)
        {
            if (b - a < 0.01f || y1 - y0 < 0.01f) return;
            Vector3 Center(float offset) => alongX
                ? new Vector3((a + b) * 0.5f, (y0 + y1) * 0.5f, fixedCoord + offset)
                : new Vector3(fixedCoord + offset, (y0 + y1) * 0.5f, (a + b) * 0.5f);
            Vector3 Size(float thickness) => alongX ? new Vector3(b - a, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, b - a);

            if (inner == null)
            {
                Build.Box("Muro", Center(0f), Size(T), outer, parent);
                return;
            }

            // Exterior: el lado de afuera depende de la posición del muro
            bool outsideIsNegative = fixedCoord < 1f;
            float sign = outsideIsNegative ? -1f : 1f;
            Build.Box("Muro exterior", Center(sign * T * 0.25f), Size(T * 0.5f), outer, parent);
            Build.Box("Muro interior", Center(-sign * T * 0.25f), Size(T * 0.5f), inner, parent);
        }

        void WindowPane(Transform parent, Opening g)
        {
            float baseY = HouseLayout.FloorY(g.Floor);
            Vector3 center = g.Center;
            Vector3 paneSize = g.AlongX ? new Vector3(g.Width, g.Top - g.Bottom, 0.04f) : new Vector3(0.04f, g.Top - g.Bottom, g.Width);
            var pane = Build.Box("Vidrio", center, paneSize, Palette.Glass(new Color(0.75f, 0.9f, 1f), 0.28f), parent);
            Windows.Add(new WindowInfo { Pane = pane, Opening = g });

            // Marco, travesaño y alféizar
            Vector3 along = g.AlongX ? Vector3.right : Vector3.forward;
            Vector3 across = g.AlongX ? Vector3.forward : Vector3.right;
            float w = g.Width, h = g.Top - g.Bottom;
            Vector3 Thin(float alongSize, float height, float depth) => along * alongSize + Vector3.up * height + across * depth;

            Build.Box("Marco sup", center + Vector3.up * (h * 0.5f), Thin(w + 0.1f, 0.06f, T + 0.04f), frame, parent, false);
            Build.Box("Marco inf", center - Vector3.up * (h * 0.5f), Thin(w + 0.1f, 0.06f, T + 0.08f), frame, parent, false);
            Build.Box("Marco izq", center - along * (w * 0.5f), Thin(0.06f, h, T + 0.04f), frame, parent, false);
            Build.Box("Marco der", center + along * (w * 0.5f), Thin(0.06f, h, T + 0.04f), frame, parent, false);
            Build.Box("Travesaño", center, Thin(0.04f, h, 0.07f), frame, parent, false);
            Build.Box("Alféizar", new Vector3(center.x, baseY + g.Bottom - 0.03f, center.z) + g.InwardDirection * 0.12f,
                      Thin(w + 0.2f, 0.05f, 0.18f), Palette.Solid(new Color(0.8f, 0.78f, 0.74f)), parent, false);
        }

        void DoorFrame(Transform parent, Opening g)
        {
            float baseY = HouseLayout.FloorY(g.Floor);
            Vector3 along = g.AlongX ? Vector3.right : Vector3.forward;
            Vector3 across = g.AlongX ? Vector3.forward : Vector3.right;
            Vector3 bottom = g.AlongX ? new Vector3(g.Middle, baseY, g.Fixed) : new Vector3(g.Fixed, baseY, g.Middle);
            float w = g.Width, h = g.Top;
            var wood = Palette.Solid(Palette.DarkWood, 0.3f);
            Build.Box("Jamba izq", bottom - along * (w * 0.5f) + Vector3.up * (h * 0.5f), along * 0.08f + Vector3.up * h + across * (T + 0.05f), wood, parent, false);
            Build.Box("Jamba der", bottom + along * (w * 0.5f) + Vector3.up * (h * 0.5f), along * 0.08f + Vector3.up * h + across * (T + 0.05f), wood, parent, false);
            Build.Box("Dintel", bottom + Vector3.up * h, along * (w + 0.16f) + Vector3.up * 0.08f + across * (T + 0.05f), wood, parent, false);
        }

        // ---------------------------------------------------------------------
        // Escalera y barandas
        // ---------------------------------------------------------------------
        void BuildStairs()
        {
            var t = transform;
            var stairMat = Palette.Textured("wood", new Color(0.85f, 0.75f, 0.65f), new Vector2(1f, 1f), 0.3f);
            var sideMat = Palette.Solid(new Color(0.9f, 0.88f, 0.84f));
            float zMin = HouseLayout.StairZMin, zMax = HouseLayout.StairZMax;
            float rise = HouseLayout.FloorY(1) / (HouseLayout.StairSteps + 1);
            float tread = (HouseLayout.StairBottomX - HouseLayout.StairTopX) / HouseLayout.StairSteps;

            // Descanso al pie (se entra desde el sur)
            Build.Block("Descanso escalera", new Vector3(HouseLayout.StairBottomX, 0f, 7.7f), new Vector3(11.9f, rise, zMax), stairMat, t);

            // Peldaños macizos (con colisión: el CharacterController sube escalones de 0.2 m)
            for (int i = 1; i <= HouseLayout.StairSteps; i++)
            {
                float xHigh = HouseLayout.StairBottomX - (i - 1) * tread;
                float xLow = xHigh - tread;
                float top = rise * (i + 1);
                Build.Block("Peldaño " + i, new Vector3(xLow, 0f, zMin), new Vector3(xHigh, top, zMax), i % 2 == 0 ? stairMat : stairMat, t);
                Build.Block("Contrahuella " + i, new Vector3(xHigh - 0.01f, top - rise, zMin), new Vector3(xHigh + 0.005f, top, zMax), sideMat, t, false);
            }

            // Rampa invisible sobre las narices de los peldaños: la subida se siente suave
            Vector3 a = new Vector3(HouseLayout.StairBottomX + tread, rise, (zMin + zMax) * 0.5f);
            Vector3 b = new Vector3(HouseLayout.StairBottomX - (HouseLayout.StairSteps - 1) * tread, HouseLayout.FloorY(1), (zMin + zMax) * 0.5f);
            Vector3 dir = b - a;
            var ramp = Build.Box("Rampa escalera", (a + b) * 0.5f, new Vector3(dir.magnitude, 0.05f, zMax - zMin), sideMat, t);
            ramp.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir.normalized);
            ramp.transform.position -= ramp.transform.up * 0.025f;
            ramp.GetComponent<Renderer>().enabled = false;

            // Pasamanos inclinado
            var rail = Palette.Solid(Palette.DarkWood, 0.5f);
            var handrail = Build.Box("Pasamanos", (a + b) * 0.5f + new Vector3(0f, 0.9f, zMin - (zMax + zMin) * 0.5f + 0.04f),
                                     new Vector3(dir.magnitude, 0.06f, 0.06f), rail, t, false);
            handrail.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir.normalized);
            for (int i = 1; i <= HouseLayout.StairSteps; i += 2)
            {
                float x = HouseLayout.StairBottomX - (i - 0.5f) * tread;
                float y = rise * (i + 1);
                Build.Box("Balaustre", new Vector3(x, y + 0.45f, zMin + 0.04f), new Vector3(0.04f, 0.9f, 0.04f), rail, t, false);
            }

            // Baranda del 2do piso alrededor del hueco
            float fy = HouseLayout.FloorY(1);
            Vector2 hMin = HouseLayout.StairHoleMin, hMax = HouseLayout.StairHoleMax;
            Build.Block("Baranda (colisión)", new Vector3(hMin.x, fy, hMin.y - 0.06f), new Vector3(hMax.x, fy + 1.0f, hMin.y), rail, t).GetComponent<Renderer>().enabled = false;
            Build.Box("Baranda pasamanos", new Vector3((hMin.x + hMax.x) * 0.5f, fy + 0.97f, hMin.y - 0.03f), new Vector3(hMax.x - hMin.x, 0.06f, 0.08f), rail, t, false);
            for (float x = hMin.x + 0.1f; x < hMax.x; x += 0.3f)
                Build.Box("Balaustre", new Vector3(x, fy + 0.5f, hMin.y - 0.03f), new Vector3(0.035f, 0.95f, 0.035f), rail, t, false);
        }

        // ---------------------------------------------------------------------
        // Azotea típica: losa plana, parapeto, tanque de agua y fierros de columna
        // ---------------------------------------------------------------------
        void BuildRoof()
        {
            var t = transform;
            float top = HouseLayout.FloorY(1) + HouseLayout.WallHeight(1);
            Build.Block("Losa azotea", new Vector3(-0.1f, top, -0.1f), new Vector3(12.1f, top + 0.2f, 10.1f), slab, t);
            float py = top + 0.2f;
            var parapet = exteriorWall;
            Build.Block("Parapeto sur", new Vector3(-0.1f, py, -0.1f), new Vector3(12.1f, py + 0.7f, 0.1f), parapet, t);
            Build.Block("Parapeto norte", new Vector3(-0.1f, py, 9.9f), new Vector3(12.1f, py + 0.7f, 10.1f), parapet, t);
            Build.Block("Parapeto oeste", new Vector3(-0.1f, py, 0.1f), new Vector3(0.1f, py + 0.7f, 9.9f), parapet, t);
            Build.Block("Parapeto este", new Vector3(11.9f, py, 0.1f), new Vector3(12.1f, py + 0.7f, 9.9f), parapet, t);
            Build.Block("Remate parapeto", new Vector3(-0.15f, py + 0.7f, -0.15f), new Vector3(12.15f, py + 0.78f, 0.15f), band, t, false);

            // Tanque de agua (negro) sobre una base
            Build.Block("Base tanque", new Vector3(9.4f, py, 7.6f), new Vector3(11.2f, py + 0.4f, 9.4f), Palette.Textured("concrete", Color.white, Vector2.one), t, false);
            Build.Cylinder("Tanque de agua", new Vector3(10.3f, py + 1.15f, 8.5f), 1.3f, 1.5f, Palette.Solid(new Color(0.08f, 0.08f, 0.09f), 0.6f), t, false);
            Build.Cylinder("Tapa tanque", new Vector3(10.3f, py + 1.95f, 8.5f), 0.6f, 0.1f, Palette.Solid(new Color(0.12f, 0.12f, 0.13f), 0.6f), t, false);

            // Fierros de construcción en las esquinas (muy típico en el Perú)
            var rebar = Palette.Solid(new Color(0.35f, 0.22f, 0.15f), 0.3f, 0.6f);
            foreach (var corner in new[] { new Vector2(0f, 0f), new Vector2(12f, 0f), new Vector2(0f, 10f), new Vector2(12f, 10f), new Vector2(7f, 10f) })
                for (int i = 0; i < 4; i++)
                {
                    float ox = (i % 2) * 0.12f - 0.06f, oz = (i / 2) * 0.12f - 0.06f;
                    Build.Cylinder("Fierro", new Vector3(corner.x + ox, py + 1.2f, corner.y + oz), 0.025f, 1.0f, rebar, t, false);
                }

            // Antena
            Build.Cylinder("Antena", new Vector3(2f, py + 1.4f, 8.5f), 0.04f, 2.8f, Palette.Solid(Palette.Steel, 0.6f, 0.8f), t, false);
            Build.Box("Antena brazo", new Vector3(2f, py + 2.5f, 8.5f), new Vector3(1.2f, 0.03f, 0.03f), Palette.Solid(Palette.Steel, 0.6f, 0.8f), t, false);
        }

        // ---------------------------------------------------------------------
        // Iluminación interior
        // ---------------------------------------------------------------------
        void BuildLights()
        {
            AddRoomLight("Luz sala", new Vector3(3.5f, 2.72f, 3.0f), 2.6f, true);
            AddRoomLight("Luz cocina", new Vector3(9.5f, 2.72f, 3.0f), 2.4f, true);
            AddRoomLight("Luz hall", new Vector3(9.3f, 2.72f, 7.3f), 1.8f);
            AddRoomLight("Luz estudio", new Vector3(5.0f, 2.72f, 8.0f), 2.0f);
            AddRoomLight("Luz baño", new Vector3(1.5f, 2.72f, 8.0f), 1.6f);
            AddRoomLight("Luz dorm. padres", new Vector3(3.0f, 5.82f, 2.5f), 2.2f, true);
            AddRoomLight("Luz dorm. joven", new Vector3(9.0f, 5.82f, 2.5f), 2.2f, true);
            AddRoomLight("Luz pasillo", new Vector3(5.5f, 5.82f, 7.4f), 1.8f);
            AddRoomLight("Luz baño 2", new Vector3(1.5f, 5.82f, 8.5f), 1.4f);
        }

        void AddRoomLight(string name, Vector3 position, float intensity, bool shadows = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position - Vector3.up * 0.15f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 7f;
            light.intensity = intensity;
            light.color = new Color(1f, 0.86f, 0.68f);
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.75f;
            RoomLights.Add(light);
            RoomLightIntensity.Add(intensity);

            // Plafón
            Build.Cylinder("Plafón", position + Vector3.up * 0.18f, 0.55f, 0.08f, Palette.Glow(new Color(1f, 0.9f, 0.72f), 2.2f, true), transform, false);
            Build.Cylinder("Aro plafón", position + Vector3.up * 0.23f, 0.62f, 0.03f, Palette.Solid(Palette.WarmWhite), transform, false);
        }

        public void SetLights(bool on, float factor = 1f)
        {
            for (int i = 0; i < RoomLights.Count; i++)
            {
                if (RoomLights[i] == null) continue;
                RoomLights[i].enabled = on;
                RoomLights[i].intensity = RoomLightIntensity[i] * factor;
            }
            Palette.SetPower(on, factor);
        }

        // ---------------------------------------------------------------------
        void BuildFrontDoor()
        {
            var hinge = Build.Group("Puerta principal", new Vector3(3.22f, 0f, 0.08f), transform, -95f);
            var wood = Palette.Solid(new Color(0.45f, 0.25f, 0.14f), 0.35f);
            Build.Part(hinge, PrimitiveType.Cube, new Vector3(0.58f, 1.08f, 0f), new Vector3(1.14f, 2.14f, 0.05f), wood);
            for (int i = 0; i < 3; i++)
                Build.Part(hinge, PrimitiveType.Cube, new Vector3(0.58f, 0.45f + i * 0.62f, 0.03f), new Vector3(0.9f, 0.45f, 0.02f), Palette.Solid(new Color(0.38f, 0.2f, 0.11f), 0.35f));
            Build.Part(hinge, PrimitiveType.Sphere, new Vector3(1.0f, 1.0f, 0.06f), Vector3.one * 0.07f, Palette.Solid(Palette.Mustard, 0.8f, 0.9f));

            // Felpudo y escalón de ingreso
            Build.Block("Escalón ingreso", new Vector3(2.9f, -0.1f, -0.9f), new Vector3(4.7f, 0.0f, -0.05f), Palette.Textured("concrete", Color.white, Vector2.one), transform);
            Build.Box("Felpudo", new Vector3(3.8f, 0.005f, -0.5f), new Vector3(1.0f, 0.01f, 0.6f), Palette.Textured("manta", Color.white, new Vector2(1f, 1f)), transform, false);
        }

        void BuildExitSigns()
        {
            var t = transform;
            // Segundo piso (↑ = sigue de frente; → / ← según quien lee el letrero)
            Build.ExitSign(new Vector3(7.5f, 5.6f, 4.86f), Vector3.back, "↑", t);                 // puerta del dorm. joven
            Build.ExitSign(new Vector3(4.7f, 5.6f, 4.86f), Vector3.back, "↑", t);                 // puerta del dorm. padres
            Build.ExitSign(new Vector3(3.14f, 5.3f, 7.4f), Vector3.right, "→", t, "ESCALERA");   // pasillo: la escalera está a la derecha
            Build.ExitSign(new Vector3(9.5f, 5.55f, 9.86f), Vector3.back, "→", t, "ESCALERA");   // sobre el hueco de la escalera
            // Primer piso
            Build.ExitSign(new Vector3(11.86f, 2.4f, 8.0f), Vector3.left, "→", t);                // al terminar de bajar
            Build.ExitSign(new Vector3(7.14f, 2.5f, 7.2f), Vector3.right, "↑", t);                // puerta hacia el estudio
            Build.ExitSign(new Vector3(5.1f, 2.5f, 6.14f), Vector3.forward, "↑", t);              // estudio → sala
            Build.ExitSign(new Vector3(3.8f, 2.55f, 0.14f), Vector3.forward, "↑", t);             // puerta principal
        }
    }
}
