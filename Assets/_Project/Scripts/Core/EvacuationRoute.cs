using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Puntos de la ruta de evacuación (los define el módulo Nivel, la UI los usa para la brújula).
    /// </summary>
    public static class EvacuationRoute
    {
        static readonly List<Vector3> points = new List<Vector3>();
        public static IReadOnlyList<Vector3> Points => points;

        public static void Set(IEnumerable<Vector3> route)
        {
            points.Clear();
            points.AddRange(route);
        }

        /// <summary>Siguiente punto hacia donde debe ir el jugador.</summary>
        public static bool TryGetNext(Vector3 from, out Vector3 next)
        {
            next = Vector3.zero;
            if (points.Count == 0) return false;

            // Punto más cercano (dando prioridad al mismo piso)
            int closest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 d = points[i] - from;
                float score = new Vector2(d.x, d.z).sqrMagnitude + Mathf.Abs(d.y) * 25f;
                if (score < best)
                {
                    best = score;
                    closest = i;
                }
            }

            // Si ya está cerca de ese punto, apuntar al siguiente
            int index = closest;
            Vector3 flat = points[closest] - from;
            flat.y = 0f;
            if (flat.magnitude < 1.6f || index == 0) index = Mathf.Min(closest + 1, points.Count - 1);
            if (index == 0) index = Mathf.Min(1, points.Count - 1);
            next = points[index];
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => points.Clear();
    }
}
