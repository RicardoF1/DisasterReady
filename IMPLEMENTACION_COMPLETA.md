# Disaster Ready — implementación completa y guía por integrante

Entrega A–E. Incluye cada archivo completo; no hay fragmentos omitidos. El ZIP contiene además el proyecto base real, sus escenas, paquetes, ajustes y los `.meta`.

**Base comprobada:** repositorio https://github.com/RicardoF1/DisasterReady.git, commit `60982ac7224fea89e74cadd90f8e485ee89b61aa`. Se confirmaron Unity 6000.6.3f1, URP 17.6.0, Input System 1.20.0, uGUI 2.6.0 y las dos escenas habilitadas.

**Alcance de validación:** los 12 archivos C# pasaron análisis sintáctico; se comprobaron las dependencias entre módulos y la ausencia de llamadas al Input Manager antiguo. No se ejecutó Unity ni se generó un ejecutable Windows en este entorno. Compilación, geometría en Play y build Windows permanecen pendientes de la checklist. No se subió nada a GitHub ni se crearon commits atribuidos artificialmente a los integrantes.

## A. Arquitectura

```text
Assets/_Project/
    Scenes/                    MainMenu.unity, House.unity (sin modificar)
    Scripts/
        Core/                  Keen
            Contracts.cs       enums, datos, interfaces y eventos
            GameConfig.cs      ScriptableObject con valores de respaldo
            SceneBootstrap.cs  registro e instalación por escena
            GameManager.cs     fases, salud, puntos y resultados
        Player/                Ricardo
            FirstPersonPlayer.cs
            PlayerInteraction.cs
        Level/                 Joshua
            HouseBuilder.cs
            LevelContent.cs
            LevelInteractable.cs
            FallingHazard.cs
        UI/                    Abraham
            GameUI.cs
            QA.md
        Audio/                 Abraham
            AudioManager.cs
    Audio/Resources/           clips opcionales
README.md                      Keen
```

Dependencias: Player → Core; Level → Core; UI → Core; Audio → Core. Los módulos no se referencian entre sí. Se usa Assembly-CSharp para evitar configuración extra; la separación se conserva mediante interfaces y revisión de dependencias. Core recibe las implementaciones de `IPlayerAgent` y `IWorldQuery` sin conocer sus clases.

| Contrato público, namespace `DisasterReady` | Dueño | Consumidores y uso |
|---|---|---|
| `GameState` | Core | Menu, Preparation, Alarm, Earthquake, Evacuation, Result |
| `GameConfig` | Core | Duraciones, capacidad, recompensas, penalizaciones y umbrales |
| `GameManager.Instance` | Core | Acceso a partida actual; se reinicia por sesión |
| `State`, `Remaining`, `Health`, `Score`, `Paused`, `Shaking`, `Protected`, `Won`, `Grade`, `Notes` | Core | Estado de solo lectura para presentación y reglas |
| `Player: IPlayerAgent`, `World: IWorldQuery` | Core | Inyección de consultas de jugador y nivel |
| `IPlayerAgent` | Core; implementa Player | Position, Crouched, Holding, Running, Backpack |
| `IBackpack` | Core; implementa Player | Used, Capacity, Items, Contains(id), TryAdd(item) |
| `IWorldQuery` | Core; implementa Level | IsSheltered(feet), NearGlass(feet) |
| `ItemData` | Core | Id, Name, Size, Points; valor intercambiado entre nivel e inventario |
| `IInteractable` | Core; implementa Level | Prompt, Interact(IPlayerAgent) |
| `Award`, `AwardOnce`, `Damage`, `UseUtility`, `TryEvacuate` | Core | Mutaciones validadas de la partida |
| `SetPause`, `Play`, `Menu` | Core | Comandos para UI; carga restaurando tiempo/audio |
| `InteractionHint` | Core | Player publica; UI muestra |
| `SceneBootstrap.Register(order, installer)` | Core | Level 10/20; Player 30/40; UI 50; Audio 60 |
| `GameEvents.OnStateChanged(GameState)` | Core | Notifica cambios de fase |
| `OnScoreChanged(int)`, `OnPlayerHit(float)` | Core | Notifica puntos totales y cantidad de daño |
| `OnItemAdded(ItemData)` | Core | Notifica un suministro incorporado |
| `OnMessage(string)`, `OnSound(string)` | Core | Mensajes y efectos sin referencias concretas |
| `OnTremorChanged(bool)` | Core | Inicia/termina sismo principal o réplica |

Todos los registros estáticos, singleton y eventos se reinician en SubsystemRegistration. Los módulos registran instaladores en BeforeSceneLoad; el bootstrap instala después de cargar y atiende futuras cargas. Una raíz por escena y una limpieza defensiva evitan duplicados con las recargas desactivadas.

Se utiliza uGUI Text y `LegacyRuntime.ttf`; no hay que importar TMP Essential Resources. EventSystem usa InputSystemUIInputModule. No hay NavMesh, prefabs ni conexiones del Inspector obligatorios.

Decisiones educativas: alarma de simulacro, zonas estructurales ficticias previamente evaluadas y corte de suministros opcional después del movimiento. Se explica que no se debe operar electricidad ante olor a gas ni retrasar la salida. La mesa exige Ctrl+H y permanecer quieto. La preparación real de la mochila debe hacerse con anticipación.

## B. Código por integrante y por commit

Cada bloque corresponde a un archivo completo. Copiar archivos junto con sus `.meta` desde el ZIP, o dejar que Unity genere los `.meta` si se escribe el código manualmente. No mezclar ambos procedimientos para un mismo archivo ya versionado.

### Keen – Commit 1: `feat(core): definir contratos y configuracion del simulacro`

Rama: `feature/fases-keen`.

**Ruta: `Assets/_Project/Scripts/Core/Contracts.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public enum GameState { Menu, Preparation, Alarm, Earthquake, Evacuation, Result }

    [Serializable]
    public struct ItemData
    {
        public string Id, Name;
        public int Size, Points;
        public ItemData(string id, string name, int size, int points)
        { Id = id; Name = name; Size = size; Points = points; }
    }

    public interface IBackpack
    {
        int Used { get; }
        int Capacity { get; }
        IReadOnlyList<ItemData> Items { get; }
        bool Contains(string id);
        bool TryAdd(ItemData item);
    }

    public interface IPlayerAgent
    {
        Vector3 Position { get; }
        bool Crouched { get; }
        bool Holding { get; }
        bool Running { get; }
        IBackpack Backpack { get; }
    }

    public interface IWorldQuery
    {
        bool IsSheltered(Vector3 feet);
        bool NearGlass(Vector3 feet);
    }

    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(IPlayerAgent player);
    }

    // Solo notificaciones: las reglas y las mutaciones pertenecen a GameManager.
    public static class GameEvents
    {
        public static event Action<GameState> OnStateChanged;
        public static event Action<int> OnScoreChanged;
        public static event Action<float> OnPlayerHit;
        public static event Action<ItemData> OnItemAdded;
        public static event Action<string> OnMessage;
        public static event Action<bool> OnTremorChanged;
        public static event Action<string> OnSound;
        public static void State(GameState value) => OnStateChanged?.Invoke(value);
        public static void Score(int value) => OnScoreChanged?.Invoke(value);
        public static void Hit(float value) => OnPlayerHit?.Invoke(value);
        public static void Item(ItemData value) => OnItemAdded?.Invoke(value);
        public static void Message(string value) => OnMessage?.Invoke(value);
        public static void Tremor(bool value) => OnTremorChanged?.Invoke(value);
        public static void Sound(string value) => OnSound?.Invoke(value);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            OnStateChanged = null; OnScoreChanged = null; OnPlayerHit = null;
            OnItemAdded = null; OnMessage = null; OnTremorChanged = null; OnSound = null;
        }
    }
}
```

**Ruta: `Assets/_Project/Scripts/Core/GameConfig.cs`**

```csharp
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
```

### Keen – Commit 2: `feat(core): integrar fases puntuacion y bootstrap automatico`

Rama: `feature/fases-keen`.

**Ruta: `Assets/_Project/Scripts/Core/SceneBootstrap.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DisasterReady
{
    public static class SceneBootstrap
    {
        // Los módulos se registran antes de cargar; Core no conoce sus clases.
        static readonly SortedDictionary<int, Action<Transform, bool>> installers = new();
        static readonly HashSet<int> installedScenes = new();
        public static void Register(int order, Action<Transform, bool> install)
            => installers[order] = install;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            installers.Clear(); installedScenes.Clear();
            Time.timeScale = 1; AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitialScene() => Build(SceneManager.GetActiveScene());
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Build(scene);

        static void Build(Scene scene)
        {
            if (scene.name != "MainMenu" && scene.name != "House") return;
            if (!installedScenes.Add(scene.handle)) return;
            // Limpieza defensiva para sesiones sin recarga de dominio/escena.
            foreach (GameObject obj in scene.GetRootGameObjects())
                if (obj.name == "__DisasterReady")
                { obj.SetActive(false); UnityEngine.Object.Destroy(obj); }
            Time.timeScale = 1; AudioListener.pause = false;
            var root = new GameObject("__DisasterReady");
            SceneManager.MoveGameObjectToScene(root, scene);
            bool house = scene.name == "House";
            root.AddComponent<GameManager>().Initialize(house);
            foreach (var install in installers.Values) install(root.transform, house);
        }

        public static void Load(string name)
        {
            Time.timeScale = 1; AudioListener.pause = false;
            // Los handles se pueden reutilizar después de descargar escenas.
            installedScenes.Clear();
            SceneManager.LoadScene(name);
        }
    }
}
```

