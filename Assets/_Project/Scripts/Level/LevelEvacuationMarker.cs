using UnityEngine;

namespace DisasterReady.Level
{
    /// <summary>
    /// Stores local assembly-point identity and guidance data without invoking gameplay or evacuation callbacks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelEvacuationMarker : MonoBehaviour
    {
        [SerializeField] private string markerId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string guidance;

        /// <summary>
        /// Gets the marker's local identifier.
        /// </summary>
        public string MarkerId => markerId;

        /// <summary>
        /// Gets the marker's display name.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Gets the descriptive guidance stored with this marker.
        /// </summary>
        public string Guidance => guidance;

        /// <summary>
        /// Assigns the marker's local data without registering external callbacks or gameplay behavior.
        /// </summary>
        public void Configure(string configuredMarkerId, string configuredDisplayName, string configuredGuidance)
        {
            markerId = configuredMarkerId ?? string.Empty;
            displayName = configuredDisplayName ?? string.Empty;
            guidance = configuredGuidance ?? string.Empty;
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider markerCollider = GetComponent<BoxCollider>();
            if (markerCollider == null || !markerCollider.isTrigger)
            {
                return;
            }

            Gizmos.color = new Color(0.18f, 0.9f, 0.3f, 1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(markerCollider.center, markerCollider.size);
        }
    }
}
