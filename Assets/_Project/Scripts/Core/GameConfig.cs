using UnityEngine;

namespace DisasterReady
{
    [CreateAssetMenu(menuName = "Disaster Ready/Configuración")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(10)] public float PreparationSeconds = 90;
        [Min(1)] public float AlarmSeconds = 5;
        [Min(5)] public float EarthquakeSeconds = 24;
        [Min(15)] public float EvacuationSeconds = 100;
        [Min(0)] public float AftershockDelay = 8;
        [Min(1)] public float AftershockSeconds = 6;
        [Min(1)] public int BackpackCapacity = 12;
        [Min(1)] public int UsefulItemPoints = 8;
        public int UselessItemPoints = -12;
        [Min(0)] public int ProtectionPointsPerSecond = 2;
        [Min(0)] public int RunningPenalty = 3;
        [Min(0)] public int WindowPenalty = 15;
        [Min(0)] public int UtilityPoints = 8;
        [Min(0)] public int EvacuationPoints = 25;
        [Min(0)] public int GradeA = 100;
        [Min(0)] public int GradeB = 65;
    }
}