**Ruta: `Assets/_Project/Scripts/Core/GameManager.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameConfig Config { get; private set; }
        public GameState State { get; private set; }
        public IPlayerAgent Player { get; set; }
        public IWorldQuery World { get; set; }
        public float Remaining { get; private set; }
        public float Health { get; private set; } = 100;
        public int Score { get; private set; }
        public bool Paused { get; private set; }
        public bool Shaking { get; private set; }
        public bool Won { get; private set; }
        public bool Protected => Player != null && World != null &&
            Player.Crouched && Player.Holding && World.IsSheltered(Player.Position);
        public bool Playing => State != GameState.Menu && State != GameState.Result;
        public string Grade => Score >= Config.GradeA ? "A" : Score >= Config.GradeB ? "B" : "C";
        public IReadOnlyList<string> Notes => notes;
        public string InteractionHint { get; set; } = "";
        readonly List<string> notes = new();
        readonly HashSet<string> once = new();
        float tick, evacuationElapsed;
        bool house, aftershockStarted, ownedConfig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Instance = null;

        public void Initialize(bool isHouse)
        {
            Instance = this; house = isHouse;
            Config = Resources.Load<GameConfig>("DisasterConfig");
            ownedConfig = Config == null;
            if (ownedConfig) Config = ScriptableObject.CreateInstance<GameConfig>();
            State = isHouse ? GameState.Preparation : GameState.Menu;
            Remaining = isHouse ? Config.PreparationSeconds : 0;
        }

        void Start() => GameEvents.State(State);

        void Update()
        {
            if (!Playing || Paused) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (State == GameState.Evacuation)
            {
                evacuationElapsed += Time.deltaTime;
                if (!aftershockStarted && evacuationElapsed >= Config.AftershockDelay)
                { aftershockStarted = true; SetShaking(true); GameEvents.Message("¡Réplica! Protégete de nuevo."); }
                if (aftershockStarted && Shaking &&
                    evacuationElapsed >= Config.AftershockDelay + Config.AftershockSeconds)
                    SetShaking(false);
            }
            tick += Time.deltaTime;
            if (tick >= 1)
            {
                tick -= 1;
                if (Shaking && Player != null)
                {
                    if (Protected) Award(Config.ProtectionPointsPerSecond, "Te agachaste, cubriste y sujetaste.");
                    else Damage(1, "Durante el movimiento busca una zona segura y protege tu cabeza.");
                    if (Player.Running) Award(-Config.RunningPenalty, "No corras durante el movimiento.");
                    if (World != null && World.NearGlass(Player.Position))
                        Damage(5, "Aléjate de ventanas y vidrios.");
                }
            }
            if (!Playing || Remaining > 0) return;
            switch (State)
            {
                case GameState.Preparation: Change(GameState.Alarm, Config.AlarmSeconds); break;
                case GameState.Alarm:
                    Change(GameState.Earthquake, Config.EarthquakeSeconds); SetShaking(true); break;
                case GameState.Earthquake:
                    SetShaking(false); Change(GameState.Evacuation, Config.EvacuationSeconds); break;
                case GameState.Evacuation: Finish(false, "Se agotó el tiempo de evacuación."); break;
            }
        }

        void Change(GameState state, float seconds)
        { State = state; Remaining = seconds; tick = 0; GameEvents.State(state); }
        void SetShaking(bool value)
        { Shaking = value; GameEvents.Tremor(value); }
        public void Award(int points, string note)
        {
            if (!Playing) return;
            Score += points; AddNote(note); GameEvents.Score(Score);
        }
        public void AddNote(string note)
        { if (!notes.Contains(note)) notes.Add(note); }
        public void AwardOnce(string key, int points, string note)
        { if (Playing && once.Add(key)) Award(points, note); }
        public void Damage(float amount, string advice)
        {
            if (!Playing || Paused || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount); AddNote(advice); GameEvents.Hit(amount);
            if (Health <= 0) Finish(false, "Tu salud llegó a cero.");
        }
        public bool UseUtility(string key, string label)
        {
            if (State != GameState.Evacuation || Shaking || Paused)
            { GameEvents.Message("Hazlo solo después del movimiento y si es seguro."); return false; }
            if (!once.Add(key)) return false;
            Award(Config.UtilityPoints, label + ": acción realizada sin exposición adicional.");
            GameEvents.Sound("switch"); return true;
        }
        public void TryEvacuate()
        {
            if (State != GameState.Evacuation || Shaking || Paused) return;
            AwardOnce("exit", Config.EvacuationPoints, "Llegaste al punto de reunión.");
            Finish(true, "No regreses a una vivienda dañada hasta que sea autorizada.");
        }
        void Finish(bool won, string note)
        {
            Won = won; AddNote(note); SetShaking(false); SetPause(false);
            Change(GameState.Result, 0);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void SetPause(bool value)
        {
            Paused = value && Playing;
            Time.timeScale = Paused ? 0 : 1; AudioListener.pause = Paused;
            bool capture = house && Playing && !Paused;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
        public void Play() => SceneBootstrap.Load("House");
        public void Menu() => SceneBootstrap.Load("MainMenu");
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ownedConfig && Config != null) Destroy(Config);
        }
    }
}
```

**Ruta: `README.md`**

```markdown
# Disaster Ready

Prototipo educativo 3D para Windows, ambientado en una vivienda ficticia de dos pisos en Huancayo.
Unity 6000.6.3f1, URP 17.6, Input System 1.20 y uGUI 2.6.

## Arranque

1. Abrir este proyecto desde Unity Hub con la versión indicada.
2. Esperar a que se importen los scripts y comprobar Console.
3. Abrir Assets/_Project/Scenes/MainMenu.unity y pulsar Play.
4. Pulsar Jugar. No agregar componentes manualmente a las escenas.

Las escenas MainMenu y House deben permanecer habilitadas en Build Profiles.
El proyecto usa exclusivamente el nuevo Input System. No requiere TMP ni archivos de audio.
Los objetos se generan durante Play; no guardar esos objetos como cambios de escena.

## Controles

WASD: caminar. Ratón: mirar. Shift: correr. Ctrl: agacharse.
H: sujetarse estando quieto y agachado en una zona marcada.
E: interactuar. F: encender la linterna recogida. Esc: pausa.
No hay salto: las escaleras se recorren caminando.

## Arquitectura

Todos los contratos viven en Core, namespace DisasterReady.
Cada módulo registra su instalación en BeforeSceneLoad.
Core ejecuta los instaladores por orden y construye una sola raíz por escena.
SubsystemRegistration limpia las referencias estáticas y los eventos.
Level: prioridad 10 y 20. Player: 30 y 40. UI: 50. Audio: 60.
UI y Audio no conocen clases concretas de Player o Level.

## Configuración opcional

Create > Disaster Ready > Configuración permite crear un GameConfig.
Para cargarlo automáticamente guardarlo como:
Assets/_Project/Scripts/Core/Resources/DisasterConfig.asset
Sin ese archivo se utilizan valores de respaldo en memoria, sin modificar assets.

## Audio opcional

Colocar clips bajo Assets/_Project/Audio/Resources con estos nombres base:
ambient, alarm, quake, calm, pickup, impact, glass, switch.
Se admiten WAV/OGG importados por Unity; no incluir extensión en Resources.Load.
Sin clips, AudioClip.Create genera sonidos de respaldo.
Registrar autor, URL, licencia y modificaciones de cada audio externo.

## Reparto de commits

Keen 1: Contracts.cs y GameConfig.cs.
Keen 2: SceneBootstrap.cs, GameManager.cs y README.md.
Ricardo 1: FirstPersonPlayer.cs.
Ricardo 2: PlayerInteraction.cs.
Joshua 1: HouseBuilder.cs.
Joshua 2: LevelContent.cs, LevelInteractable.cs y FallingHazard.cs.
Abraham 1: GameUI.cs.
Abraham 2: AudioManager.cs y QA.md dentro de Scripts/UI.
Cada integrante incluye los archivos .meta de sus assets.

## Diseño educativo

Mochila: 12 espacios. Los nueve suministros útiles ocupan 12 en total.
Preparación: 90 s; alarma: 5 s; sismo: 24 s; evacuación: 100 s.
Réplica: comienza 8 s después del inicio de evacuación y dura 6 s.
La preparación y la alarma son partes de un simulacro, no predicción sísmica.
Las zonas estructurales son ficticias y se consideran previamente evaluadas.
Cerrar gas y electricidad solo se permite después del movimiento.
En una emergencia real, no operar interruptores si existe olor a gas.
No retrasar la salida ni exponerse para obtener una bonificación.

ODS 11.5: preparación para reducir consecuencias de desastres.
ODS 13.1: contribución educativa a la resiliencia.
Los sismos son geológicos; no los causa el cambio climático.
Referencias: https://www.gob.pe/1053-que-hacer-en-caso-de-sismo
https://www.gob.pe/institucion/indeci/noticias/576089-indeci-recomienda-medidas-de-preparacion-ante-sismos

## Validación

Consultar Assets/_Project/Scripts/UI/QA.md.
La revisión de generación no ejecutó Unity: compilar, probar Play y generar
un build Windows en el editor antes de considerar aprobada la entrega.
```

