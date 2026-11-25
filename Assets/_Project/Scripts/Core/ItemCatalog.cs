using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Lista de suministros del juego. Los útiles suman más espacio (13) que la capacidad
    /// de la mochila (12): el jugador debe priorizar, como en 60 Seconds!.
    /// </summary>
    public static class ItemCatalog
    {
        public static readonly ItemData[] All =
        {
            new ItemData("agua", "Agua (bidón)", 2, 15, true,
                "Al menos 2 litros por persona al día: es lo primero que falta tras un sismo.", new Color(0.3f, 0.65f, 0.95f)),
            new ItemData("botiquin", "Botiquín", 2, 15, true,
                "Permite atender heridas leves mientras llega la ayuda.", new Color(0.9f, 0.25f, 0.25f)),
            new ItemData("linterna", "Linterna", 1, 12, true,
                "Sin electricidad la casa queda a oscuras. (Tecla F para usarla)", new Color(1f, 0.8f, 0.2f)),
            new ItemData("documentos", "Documentos en bolsa", 1, 10, true,
                "DNI y papeles importantes protegidos del agua y del polvo.", new Color(0.55f, 0.75f, 1f)),
            new ItemData("alimentos", "Alimentos no perecibles", 2, 10, true,
                "Latas y galletas que no necesitan cocción ni refrigeración.", new Color(0.95f, 0.55f, 0.2f)),
            new ItemData("radio", "Radio a pilas", 1, 8, true,
                "Para recibir información oficial si fallan los celulares.", new Color(0.6f, 0.45f, 0.3f)),
            new ItemData("silbato", "Silbato", 1, 8, true,
                "Si quedas atrapado, un silbato se oye más lejos que tu voz.", new Color(1f, 0.5f, 0.1f)),
            new ItemData("pilas", "Pilas de repuesto", 1, 6, true,
                "Para la linterna y la radio.", new Color(0.75f, 0.75f, 0.8f)),
            new ItemData("abrigo", "Abrigo", 2, 6, true,
                "Las noches en Huancayo son frías: el abrigo evita la hipotermia.", new Color(0.3f, 0.55f, 0.35f)),

            new ItemData("laptop", "Laptop", 3, -10, false,
                "Pesada y frágil: no te ayuda a sobrevivir las primeras 72 horas.", new Color(0.5f, 0.5f, 0.55f)),
            new ItemData("consola", "Consola de videojuegos", 3, -10, false,
                "Ocupa espacio que necesitas para agua o medicinas.", new Color(0.25f, 0.25f, 0.3f)),
            new ItemData("adorno", "Trofeo decorativo", 2, -10, false,
                "Un recuerdo valioso, pero inútil en una emergencia.", new Color(0.95f, 0.8f, 0.3f)),
        };

        public static ItemData Get(string id)
        {
            foreach (var item in All)
                if (item.Id == id) return item;
            Debug.LogWarning("ItemCatalog: no existe el objeto '" + id + "'");
            return new ItemData(id, id, 1, 0, false, "", Color.white);
        }

        public static int UsefulCount
        {
            get
            {
                int n = 0;
                foreach (var item in All) if (item.Useful) n++;
                return n;
            }
        }
    }
}
