using UnityEngine;

namespace DisasterReady
{
    public enum InteractionKind { Item, Utility, Window }
    public sealed class LevelInteractable : MonoBehaviour, IInteractable
    {
        public InteractionKind Kind;
        public ItemData Item;
        public string Key, Label;
        bool used;
        public string Prompt => used ? "Ya realizado" : Kind switch
        {
            InteractionKind.Item => $"Recoger {Item.Name} · espacio {Item.Size}",
            InteractionKind.Utility => Label + " (solo si es seguro)",
            _ => "Intentar usar ventana (peligroso)"
        };
        public void Interact(IPlayerAgent player)
        {
            var game = GameManager.Instance;
            if (used || !game.Playing || game.Paused) return;
            if (Kind == InteractionKind.Item)
            {
                if (game.State != GameState.Preparation)
                { GameEvents.Message("La preparación terminó. Prioriza protegerte y evacuar."); return; }
                if (player.Backpack != null && player.Backpack.TryAdd(Item))
                { used = true; gameObject.SetActive(false); }
            }
            else if (Kind == InteractionKind.Utility) used = game.UseUtility(Key, Label);
            else
            {
                game.AwardOnce("window", -game.Config.WindowPenalty, "No uses las ventanas como salida.");
                GameEvents.Message("Usa la escalera y la puerta: las ventanas no son rutas de evacuación.");
            }
        }
    }
}