### Ricardo – Commit 1: `feat(player): agregar movimiento primera persona y linterna`

Rama: `feature/gameplay-ricardo`.

**Ruta: `Assets/_Project/Scripts/Player/FirstPersonPlayer.cs`**

```csharp
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
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
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
```

### Ricardo – Commit 2: `feat(player): implementar mochila e interaccion desacoplada`

Rama: `feature/gameplay-ricardo`.

**Ruta: `Assets/_Project/Scripts/Player/PlayerInteraction.cs`**

```csharp
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
        { player = GetComponent<FirstPersonPlayer>(); player.Backpack = this; }
        public bool TryAdd(ItemData item)
        {
            var game = GameManager.Instance;
            if (game.State != GameState.Preparation || game.Paused) return false;
            if (Contains(item.Id)) return false;
            if (Used + item.Size > Capacity)
            { GameEvents.Message("No cabe: prioriza los suministros útiles."); return false; }
            items.Add(item); Used += item.Size;
            game.Award(item.Points, item.Points > 0 ? "Suministro útil: " + item.Name : "Objeto prescindible: " + item.Name);
            GameEvents.Item(item); GameEvents.Sound("pickup"); return true;
        }
        void Update()
        {
            var game = GameManager.Instance; game.InteractionHint = "";
            if (!game.Playing || game.Paused) return;
            var ray = new Ray(player.View.transform.position, player.View.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) return;
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
```

### Joshua – Commit 1: `feat(level): construir vivienda de dos pisos y zonas seguras`

Rama: `feature/nivel-joshua`.

**Ruta: `Assets/_Project/Scripts/Level/HouseBuilder.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public sealed class HouseBuilder : MonoBehaviour, IWorldQuery
    {
        readonly List<Bounds> shelters = new();
        readonly List<Bounds> glassZones = new();
        readonly List<Material> materials = new();
        readonly List<Transform> labels = new();
        public readonly List<GameObject> Windows = new();
        public readonly List<Light> HouseLights = new();
        public readonly List<GameObject> FallingFurniture = new();
        public GameObject ExitGate { get; private set; }
        public bool IsSheltered(Vector3 feet) => shelters.Exists(b => b.Contains(feet + Vector3.up * .4f));
        public bool NearGlass(Vector3 feet) => glassZones.Exists(b => b.Contains(feet + Vector3.up));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(10, (root, house) =>
        { if (house) root.gameObject.AddComponent<HouseBuilder>().Build(); });

        public GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(transform);
            obj.transform.position = position; obj.transform.localScale = scale;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color; materials.Add(material);
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        public void Label(string text, Vector3 position, Color color, float size = .15f)
        {
            var obj = new GameObject("Señal " + text); obj.transform.SetParent(transform);
            obj.transform.position = position;
            var mesh = obj.AddComponent<TextMesh>(); mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            mesh.fontSize = 48; mesh.characterSize = size; mesh.color = color;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            labels.Add(obj.transform);
        }
        void Table(float x, float floor, float z)
        {
            Color wood = new(.36f, .20f, .10f);
            Box("Mesa firme", new(x, floor + 1.22f, z), new(2.8f, .16f, 2), wood);
            foreach (float dx in new[] { -1.22f, 1.22f })
                foreach (float dz in new[] { -.82f, .82f })
                    Box("Pata", new(x + dx, floor + .57f, z + dz), new(.16f, 1.14f, .16f), wood);
            shelters.Add(new Bounds(new(x, floor + .5f, z), new(2, 1, 1.35f)));
            Label("MESA FIRME\nCtrl + H bajo la mesa", new(x, floor + 1.7f, z), Color.green, .09f);
        }
        void Window(float x, float floor, float z)
        {
            var pane = Box("Vidrio", new(x, floor + 1.65f, z), new(.12f, 1.2f, 2), new(.45f, .8f, .95f));
            Windows.Add(pane);
            glassZones.Add(new Bounds(new(x, floor + 1, z), new(2.2f, 2.5f, 3)));
        }
        void Build()
        {
            GameManager.Instance.World = this;
            Color wall = new(.85f, .77f, .62f), floor = new(.52f, .36f, .22f);
            Box("Terreno", new(0, -.3f, -7), new(50, .3f, 60), new(.25f, .4f, .22f));
            Box("Piso 1", new(0, -.12f, 0), new(12, .24f, 14), floor);
            // Hueco real de escalera: x=3.5..6, z=-5..3.
            Box("Piso 2 oeste", new(-1.25f, 3.08f, 0), new(9.5f, .24f, 14), floor);
            Box("Descanso superior", new(4.75f, 3.08f, 5), new(2.5f, .24f, 4), floor);
            Box("Piso 2 sur", new(4.75f, 3.08f, -6), new(2.5f, .24f, 2), floor);
            Box("Techo", new(0, 6.5f, 0), new(12.4f, .2f, 14.4f), new(.5f, .22f, .16f));
            for (int i = 0; i < 20; i++)
            {
                float h = (i + 1) * .16f;
                Box("Escalón " + (i + 1), new(4.75f, h / 2, -4.8f + i * .4f), new(2, h, .4f), floor);
            }
            // Barandilla lateral continua; extremos abiertos para entrar y salir.
            Box("Baranda hueco", new(3.55f, 3.65f, -1), new(.12f, .9f, 8), wall);
            for (int level = 0; level < 2; level++)
            {
                float y = level * 3.2f;
                Box("Pared norte", new(0, y + 1.6f, 7), new(12, 3.2f, .2f), wall);
                Box("Fachada izquierda", new(-3.5f, y + 1.6f, -7), new(5, 3.2f, .2f), wall);
                Box("Fachada derecha", new(3.5f, y + 1.6f, -7), new(5, 3.2f, .2f), wall);
                if (level == 1) Box("Fachada superior centro", new(0, y + 1.6f, -7), new(2, 3.2f, .2f), wall);
                else Box("Dintel puerta", new(0, 2.85f, -7), new(2, .7f, .2f), wall);
                foreach (float x in new[] { -6f, 6f })
                {
                    Box("Antepecho", new(x, y + .5f, 0), new(.2f, 1, 14), wall);
                    Box("Dintel lateral", new(x, y + 2.75f, 0), new(.2f, .9f, 14), wall);
                    foreach (float z in new[] { -5f, 0f, 5f })
                        Box("Machón", new(x, y + 1.65f, z), new(.2f, 1.3f, 4), wall);
                    Window(x, y, -2.5f); Window(x, y, 2.5f);
                    // Cierra los huecos alrededor del vidrio, sin crear salidas utilizables.
                    Box("Marco central", new(x, y + 1.65f, 0), new(.2f, 1.3f, 3), wall);
                }
                foreach (float x in new[] { -5.7f, 5.7f })
                    foreach (float z in new[] { -6.7f, 6.7f })
                        Box("Columna estructural", new(x, y + 1.6f, z), new(.4f, 3.2f, .4f), Color.gray);
                // División dormitorios/sala con paso central de dos metros.
                Box("Tabique oeste", new(-3.5f, y + 1.5f, 1), new(5, 3, .15f), wall);
                Box("Tabique este", new(2.25f, y + 1.5f, 1), new(2.5f, 3, .15f), wall);
                Label(level == 0 ? "SALA / COMEDOR" : "DORMITORIO 1", new(-2, y + 2.6f, -5), Color.white);
                Label(level == 0 ? "COCINA" : "DORMITORIO 2", new(-2, y + 2.6f, 5.8f), Color.white);
                Table(-3, y, -3.5f);
                // Zona estructural solo válida en esta vivienda ficticia previamente evaluada.
                shelters.Add(new Bounds(new(-4.6f, y + .5f, 6), new(1.3f, 1, 1.1f)));
                Label("ZONA EVALUADA\nCtrl + H", new(-4.6f, y + 1.6f, 6), Color.green, .10f);
                var shelf = Box("Estante", new(-5.1f, y + 1.1f, 0), new(.65f, 2.2f, 1.5f), new(.3f, .18f, .1f));
                FallingFurniture.Add(shelf);
                var wardrobe = Box("Ropero", new(-2.7f, y + 1.2f, 6), new(1.4f, 2.4f, .7f), floor);
                FallingFurniture.Add(wardrobe);
                var lightObject = new GameObject("Luz cálida"); lightObject.transform.SetParent(transform);
                lightObject.transform.position = new(0, y + 2.6f, 0);
                Light light = lightObject.AddComponent<Light>(); light.type = LightType.Point;
                light.range = 13; light.intensity = 3; light.color = new(1, .83f, .61f); HouseLights.Add(light);
            }
            Box("Sofá", new(-4, .45f, -5.7f), new(2.5f, .9f, 1), new(.28f, .42f, .5f));
            Box("Encimera", new(0, .5f, 6.3f), new(3, 1, 1), Color.gray);
            Box("Cama 1", new(-4, 3.5f, -.1f), new(2, .6f, 1.4f), new(.3f, .55f, .75f));
            Box("Cama 2", new(-.2f, 3.5f, 5.8f), new(2, .6f, 1.4f), new(.5f, .6f, .3f));
            Box("Baño pared", new(2, 1.4f, 4.7f), new(.15f, 2.8f, 4.4f), wall);
            Box("Baño frente", new(2.4f, 1.4f, 2.5f), new(.8f, 2.8f, .15f), wall);
            Box("Inodoro", new(2.8f, .35f, 6.3f), new(.6f, .7f, .8f), Color.white);
            Label("BAÑO", new(2.9f, 2.3f, 3), Color.white, .1f);
            Label("ESCALERA\nBajar después del sismo", new(4.5f, 4.5f, 3.7f), Color.green, .09f);
            Label("SALIDA", new(0, 2.4f, -6.8f), Color.green);
            ExitGate = Box("Puerta: espera fin del sismo", new(0, 1.1f, -7), new(2, 2.2f, .2f), floor);
            Box("Vereda", new(0, -.08f, -9), new(20, .16f, 3.5f), Color.gray);
            Box("Calle", new(0, -.12f, -14), new(40, .1f, 6), new(.16f, .17f, .18f));
            Box("Punto de reunión", new(0, -.08f, -22), new(6, .16f, 6), new(.1f, .55f, .25f));
            Label("PUNTO DE REUNIÓN\nHUANCAYO", new(0, 2, -22), Color.white, .2f);
        }
        void LateUpdate()
        {
            if (Camera.main == null) return;
            foreach (Transform label in labels) label.rotation = Camera.main.transform.rotation;
        }
        void OnDestroy() { foreach (Material material in materials) Destroy(material); }
    }
}
```

