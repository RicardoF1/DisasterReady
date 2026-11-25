using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>Hueco en un muro: puerta o ventana.</summary>
    public struct Opening
    {
        public int Floor;        // 0 = primer piso, 1 = segundo piso
        public bool AlongX;      // muro paralelo al eje X (norte/sur)
        public float Fixed;      // z del muro si AlongX, x si no
        public float From, To;   // extremos a lo largo del muro
        public float Bottom, Top;// alturas relativas al piso
        public bool IsWindow;
        public float Inward;     // +1/-1: hacia dónde está el interior (solo exteriores)

        public float Middle => (From + To) * 0.5f;
        public float Width => To - From;

        public static Opening Door(int floor, bool alongX, float fixedCoord, float from, float to)
            => new Opening { Floor = floor, AlongX = alongX, Fixed = fixedCoord, From = from, To = to, Bottom = 0f, Top = HouseLayout.DoorTop };

        public static Opening Window(int floor, bool alongX, float fixedCoord, float from, float to, float inward,
                                     float bottom = HouseLayout.WindowBottom, float top = HouseLayout.WindowTop)
            => new Opening { Floor = floor, AlongX = alongX, Fixed = fixedCoord, From = from, To = to, Bottom = bottom, Top = top, IsWindow = true, Inward = inward };

        /// <summary>Centro del hueco en coordenadas de mundo.</summary>
        public Vector3 Center
        {
            get
            {
                float y = HouseLayout.FloorY(Floor) + (Bottom + Top) * 0.5f;
                return AlongX ? new Vector3(Middle, y, Fixed) : new Vector3(Fixed, y, Middle);
            }
        }

        /// <summary>Dirección hacia el interior de la casa.</summary>
        public Vector3 InwardDirection => AlongX ? new Vector3(0f, 0f, Inward) : new Vector3(Inward, 0f, 0f);
    }

    /// <summary>
    /// Plano de la vivienda de dos pisos (12 x 10 m), origen en la esquina suroeste.
    ///
    /// PRIMER PISO (y = 0)                      SEGUNDO PISO (y = 3.2)
    ///  z=10 +--------+--------+---------+       +-------+--------------------+
    ///       | BAÑO   |ESTUDIO | ESCALERA|       | BAÑO  |  PASILLO   [hueco] |
    ///  z=6  +---  ---+--  ----+--  -----+  z=5  +-------+--  ------+--  -----+
    ///       |      SALA       | COCINA  |       |  DORM. PADRES   | DORM. JOVEN|
    ///  z=0  +----- PUERTA ----+---------+       +-----------------+-----------+
    ///      x=0               x=7      x=12     x=0               x=6         x=12
    /// </summary>
    public static class HouseLayout
    {
        public const float Width = 12f;
        public const float Depth = 10f;
        public const float Thickness = 0.2f;
        public const float SlabThickness = 0.2f;
        public const float DoorTop = 2.2f;
        public const float WindowBottom = 0.95f;
        public const float WindowTop = 2.15f;

        public static float FloorY(int floor) => floor == 0 ? 0f : 3.2f;
        public static float WallHeight(int floor) => floor == 0 ? 3.0f : 2.9f;

        // Escalera: sube hacia -x pegada al muro norte del primer piso
        public const float StairBottomX = 11.3f;  // borde del descanso
        public const float StairTopX = 7.25f;
        public const float StairZMin = 8.6f;
        public const float StairZMax = 9.85f;
        public const int StairSteps = 15;
        public static readonly Vector2 StairHoleMin = new Vector2(7.2f, 8.5f);   // hueco en la losa (x, z)
        public static readonly Vector2 StairHoleMax = new Vector2(11.9f, 9.9f);

        public static readonly Vector3 MeetingPoint = new Vector3(5f, 0f, -12f);
        public static readonly Vector3 MeetingPointSize = new Vector3(7f, 3f, 5f);

        public static readonly List<Opening> Openings = new List<Opening>
        {
            // ---------- Primer piso: exteriores ----------
            Opening.Window(0, true, 0f, 0.8f, 2.4f, +1f),        // sala, sur
            Opening.Door(0, true, 0f, 3.2f, 4.4f),               // PUERTA PRINCIPAL
            Opening.Window(0, true, 0f, 5.0f, 6.4f, +1f),        // sala, sur
            Opening.Window(0, true, 0f, 8.5f, 10.5f, +1f),       // cocina, sur
            Opening.Window(0, false, 0f, 2.0f, 4.0f, +1f),       // sala, oeste
            Opening.Window(0, false, 0f, 7.8f, 8.8f, +1f, 1.5f, 2.2f), // baño, oeste (alta)
            Opening.Window(0, false, 12f, 2.0f, 4.0f, -1f),      // cocina, este
            Opening.Window(0, true, 10f, 3.6f, 4.6f, -1f),       // estudio, norte

            // ---------- Primer piso: interiores ----------
            Opening.Door(0, false, 7f, 2.0f, 3.2f),              // sala - cocina
            Opening.Door(0, false, 7f, 6.6f, 7.8f),              // estudio - hall
            Opening.Door(0, true, 6f, 4.6f, 5.6f),               // sala - estudio
            Opening.Door(0, true, 6f, 9.0f, 10.2f),              // cocina - hall (se bloquea con escombros)
            Opening.Door(0, false, 3f, 8.0f, 8.9f),              // estudio - baño

            // ---------- Segundo piso: exteriores ----------
            Opening.Window(1, true, 0f, 1.5f, 3.5f, +1f),        // dorm. padres, sur
            Opening.Window(1, true, 0f, 8.0f, 10.5f, +1f),       // dorm. joven, sur
            Opening.Window(1, false, 0f, 2.0f, 3.5f, +1f),       // dorm. padres, oeste
            Opening.Window(1, false, 0f, 8.2f, 9.0f, +1f, 1.5f, 2.2f), // baño 2, oeste
            Opening.Window(1, false, 12f, 2.0f, 3.5f, -1f),      // dorm. joven, este
            Opening.Window(1, true, 10f, 4.0f, 6.0f, -1f),       // pasillo, norte

            // ---------- Segundo piso: interiores ----------
            Opening.Door(1, true, 5f, 4.2f, 5.2f),               // dorm. padres
            Opening.Door(1, true, 5f, 7.0f, 8.0f),               // dorm. joven
            Opening.Door(1, false, 3f, 8.0f, 8.9f),              // baño 2
        };

        /// <summary>Ruta de evacuación desde el segundo piso hasta el punto de reunión.</summary>
        public static readonly Vector3[] EvacuationRoute =
        {
            new Vector3(7.5f, 3.2f, 5.6f),    // salir del dormitorio
            new Vector3(6.4f, 3.2f, 9.2f),    // pasillo hacia la escalera
            new Vector3(11.55f, 0.2f, 8.0f),  // descanso al pie de la escalera
            new Vector3(9.0f, 0f, 7.2f),      // hall
            new Vector3(6.4f, 0f, 7.2f),      // puerta al estudio
            new Vector3(5.1f, 0f, 6.6f),      // estudio
            new Vector3(5.1f, 0f, 5.2f),      // puerta a la sala
            new Vector3(3.8f, 0f, 1.6f),      // sala
            new Vector3(3.8f, 0f, -1.0f),     // puerta principal
            new Vector3(4.5f, 0f, -7.0f),     // jardín
            new Vector3(5f, 0f, -12f),        // PUNTO DE REUNIÓN
        };
    }
}
