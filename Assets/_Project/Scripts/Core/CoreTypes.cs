using UnityEngine;

namespace DisasterReady
{
    /// <summary>Fases de la simulación (GDD: preparación → protección → evacuación).</summary>
    public enum GameState
    {
        Preparacion,  // explorar la casa y armar la mochila de emergencia
        Alerta,       // suena la alarma sísmica: pocos segundos para reaccionar
        Sismo,        // agáchate, cúbrete y sujétate
        Evacuacion,   // cortar gas/luz y salir al punto de reunión
        Resultado     // calificación A, B o C
    }

    /// <summary>Tipos de zona que reconoce el GameManager.</summary>
    public enum ZoneType
    {
        Mesa,          // debajo de una mesa firme (segura)
        Columna,       // junto a una columna con señal ZONA SEGURA (segura)
        Ventana,       // cerca de vidrios (peligrosa)
        PuntoReunion   // exterior: fin de la evacuación
    }

    /// <summary>Efectos de sonido. El módulo de Audio decide qué archivo usa cada uno.</summary>
    public enum SoundId
    {
        Click, Hover, Pickup, PickupBad, Drop, Denied,
        StepWood, StepGrass, Switch, Valve,
        Glass, ImpactLight, ImpactHeavy, Creak,
        Hurt, Win, Lose, Whoosh, Protected
    }

    /// <summary>Estilo visual de los mensajes emergentes del HUD.</summary>
    public enum MessageKind { Info, Good, Bad, Warning }

    /// <summary>Datos de un suministro que puede ir en la mochila.</summary>
    [System.Serializable]
    public struct ItemData
    {
        public string Id;
        public string Name;
        public int Size;       // espacios que ocupa en la mochila
        public int Points;     // positivo = útil, negativo = prescindible
        public bool Useful;
        public string Tip;     // por qué sirve (o por qué no) en una emergencia
        public Color Color;    // color de la ficha en el HUD

        public ItemData(string id, string name, int size, int points, bool useful, string tip, Color color)
        {
            Id = id; Name = name; Size = size; Points = points; Useful = useful; Tip = tip; Color = color;
        }
    }

    /// <summary>Una fila de la pantalla de resultados.</summary>
    public struct ResultRow
    {
        public string Label;
        public string Value;
        public bool Good;

        public ResultRow(string label, string value, bool good)
        {
            Label = label; Value = value; Good = good;
        }
    }
}