### Joshua – Commit 2: `feat(level): incorporar suministros peligros y evacuacion`

Rama: `feature/nivel-joshua`.

**Ruta: `Assets/_Project/Scripts/Level/LevelContent.cs`**

```csharp
using UnityEngine;

namespace DisasterReady
{
    public sealed class LevelContent : MonoBehaviour
    {
        HouseBuilder house;
        Light sun;
        float sunIntensity;
        bool started;
        GameObject debris;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(20, (root, isHouse) =>
        { if (isHouse) root.gameObject.AddComponent<LevelContent>().Build(); });
        void Build()
        {
            house = GetComponent<HouseBuilder>();
            var config = GameManager.Instance.Config;
            string[] ids = { "water", "flashlight", "firstaid", "radio", "batteries", "whistle", "documents", "food", "coat", "laptop", "console", "ornament" };
            string[] names = { "Agua", "Linterna", "Botiquín", "Radio a pilas", "Pilas", "Silbato", "Documentos en bolsa", "Alimentos no perecibles", "Abrigo", "Laptop", "Consola", "Adorno" };
            for (int i = 0; i < ids.Length; i++)
            {
                float x = -4.6f + (i % 4) * 1.4f;
                float z = i < 4 ? -5.5f : i < 8 ? 2.6f : 4.2f;
                Vector3 pos = new(x, 3.9f, z);
                house.Box("Soporte de suministro", pos - Vector3.up * .4f, new(.5f, .6f, .5f), new(.5f, .35f, .2f));
                var obj = house.Box(names[i], pos, new(.35f, .35f, .35f), i < 9 ? new(.3f, .75f, .65f) : new(.75f, .4f, .35f));
                int size = i == 0 || i == 2 || i == 7 ? 2 : i >= 9 ? 4 : 1;
                obj.AddComponent<LevelInteractable>().Item = new ItemData(ids[i], names[i], size,
                    i < 9 ? config.UsefulItemPoints : config.UselessItemPoints);
                house.Label(names[i], pos + Vector3.up * .35f, Color.white, .065f);
            }
            foreach (GameObject window in house.Windows)
            { window.AddComponent<LevelInteractable>().Kind = InteractionKind.Window; }
            Utility("gas", "Cerrar llave de gas", new(.9f, 1.3f, 5.7f), Color.yellow);
            Utility("power", "Bajar llave general", new(1.5f, 1.4f, -6.7f), Color.gray);
            for (int floor = 0; floor < 2; floor++)
            {
                float y = floor * 3.2f;
                for (int i = 0; i < 4; i++)
                    house.FallingFurniture.Add(house.Box("Libro", new(-4.9f, y + 2.3f, -.5f + i * .3f), new(.3f, .2f, .2f), Color.red));
                house.FallingFurniture.Add(house.Box("Lámpara", new(1, y + 2.7f, -4.5f), new(.4f, .3f, .4f), Color.yellow));
                house.FallingFurniture.Add(house.Box("Cuadro", new(-5.8f, y + 2, -5), new(.1f, .7f, .8f), Color.magenta));
            }
            foreach (GameObject obj in house.FallingFurniture) obj.AddComponent<FallingHazard>();
            // Bloquea el atajo oeste; el paso central y la escalera este permanecen transitables.
            debris = house.Box("Escombros: desvío", new(-2.6f, .6f, -1.3f), new(3, 1.2f, 1), Color.gray);
            debris.SetActive(false);
            house.Label("RUTA: PASO CENTRAL → SALIDA", new(0, 2.6f, -.5f), Color.green, .09f);
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { sun = light; sunIntensity = light.intensity; break; }
            GameEvents.OnTremorChanged += Tremor;
            GameEvents.OnStateChanged += State;
        }
        void Utility(string key, string label, Vector3 pos, Color color)
        {
            var obj = house.Box(label, pos, new(.45f, .45f, .25f), color);
            var interaction = obj.AddComponent<LevelInteractable>();
            interaction.Kind = InteractionKind.Utility; interaction.Key = key; interaction.Label = label;
            house.Label(label, pos + Vector3.up * .5f, Color.white, .075f);
        }
        void Tremor(bool shaking)
        {
            if (!shaking || started) return;
            started = true; debris.SetActive(true);
            foreach (Light light in house.HouseLights) light.enabled = false;
            if (sun != null) sun.intensity = .12f;
            foreach (GameObject window in house.Windows)
            {
                window.GetComponent<Renderer>().enabled = false;
                // El collider sigue detectando el intento peligroso de usar la ventana.
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = window.transform.position + Vector3.up * (.1f * i);
                    var shard = house.Box("Fragmento de vidrio", pos, new(.12f, .12f, .12f), Color.cyan);
                    shard.AddComponent<Rigidbody>().mass = .1f;
                    Destroy(shard, 4);
                }
            }
            GameEvents.Sound("glass");
        }
        void State(GameState state)
        { if (state == GameState.Evacuation) house.ExitGate.SetActive(false); }
        void Update()
        {
            var game = GameManager.Instance;
            if (game.Player == null || game.Paused) return;
            Vector3 p = game.Player.Position;
            if (Mathf.Abs(p.x) < 3 && Mathf.Abs(p.z + 22) < 3 && p.y < 1)
                game.TryEvacuate();
        }
        void OnDestroy()
        {
            GameEvents.OnTremorChanged -= Tremor; GameEvents.OnStateChanged -= State;
            if (sun != null) sun.intensity = sunIntensity;
        }
    }
}
```

**Ruta: `Assets/_Project/Scripts/Level/LevelInteractable.cs`**

```csharp
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
```

**Ruta: `Assets/_Project/Scripts/Level/FallingHazard.cs`**

```csharp
using UnityEngine;

namespace DisasterReady
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallingHazard : MonoBehaviour
    {
        Rigidbody body;
        Collider shape;
        float cooldown;
        void Awake()
        {
            body = GetComponent<Rigidbody>(); body.isKinematic = true; body.mass = 3;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            shape = GetComponent<Collider>(); GameEvents.OnTremorChanged += Tremor;
        }
        void Tremor(bool value)
        {
            if (!value) return;
            body.isKinematic = false;
            body.AddForce(new Vector3(1.2f, .2f, -.4f), ForceMode.Impulse);
            body.AddTorque(new Vector3(0, 0, -4), ForceMode.Impulse);
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || game.Paused || !game.Playing || game.Player == null) return;
            cooldown -= Time.deltaTime;
            if (body.isKinematic || body.linearVelocity.sqrMagnitude < .5f || cooldown > 0) return;
            Vector3 center = game.Player.Position + Vector3.up * (game.Player.Crouched ? .45f : .9f);
            if (Vector3.Distance(shape.ClosestPoint(center), center) > .45f) return;
            cooldown = 1;
            if (!game.Protected) game.Damage(12, "Evita muebles altos y objetos que pueden caer.");
        }
        void OnCollisionEnter(Collision collision)
        { if (collision.relativeVelocity.sqrMagnitude > 2) GameEvents.Sound("impact"); }
        void OnDestroy() => GameEvents.OnTremorChanged -= Tremor;
    }
}
```

