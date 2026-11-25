using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Acabados de cada ambiente: pintura de color propio en cada cuarto, mayólica en cocina y baños,
    /// zócalos, cornisas, cortinas e interruptores. Le da identidad visual a cada espacio.
    /// </summary>
    public static class RoomFinish
    {
        /// <summary>Un lado de un ambiente: el muro sobre el que se pinta.</summary>
        struct Side
        {
            public bool AlongX;
            public float Fixed, From, To, Inward;
            public Side(bool alongX, float fixedCoord, float from, float to, float inward)
            {
                AlongX = alongX; Fixed = fixedCoord; From = from; To = to; Inward = inward;
            }
        }

        static Transform root;
        static Material baseboard, crown;

        public static void Build(Transform parent)
        {
            root = DisasterReady.Build.Group("Acabados", Vector3.zero, parent);
            baseboard = Palette.Solid(new Color(0.42f, 0.3f, 0.2f), 0.35f);
            crown = Palette.Solid(new Color(0.98f, 0.97f, 0.94f), 0.3f);

            var tilesWhite = Palette.Textured("tiles", Color.white, new Vector2(4f, 2f), 0.6f);
            var tilesBlue = Palette.Textured("tiles_blue", Color.white, new Vector2(3f, 2f), 0.6f);

            // -------- Primer piso --------
            Room(0, Paint(new Color(0.95f, 0.8f, 0.66f)), null, 0f,                         // sala: terracota suave
                 S(true, 0f, 0f, 7f, +1), S(true, 6f, 0f, 7f, -1), S(false, 0f, 0f, 6f, +1), S(false, 7f, 0f, 6f, -1));
            Room(0, Paint(new Color(0.86f, 0.94f, 0.88f)), tilesWhite, 1.45f,               // cocina: menta + mayólica
                 S(true, 0f, 7f, 12f, +1), S(true, 6f, 7f, 12f, -1), S(false, 7f, 0f, 6f, +1), S(false, 12f, 0f, 6f, -1));
            Room(0, Paint(new Color(0.97f, 0.92f, 0.82f)), null, 0f,                         // hall: crema
                 S(true, 6f, 7f, 12f, +1), S(true, 10f, 7f, 12f, -1), S(false, 7f, 6f, 10f, +1), S(false, 12f, 6f, 10f, -1));
            Room(0, Paint(new Color(0.76f, 0.85f, 0.74f)), null, 0f,                         // estudio: verde salvia
                 S(true, 6f, 3f, 7f, +1), S(true, 10f, 3f, 7f, -1), S(false, 3f, 6f, 10f, +1), S(false, 7f, 6f, 10f, -1));
            Room(0, Paint(new Color(0.9f, 0.95f, 0.97f)), tilesBlue, 1.6f,                   // baño
                 S(true, 6f, 0f, 3f, +1), S(true, 10f, 0f, 3f, -1), S(false, 0f, 6f, 10f, +1), S(false, 3f, 6f, 10f, -1));

            // -------- Segundo piso --------
            Room(1, Paint(new Color(0.78f, 0.84f, 0.94f)), null, 0f,                         // dorm. padres: azul grisáceo
                 S(true, 0f, 0f, 6f, +1), S(true, 5f, 0f, 6f, -1), S(false, 0f, 0f, 5f, +1), S(false, 6f, 0f, 5f, -1));
            Room(1, Paint(new Color(0.99f, 0.84f, 0.58f)), null, 0f,                         // dorm. joven: mostaza
                 S(true, 0f, 6f, 12f, +1), S(true, 5f, 6f, 12f, -1), S(false, 6f, 0f, 5f, +1), S(false, 12f, 0f, 5f, -1));
            Room(1, Paint(new Color(0.97f, 0.92f, 0.84f)), null, 0f,                         // pasillo
                 S(true, 5f, 0f, 12f, +1), S(true, 10f, 3f, 12f, -1), S(true, 7f, 0f, 3f, -1),
                 S(false, 0f, 5f, 7f, +1), S(false, 3f, 7f, 10f, +1), S(false, 12f, 5f, 10f, -1));
            Room(1, Paint(new Color(0.9f, 0.95f, 0.97f)), tilesBlue, 1.6f,                   // baño 2
                 S(true, 7f, 0f, 3f, +1), S(true, 10f, 0f, 3f, -1), S(false, 0f, 7f, 10f, +1), S(false, 3f, 7f, 10f, -1));

            Curtains();
            Switches();
        }

        static Side S(bool alongX, float fixedCoord, float from, float to, float inward) => new Side(alongX, fixedCoord, from, to, inward);

        static Material Paint(Color c) => Palette.Textured("plaster", c, new Vector2(2f, 1f), 0.08f);

        // ---------------------------------------------------------------------
        static void Room(int floor, Material paint, Material wainscot, float wainscotHeight, params Side[] sides)
        {
            float y0 = HouseLayout.FloorY(floor);
            float h = HouseLayout.WallHeight(floor);
            foreach (var side in sides)
            {
                // Huecos de puertas y ventanas sobre este lado
                var gaps = new List<Opening>();
                foreach (var o in HouseLayout.Openings)
                    if (o.Floor == floor && o.AlongX == side.AlongX && Mathf.Abs(o.Fixed - side.Fixed) < 0.01f && o.To > side.From && o.From < side.To)
                        gaps.Add(o);
                gaps.Sort((a, b) => a.From.CompareTo(b.From));

                float from = side.From + 0.1f, to = side.To - 0.1f;
                float cursor = from;
                foreach (var g in gaps)
                {
                    if (g.From > cursor) Strip(side, floor, cursor, g.From, 0f, h, paint, wainscot, wainscotHeight, true);
                    if (g.Bottom > 0f) Strip(side, floor, g.From, g.To, 0f, g.Bottom, paint, wainscot, wainscotHeight, true);
                    if (g.Top < h) Strip(side, floor, g.From, g.To, g.Top, h, paint, wainscot, wainscotHeight, false);
                    cursor = Mathf.Max(cursor, g.To);
                }
                if (cursor < to) Strip(side, floor, cursor, to, 0f, h, paint, wainscot, wainscotHeight, true);

                // Cornisa a lo largo de todo el lado
                Piece(side, floor, from, to, h - 0.08f, h, 0.05f, crown);
            }
        }

        /// <summary>Franja de pared pintada (con mayólica abajo si corresponde) y su zócalo.</summary>
        static void Strip(Side side, int floor, float a, float b, float y0, float y1, Material paint, Material wainscot, float wainscotHeight, bool skirting)
        {
            a = Mathf.Max(a, side.From + 0.1f);
            b = Mathf.Min(b, side.To - 0.1f);
            if (b - a < 0.02f) return;

            if (wainscot != null && y0 < wainscotHeight)
            {
                float split = Mathf.Min(wainscotHeight, y1);
                Piece(side, floor, a, b, y0, split, 0.012f, wainscot);
                if (split < y1) Piece(side, floor, a, b, split, y1, 0.008f, paint);
                if (split < y1) Piece(side, floor, a, b, split - 0.02f, split + 0.02f, 0.02f, crown); // remate de la mayólica
            }
            else Piece(side, floor, a, b, y0, y1, 0.008f, paint);

            if (skirting && y0 <= 0.001f && wainscot == null) Piece(side, floor, a, b, 0f, 0.1f, 0.022f, baseboard);
        }

        /// <summary>Caja delgada pegada a la cara interior del muro.</summary>
        static void Piece(Side side, int floor, float a, float b, float y0, float y1, float depth, Material m)
        {
            float baseY = HouseLayout.FloorY(floor);
            float face = side.Fixed + side.Inward * (HouseLayout.Thickness * 0.5f + depth * 0.5f + 0.002f);
            float mid = (a + b) * 0.5f;
            Vector3 center = side.AlongX ? new Vector3(mid, baseY + (y0 + y1) * 0.5f, face) : new Vector3(face, baseY + (y0 + y1) * 0.5f, mid);
            Vector3 size = side.AlongX ? new Vector3(b - a, y1 - y0, depth) : new Vector3(depth, y1 - y0, b - a);
            DisasterReady.Build.Box("Acabado", center, size, m, root, false);
        }

        // ---------------------------------------------------------------------
        /// <summary>Cortinas a ambos lados de cada ventana exterior, con riel.</summary>
        static void Curtains()
        {
            Color[] fabrics = { new Color(0.85f, 0.35f, 0.3f), new Color(0.95f, 0.9f, 0.78f), new Color(0.3f, 0.5f, 0.6f) };
            int i = 0;
            foreach (var o in HouseLayout.Openings)
            {
                if (!o.IsWindow || Mathf.Abs(o.Inward) < 0.5f || o.Bottom > 1.2f) continue;
                var fabric = Palette.Solid(fabrics[i++ % fabrics.Length], 0.05f);
                float baseY = HouseLayout.FloorY(o.Floor);
                Vector3 along = o.AlongX ? Vector3.right : Vector3.forward;
                Vector3 inward = o.InwardDirection;
                Vector3 wallFace = (o.AlongX ? new Vector3(0f, 0f, o.Fixed) : new Vector3(o.Fixed, 0f, 0f)) + inward * 0.18f;
                Vector3 Line(float s) => (o.AlongX ? new Vector3(s, 0f, 0f) : new Vector3(0f, 0f, s)) + wallFace;

                float top = baseY + o.Top + 0.18f;
                float length = top - (baseY + o.Bottom - 0.12f); // cortinas hasta el alféizar
                // Riel
                var rail = DisasterReady.Build.Box("Riel cortina", Line(o.Middle) + Vector3.up * top, along * (o.Width + 0.6f) + Vector3.up * 0.03f + inward * 0.03f,
                                                   Palette.Solid(Palette.DarkWood, 0.4f), root, false);
                // Paños recogidos a cada lado, con pliegues
                foreach (float edge in new[] { o.From - 0.15f, o.To + 0.15f })
                    for (int f = 0; f < 3; f++)
                    {
                        float offset = (f - 1) * 0.09f;
                        Vector3 p = Line(edge + offset) + Vector3.up * (top - length * 0.5f) + inward * ((f % 2) * 0.03f);
                        DisasterReady.Build.Box("Cortina", p, along * 0.1f + Vector3.up * length + inward * 0.035f, fabric, root, false);
                    }
            }
        }

        /// <summary>Interruptores junto a las puertas.</summary>
        static void Switches()
        {
            var plate = Palette.Solid(new Color(0.96f, 0.96f, 0.94f), 0.5f);
            var key = Palette.Solid(new Color(0.85f, 0.85f, 0.83f), 0.5f);
            foreach (var o in HouseLayout.Openings)
            {
                if (o.IsWindow) continue;
                float baseY = HouseLayout.FloorY(o.Floor);
                foreach (float side in new[] { 1f, -1f })
                {
                    Vector3 p = o.AlongX
                        ? new Vector3(o.To + 0.2f, baseY + 1.2f, o.Fixed + side * 0.105f)
                        : new Vector3(o.Fixed + side * 0.105f, baseY + 1.2f, o.To + 0.2f);
                    Vector3 size = o.AlongX ? new Vector3(0.08f, 0.12f, 0.01f) : new Vector3(0.01f, 0.12f, 0.08f);
                    DisasterReady.Build.Box("Interruptor", p, size, plate, root, false);
                    Vector3 keySize = o.AlongX ? new Vector3(0.03f, 0.05f, 0.016f) : new Vector3(0.016f, 0.05f, 0.03f);
                    DisasterReady.Build.Box("Tecla", p, keySize, key, root, false);
                    if (o.Fixed < 0.5f) break; // puerta principal: solo por dentro
                }
            }
        }
    }
}
