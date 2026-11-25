using UnityEngine;

namespace DisasterReady
{
    /// <summary>Estado del jugador que otros módulos leen (el módulo Jugador lo escribe).</summary>
    public static class PlayerInfo
    {
        public static Transform Transform;
        public static Camera Camera;
        public static bool IsCrouching;
        public static bool IsHolding;   // "sujétate" (tecla H)
        public static bool IsRunning;
        public static bool IsMoving;
        public static Interactable Focused; // objeto que el jugador está mirando (o null)

        public static bool HasPlayer => Transform != null;

        /// <summary>Centro aproximado del cuerpo.</summary>
        public static Vector3 Position =>
            Transform == null ? Vector3.zero : Transform.position + Vector3.up * (IsCrouching ? 0.35f : 0.9f);

        public static Vector3 Feet => Transform == null ? Vector3.zero : Transform.position;

        public static Vector3 HeadPosition => Camera != null ? Camera.transform.position : Position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Transform = null; Camera = null;
            IsCrouching = false; IsHolding = false; IsRunning = false; IsMoving = false; Focused = null;
        }
    }

    /// <summary>Intensidad actual del temblor (0 = nada, 1 = máximo). La escribe el GameManager.</summary>
    public static class QuakeInfo
    {
        public static float Intensity;
        public static bool PowerOn = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Intensity = 0f;
            PowerOn = true;
        }
    }
}
