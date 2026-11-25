using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Audio del juego en tres momentos (GDD): calma antes del sismo, alarma y vibración durante,
    /// tensión y calma en la evacuación. Carga los clips desde Assets/_Project/Audio/Resources/Audio.
    /// Para cambiar un sonido basta con reemplazar el .wav manteniendo el nombre.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        AudioSource musicA, musicB, ambience, alarm, rumble, heartbeat;
        AudioSource activeMusic;
        AudioLowPassFilter musicFilterA, musicFilterB;
        readonly List<AudioSource> pool = new List<AudioSource>();
        int poolIndex;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, int> lastVariant = new Dictionary<string, int>();

        float musicVolume = 1f;   // multiplicador por fase
        float alarmTarget;
        bool menuScene;
        string currentMusic = "";
        string currentAmbience = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister()
        {
            SceneBoot.Register(GameConfig.HouseScene, () => Spawn(false));
            SceneBoot.Register(GameConfig.MenuScene, () => Spawn(true));
        }

        static void Spawn(bool menu)
        {
            if (FindAnyObjectByType<AudioManager>() != null) return;
            var am = new GameObject("Audio").AddComponent<AudioManager>();
            am.menuScene = menu;
        }

        // ---------------------------------------------------------------------
        void Awake()
        {
            musicA = Source("Música A", true, true);
            musicB = Source("Música B", true, true);
            musicFilterA = musicA.gameObject.AddComponent<AudioLowPassFilter>();
            musicFilterB = musicB.gameObject.AddComponent<AudioLowPassFilter>();
            musicFilterA.cutoffFrequency = 22000f;
            musicFilterB.cutoffFrequency = 22000f;
            ambience = Source("Ambiente", true, false);
            alarm = Source("Alarma", true, false);
            rumble = Source("Retumbo", true, false);
            heartbeat = Source("Latido", true, false);
            for (int i = 0; i < 20; i++) pool.Add(Source("Efecto " + i, false, false));
            activeMusic = musicA;

            GameEvents.Sound += OnSound;
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.FadeOutRequested += OnFadeOut;
            Settings.Changed += ApplyMaster;
            ApplyMaster();
        }

        void OnDestroy()
        {
            GameEvents.Sound -= OnSound;
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.FadeOutRequested -= OnFadeOut;
            Settings.Changed -= ApplyMaster;
        }

        void Start()
        {
            rumble.clip = Clip("quake_rumble");
            alarm.clip = Clip("alarm_siren");
            heartbeat.clip = Clip("heartbeat");
            rumble.volume = 0f;
            alarm.volume = 0f;
            heartbeat.volume = 0f;
            if (rumble.clip != null) rumble.Play();
            if (heartbeat.clip != null) heartbeat.Play();

            if (menuScene)
            {
                PlayMusic("music_menu", 1f);
                PlayAmbience("amb_house");
            }
        }

        AudioSource Source(string name, bool loop, bool ignorePause)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = ignorePause;
            s.dopplerLevel = 0f;
            return s;
        }

        void ApplyMaster() => AudioListener.volume = Settings.Master;

        // ---------------------------------------------------------------------
        AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning($"[Audio] Falta el clip Audio/{name}.wav en Assets/_Project/Audio/Resources");
            clips[name] = c;
            return c;
        }

        /// <summary>Elige una variante al azar (nombre_1, nombre_2...) sin repetir la anterior.</summary>
        AudioClip Variant(string baseName, int count)
        {
            if (count <= 1) return Clip(baseName);
            lastVariant.TryGetValue(baseName, out int last);
            int pick = Random.Range(1, count + 1);
            if (pick == last) pick = pick % count + 1;
            lastVariant[baseName] = pick;
            return Clip(baseName + "_" + pick);
        }

        AudioClip ClipFor(SoundId id)
        {
            switch (id)
            {
                case SoundId.Click: return Clip("ui_click");
                case SoundId.Hover: return Clip("ui_hover");
                case SoundId.Pickup: return Clip("pickup");
                case SoundId.PickupBad: return Clip("pickup_bad");
                case SoundId.Drop: return Clip("drop");
                case SoundId.Denied: return Clip("denied");
                case SoundId.StepWood: return Variant("step_wood", 4);
                case SoundId.StepGrass: return Variant("step_grass", 3);
                case SoundId.Switch: return Clip("switch");
                case SoundId.Valve: return Clip("valve");
                case SoundId.Glass: return Variant("glass", 2);
                case SoundId.ImpactLight: return Variant("impact_light", 3);
                case SoundId.ImpactHeavy: return Variant("impact_heavy", 2);
                case SoundId.Creak: return Variant("creak", 2);
                case SoundId.Hurt: return Variant("hurt", 2);
                case SoundId.Win: return Clip("win");
                case SoundId.Lose: return Clip("lose");
                case SoundId.Whoosh: return Clip("whoosh");
                case SoundId.Protected: return Clip("protected");
                default: return null;
            }
        }

        void OnSound(SoundId id, Vector3? position, float volume)
        {
            var clip = ClipFor(id);
            if (clip == null) return;

            var s = pool[poolIndex];
            poolIndex = (poolIndex + 1) % pool.Count;
            bool ui = id == SoundId.Click || id == SoundId.Hover;
            s.ignoreListenerPause = ui;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * Settings.Sfx;
            bool varied = id == SoundId.StepWood || id == SoundId.StepGrass || id == SoundId.ImpactLight || id == SoundId.ImpactHeavy || id == SoundId.Glass;
            s.pitch = varied ? Random.Range(0.9f, 1.1f) : 1f;

            if (position.HasValue && PlayerInfo.HasPlayer)
            {
                s.transform.position = position.Value;
                s.spatialBlend = 0.85f;
                s.minDistance = 1.5f;
                s.maxDistance = 30f;
                s.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                s.transform.localPosition = Vector3.zero;
                s.spatialBlend = 0f;
            }
            s.Play();
        }

        // ---------------------------------------------------------------------
        void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Preparacion:
                    PlayMusic("music_prep", 0.8f);
                    PlayAmbience("amb_house");
                    break;
                case GameState.Alerta:
                    PlayMusic("", 0f);
                    alarmTarget = 1f;
                    if (alarm.clip != null && !alarm.isPlaying) alarm.Play();
                    break;
                case GameState.Sismo:
                    alarmTarget = 0.35f; // la alarma sigue de fondo unos segundos
                    Invoke(nameof(StopAlarm), 6f);
                    PlayAmbience("");
                    break;
                case GameState.Evacuacion:
                    PlayMusic("music_evac", 0.75f);
                    PlayAmbience("amb_after");
                    break;
                case GameState.Resultado:
                    alarmTarget = 0f;
                    PlayMusic("music_menu", 0.55f);
                    break;
            }
        }

        void StopAlarm() => alarmTarget = 0f;

        void OnFadeOut()
        {
            PlayMusic("", 0f);
            PlayAmbience("");
        }

        void PlayMusic(string name, float volume)
        {
            musicVolume = volume;
            if (name == currentMusic) return;
            currentMusic = name;
            var next = activeMusic == musicA ? musicB : musicA;
            next.clip = string.IsNullOrEmpty(name) ? null : Clip(name);
            next.volume = 0f;
            if (next.clip != null) next.Play();
            activeMusic = next;
        }

        void PlayAmbience(string name)
        {
            if (name == currentAmbience) return;
            currentAmbience = name;
            var clip = string.IsNullOrEmpty(name) ? null : Clip(name);
            if (clip == null)
            {
                ambience.Stop();
                return;
            }
            ambience.clip = clip;
            ambience.volume = 0f;
            ambience.Play();
        }

        // ---------------------------------------------------------------------
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var gm = GameManager.Instance;
            bool paused = gm != null && gm.Paused;

            // Fundido cruzado de la música
            float musicTarget = Settings.Music * musicVolume * 0.7f;
            var inactive = activeMusic == musicA ? musicB : musicA;
            activeMusic.volume = Mathf.MoveTowards(activeMusic.volume, activeMusic.clip != null ? musicTarget : 0f, dt * 0.6f);
            inactive.volume = Mathf.MoveTowards(inactive.volume, 0f, dt * 0.8f);
            if (inactive.volume <= 0.001f && inactive.isPlaying) inactive.Stop();

            // Música apagada (filtro) en pausa
            float cutoff = paused ? 900f : 22000f;
            musicFilterA.cutoffFrequency = Mathf.Lerp(musicFilterA.cutoffFrequency, cutoff, dt * 8f);
            musicFilterB.cutoffFrequency = musicFilterA.cutoffFrequency;

            float ambienceTarget = string.IsNullOrEmpty(currentAmbience) ? 0f : Settings.Sfx * 0.5f;
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceTarget, dt * 0.5f);

            alarm.volume = Mathf.MoveTowards(alarm.volume, alarmTarget * Settings.Sfx * 0.8f, dt * 1.5f);
            if (alarm.volume <= 0.001f && alarm.isPlaying && alarmTarget <= 0f) alarm.Stop();

            // Retumbo según la intensidad del sismo
            float q = QuakeInfo.Intensity;
            rumble.volume = Mathf.Lerp(rumble.volume, Mathf.Clamp01(q * 1.2f) * Settings.Sfx, dt * 6f);
            rumble.pitch = 0.85f + q * 0.25f;

            // Latido con poca salud
            float beat = gm != null && gm.Playing && gm.Health < 35 ? (1f - gm.Health / 35f) * 0.8f + 0.2f : 0f;
            heartbeat.volume = Mathf.MoveTowards(heartbeat.volume, beat * Settings.Sfx, dt);
        }
    }
}
