using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Volumen invisible que marca una zona segura o peligrosa.
    /// El GameManager pregunta cada frame en qué zona está el jugador.
    /// </summary>
    public class SafeZone : MonoBehaviour
    {
        static readonly List<SafeZone> all = new List<SafeZone>();
        public static IReadOnlyList<SafeZone> All => all;

        public ZoneType Type;
        public Vector3 Size = Vector3.one;

        public Bounds Bounds => new Bounds(transform.position, Size);

        public static SafeZone Create(ZoneType type, Vector3 worldCenter, Vector3 size, Transform parent = null)
        {
            var go = new GameObject("Zona_" + type);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = worldCenter;
            var zone = go.AddComponent<SafeZone>();
            zone.Type = type;
            zone.Size = size;
            return zone;
        }

        public bool Contains(Vector3 point) => Bounds.Contains(point);

        public bool IsSafe => Type == ZoneType.Mesa || Type == ZoneType.Columna;

        /// <summary>La zona más pequeña que contiene el punto (las mesas ganan a las ventanas).</summary>
        public static SafeZone Find(Vector3 point)
        {
            SafeZone best = null;
            float bestVolume = float.MaxValue;
            foreach (var zone in all)
            {
                if (zone == null || !zone.Contains(point)) continue;
                float volume = zone.Size.x * zone.Size.y * zone.Size.z;
                if (volume < bestVolume)
                {
                    best = zone;
                    bestVolume = volume;
                }
            }
            return best;
        }

        /// <summary>Zona más cercana de un tipo (para guiar al jugador).</summary>
        public static SafeZone Nearest(Vector3 point, ZoneType type)
        {
            SafeZone best = null;
            float bestDistance = float.MaxValue;
            foreach (var zone in all)
            {
                if (zone == null || zone.Type != type) continue;
                float d = (zone.transform.position - point).sqrMagnitude;
                if (d < bestDistance)
                {
                    best = zone;
                    bestDistance = d;
                }
            }
            return best;
        }

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);

        void OnDrawGizmos()
        {
            switch (Type)
            {
                case ZoneType.Mesa: Gizmos.color = new Color(0f, 1f, 0f, 0.25f); break;
                case ZoneType.Columna: Gizmos.color = new Color(0f, 0.8f, 0.4f, 0.25f); break;
                case ZoneType.Ventana: Gizmos.color = new Color(1f, 0f, 0f, 0.25f); break;
                default: Gizmos.color = new Color(0f, 0.6f, 1f, 0.25f); break;
            }
            Gizmos.DrawCube(transform.position, Size);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();
    }
}
