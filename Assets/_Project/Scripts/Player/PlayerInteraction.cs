using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    public sealed class PlayerInteraction : MonoBehaviour, IBackpack
    {
        readonly List<ItemData> items = new();
        FirstPersonPlayer player;
        public int Used { get; private set; }
        public int Capacity => GameManager.Instance.Config.BackpackCapacity;
        public IReadOnlyList<ItemData> Items => items;
        public bool Contains(string id) => items.Exists(item => item.Id == id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(40, (root, house) =>
        {
            if (house) root.GetComponentInChildren<FirstPersonPlayer>().gameObject.AddComponent<PlayerInteraction>();
        });

        void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            player.Backpack = this;
        }

        public bool TryAdd(ItemData item)
        {
            var game = GameManager.Instance;
            if (game == null || game.State != GameState.Preparation || game.Paused) return false;
            if (Contains(item.Id)) return false;
            if (Used + item.Size > Capacity)
            {
                GameEvents.Message("No cabe: prioriza los suministros útiles.");
                return false;
            }
            items.Add(item);
            Used += item.Size;
            game.Award(item.Points, item.Points > 0 ? "Suministro útil: " + item.Name : "Objeto prescindible: " + item.Name);
            GameEvents.Item(item);
            GameEvents.Sound("pickup");
            return true;
        }

        void Update()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            game.InteractionHint = "";
            if (!game.Playing || game.Paused) return;

            var ray = new Ray(player.View.transform.position, player.View.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return;
            if (hit.collider.transform.IsChildOf(transform)) return;

            // Busca una interfaz, nunca una clase del módulo Level.
            foreach (MonoBehaviour behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is not IInteractable target) continue;
                game.InteractionHint = "E · " + target.Prompt;
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) target.Interact(player);
                break;
            }
        }
    }
}
