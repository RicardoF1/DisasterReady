using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DisasterReady
{
    /// <summary>
    /// Instala los sistemas de cada escena desde código: nadie tiene que arrastrar objetos
    /// en el editor y los archivos .unity no generan conflictos de Git.
    ///
    /// Uso dentro de cualquier clase:
    ///   [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    ///   static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);
    /// </summary>
    public static class SceneBoot
    {
        static readonly List<KeyValuePair<string, Action>> handlers = new List<KeyValuePair<string, Action>>();
        static bool loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            handlers.Clear();
            loading = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        /// <summary>Registra una acción para una escena. Si ya está abierta, se ejecuta ahora.</summary>
        public static void Register(string sceneName, Action spawn)
        {
            handlers.Add(new KeyValuePair<string, Action>(sceneName, spawn));
            if (SceneManager.GetActiveScene().name == sceneName) Run(spawn);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            loading = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            foreach (var handler in handlers)
                if (handler.Key == scene.name) Run(handler.Value);
        }

        static void Run(Action spawn)
        {
            try { spawn(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Cambia de escena con un fundido a negro.</summary>
        public static void Load(string sceneName)
        {
            if (loading) return;
            loading = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameEvents.RaiseFadeOut();
            var runner = new GameObject("SceneLoader").AddComponent<SceneLoaderRunner>();
            runner.StartCoroutine(runner.LoadAfterFade(sceneName));
        }
    }

    /// <summary>Objeto temporal que espera el fundido y carga la escena.</summary>
    internal sealed class SceneLoaderRunner : MonoBehaviour
    {
        public IEnumerator LoadAfterFade(string sceneName)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            SceneManager.LoadScene(sceneName);
        }
    }
}