### Abraham – Commit 1: `feat(ui): crear menu HUD pausa y resultados`

Rama: `feature/ui-audio-abraham`.

**Ruta: `Assets/_Project/Scripts/UI/GameUI.cs`**

```csharp
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DisasterReady
{
    public sealed class GameUI : MonoBehaviour
    {
        Canvas canvas;
        Font font;
        Text hud, hint, message, reticle;
        GameObject panel;
        float messageUntil;
        GameManager Game => GameManager.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(50, (root, house) =>
            root.gameObject.AddComponent<GameUI>().Build());
        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Interfaz", typeof(RectTransform));
            canvasObject.transform.SetParent(transform);
            canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var events = Object.FindFirstObjectByType<EventSystem>();
            if (events == null)
            {
                var obj = new GameObject("EventSystem"); obj.transform.SetParent(transform);
                events = obj.AddComponent<EventSystem>();
            }
            foreach (BaseInputModule module in events.GetComponents<BaseInputModule>()) module.enabled = false;
            var input = events.GetComponent<InputSystemUIInputModule>();
            if (input == null) input = events.gameObject.AddComponent<InputSystemUIInputModule>();
            input.enabled = true; input.AssignDefaultActions();
            hud = TextAt(canvas.transform, "", 18, new(-210, 235), new(820, 220));
            hint = TextAt(canvas.transform, "", 21, new(0, -200), new(1100, 70), TextAnchor.MiddleCenter);
            message = TextAt(canvas.transform, "", 20, new(0, -300), new(1120, 65), TextAnchor.MiddleCenter);
            reticle = TextAt(canvas.transform, "+", 22, Vector2.zero, new(40, 40), TextAnchor.MiddleCenter);
            GameEvents.OnStateChanged += State;
            GameEvents.OnMessage += Message;
            State(Game.State);
        }
        RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        Text TextAt(Transform parent, string value, int size, Vector2 pos, Vector2 bounds,
            TextAnchor align = TextAnchor.UpperLeft)
        {
            var text = Rect(parent, "Texto", pos, bounds).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Color.white;
            text.alignment = align; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
        void Button(string label, float y, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(panel.transform, label, new(0, y), new(340, 42));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new(.12f, .42f, .46f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(action);
            TextAt(rect, label, 20, Vector2.zero, new(330, 40), TextAnchor.MiddleCenter);
        }
        void ClearPanel()
        { if (panel != null) { panel.SetActive(false); Destroy(panel); } }
        void Panel(string title, string body)
        {
            ClearPanel();
            panel = Rect(canvas.transform, "Panel", Vector2.zero, new(920, 650)).gameObject;
            panel.AddComponent<Image>().color = new(.045f, .075f, .11f, .98f);
            TextAt(panel.transform, title, 30, new(0, 270), new(860, 65), TextAnchor.MiddleCenter);
            // Texto desplazable: resultados completos incluso en partidas con muchos errores.
            var viewport = Rect(panel.transform, "Resumen desplazable", new(0, 45), new(840, 350));
            viewport.gameObject.AddComponent<Image>().color = new(0, 0, 0, .12f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var text = TextAt(viewport, body, 20, Vector2.zero, new(810, 350));
            text.raycastTarget = true;
            var content = text.rectTransform; content.anchorMin = content.anchorMax = new(.5f, 1);
            content.pivot = new(.5f, 1); content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new(810, Mathf.Max(350, text.preferredHeight + 20));
            scroll.content = content; scroll.verticalNormalizedPosition = 1;
        }
        void MainMenu()
        {
            Panel("DISASTER READY", "Simulación educativa · Huancayo, Perú\n\nPrepara tu mochila, protégete durante el sismo y evacúa al punto de reunión.\n\nUna vivienda de dos pisos, decisiones bajo presión y una oportunidad para aprender antes de una emergencia real.");
            Button("Jugar", -160, Game.Play); Button("Cómo jugar", -210, Help);
            Button("Créditos", -260, Credits); Button("Salir", -310, Quit);
        }
        void Help()
        {
            Panel("CÓMO JUGAR", "WASD: caminar · Ratón: mirar · Shift: correr\nCtrl: agacharte · H: sujetarte (quieto y agachado)\nE: interactuar · F: linterna recogida · Esc: pausa\n\n1. Recoge suministros durante la preparación. La mochila tiene capacidad limitada.\n2. La alarma pertenece a este simulacro: no predice todos los sismos.\n3. Agáchate bajo la mesa firme y mantén H sin moverte. Aléjate de ventanas y muebles.\n4. Tras el movimiento, sigue el paso central, baja por la escalera este y sal por la puerta sur.\n5. En una réplica, vuelve a protegerte. Llega al punto verde exterior.\n\nLas zonas estructurales del escenario se consideran previamente evaluadas. En la vida real, identifica las zonas con asesoramiento competente. Corta suministros solo si sabes hacerlo, es seguro y no retrasa la evacuación. Ante olor a gas, evita interruptores eléctricos.\n\nLa mochila se prepara con anticipación; el tiempo limitado es una mecánica del simulacro.");
            Button("Volver", -245, MainMenu);
        }
        void Credits()
        {
            Panel("CRÉDITOS", "Keen · Dirección, diseño, fases e integración\nRicardo · Movimiento, interacción y mochila\nJoshua · Casa, objetos, zonas y peligros\nAbraham · UI/UX, audio y QA\n\nInspiraciones: 60 Seconds!, The Long Dark, Disaster Report 4 y Stop Disasters!\n\nODS 11, meta 11.5: preparación para reducir consecuencias de desastres.\nODS 13, meta 13.1: educación para la resiliencia.\nLos sismos son fenómenos geológicos, no causados por el cambio climático.\n\nReferencias educativas: INDECI, Plan Familiar de Emergencia y recomendaciones ante sismos. Prototipo universitario; no evalúa la seguridad real de una vivienda.");
            Button("Volver", -245, MainMenu);
        }
        void State(GameState state)
        {
            ClearPanel();
            if (state == GameState.Menu) MainMenu();
            if (state == GameState.Result)
            {
                string title = Game.Won ? "EVACUACIÓN COMPLETADA · " + Game.Grade : "DERROTA · " + Game.Grade;
                Panel(title, $"Puntaje: {Game.Score} · Salud: {Game.Health:0}\nCalificación: A ≥ {Game.Config.GradeA}; B ≥ {Game.Config.GradeB}; C por debajo.\nLa calificación mide decisiones; una derrota sigue siendo derrota.\n\nACIERTOS, ERRORES Y CONSEJOS\n• " + string.Join("\n• ", Game.Notes) + "\n\nPrepara la mochila antes de la emergencia. Camina con calma, no regreses por objetos y sigue las indicaciones oficiales.");
                Button("Reintentar", -210, Game.Play); Button("Menú", -265, Game.Menu);
            }
        }
        void Message(string text) { message.text = text; messageUntil = Time.unscaledTime + 4; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Game.Playing)
            {
                Game.SetPause(!Game.Paused);
                if (Game.Paused)
                {
                    Panel("PAUSA", "El tiempo y el audio están detenidos.");
                    Button("Continuar", -180, () => { Game.SetPause(false); ClearPanel(); });
                    Button("Reintentar", -235, Game.Play); Button("Menú", -290, Game.Menu);
                }
                else ClearPanel();
            }
            bool show = Game.Playing && !Game.Paused;
            hud.gameObject.SetActive(show); hint.gameObject.SetActive(show); reticle.gameObject.SetActive(show);
            if (Time.unscaledTime > messageUntil) message.text = "";
            if (!show) return;
            var bag = Game.Player?.Backpack;
            string bagText = bag == null ? "Sin módulo de mochila" :
                $"Mochila: {bag.Used}/{bag.Capacity}\n" + string.Join(", ", bag.Items.Select(i => i.Name));
            hud.text = $"{PhaseName()} · {Game.Remaining:0}s\nSalud: {Game.Health:0} · Puntos: {Game.Score}\n{bagText}\n" +
                (Game.Shaking ? Game.Protected ? "PROTEGIDO" : "EXPUESTO · Ctrl + H en zona segura" : "Ctrl: agacharse · E: usar · F: linterna · Esc: pausa");
            hint.text = string.IsNullOrEmpty(Game.InteractionHint) ? Instruction() : Game.InteractionHint;
        }
        string PhaseName() => Game.State switch
        {
            GameState.Preparation => "PREPARACIÓN", GameState.Alarm => "ALARMA DE SIMULACRO",
            GameState.Earthquake => "SISMO", GameState.Evacuation => Game.Shaking ? "RÉPLICA" : "EVACUACIÓN", _ => ""
        };
        string Instruction() => Game.State switch
        {
            GameState.Preparation => "Recoge suministros útiles; encuentra la mesa y la escalera.",
            GameState.Alarm => "¡Alarma del simulacro! Ubícate en tu zona segura.",
            GameState.Earthquake => "Agáchate, cúbrete y sujétate: Ctrl + H, sin moverte.",
            GameState.Evacuation => Game.Shaking ? "¡Réplica! Protégete y espera." : "Camina hacia la salida y el punto de reunión exterior.", _ => ""
        };
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy()
        { GameEvents.OnStateChanged -= State; GameEvents.OnMessage -= Message; }
    }
}
```

