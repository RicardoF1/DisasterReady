using UnityEngine;

namespace DisasterReady.Level
{
    /// <summary>
    /// Explicitly starts and ends bounded earthquake effects for components beneath the generated Level house root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelEarthquakeEffects : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Level - DisasterReady";
        [SerializeField] private bool includeInactiveDescendants = true;

        private bool earthquakeActive;

        /// <summary>
        /// Configures this coordinator to scan only its generated-root descendants, including inactive objects.
        /// </summary>
        public void Configure()
        {
            includeInactiveDescendants = true;
            earthquakeActive = false;
        }

        /// <summary>
        /// Activates all generated earthquake props and breakable panes beneath this coordinator.
        /// </summary>
        /// <remarks>Core phase wiring is intentionally absent; a future Core adapter must call this explicitly.</remarks>
        public void BeginEarthquake()
        {
            if (!Application.isPlaying || !IsGeneratedLevelRoot() || earthquakeActive)
            {
                return;
            }

            earthquakeActive = true;
            LevelEarthquakeProp[] props = GetComponentsInChildren<LevelEarthquakeProp>(includeInactiveDescendants);
            for (int propIndex = 0; propIndex < props.Length; propIndex++)
            {
                LevelEarthquakeProp prop = props[propIndex];
                if (prop != null && IsDescendantOfGeneratedRoot(prop.transform))
                {
                    prop.ActivateEarthquake();
                }
            }

            LevelBreakableWindow[] windows = GetComponentsInChildren<LevelBreakableWindow>(includeInactiveDescendants);
            for (int windowIndex = 0; windowIndex < windows.Length; windowIndex++)
            {
                LevelBreakableWindow window = windows[windowIndex];
                if (window != null && IsDescendantOfGeneratedRoot(window.transform))
                {
                    window.Break();
                }
            }
        }

        /// <summary>
        /// Resets every generated prop and pane beneath this coordinator and removes its emitted fragments.
        /// </summary>
        /// <remarks>Core phase wiring is intentionally absent; a future Core adapter must call this explicitly.</remarks>
        public void EndEarthquake()
        {
            if (!Application.isPlaying || !IsGeneratedLevelRoot())
            {
                return;
            }

            earthquakeActive = false;
            LevelEarthquakeProp[] props = GetComponentsInChildren<LevelEarthquakeProp>(includeInactiveDescendants);
            for (int propIndex = 0; propIndex < props.Length; propIndex++)
            {
                LevelEarthquakeProp prop = props[propIndex];
                if (prop != null && IsDescendantOfGeneratedRoot(prop.transform))
                {
                    prop.ResetToAuthoredPose();
                }
            }

            LevelBreakableWindow[] windows = GetComponentsInChildren<LevelBreakableWindow>(includeInactiveDescendants);
            for (int windowIndex = 0; windowIndex < windows.Length; windowIndex++)
            {
                LevelBreakableWindow window = windows[windowIndex];
                if (window != null && IsDescendantOfGeneratedRoot(window.transform))
                {
                    window.ResetWindow();
                }
            }
        }

        [ContextMenu("TEST - Begin Earthquake (Play Mode Only)")]
        private void ContextTestBeginEarthquake()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Earthquake test is available only in Play Mode.", this);
                return;
            }

            BeginEarthquake();
        }

        [ContextMenu("TEST - End Earthquake (Play Mode Only)")]
        private void ContextTestEndEarthquake()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Earthquake reset test is available only in Play Mode.", this);
                return;
            }

            EndEarthquake();
        }

        private bool IsGeneratedLevelRoot()
        {
            if (transform.name != GeneratedRootName || transform.parent == null || transform.parent.name != "House")
            {
                return false;
            }

            string sceneName = gameObject.scene.name;
            return gameObject.scene.IsValid() && (sceneName == "Level" || sceneName == "House");
        }

        private bool IsDescendantOfGeneratedRoot(Transform candidate)
        {
            return candidate == transform || candidate.IsChildOf(transform);
        }
    }
}
