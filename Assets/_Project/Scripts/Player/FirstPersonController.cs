using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    /// <summary>
    /// Jugador en primera persona: caminar, correr, agacharse (C / Ctrl), sujetarse (H),
    /// subir escaleras, pasos con sonido, balanceo de cámara y sacudida durante el sismo.
    /// Se crea solo en la escena House.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public float WalkSpeed = 3.0f;
        public float RunSpeed = 5.2f;
        public float CrouchSpeed = 1.5f;
        public float Gravity = 20f;

        const float StandHeight = 1.8f;
        const float CrouchHeight = 0.6f;
        const float StandEye = 1.62f;
        const float CrouchEye = 0.48f;
        const float StepDistanceWalk = 1.7f;

        CharacterController controller;
        Transform pivot;      // gira en vertical (pitch)
        Transform shaker;     // recibe sacudidas y balanceo
        Camera cam;

        float pitch;
        float verticalVelocity;
        bool crouchToggled;
        bool crouching;
        float eyeHeight = StandEye;
        Vector3 planarVelocity;
        float bobPhase;
        float stepAccumulator;
        float hitKick;
        float lastHitTime = -10f;

        // ---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<FirstPersonController>() != null) return;
            var go = new GameObject("Jugador");
            go.tag = "Player";
            go.transform.SetPositionAndRotation(GameConfig.PlayerSpawn, Quaternion.Euler(0f, GameConfig.PlayerSpawnYaw, 0f));
            go.AddComponent<FirstPersonController>();
            go.AddComponent<PlayerInteraction>();
            go.AddComponent<Flashlight>();
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.radius = 0.28f;
            controller.height = StandHeight;
            controller.center = new Vector3(0f, StandHeight / 2f, 0f);
            controller.stepOffset = 0.32f;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.02f;
            controller.minMoveDistance = 0f;

            pivot = new GameObject("Pivote").transform;
            pivot.SetParent(transform, false);
            pivot.localPosition = Vector3.up * StandEye;

            shaker = new GameObject("Sacudida").transform;
            shaker.SetParent(pivot, false);

            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.SetParent(shaker, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.03f;
            cam.fieldOfView = 70f;

            PlayerInfo.Transform = transform;
            PlayerInfo.Camera = cam;

            GameEvents.PlayerHit += OnHit;
        }

        void OnDestroy()
        {
            GameEvents.PlayerHit -= OnHit;
            if (PlayerInfo.Transform == transform)
            {
                PlayerInfo.Transform = null;
                PlayerInfo.Camera = null;
            }
        }

        void OnHit(int damage)
        {
            hitKick = Mathf.Clamp(damage / 20f, 0.4f, 1.2f);
            lastHitTime = Time.time;
        }

        bool InputActive
        {
            get
            {
                var gm = GameManager.Instance;
                return gm == null || (gm.Playing && !gm.Paused);
            }
        }

        // ---------------------------------------------------------------------
        void Update()
        {
            bool active = InputActive;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;

            var keyboard = active ? Keyboard.current : null;
            var mouse = active ? Mouse.current : null;

            Look(mouse);
            Crouch(keyboard);
            Move(keyboard);
            CameraEffects();

            PlayerInfo.IsCrouching = crouching;
            PlayerInfo.IsHolding = crouching && keyboard != null && keyboard.hKey.isPressed;
        }

        void Look(Mouse mouse)
        {
            if (mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue() * Settings.Sensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            pivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Crouch(Keyboard keyboard)
        {
            if (keyboard != null && keyboard.cKey.wasPressedThisFrame) crouchToggled = !crouchToggled;
            bool wantCrouch = crouchToggled || (keyboard != null && keyboard.leftCtrlKey.isPressed);

            if (wantCrouch) crouching = true;
            else if (crouching)
            {
                if (CanStandUp()) crouching = false;
                else crouchToggled = true; // hay algo encima (mesa): seguimos agachados
            }

            float targetHeight = crouching ? CrouchHeight : StandHeight;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, Time.deltaTime * 6f);
            controller.center = new Vector3(0f, controller.height / 2f, 0f);
            eyeHeight = Mathf.MoveTowards(eyeHeight, crouching ? CrouchEye : StandEye, Time.deltaTime * 5f);
        }

        bool CanStandUp()
        {
            float radius = controller.radius * 0.9f;
            Vector3 origin = transform.position + Vector3.up * (CrouchHeight - radius);
            float distance = StandHeight - CrouchHeight + 0.05f;
            var hits = Physics.SphereCastAll(origin, radius, Vector3.up, distance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
                if (hit.collider != controller && !hit.collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        void Move(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;
            bool runKey = false;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                runKey = keyboard.leftShiftKey.isPressed;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            bool moving = input.sqrMagnitude > 0.01f;
            bool running = runKey && moving && !crouching;

            float speed = crouching ? CrouchSpeed : (running ? RunSpeed : WalkSpeed);
            float quake = QuakeInfo.Intensity;
            speed *= 1f - 0.55f * quake; // cuesta caminar mientras tiembla

            Vector3 target = (transform.right * input.x + transform.forward * input.y) * speed;

            // El suelo "empuja" al jugador durante el sismo
            if (quake > 0.05f)
            {
                float t = Time.time * 1.3f;
                target += new Vector3(Mathf.PerlinNoise(t, 3.1f) - 0.5f, 0f, Mathf.PerlinNoise(7.7f, t) - 0.5f) * (2.2f * quake);
            }

            planarVelocity = Vector3.MoveTowards(planarVelocity, target, 20f * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= Gravity * Time.deltaTime;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            PlayerInfo.IsMoving = moving;
            PlayerInfo.IsRunning = running;

            // Pasos
            if (controller.isGrounded && moving)
            {
                float horizontal = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
                stepAccumulator += horizontal * Time.deltaTime;
                bobPhase += horizontal * Time.deltaTime * 2.2f;
                float stride = crouching ? StepDistanceWalk * 0.7f : (running ? StepDistanceWalk * 1.25f : StepDistanceWalk);
                if (stepAccumulator >= stride)
                {
                    stepAccumulator = 0f;
                    bool inside = IsInsideHouse(transform.position);
                    float volume = crouching ? 0.35f : (running ? 1f : 0.7f);
                    GameEvents.PlaySoundAt(inside ? SoundId.StepWood : SoundId.StepGrass, transform.position, volume);
                }
            }

            // Seguridad: si cae fuera del mapa, volver al inicio
            if (transform.position.y < -10f)
            {
                controller.enabled = false;
                transform.position = GameConfig.PlayerSpawn;
                controller.enabled = true;
                verticalVelocity = 0f;
            }
        }

        static bool IsInsideHouse(Vector3 p) => p.x > 0f && p.x < 12f && p.z > 0f && p.z < 10f;

        void CameraEffects()
        {
            pivot.localPosition = Vector3.up * eyeHeight;

            // Balanceo al caminar
            float bobAmount = PlayerInfo.IsMoving && controller.isGrounded ? (PlayerInfo.IsRunning ? 0.05f : 0.03f) : 0f;
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * bobAmount * 0.5f, Mathf.Abs(Mathf.Sin(bobPhase)) * bobAmount, 0f);

            // Sacudida del sismo
            float q = QuakeInfo.Intensity;
            float t = Time.time * 16f;
            Vector3 shake = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * (0.14f * q);
            float roll = (Mathf.PerlinNoise(t * 0.5f, 5f) - 0.5f) * 7f * q;
            float yaw = (Mathf.PerlinNoise(9f, t * 0.5f) - 0.5f) * 3f * q;

            // Golpe recibido
            hitKick = Mathf.MoveTowards(hitKick, 0f, Time.deltaTime * 3f);
            float kick = hitKick * Mathf.Sin((Time.time - lastHitTime) * 30f) * 4f;

            shaker.localPosition = bob + shake;
            shaker.localRotation = Quaternion.Euler(kick, yaw, roll + kick * 0.5f);

            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, PlayerInfo.IsRunning ? 76f : 70f, Time.deltaTime * 6f);
        }
    }
}
