using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayer : MonoBehaviour, IPlayerAgent
    {
        public Vector3 Position => transform.position;
        public bool Crouched { get; private set; }
        public bool Holding { get; private set; }
        public bool Running { get; private set; }
        public IBackpack Backpack { get; set; }
        public Camera View { get; private set; }
        CharacterController controller;
        float pitch, vertical;
        Light torch;
        readonly Collider[] overhead = new Collider[16];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(30, Install);
        static void Install(Transform root, bool house)
        {
            if (!house) return;
            // Se conserva la cámara original en la escena, desactivada durante la partida.
            foreach (Camera camera in root.gameObject.scene.GetRootGameObjects()
                .SelectMany(obj => obj.GetComponentsInChildren<Camera>()).ToArray())
                camera.gameObject.SetActive(false);
            var obj = new GameObject("Jugador"); obj.transform.SetParent(root);
            obj.transform.position = new Vector3(0, 3.23f, -3);
            obj.AddComponent<FirstPersonPlayer>();
        }
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .28f;
            controller.center = Vector3.up * .9f;
            controller.stepOffset = .25f; controller.slopeLimit = 50;
            controller.skinWidth = .025f; controller.minMoveDistance = 0;
            var cameraObject = new GameObject("Vista"); cameraObject.transform.SetParent(transform, false);
            View = cameraObject.AddComponent<Camera>(); View.nearClipPlane = .04f; View.fieldOfView = 72;
            cameraObject.tag = "MainCamera"; cameraObject.AddComponent<AudioListener>();
            var lamp = new GameObject("Linterna"); lamp.transform.SetParent(cameraObject.transform, false);
            torch = lamp.AddComponent<Light>(); torch.type = LightType.Spot;
            torch.range = 18; torch.spotAngle = 65; torch.intensity = 5; torch.enabled = false;
            GameManager.Instance.Player = this;
            GameManager.Instance.SetPause(false);
        }
        bool CanStand()
        {
            int count = Physics.OverlapCapsuleNonAlloc(Position + Vector3.up * .95f,
                Position + Vector3.up * 1.5f, .27f, overhead, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) if (overhead[i] != controller) return false;
            return count < overhead.Length;
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (!game.Playing || game.Paused) { Running = false; Holding = false; return; }
            Keyboard keys = Keyboard.current; Mouse mouse = Mouse.current;
            Vector2 delta = mouse == null ? Vector2.zero : mouse.delta.ReadValue() * .09f;
            transform.Rotate(0, delta.x, 0); pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
            bool wantCrouch = keys != null && keys.leftCtrlKey.isPressed;
            Crouched = wantCrouch || (Crouched && !CanStand());
            float height = Crouched ? .85f : 1.8f;
            controller.height = height; controller.center = Vector3.up * (height / 2);
            Vector2 move = Vector2.zero;
            if (keys != null)
            {
                move.x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                move.y = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
            }
            move = Vector2.ClampMagnitude(move, 1);
            Holding = keys != null && keys.hKey.isPressed && Crouched && move.sqrMagnitude < .01f;
            Running = keys != null && keys.leftShiftKey.isPressed && !Crouched && move.sqrMagnitude > .01f;
            float speed = Crouched ? 1.5f : Running ? 4.8f : 2.8f;
            if (game.Shaking) speed *= .55f;
            if (controller.isGrounded && vertical < 0) vertical = -2;
            vertical = Mathf.Max(-25, vertical - 20 * Time.deltaTime);
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * speed;
            velocity.y = vertical; controller.Move(velocity * Time.deltaTime);
            float shake = game.Shaking ? .65f : 0;
            View.transform.localRotation = Quaternion.Euler(pitch + Mathf.Sin(Time.time * 29) * shake,
                Mathf.Sin(Time.time * 37) * shake, Mathf.Sin(Time.time * 23) * shake);
            View.transform.localPosition = Vector3.up * (Crouched ? .7f : 1.63f);
            if (keys != null && keys.fKey.wasPressedThisFrame)
            {
                if (Backpack != null && Backpack.Contains("flashlight")) torch.enabled = !torch.enabled;
                else GameEvents.Message("Primero recoge la linterna.");
            }
            if (Position.y < -5) game.Damage(100, "Caíste fuera de una ruta transitable.");
        }
    }
}
