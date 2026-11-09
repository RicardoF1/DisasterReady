using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DisasterReady
{
    public static class SceneBootstrap
    {
        // Los módulos se registran antes de cargar; Core no conoce sus clases.
        static readonly SortedDictionary<int, Action<Transform, bool>>
            installers = new();

        // GetRawData() devuelve ulong.
        static readonly HashSet<ulong> installedScenes = new();

        public static void Register(int order, Action<Transform, bool> install)
            => installers[order] = install;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            installers.Clear();
            installedScenes.Clear();

            Time.timeScale = 1;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitialScene()
            => Build(SceneManager.GetActiveScene());

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            => Build(scene);

        static void Build(Scene scene)
        {
            if (scene.name != "MainMenu" && scene.name != "House")
                return;

            if (!installedScenes.Add(scene.handle.GetRawData()))
                return;

            // Limpieza defensiva para sesiones sin recarga de dominio/escena.
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == "__DisasterReady")
                {
                    obj.SetActive(false);
                    UnityEngine.Object.Destroy(obj);
                }
            }

            Time.timeScale = 1;
            AudioListener.pause = false;

            var root = new GameObject("__DisasterReady");
            SceneManager.MoveGameObjectToScene(root, scene);

            bool house = scene.name == "House";
            root.AddComponent<GameManager>().Initialize(house);

            foreach (var install in installers.Values)
                install(root.transform, house);
        }

        public static void Load(string name)
        {
            Time.timeScale = 1;
            AudioListener.pause = false;

            // Los handles se pueden reutilizar después de descargar escenas.
            installedScenes.Clear();
            SceneManager.LoadScene(name);
        }
    }
}