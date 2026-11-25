
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

        public bool Protected =>
            Player != null &&
            World != null &&
            Player.Crouched &&
            Player.Holding &&
            World.IsSheltered(Player.Position);

        public bool Playing =>
            State != GameState.Menu &&
            State != GameState.Result;

        public string Grade =>
            Score >= Config.GradeA ? "A" :
            Score >= Config.GradeB ? "B" : "C";

        public IReadOnlyList<string> Notes => notes;

        public string InteractionHint { get; set; } = "";

        readonly List<string> notes = new();
        readonly HashSet<string> once = new();

        float tick;
        float evacuationElapsed;

        bool house;
        bool aftershockStarted;
        bool ownedConfig;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            Instance = null;
        }

        public void Initialize(bool isHouse)
        {
            Instance = this;
            house = isHouse;

            Config = Resources.Load<GameConfig>("DisasterConfig");

            ownedConfig = Config == null;

            if (ownedConfig)
                Config = ScriptableObject.CreateInstance<GameConfig>();

            State = isHouse
                ? GameState.Preparation
                : GameState.Menu;

            Remaining = isHouse
                ? Config.PreparationSeconds
                : 0;
        }

        void Start()
        {
            GameEvents.State(State);
        }

        void Update()
        {
            if (!Playing || Paused)
                return;

            Remaining = Mathf.Max(
                0,
                Remaining - Time.deltaTime
            );

            // Control de la réplica durante la evacuación.
            if (State == GameState.Evacuation)
            {
                evacuationElapsed += Time.deltaTime;

                if (!aftershockStarted &&
                    evacuationElapsed >= Config.AftershockDelay)
                {
                    aftershockStarted = true;
                    SetShaking(true);

                    GameEvents.Message(
                        "¡Réplica! Protégete de nuevo."
                    );
                }

                if (aftershockStarted &&
                    Shaking &&
                    evacuationElapsed >=
                    Config.AftershockDelay + Config.AftershockSeconds)
                {
                    SetShaking(false);
                }
            }

            // Evaluación del jugador una vez por segundo.
            tick += Time.deltaTime;

            if (tick >= 1)
            {
                tick -= 1;

                if (Shaking && Player != null)
                {
                    if (Protected)
                    {
                        Award(
                            Config.ProtectionPointsPerSecond,
                            "Te agachaste, cubriste y sujetaste."
                        );
                    }
                    else
                    {
                        Damage(
                            1,
                            "Durante el movimiento busca una zona segura " +
                            "y protege tu cabeza."
                        );
                    }

                    if (Player.Running)
                    {
                        Award(
                            -Config.RunningPenalty,
                            "No corras durante el movimiento."
                        );
                    }

                    if (World != null &&
                        World.NearGlass(Player.Position))
                    {
                        Damage(
                            5,
                            "Aléjate de ventanas y vidrios."
                        );
                    }
                }
            }

            if (!Playing || Remaining > 0)
                return;

            // Cambio automático entre las fases.
            switch (State)
            {
                case GameState.Preparation:
                    Change(
                        GameState.Alarm,
                        Config.AlarmSeconds
                    );
                    break;

                case GameState.Alarm:
                    Change(
                        GameState.Earthquake,
                        Config.EarthquakeSeconds
                    );

                    SetShaking(true);
                    break;

                case GameState.Earthquake:
                    SetShaking(false);

                    Change(
                        GameState.Evacuation,
                        Config.EvacuationSeconds
                    );
                    break;

                case GameState.Evacuation:
                    Finish(
                        false,
                        "Se agotó el tiempo de evacuación."
                    );
                    break;
            }
        }

        void Change(GameState state, float seconds)
        {
            State = state;
            Remaining = seconds;
            tick = 0;

            GameEvents.State(state);
        }

        void SetShaking(bool value)
        {
            Shaking = value;
            GameEvents.Tremor(value);
        }

        public void Award(int points, string note)
        {
            if (!Playing)
                return;

            Score += points;
            AddNote(note);

            GameEvents.Score(Score);
        }

        public void AddNote(string note)
        {
            if (!notes.Contains(note))
                notes.Add(note);
        }

        public void AwardOnce(
            string key,
            int points,
            string note)
        {
            if (Playing && once.Add(key))
                Award(points, note);
        }

        public void Damage(float amount, string advice)
        {
            if (!Playing || Paused || amount <= 0)
                return;

            Health = Mathf.Max(0, Health - amount);

            AddNote(advice);
            GameEvents.Hit(amount);

            if (Health <= 0)
            {
                Finish(
                    false,
                    "Tu salud llegó a cero."
                );
            }
        }

        public bool UseUtility(string key, string label)
        {
            if (State != GameState.Evacuation ||
                Shaking ||
                Paused)
            {
                GameEvents.Message(
                    "Hazlo solo después del movimiento y si es seguro."
                );

                return false;
            }

            if (!once.Add(key))
                return false;

            Award(
                Config.UtilityPoints,
                label + ": acción realizada sin exposición adicional."
            );

            GameEvents.Sound("switch");

            return true;
        }

        public void TryEvacuate()
        {
            if (State != GameState.Evacuation ||
                Shaking ||
                Paused)
            {
                return;
            }

            AwardOnce(
                "exit",
                Config.EvacuationPoints,
                "Llegaste al punto de reunión."
            );

            Finish(
                true,
                "No regreses a una vivienda dañada " +
                "hasta que sea autorizada."
            );
        }

        void Finish(bool won, string note)
        {
            Won = won;

            AddNote(note);
            SetShaking(false);
            SetPause(false);

            Change(GameState.Result, 0);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetPause(bool value)
        {
            Paused = value && Playing;

            Time.timeScale = Paused ? 0 : 1;
            AudioListener.pause = Paused;

            bool capture = house && Playing && !Paused;

            Cursor.lockState = capture
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !capture;
        }

        public void Play()
        {
            SceneBootstrap.Load("House");
        }

        public void Menu()
        {
            SceneBootstrap.Load("MainMenu");
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (ownedConfig && Config != null)
                Destroy(Config);
        }
    }
}