### Abraham – Commit 2: `feat(audio): agregar sonidos de respaldo y checklist QA`

Rama: `feature/ui-audio-abraham`.

**Ruta: `Assets/_Project/Scripts/Audio/AudioManager.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public sealed class AudioManager : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new();
        readonly List<AudioClip> generated = new();
        AudioSource loop, effects;
        float lastImpact = -10;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(60, (root, house) =>
            root.gameObject.AddComponent<AudioManager>().Build());
        void Build()
        {
            loop = gameObject.AddComponent<AudioSource>(); loop.loop = true; loop.volume = .15f;
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .3f;
            foreach (string name in new[] { "ambient", "alarm", "quake", "calm", "pickup", "impact", "glass", "switch" })
            {
                var clip = Resources.Load<AudioClip>(name);
                if (clip == null) { clip = Generate(name); generated.Add(clip); }
                clips[name] = clip;
            }
            GameEvents.OnStateChanged += State;
            GameEvents.OnTremorChanged += Tremor;
            GameEvents.OnSound += Sound;
            State(GameManager.Instance.State);
        }
        AudioClip Generate(string name)
        {
            const int rate = 22050;
            bool longClip = name == "ambient" || name == "alarm" || name == "quake" || name == "calm";
            int count = Mathf.RoundToInt(rate * (longClip ? 2 : .2f));
            var data = new float[count]; var random = new System.Random(77);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / rate;
                float wave = name switch
                {
                    "quake" => (float)(random.NextDouble() * 2 - 1) * .3f + Mathf.Sin(2 * Mathf.PI * 45 * t) * .4f,
                    "alarm" => Mathf.Sin(2 * Mathf.PI * (Mathf.FloorToInt(t * 4) % 2 == 0 ? 660 : 880) * t) * .5f,
                    "impact" or "glass" => (float)(random.NextDouble() * 2 - 1) * (1 - (float)i / count),
                    "ambient" or "calm" => Mathf.Sin(2 * Mathf.PI * 220 * t) * .15f + Mathf.Sin(2 * Mathf.PI * 330 * t) * .08f,
                    _ => Mathf.Sin(2 * Mathf.PI * 600 * t) * .4f
                };
                // Rampas cortas evitan clics al principio/final del búfer.
                float envelope = Mathf.Min(1, Mathf.Min(i / 220f, (count - 1 - i) / 220f));
                data[i] = wave * envelope;
            }
            var clip = AudioClip.Create("Respaldo " + name, count, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
        void PlayLoop(string key)
        { if (loop.clip == clips[key] && loop.isPlaying) return; loop.clip = clips[key]; loop.Play(); }
        void State(GameState state)
        {
            PlayLoop(state switch
            {
                GameState.Alarm => "alarm", GameState.Earthquake => "quake",
                GameState.Evacuation or GameState.Result => "calm", _ => "ambient"
            });
        }
        void Tremor(bool active)
        { if (active) PlayLoop("quake"); else State(GameManager.Instance.State); }
        void Sound(string key)
        {
            if (!clips.TryGetValue(key, out AudioClip clip)) return;
            if (key == "impact" && Time.unscaledTime - lastImpact < .12f) return;
            if (key == "impact") lastImpact = Time.unscaledTime;
            effects.PlayOneShot(clip);
        }
        void OnDestroy()
        {
            GameEvents.OnStateChanged -= State; GameEvents.OnTremorChanged -= Tremor;
            GameEvents.OnSound -= Sound;
            foreach (AudioClip clip in generated) Destroy(clip);
        }
    }
}
```

**Ruta: `Assets/_Project/Scripts/UI/QA.md`**

```markdown
# QA manual — Abraham

Estado inicial: PENDIENTE. Anotar fecha, equipo, versión y evidencia por caso.
No marcar aprobado sin ejecutar la prueba en Unity o en el build indicado.

| Caso | Procedimiento | Resultado esperado |
|---|---|---|
| Importación | Abrir en Unity 6000.6.3f1 | Console sin errores de compilación |
| Arranque repetido | Play/Stop cinco veces con ambas recargas desactivadas | Una raíz, un jugador, un Canvas; puntaje 0, salud 100, mochila vacía |
| Menú | Usar todos los botones | Ayuda y créditos visibles; Salir finaliza Play/editor o ejecutable |
| House directa | Abrir House y Play | Casa, jugador, HUD y audio aparecen |
| Preparación | Recorrer los dos pisos | Escalera transitable en ambos sentidos; jugador comienza arriba |
| Mesa | Entrar con Ctrl y soltar Ctrl bajo tablero | Cabe agachado; no se levanta atravesando la mesa |
| Protección | Mantener Ctrl+H bajo mesa sin moverse | PROTEGIDO durante sismo/réplica; soltar H o salir produce EXPUESTO |
| Inventario | Recoger los nueve suministros | 12/12 espacios y 72 puntos con configuración predeterminada |
| Capacidad | Recoger primero laptop y luego suministros | Laptop resta 12; se rechaza lo que no cabe sin consumirlo |
| Unicidad | Pulsar E repetidamente sobre un recogible | Una sola incorporación y una sola recompensa |
| Oclusión | Intentar interactuar a través de una pared | No se recoge el objeto oculto |
| Fases | Esperar sin pausar | 90 s, 5 s, 24 s; después evacuación de 100 s |
| Alarma | Observar transición | Suena antes del sismo y se explica como simulacro |
| Peligros | Permanecer junto a ventana/mueble | Daño, caída de muebles, vidrios rotos y sonidos; no daño permanente de muebles inmóviles |
| Ventanas | Intentar usar con E varias veces | Penalización única de 15 puntos; consejo de salida segura |
| Carrera | Correr durante sismo | Movimiento dificultado y penalización de 3 puntos por segundo evaluado |
| Luz | Esperar sismo y pulsar F | Se apagan luces domésticas; linterna solo si fue recogida |
| Rutas | Evacuar con escombros activos | Atajo bloqueado, paso central y escalera este transitables |
| Suministros | Usar gas/tablero antes, durante y después | Solo se aceptan en evacuación sin temblor; una bonificación por dispositivo |
| Réplica | Permanecer en evacuación más de 8 s | Temblor de 6 s; protección vuelve a funcionar |
| Victoria | Llegar al cuadrado verde sin temblor | Resultado exitoso, puntaje, A/B/C y consejos desplazables |
| Derrota por tiempo | Esperar 100 s de evacuación | Derrota; no se confunde calificación con victoria |
| Derrota por salud | Acumular impactos/exposición | Salud llega a 0 y finaliza una sola vez |
| Pausa | Esc durante alarma/sismo/réplica | Tiempo, física, interacción y audio se detienen; cursor visible |
| Reintento | Reintentar desde pausa y resultado cinco veces | Estado inicial completo; sin suscripciones ni audio duplicados |
| Audio vacío | Probar sin clips externos | Se escucha respaldo; no hay excepciones |
| Audio externo | Añadir ambient.wav con ese nombre | Se utiliza el archivo importado |
| Resolución | 1280x720 y 1920x1080 | Texto legible; botones y resumen accesibles |
| Build Windows | Build and Run de MainMenu y House | Mismo flujo, entrada y audio; sin shader rosa |
| Integración Git | Abrir clon limpio tras todos los merges | Ninguna referencia ausente; escenas originales sin cambios |

Regresión mínima tras cada merge: importación, Play/Stop dos veces y función nueva.
Regresión final: todos los casos, incluyendo ejecutable Windows.
```

## C. Guía de Git paso a paso

### 1. Preparar equipo, identidad y permisos

Instalar Git para Windows, Git LFS, Unity Hub, Unity 6000.6.3f1 y el módulo Windows Build Support. Usar PowerShell para los comandos de esta guía.

Cada integrante configura SU identidad. Reemplazar los valores de ejemplo, no copiarlos literalmente. El correo debe estar asociado y verificado en su cuenta, o ser el noreply exacto que GitHub muestra en Settings > Emails. `user.name` puede ser su nombre visible; no tiene que coincidir con el login. El correo es el que GitHub utiliza para asociar commits.

```powershell
git config --global user.name "TU NOMBRE EN GITHUB"
git config --global user.email "TU CORREO VERIFICADO O NOREPLY"
git config --global init.defaultBranch main
git lfs install
git config --global --get user.name
git config --global --get user.email
```

Ricardo, como propietario, abre el repositorio en GitHub > Settings > Collaborators > Add people y agrega las cuentas de Keen, Joshua y Abraham. Cada uno acepta su invitación. No usar contraseñas de otros compañeros ni fabricar commits con su autoría. La autenticación HTTPS de Git puede abrir el navegador mediante Git Credential Manager.

