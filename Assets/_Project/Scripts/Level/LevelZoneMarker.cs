using UnityEngine;

namespace DisasterReady.Level
{
    public enum LevelZoneKind
    {
        SafeRefuge,
        Danger
    }

    /// <summary>
    /// Identifies a level-design zone for visual QA without depending on Core contracts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelZoneMarker : MonoBehaviour
    {
        [SerializeField] private LevelZoneKind zoneKind;
        [SerializeField, TextArea] private string designRationale;

        public LevelZoneKind ZoneKind => zoneKind;
        public string DesignRationale => designRationale;

        /// <summary>
        /// Assigns the zone category and the design rationale shown to QA.
        /// </summary>
        public void Configure(LevelZoneKind kind, string rationale)
        {
            zoneKind = kind;
            designRationale = rationale;
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider zoneCollider = GetComponent<BoxCollider>();
            if (zoneCollider == null)
            {
                return;
            }

            Gizmos.color = zoneKind == LevelZoneKind.SafeRefuge
                ? new Color(0.18f, 0.9f, 0.3f, 1f)
                : new Color(1f, 0.25f, 0.08f, 1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(zoneCollider.center, zoneCollider.size);
        }
    }
}
