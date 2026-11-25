using UnityEngine;

namespace DisasterReady
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallingHazard : MonoBehaviour
    {
        Rigidbody body;
        Collider shape;
        float cooldown;
        void Awake()
        {
            body = GetComponent<Rigidbody>(); body.isKinematic = true; body.mass = 3;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            shape = GetComponent<Collider>(); GameEvents.OnTremorChanged += Tremor;
        }
        void Tremor(bool value)
        {
            if (!value) return;
            body.isKinematic = false;
            body.AddForce(new Vector3(1.2f, .2f, -.4f), ForceMode.Impulse);
            body.AddTorque(new Vector3(0, 0, -4), ForceMode.Impulse);
        }
        void Update()
        {
            var game = GameManager.Instance;
            if (game == null || game.Paused || !game.Playing || game.Player == null) return;
            cooldown -= Time.deltaTime;
            if (body.isKinematic || body.linearVelocity.sqrMagnitude < .5f || cooldown > 0) return;
            Vector3 center = game.Player.Position + Vector3.up * (game.Player.Crouched ? .45f : .9f);
            if (Vector3.Distance(shape.ClosestPoint(center), center) > .45f) return;
            cooldown = 1;
            if (!game.Protected) game.Damage(12, "Evita muebles altos y objetos que pueden caer.");
        }
        void OnCollisionEnter(Collision collision)
        { if (collision.relativeVelocity.sqrMagnitude > 2) GameEvents.Sound("impact"); }
        void OnDestroy() => GameEvents.OnTremorChanged -= Tremor;
    }
}
