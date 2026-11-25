using System;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Eventos globales. Los módulos (Jugador, Nivel, UI y Audio) solo dependen de Core
    /// y se comunican por aquí, así cada integrante trabaja en su carpeta sin conflictos.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        public static event Action<string, MessageKind> Message;          // mensaje emergente
        public static event Action<int, int, string> ScoreChanged;        // total, cambio, motivo
        public static event Action<int> PlayerHit;                        // daño recibido
        public static event Action InventoryChanged;
        public static event Action<string> ItemReturned;                  // id del objeto devuelto a su lugar
        public static event Action<bool> TremorChanged;                   // empieza / termina un temblor
        public static event Action<SoundId, Vector3?, float> Sound;       // sonido (posición opcional), volumen
        public static event Action<Vector3, bool> Impact;                 // posición, impacto fuerte
        public static event Action<string> TaskCompleted;                 // "gas", "luz"
        public static event Action FadeOutRequested;                      // antes de cambiar de escena

        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaiseMessage(string text, MessageKind kind = MessageKind.Info) => Message?.Invoke(text, kind);
        public static void RaiseScoreChanged(int total, int delta, string reason) => ScoreChanged?.Invoke(total, delta, reason);
        public static void RaisePlayerHit(int damage) => PlayerHit?.Invoke(damage);
        public static void RaiseInventoryChanged() => InventoryChanged?.Invoke();
        public static void RaiseItemReturned(string id) => ItemReturned?.Invoke(id);
        public static void RaiseTremorChanged(bool shaking) => TremorChanged?.Invoke(shaking);
        public static void PlaySound(SoundId id, float volume = 1f) => Sound?.Invoke(id, null, volume);
        public static void PlaySoundAt(SoundId id, Vector3 position, float volume = 1f) => Sound?.Invoke(id, position, volume);
        public static void RaiseImpact(Vector3 position, bool heavy) => Impact?.Invoke(position, heavy);
        public static void RaiseTaskCompleted(string task) => TaskCompleted?.Invoke(task);
        public static void RaiseFadeOut() => FadeOutRequested?.Invoke();

        // La recarga de dominio está desactivada en el proyecto: limpiar al dar Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            StateChanged = null; Message = null; ScoreChanged = null; PlayerHit = null;
            InventoryChanged = null; ItemReturned = null; TremorChanged = null; Sound = null;
            Impact = null; TaskCompleted = null; FadeOutRequested = null;
        }
    }
}
