using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Valores de diseño del juego. Cambiar aquí duraciones, puntajes y umbrales.
    /// </summary>
    public static class GameConfig
    {
        // Escenas
        public const string MenuScene = "MainMenu";
        public const string HouseScene = "House";

        // Duración de cada fase (segundos)
        public const float PreparationSeconds = 90f;
        public const float AlertSeconds = 6f;
        public const float QuakeSeconds = 25f;
        public const float EvacuationSeconds = 90f;
        public const float AftershockAt = 22f;       // segundos después de iniciar la evacuación
        public const float AftershockSeconds = 6f;
        public const float AftershockIntensity = 0.45f;

        // Jugador
        public const int MaxHealth = 100;
        public const int BackpackCapacity = 12;
        public static readonly Vector3 PlayerSpawn = new Vector3(9.4f, 3.25f, 1.6f); // 2do piso, cuarto del protagonista
        public const float PlayerSpawnYaw = -30f;

        // Puntajes
        public const int FirstProtectionBonus = 10;
        public const float ProtectedPointsPerSecond = 2f;
        public const float HoldingPointsPerSecond = 1f;
        public const float WindowPenaltyPerSecond = 3f;
        public const float RunningPenaltyPerSecond = 2f;
        public const int HitPenalty = 10;
        public const int UtilityPoints = 15;
        public const int EvacuationPoints = 40;
        public const int LeftDuringQuakePenalty = 20;

        // Calificación
        public const int GradeA = 170;
        public const int GradeB = 110;

        // Tareas
        public const string TaskGas = "gas";
        public const string TaskPower = "luz";
    }
}