```powershell
New-Item -ItemType Directory -Force C:/Proyectos
Set-Location C:/Proyectos
git clone https://github.com/RicardoF1/DisasterReady.git
Set-Location C:/Proyectos/DisasterReady
git lfs pull
git status
git log --oneline -5
```

Si ya tienen el clon, entrar a su carpeta y usar `git status`, `git switch main`, `git pull --ff-only origin main`; no volver a clonar encima.

### 2. Preparar la copia de entrega

Extraer el ZIP en `C:/DisasterReady-Entrega`. Debe quedar `C:/DisasterReady-Entrega/DisasterReady/Assets`. El ZIP no contiene `.git`: es una copia abrible de Unity y una fuente de archivos para sus ramas, no reemplaza su clon de GitHub.

En PowerShell, estando dentro de SU clon, definir lo siguiente. La función copia un archivo y, cuando existe, su `.meta`. También copia los metadatos de la carpeta propia si todavía no existen en el clon. No sobrescribe metadatos de carpetas que ya están versionados.

```powershell
$entrega = "C:/DisasterReady-Entrega/DisasterReady"
function Copiar-Entrega([string]$Ruta) {
    $origen = Join-Path $entrega $Ruta
    $destino = Join-Path (Get-Location) $Ruta
    $carpeta = Split-Path $destino -Parent
    New-Item -ItemType Directory -Force $carpeta | Out-Null
    Copy-Item -LiteralPath $origen -Destination $destino
    if (Test-Path -LiteralPath "$origen.meta") {
        Copy-Item -LiteralPath "$origen.meta" -Destination "$destino.meta"
    }
    $carpetaOrigen = Split-Path $origen -Parent
    if ((Test-Path -LiteralPath "$carpetaOrigen.meta") -and
        -not (Test-Path -LiteralPath "$carpeta.meta")) {
        Copy-Item -LiteralPath "$carpetaOrigen.meta" -Destination "$carpeta.meta"
    }
}
```

**Antes de cada commit:** volver a Unity, esperar importación, comprobar Console sin errores, probar la función incorporada, detener Play y volver a PowerShell. No guardar cambios de escenas. Los commits intermedios son compilables pero no contienen aún el juego completo: Core solo no genera interfaces, Player solo no crea casa y Level 1 no contiene peligros.

### 3. Ramas y ocho commits propios

Keen realiza primero sus dos commits y abre su PR. Una vez fusionado Core, los otros tres pueden avanzar simultáneamente en sus propias ramas. Si ya habían creado las ramas, no repetir `git switch -c`: utilizar `git switch NOMBRE` y actualizar como se indica más abajo.

#### Keen — commit 1

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/fases-keen
Copiar-Entrega "Assets/_Project/Scripts/Core/Contracts.cs"
Copiar-Entrega "Assets/_Project/Scripts/Core/GameConfig.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Core/Contracts.cs" "Assets/_Project/Scripts/Core/GameConfig.cs" "Assets/_Project/Scripts/Core/Contracts.cs.meta" "Assets/_Project/Scripts/Core/GameConfig.cs.meta" "Assets/_Project/Scripts/Core.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(core): definir contratos y configuracion del simulacro"
git push -u origin feature/fases-keen
```

#### Keen — commit 2

```powershell
Copiar-Entrega "Assets/_Project/Scripts/Core/SceneBootstrap.cs"
Copiar-Entrega "Assets/_Project/Scripts/Core/GameManager.cs"
Copiar-Entrega "README.md"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Core/SceneBootstrap.cs" "Assets/_Project/Scripts/Core/GameManager.cs" "README.md" "Assets/_Project/Scripts/Core/SceneBootstrap.cs.meta" "Assets/_Project/Scripts/Core/GameManager.cs.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(core): integrar fases puntuacion y bootstrap automatico"
git push
```

#### Ricardo — commit 1

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/gameplay-ricardo
Copiar-Entrega "Assets/_Project/Scripts/Player/FirstPersonPlayer.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Player/FirstPersonPlayer.cs" "Assets/_Project/Scripts/Player/FirstPersonPlayer.cs.meta" "Assets/_Project/Scripts/Player.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(player): agregar movimiento primera persona y linterna"
git push -u origin feature/gameplay-ricardo
```

#### Ricardo — commit 2

```powershell
Copiar-Entrega "Assets/_Project/Scripts/Player/PlayerInteraction.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Player/PlayerInteraction.cs" "Assets/_Project/Scripts/Player/PlayerInteraction.cs.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(player): implementar mochila e interaccion desacoplada"
git push
```

#### Joshua — commit 1

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/nivel-joshua
Copiar-Entrega "Assets/_Project/Scripts/Level/HouseBuilder.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Level/HouseBuilder.cs" "Assets/_Project/Scripts/Level/HouseBuilder.cs.meta" "Assets/_Project/Scripts/Level.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(level): construir vivienda de dos pisos y zonas seguras"
git push -u origin feature/nivel-joshua
```

#### Joshua — commit 2

```powershell
Copiar-Entrega "Assets/_Project/Scripts/Level/LevelContent.cs"
Copiar-Entrega "Assets/_Project/Scripts/Level/LevelInteractable.cs"
Copiar-Entrega "Assets/_Project/Scripts/Level/FallingHazard.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Level/LevelContent.cs" "Assets/_Project/Scripts/Level/LevelInteractable.cs" "Assets/_Project/Scripts/Level/FallingHazard.cs" "Assets/_Project/Scripts/Level/LevelContent.cs.meta" "Assets/_Project/Scripts/Level/LevelInteractable.cs.meta" "Assets/_Project/Scripts/Level/FallingHazard.cs.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(level): incorporar suministros peligros y evacuacion"
git push
```

#### Abraham — commit 1

```powershell
git switch main
git pull --ff-only origin main
git switch -c feature/ui-audio-abraham
Copiar-Entrega "Assets/_Project/Scripts/UI/GameUI.cs"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/UI/GameUI.cs" "Assets/_Project/Scripts/UI/GameUI.cs.meta" "Assets/_Project/Scripts/UI.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(ui): crear menu HUD pausa y resultados"
git push -u origin feature/ui-audio-abraham
```

#### Abraham — commit 2

```powershell
Copiar-Entrega "Assets/_Project/Scripts/Audio/AudioManager.cs"
Copiar-Entrega "Assets/_Project/Scripts/UI/QA.md"
```

Abrir Unity, comprobar importación y hacer la prueba correspondiente antes de continuar.

```powershell
git status
git add "Assets/_Project/Scripts/Audio/AudioManager.cs" "Assets/_Project/Scripts/UI/QA.md" "Assets/_Project/Scripts/Audio/AudioManager.cs.meta" "Assets/_Project/Scripts/UI/QA.md.meta" "Assets/_Project/Scripts/Audio.meta"
git diff --cached --check
git diff --cached --stat
git commit -m "feat(audio): agregar sonidos de respaldo y checklist QA"
git push
```

### 4. Pull Requests y orden de integración

En GitHub, abrir Pull requests > New pull request. Base: `main`; compare: rama propia. Incluir problema resuelto, archivos, prueba hecha en Unity y evidencia breve. Pedir revisión a otro integrante.

Orden: **Keen → Ricardo → Joshua → Abraham**. Solo Keen es dependencia de código de los demás; el orden posterior facilita revisar el juego por capas. Cada PR debe contener los dos commits propios. Usar **Create a merge commit**, no squash, para conservar ambos commits originales y su autoría. No hacer push directo a main.

Antes de fusionar cada PR, actualizar su rama con main. Cerrar Unity y comprobar que `git status` no muestra cambios sin guardar. Cada uno ejecuta únicamente su bloque:

```powershell
# Ricardo
git switch feature/gameplay-ricardo
git fetch origin
git merge origin/main
git push
```

```powershell
# Joshua
git switch feature/nivel-joshua
git fetch origin
git merge origin/main
git push
```

```powershell
# Abraham
git switch feature/ui-audio-abraham
git fetch origin
git merge origin/main
git push
```

Después de actualizar: abrir Unity y repetir importación + Play/Stop dos veces + prueba del módulo. Luego revisar y fusionar en GitHub. Los commits de merge adicionales no sustituyen los dos commits de implementación.

Después del último merge, todos ejecutan:

```powershell
git switch main
git pull --ff-only origin main
git lfs pull
git log --format="%h %an <%ae> %s" -15
git shortlog -sne main
```

Keen hace la prueba de integración final y revisa el README. Si detecta fallos, el propietario del módulo corrige en su misma rama mediante un commit adicional y otro PR; no reescribir los commits ya compartidos.

### 5. Resolver un conflicto sin perder trabajo

Ejemplo: un conflicto real en `GameManager.cs`. No ejecutar este ejemplo si no hay conflicto en ese archivo.

```powershell
git status
git diff --name-only --diff-filter=U
```

Abrir el archivo listado; los marcadores `<<<<<<<`, `=======`, `>>>>>>>` separan las versiones. Comparar ambas, conservar la solución que respeta el contrato y eliminar TODOS los marcadores. No escoger una versión sin leer. Volver a Unity y comprobar que compila y funciona. Después:

```powershell
git add Assets/_Project/Scripts/Core/GameManager.cs
git diff --cached --check
git merge --continue
git push
```

Si se necesita cancelar el merge para revisarlo con el equipo, antes de finalizarlo:

```powershell
git merge --abort
```

No usar `reset --hard`, `clean -fd` ni force-push como solución para principiantes. Si hay trabajo local pendiente, hacer su commit en la rama propia antes de integrar main.

### 6. .meta, escenas y archivos que no se suben

- Un asset y su `.meta` viajan juntos. El `.meta` guarda el GUID, por lo que no se regenera arbitrariamente ni se borran los del compañero.
- Mover/renombrar assets desde Unity mueve también sus metadatos. Versionar ambos cambios.
- Si dos personas crearon el mismo archivo con GUID distintos, acordar cuál es el asset canónico y revisar las referencias antes de resolver. No combinar dos GUID dentro de un `.meta`.
- No editar `MainMenu.unity` ni `House.unity` para instalar estos sistemas. Las escenas del ZIP son las originales. Tampoco modificar paquetes o ajustes sin acordarlo.
- Versionar `Assets`, `Packages`, `ProjectSettings`, `.gitignore`, `.gitattributes`, README y sus cambios intencionados.
- No subir `Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, builds, `.vs` ni cachés. Mantener el `.gitignore` existente.
- Mantener Git LFS para los tipos pesados existentes. No poner los scripts C# en LFS.
- Si Unity muestra modificaciones incidentales, revisarlas con `git diff`. No incluirlas en el commit por comodidad; tampoco borrarlas sin comprobar si pertenecen a otro trabajo.

