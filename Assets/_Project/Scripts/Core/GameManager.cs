using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Director de la simulación: fases, tiempo, intensidad del sismo, salud, puntaje,
    /// mochila y calificación final. Se crea solo al cargar la escena House.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---------- Estado público (solo lectura para los demás módulos) ----------
        public GameState State { get; private set; } = GameState.Preparacion;
        public float TimeLeft { get; private set; }
        public float PhaseDuration { get; private set; }
        public int Score { get; private set; }
        public int Health { get; private set; } = GameConfig.MaxHealth;
        public bool Paused { get; private set; }
        public bool IsShaking => QuakeInfo.Intensity > 0.02f;
        public bool InAftershock { get; private set; }
        public Inventory Backpack { get; } = new Inventory(GameConfig.BackpackCapacity);

        public bool PlayerProtected { get; private set; }
        public bool PlayerHolding { get; private set; }
        public bool PlayerExposed { get; private set; }
        public SafeZone CurrentZone { get; private set; }

        public string Objective { get; private set; } = "";

        // Resultado
        public bool Evacuated { get; private set; }
        public string Grade { get; private set; } = "C";
        public string ResultTitle { get; private set; } = "";
        public readonly List<ResultRow> ResultRows = new List<ResultRow>();
        public readonly List<string> Tips = new List<string>();

        public bool Playing => State != GameState.Resultado;

        // ---------- Estadísticas internas ----------
        readonly HashSet<string> tasks = new HashSet<string>();
        readonly HashSet<string> onceMessages = new HashSet<string>();
        float phaseElapsed;
        float protectedTime, holdTime, exposedTime, runningTime, unprotectedQuakeTime;
        float scoreAccumulator;
        int hits;
        bool firstProtection;
        bool aftershockDone;
        float evacuationTimeUsed;
        float quakeSeed;

        // ---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (Instance != null) return;
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake()
        {
            Instance = this;
            QuakeInfo.Intensity = 0f;
            QuakeInfo.PowerOn = true;
            quakeSeed = Random.value * 100f;
        }

        void Start() => SetState(GameState.Preparacion);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            QuakeInfo.Intensity = 0f;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        // ---------------------------------------------------------------------
        void Update()
        {
            if (!Playing || Paused) return;

            float dt = Time.deltaTime;
            TimeLeft = Mathf.Max(0f, TimeLeft - dt);
            phaseElapsed += dt;

            UpdateZone();
            UpdateIntensity();

            switch (State)
            {
                case GameState.Preparacion:
                    if (TimeLeft <= 0f) SetState(GameState.Alerta);
                    break;

                case GameState.Alerta:
                    if (TimeLeft <= 0f) SetState(GameState.Sismo);
                    break;

                case GameState.Sismo:
                    ScoreShaking(dt, 1f);
                    CheckLeftDuringQuake();
                    if (TimeLeft <= 0f) SetState(GameState.Evacuacion);
                    break;

                case GameState.Evacuacion:
                    evacuationTimeUsed += dt;
                    UpdateAftershock();
                    if (InAftershock) ScoreShaking(dt, 0.5f);
                    if (CurrentZone != null && CurrentZone.Type == ZoneType.PuntoReunion && !InAftershock)
                    {
                        Evacuated = true;
                        Finish();
                    }
                    else if (TimeLeft <= 0f)
                    {
                        AddTip("Se acabó el tiempo. Ubica con anticipación tu ruta de evacuación y no te detengas por objetos.");
                        Finish();
                    }
                    break;
            }

            FlushScore();
        }

        void UpdateZone()
        {
            CurrentZone = PlayerInfo.HasPlayer ? SafeZone.Find(PlayerInfo.Position) : null;
            bool safe = CurrentZone != null && CurrentZone.IsSafe && PlayerInfo.IsCrouching;
            PlayerProtected = safe;
            PlayerHolding = safe && PlayerInfo.IsHolding;
            PlayerExposed = !safe && CurrentZone != null && CurrentZone.Type == ZoneType.Ventana;
        }

        /// <summary>Curva del sismo: onda P suave en la alerta, fuerte en el sismo, réplica después.</summary>
        void UpdateIntensity()
        {
            float target = 0f;
            float t = Time.time;
            switch (State)
            {
                case GameState.Alerta:
                    // Últimos 2 segundos: primeras vibraciones (onda P)
                    if (TimeLeft < 2f) target = Mathf.Lerp(0.05f, 0.18f, 1f - TimeLeft / 2f);
                    break;
                case GameState.Sismo:
                {
                    float n = Mathf.Clamp01(phaseElapsed / GameConfig.QuakeSeconds);
                    float envelope = n < 0.12f ? Mathf.Lerp(0.3f, 1f, n / 0.12f)
                                   : n > 0.72f ? Mathf.Lerp(1f, 0.08f, (n - 0.72f) / 0.28f)
                                   : 1f;
                    float wobble = 0.82f + 0.18f * Mathf.PerlinNoise(quakeSeed, t * 1.7f);
                    target = envelope * wobble;
                    break;
                }
                case GameState.Evacuacion:
                    if (InAftershock)
                    {
                        float a = (phaseElapsed - GameConfig.AftershockAt) / GameConfig.AftershockSeconds;
                        target = GameConfig.AftershockIntensity * Mathf.Sin(Mathf.Clamp01(a) * Mathf.PI);
                    }
                    break;
            }
            QuakeInfo.Intensity = Mathf.MoveTowards(QuakeInfo.Intensity, target, Time.deltaTime * 1.5f);
        }

        void UpdateAftershock()
        {
            if (!aftershockDone && !InAftershock && phaseElapsed >= GameConfig.AftershockAt)
            {
                InAftershock = true;
                GameEvents.RaiseTremorChanged(true);
                GameEvents.RaiseMessage("¡RÉPLICA! Detente, agáchate y protégete hasta que pase.", MessageKind.Warning);
                SetObjective("¡RÉPLICA! Agáchate y protégete donde estés. No bajes corriendo la escalera.");
            }
            else if (InAftershock && phaseElapsed >= GameConfig.AftershockAt + GameConfig.AftershockSeconds)
            {
                InAftershock = false;
                aftershockDone = true;
                GameEvents.RaiseTremorChanged(false);
                GameEvents.RaiseMessage("La réplica terminó. Continúa la evacuación con calma.", MessageKind.Info);
                SetObjective(EvacuationObjective());
            }
        }

        /// <summary>Puntos por segundo mientras tiembla.</summary>
        void ScoreShaking(float dt, float factor)
        {
            if (PlayerProtected)
            {
                protectedTime += dt;
                scoreAccumulator += GameConfig.ProtectedPointsPerSecond * factor * dt;
                if (!firstProtection)
                {
                    firstProtection = true;
                    AddScore(GameConfig.FirstProtectionBonus, "¡Agáchate, cúbrete y sujétate! Bien hecho");
                    GameEvents.PlaySound(SoundId.Protected);
                }
                if (PlayerHolding)
                {
                    holdTime += dt;
                    scoreAccumulator += GameConfig.HoldingPointsPerSecond * factor * dt;
                }
                else Once("hold", "Mantén H para SUJETARTE de la pata de la mesa: suma más puntos.", MessageKind.Info);
            }
            else
            {
                unprotectedQuakeTime += dt;
                if (unprotectedQuakeTime > 3f && State == GameState.Sismo)
                    Once("seek", "Busca una mesa firme o una columna con señal ZONA SEGURA y agáchate (C).", MessageKind.Warning);
            }

            if (PlayerExposed)
            {
                exposedTime += dt;
                scoreAccumulator -= GameConfig.WindowPenaltyPerSecond * factor * dt;
                Once("window", "¡Aléjate de las ventanas! Los vidrios pueden estallar.", MessageKind.Bad);
            }

            if (PlayerInfo.IsRunning && PlayerInfo.IsMoving)
            {
                runningTime += dt;
                scoreAccumulator -= GameConfig.RunningPenaltyPerSecond * factor * dt;
                Once("run", "No corras mientras tiembla: puedes caer o ser golpeado.", MessageKind.Bad);
            }
        }

        void CheckLeftDuringQuake()
        {
            if (CurrentZone != null && CurrentZone.Type == ZoneType.PuntoReunion && !tasks.Contains("left_early"))
            {
                tasks.Add("left_early");
                AddScore(-GameConfig.LeftDuringQuakePenalty, "Salir corriendo durante el sismo es peligroso");
                AddTip("Durante el movimiento no intentes salir: la mayoría de heridos se lastiman al correr.");
            }
        }

        void FlushScore()
        {
            int whole = (int)scoreAccumulator;
            if (whole == 0) return;
            scoreAccumulator -= whole;
            AddScore(whole, null);
        }

        // ---------------------------------------------------------------------
        void SetState(GameState newState)
        {
            State = newState;
            phaseElapsed = 0f;

            switch (newState)
            {
                case GameState.Preparacion:
                    SetPhaseTime(GameConfig.PreparationSeconds);
                    SetObjective("Arma tu MOCHILA DE EMERGENCIA: busca suministros útiles en los dos pisos (E para recoger).");
                    break;

                case GameState.Alerta:
                    SetPhaseTime(GameConfig.AlertSeconds);
                    SetObjective("¡ALERTA SÍSMICA! Ve a una zona segura: debajo de una mesa firme o junto a una columna.");
                    GameEvents.RaiseMessage("¡ALERTA SÍSMICA! Busca una zona segura YA.", MessageKind.Warning);
                    break;

                case GameState.Sismo:
                    SetPhaseTime(GameConfig.QuakeSeconds);
                    SetObjective("AGÁCHATE (C), CÚBRETE bajo la mesa y SUJÉTATE (mantén H). No corras. Lejos de ventanas.");
                    GameEvents.RaiseTremorChanged(true);
                    break;

                case GameState.Evacuacion:
                    SetPhaseTime(GameConfig.EvacuationSeconds);
                    GameEvents.RaiseTremorChanged(false);
                    SetObjective(EvacuationObjective());
                    GameEvents.RaiseMessage("El sismo terminó. Evacúa con calma siguiendo las señales verdes.", MessageKind.Info);
                    break;

                case GameState.Resultado:
                    SetPhaseTime(0f);
                    SetObjective("");
                    break;
            }

            GameEvents.RaiseStateChanged(newState);
        }

        void SetPhaseTime(float seconds)
        {
            TimeLeft = seconds;
            PhaseDuration = seconds;
        }

        void SetObjective(string text) => Objective = text;

        string EvacuationObjective()
        {
            string tasksText = "";
            if (!tasks.Contains(GameConfig.TaskGas)) tasksText += "cierra el balón de GAS (cocina), ";
            if (!tasks.Contains(GameConfig.TaskPower)) tasksText += "baja la llave de LUZ (tablero del pasillo), ";
            return "Baja por la escalera, " + tasksText + "y sal al PUNTO DE REUNIÓN (zona verde frente a la casa).";
        }

        void Once(string key, string message, MessageKind kind)
        {
            if (onceMessages.Add(key)) GameEvents.RaiseMessage(message, kind);
        }

        // ---------------------------------------------------------------------
        // API para los demás módulos
        // ---------------------------------------------------------------------

        public void AddScore(int points, string reason)
        {
            if (!Playing || points == 0) return;
            Score += points;
            GameEvents.RaiseScoreChanged(Score, points, reason);
        }

        public void AddTip(string tip)
        {
            if (!Tips.Contains(tip)) Tips.Add(tip);
        }

        public bool IsTaskDone(string id) => tasks.Contains(id);

        /// <summary>Recoger un suministro. Solo durante la preparación.</summary>
        public bool TryPickUp(ItemData item)
        {
            if (State != GameState.Preparacion)
            {
                GameEvents.RaiseMessage("La preparación terminó: ahora lo importante es protegerte y evacuar.", MessageKind.Warning);
                GameEvents.PlaySound(SoundId.Denied);
                return false;
            }
            if (!Backpack.TryAdd(item, out string reason))
            {
                GameEvents.RaiseMessage(reason, MessageKind.Warning);
                GameEvents.PlaySound(SoundId.Denied);
                return false;
            }

            AddScore(item.Points, item.Useful ? item.Name : item.Name + " (no es prioritario)");
            GameEvents.RaiseMessage(item.Tip, item.Useful ? MessageKind.Good : MessageKind.Bad);
            GameEvents.PlaySound(item.Useful ? SoundId.Pickup : SoundId.PickupBad);
            GameEvents.RaiseInventoryChanged();
            return true;
        }

        /// <summary>Saca el último objeto de la mochila y lo devuelve a su lugar.</summary>
        public void DropLastItem()
        {
            if (State != GameState.Preparacion)
            {
                GameEvents.RaiseMessage("Solo puedes reorganizar la mochila durante la preparación.", MessageKind.Warning);
                return;
            }
            if (!Backpack.TryRemoveLast(out ItemData item))
            {
                GameEvents.RaiseMessage("Tu mochila está vacía.", MessageKind.Info);
                return;
            }
            AddScore(-item.Points, "Sacaste " + item.Name);
            GameEvents.RaiseItemReturned(item.Id);
            GameEvents.RaiseInventoryChanged();
            GameEvents.PlaySound(SoundId.Drop);
        }

        /// <summary>Cerrar el gas o cortar la luz. Solo después del sismo.</summary>
        public bool TryCompleteTask(string id, string doneMessage)
        {
            if (tasks.Contains(id)) return false;
            if (State == GameState.Preparacion)
            {
                GameEvents.RaiseMessage("Esto se hace DESPUÉS del sismo, si es seguro. Recuerda dónde está.", MessageKind.Info);
                return false;
            }
            if (State == GameState.Alerta || State == GameState.Sismo || InAftershock)
            {
                GameEvents.RaiseMessage("¡Ahora no! Primero protégete hasta que pase el movimiento.", MessageKind.Bad);
                GameEvents.PlaySound(SoundId.Denied);
                return false;
            }
            tasks.Add(id);
            AddScore(GameConfig.UtilityPoints, doneMessage);
            GameEvents.RaiseTaskCompleted(id);
            SetObjective(EvacuationObjective());
            return true;
        }

        /// <summary>Un objeto que cayó golpeó al jugador.</summary>
        public void PlayerHit(int damage, string source)
        {
            if (!Playing) return;
            if (PlayerProtected)
            {
                Once("table_saved", "¡La mesa te protegió de un objeto que cayó!", MessageKind.Good);
                return;
            }

            hits++;
            Health = Mathf.Max(0, Health - damage);
            GameEvents.RaisePlayerHit(damage);
            GameEvents.PlaySound(SoundId.Hurt);
            AddScore(-GameConfig.HitPenalty, "Te golpeó: " + source);

            if (Health <= 0)
            {
                AddTip("Resultaste gravemente herido. Protégete bajo una mesa firme y aléjate de muebles altos.");
                Finish();
            }
        }

        // ---------- Pausa y escenas ----------

        public void SetPaused(bool paused)
        {
            if (!Playing) paused = false;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }

        public void Restart() => SceneBoot.Load(GameConfig.HouseScene);
        public void GoToMenu() => SceneBoot.Load(GameConfig.MenuScene);

        // ---------------------------------------------------------------------
        // Resultado
        // ---------------------------------------------------------------------

        void Finish()
        {
            if (!Playing) return;

            if (Evacuated)
            {
                int timeBonus = Mathf.RoundToInt(TimeLeft / 3f);
                AddScore(GameConfig.EvacuationPoints + timeBonus, "Llegaste al punto de reunión");
            }

            InAftershock = false;
            QuakeInfo.Intensity = 0f;
            BuildResult();
            SetPaused(false);
            SetState(GameState.Resultado);
            GameEvents.PlaySound(Evacuated && Grade != "C" ? SoundId.Win : SoundId.Lose);
        }

        void BuildResult()
        {
            ResultRows.Clear();

            int usefulTotal = ItemCatalog.UsefulCount;
            ResultRows.Add(new ResultRow("Suministros útiles",
                $"{Backpack.UsefulCount} de {usefulTotal}", Backpack.UsefulCount >= 6));
            ResultRows.Add(new ResultRow("Objetos innecesarios",
                Backpack.UselessCount == 0 ? "Ninguno" : Backpack.UselessCount.ToString(), Backpack.UselessCount == 0));
            ResultRows.Add(new ResultRow("Tiempo protegido",
                $"{protectedTime:0} s", protectedTime >= GameConfig.QuakeSeconds * 0.6f));
            ResultRows.Add(new ResultRow("Tiempo sujetándote",
                $"{holdTime:0} s", holdTime >= 5f));
            ResultRows.Add(new ResultRow("Golpes recibidos", hits.ToString(), hits == 0));
            ResultRows.Add(new ResultRow("Salud final", $"{Health}%", Health >= 60));
            ResultRows.Add(new ResultRow("Llave de gas", IsTaskDone(GameConfig.TaskGas) ? "Cerrada" : "Abierta", IsTaskDone(GameConfig.TaskGas)));
            ResultRows.Add(new ResultRow("Llave de luz", IsTaskDone(GameConfig.TaskPower) ? "Cortada" : "Encendida", IsTaskDone(GameConfig.TaskPower)));
            ResultRows.Add(new ResultRow("Evacuación",
                Evacuated ? $"Lograda en {evacuationTimeUsed:0} s" : "No completada", Evacuated));

            // Consejos según los errores
            if (Backpack.UsefulCount < 6)
                AddTip("Prepara tu mochila ANTES de una emergencia: agua, botiquín, linterna, radio, documentos y alimentos.");
            if (Backpack.UselessCount > 0)
                AddTip("Los objetos de valor (laptop, consola) ocupan espacio que necesitas para sobrevivir 72 horas.");
            if (protectedTime < GameConfig.QuakeSeconds * 0.6f)
                AddTip("Durante el sismo: AGÁCHATE, CÚBRETE bajo una mesa firme y SUJÉTATE hasta que termine.");
            if (exposedTime > 1f)
                AddTip("Mantente lejos de ventanas, espejos y vitrinas: los vidrios pueden estallar.");
            if (runningTime > 1f)
                AddTip("No corras durante el movimiento: muchas lesiones ocurren por caídas y golpes al correr.");
            if (hits > 0)
                AddTip("Identifica muebles altos sin anclar (roperos, estantes) y aléjate de ellos.");
            if (!IsTaskDone(GameConfig.TaskGas))
                AddTip("Después del sismo cierra el gas: una fuga puede provocar incendios o explosiones.");
            if (!IsTaskDone(GameConfig.TaskPower))
                AddTip("Corta la energía desde el tablero para evitar cortocircuitos.");
            if (Evacuated && Tips.Count == 0)
                AddTip("¡Excelente! Comparte lo aprendido con tu familia y organicen un simulacro en casa.");

            if (!Evacuated) Grade = "C";
            else if (Score >= GameConfig.GradeA) Grade = "A";
            else if (Score >= GameConfig.GradeB) Grade = "B";
            else Grade = "C";

            if (Health <= 0) ResultTitle = "Resultaste herido";
            else if (!Evacuated) ResultTitle = "No completaste la evacuación";
            else if (Grade == "A") ResultTitle = "¡Excelente! Estás preparado";
            else if (Grade == "B") ResultTitle = "Bien, pero puedes mejorar";
            else ResultTitle = "Necesitas practicar más";
        }
    }
}
