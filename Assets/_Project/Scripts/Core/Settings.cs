using System;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>Opciones del jugador guardadas entre sesiones (PlayerPrefs).</summary>
    public static class Settings
    {
        const string KeyMaster = "dr_master", KeyMusic = "dr_music", KeySfx = "dr_sfx", KeySens = "dr_sens";

        public static event Action Changed;

        public static float Master
        {
            get => PlayerPrefs.GetFloat(KeyMaster, 0.9f);
            set { PlayerPrefs.SetFloat(KeyMaster, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float Music
        {
            get => PlayerPrefs.GetFloat(KeyMusic, 0.6f);
            set { PlayerPrefs.SetFloat(KeyMusic, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(KeySfx, 0.9f);
            set { PlayerPrefs.SetFloat(KeySfx, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        /// <summary>Sensibilidad del mouse (0.02 a 0.3).</summary>
        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(KeySens, 0.1f);
            set { PlayerPrefs.SetFloat(KeySens, Mathf.Clamp(value, 0.02f, 0.3f)); Changed?.Invoke(); }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Changed = null;
    }
}