## D. Cómo probar

1. Extraer el ZIP o usar el clon con los cuatro PR fusionados.
2. Unity Hub > Add project from disk > carpeta `DisasterReady`.
3. Abrir con **6000.6.3f1** y esperar la importación.
4. Confirmar Console sin errores. No añadir componentes ni arrastrar referencias.
5. Abrir `Assets/_Project/Scenes/MainMenu.unity`, pulsar Play y después Jugar.
6. El jugador aparece arriba, mirando hacia el pasillo. Recoger con E; Ctrl permite entrar bajo la mesa. Mantener H quieto para sujetarse.
7. Después del sismo, ir al descanso norte de la escalera este; bajar hacia el sur. Usar el paso central y la puerta sur. El punto verde está al otro lado de la calle.
8. Probar pausa, resultados y reintento; después repetir Play/Stop con las dos recargas desactivadas.
9. File > Build Profiles > Windows, confirmar MainMenu primero y House segundo; usar Build and Run. Guardar el build fuera de los archivos que se versionan.

| Control | Acción |
|---|---|
| WASD | Caminar y recorrer escaleras |
| Ratón | Mirar |
| Shift | Correr; penalizado durante movimiento sísmico |
| Ctrl | Agacharse; no se permite levantarse a través de una mesa |
| H + Ctrl, sin desplazarse | Sujetarse y protegerse en zona marcada |
| E | Recoger o interactuar |
| F | Linterna, si fue recogida |
| Esc | Pausa/reanudar |

Configuración opcional: crear `GameConfig` con Create > Disaster Ready > Configuración y guardarlo como `Assets/_Project/Scripts/Core/Resources/DisasterConfig.asset`. El archivo es opcional: los valores predeterminados funcionan sin él. Mantener duraciones positivas y `GradeA > GradeB`.

Audio opcional: colocar `ambient`, `alarm`, `quake`, `calm`, `pickup`, `impact`, `glass`, `switch` como WAV/OGG bajo `Assets/_Project/Audio/Resources`. Sin ellos se generan clips. Elegir audio con licencia compatible, editar con Audacity y documentar autor, enlace y licencia. No se descargaron archivos externos para esta entrega.

Modelos futuros: conservar el objeto raíz, collider e interacción; sustituir su representación por un hijo visual importado. Ajustar dimensiones físicas al modelo. No eliminar los componentes de lógica al cambiar una malla.

### Checklist de aceptación



Estado inicial: PENDIENTE. Anotar fecha, equipo, versión y evidencia por caso.
No marcar aprobado sin ejecutar la prueba en Unity o en el build indicado.

| Caso | Procedimiento | Resultado esperado |
|---|---|---|
| Importación | Abrir en Unity 6000.6.3f1 | Console sin errores de compilación |
| Arranque repetido | Play/Stop cinco veces con ambas recargas desactivadas | Una raíz, un jugador, un Canvas; puntaje 0, salud 100, mochila vacía |
| Menú | Usar todos los botones | Ayuda y créditos visibles; Salir finaliza Play/editor o ejecutable |
| House directa | Abrir House y Play | Casa, jugador, HUD y audio aparecen |
| Preparación | Recorrer los dos pisos | Escalera transitable en ambos sentidos; jugador comienza arriba |
| Mesa | Entrar con Ctrl y soltar Ctrl bajo tablero | Cabe agachado; no se levanta atravesando la mesa |
| Protección | Mantener Ctrl+H bajo mesa sin moverse | PROTEGIDO durante sismo/réplica; soltar H o salir produce EXPUESTO |
| Inventario | Recoger los nueve suministros | 12/12 espacios y 72 puntos con configuración predeterminada |
| Capacidad | Recoger primero laptop y luego suministros | Laptop resta 12; se rechaza lo que no cabe sin consumirlo |
| Unicidad | Pulsar E repetidamente sobre un recogible | Una sola incorporación y una sola recompensa |
| Oclusión | Intentar interactuar a través de una pared | No se recoge el objeto oculto |
| Fases | Esperar sin pausar | 90 s, 5 s, 24 s; después evacuación de 100 s |
| Alarma | Observar transición | Suena antes del sismo y se explica como simulacro |
| Peligros | Permanecer junto a ventana/mueble | Daño, caída de muebles, vidrios rotos y sonidos; no daño permanente de muebles inmóviles |
| Ventanas | Intentar usar con E varias veces | Penalización única de 15 puntos; consejo de salida segura |
| Carrera | Correr durante sismo | Movimiento dificultado y penalización de 3 puntos por segundo evaluado |
| Luz | Esperar sismo y pulsar F | Se apagan luces domésticas; linterna solo si fue recogida |
| Rutas | Evacuar con escombros activos | Atajo bloqueado, paso central y escalera este transitables |
| Suministros | Usar gas/tablero antes, durante y después | Solo se aceptan en evacuación sin temblor; una bonificación por dispositivo |
| Réplica | Permanecer en evacuación más de 8 s | Temblor de 6 s; protección vuelve a funcionar |
| Victoria | Llegar al cuadrado verde sin temblor | Resultado exitoso, puntaje, A/B/C y consejos desplazables |
| Derrota por tiempo | Esperar 100 s de evacuación | Derrota; no se confunde calificación con victoria |
| Derrota por salud | Acumular impactos/exposición | Salud llega a 0 y finaliza una sola vez |
| Pausa | Esc durante alarma/sismo/réplica | Tiempo, física, interacción y audio se detienen; cursor visible |
| Reintento | Reintentar desde pausa y resultado cinco veces | Estado inicial completo; sin suscripciones ni audio duplicados |
| Audio vacío | Probar sin clips externos | Se escucha respaldo; no hay excepciones |
| Audio externo | Añadir ambient.wav con ese nombre | Se utiliza el archivo importado |
| Resolución | 1280x720 y 1920x1080 | Texto legible; botones y resumen accesibles |
| Build Windows | Build and Run de MainMenu y House | Mismo flujo, entrada y audio; sin shader rosa |
| Integración Git | Abrir clon limpio tras todos los merges | Ninguna referencia ausente; escenas originales sin cambios |

Regresión mínima tras cada merge: importación, Play/Stop dos veces y función nueva.
Regresión final: todos los casos, incluyendo ejecutable Windows.


## E. Mejoras futuras

1. Añadir familiares que necesiten ayuda, con decisiones de rescate que no incentiven exponerse innecesariamente.
2. Crear un escenario de inundación con rutas a zonas altas y reglas educativas propias, separadas de las del sismo.
3. Sustituir primitivas por modelos low-poly y añadir accesibilidad, localización y evaluación educativa con docentes.

## Fuentes técnicas y educativas consultadas

- Unity: dominio desactivado y reinicio de estáticos: https://docs.unity.com/en-us/engine/6000.0/manual/programming-environment/code-reload-serialization/code-reloading-editor/domain-reloading
- Unity: InputSystemUIInputModule: https://docs.unity.cn/Packages/com.unity.inputsystem@1.5/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html
- Unity: metadatos de assets: https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/import-assets/asset-metadata
- GitHub: correo de autoría: https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address
- INDECI: recomendaciones de preparación: https://www.gob.pe/institucion/indeci/noticias/576089-indeci-recomienda-medidas-de-preparacion-ante-sismos
- INDECI: qué hacer en caso de sismo: https://www.gob.pe/1053-que-hacer-en-caso-de-sismo

Las referencias explican las decisiones del diseño; no sustituyen la validación en la versión exacta del editor ni una evaluación estructural real.
