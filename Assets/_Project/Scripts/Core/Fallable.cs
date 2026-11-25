using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Objeto que puede caer durante el sismo (libros, lámparas, estantes, roperos, vidrios).
    /// Permanece quieto (cinemático) hasta que el temblor lo suelta.
    /// Si golpea al jugador a velocidad, le hace daño salvo que esté protegido.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Fallable : MonoBehaviour
    {
        static readonly List<Fallable> all = new List<Fallable>();
        public static IReadOnlyList<Fallable> All => all;

        public int Damage = 10;
        public bool Heavy;                          // muebles grandes: más daño y sonido fuerte
        public Vector3 TipDirection = Vector3.zero; // hacia dónde se vuelca (cero = aleatorio)
        public float ReleaseDelay = -1f;            // segundos tras iniciar el sismo (-1 = automático)

        public bool IsReleased { get; private set; }
        public Rigidbody Body { get; private set; }

        Collider[] colliders;
        bool hasHit;
        float soundCooldown;

        const float HitSpeed = 2.2f;
        const float HitDistance = 0.32f;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (!IsReleased) Body.isKinematic = true;
            colliders = GetComponentsInChildren<Collider>();
        }

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);

        /// <summary>Punto más alto del objeto (para volcarlo desde arriba).</summary>
        public Vector3 TopPoint
        {
            get
            {
                if (colliders == null || colliders.Length == 0) return transform.position;
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++) b.Encapsulate(colliders[i].bounds);
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }
        }

        public void Release(Vector3 impulse, Vector3? atPoint = null)
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            IsReleased = true;
            Body.isKinematic = false;
            Body.WakeUp();
            if (impulse == Vector3.zero) return;
            if (atPoint.HasValue) Body.AddForceAtPosition(impulse, atPoint.Value, ForceMode.Impulse);
            else Body.AddForce(impulse, ForceMode.Impulse);
        }

        void Update()
        {
            soundCooldown -= Time.deltaTime;
            if (!IsReleased || hasHit || !PlayerInfo.HasPlayer) return;
            if (Body.linearVelocity.sqrMagnitude < HitSpeed * HitSpeed) return;

            if (IsNear(PlayerInfo.HeadPosition) || IsNear(PlayerInfo.Position))
            {
                hasHit = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerHit(Damage, gameObject.name);
            }
        }

        bool IsNear(Vector3 point)
        {
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled) continue;
                Vector3 closest = col.ClosestPoint(point);
                if ((closest - point).sqrMagnitude <= HitDistance * HitDistance) return true;
            }
            return false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsReleased || soundCooldown > 0f) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed < 1.5f) return;
            soundCooldown = 0.25f;
            float volume = Mathf.Clamp01(speed / 6f);
            GameEvents.PlaySoundAt(Heavy ? SoundId.ImpactHeavy : SoundId.ImpactLight, transform.position, volume);
            if (Heavy && speed > 2.5f) GameEvents.RaiseImpact(transform.position, true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();
    }
}
