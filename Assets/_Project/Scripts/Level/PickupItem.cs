using System.Collections;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Suministro que se puede guardar en la mochila (tecla E durante la preparación).
    /// Tiene un modelo low-poly propio, un marcador flotante y vuelve a su lugar si el
    /// jugador lo saca de la mochila (tecla Q).
    /// </summary>
    public class PickupItem : Interactable
    {
        public ItemData Item;

        Vector3 homePosition;
        Quaternion homeRotation;
        Vector3 homeScale;
        Transform marker;
        bool taken;
        bool flying;

        public static PickupItem Create(string id, Vector3 position, float yaw, Transform parent)
        {
            var data = ItemCatalog.Get(id);
            var root = new GameObject("Suministro: " + data.Name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            ItemModels.Build(id, root.transform);
            FitCollider(root);

            var pickup = root.AddComponent<PickupItem>();
            pickup.Item = data;
            pickup.CreateMarker();
            return pickup;
        }

        static void FitCollider(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            var box = root.AddComponent<BoxCollider>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            box.center = root.transform.InverseTransformPoint(b.center);
            box.size = b.size + Vector3.one * 0.04f;
        }

        void CreateMarker()
        {
            var bounds = GetComponent<BoxCollider>().bounds;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Marcador";
            Build.DestroyCollider(go);
            go.transform.SetParent(transform, true);
            go.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.22f, bounds.center.z);
            go.transform.localScale = Vector3.one * 0.07f;
            go.GetComponent<Renderer>().sharedMaterial = Palette.Glow(new Color(1f, 0.78f, 0.15f), 3f);
            marker = go.transform;
        }

        void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeScale = transform.localScale;
            GameEvents.ItemReturned += OnItemReturned;
            GameEvents.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            GameEvents.ItemReturned -= OnItemReturned;
            GameEvents.StateChanged -= OnStateChanged;
        }

        /// <summary>Los objetos que no se recogieron también se caen con el sismo.</summary>
        void OnStateChanged(GameState state)
        {
            if (state != GameState.Sismo || taken || GetComponent<Rigidbody>() != null) return;
            var body = gameObject.AddComponent<Rigidbody>();
            body.mass = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        // ---------- Interactable ----------
        static bool InPreparation
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.State == GameState.Preparacion && !gm.Paused;
            }
        }

        public override bool CanInteract => InPreparation && !taken && !flying;

        public override string DisplayName => Item.Name;

        public override string Prompt
        {
            get
            {
                if (GameManager.Instance == null) return "";
                if (!InPreparation) return Item.Name + "  ·  ya no es momento de recoger cosas";
                return $"Recoger {Item.Name}  ·  {Item.Size} espacio{(Item.Size > 1 ? "s" : "")}";
            }
        }

        protected override Color HighlightColor => Item.Useful ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 0.6f, 0.35f);

        public override void Interact()
        {
            if (!CanInteract) return;
            if (GameManager.Instance.TryPickUp(Item))
            {
                taken = true;
                StartCoroutine(FlyToPlayer());
            }
        }

        IEnumerator FlyToPlayer()
        {
            flying = true;
            if (marker != null) marker.gameObject.SetActive(false);
            Vector3 start = transform.position;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.28f;
                Vector3 target = PlayerInfo.HasPlayer ? PlayerInfo.HeadPosition - Vector3.up * 0.35f : start;
                float e = t * t;
                transform.position = Vector3.Lerp(start, target, e);
                transform.localScale = homeScale * Mathf.Lerp(1f, 0.2f, e);
                yield return null;
            }
            flying = false;
            gameObject.SetActive(false);
        }

        void OnItemReturned(string id)
        {
            if (id != Item.Id || !taken) return;
            taken = false;
            flying = false;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            transform.localScale = homeScale;
            gameObject.SetActive(true);
            if (marker != null) marker.gameObject.SetActive(true);
        }

        void Update()
        {
            if (marker == null) return;
            bool show = InPreparation && !taken;
            if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
            if (!show) return;
            marker.rotation = Quaternion.Euler(45f, Time.time * 90f, 45f);
            var p = marker.position;
            var b = GetComponent<BoxCollider>().bounds;
            marker.position = new Vector3(p.x, b.max.y + 0.2f + Mathf.Sin(Time.time * 3f + homePosition.x) * 0.04f, p.z);
        }
    }
}
