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
