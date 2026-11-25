using UnityEngine;

namespace DisasterReady.Level
{
    /// <summary>
    /// Keeps a generated level prop kinematic until an explicit earthquake call, then applies a bounded physical response.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelEarthquakeProp : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Level - DisasterReady";
        private const float MinimumMass = 0.05f;
        private const float MinimumLinearSpeed = 0.1f;
        private const float MinimumAngularSpeed = 0.1f;

        [SerializeField, Min(MinimumMass)] private float mass = 1.5f;
        [SerializeField, Min(0f)] private float activationImpulse = 0.75f;
        [SerializeField, Min(0f)] private float activationTorque = 0.25f;
        [SerializeField, Min(MinimumLinearSpeed)] private float maximumLinearSpeed = 3.5f;
        [SerializeField, Min(MinimumAngularSpeed)] private float maximumAngularSpeed = 3f;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Vector3 authoredLocalPosition;
        [SerializeField] private Quaternion authoredLocalRotation = Quaternion.identity;
        [SerializeField] private bool hasAuthoredPose;

        private bool earthquakeActive;

        /// <summary>
        /// Configures the bounded physical response and captures the prop's authored local pose.
        /// </summary>
        public void Configure(float configuredMass, float configuredImpulse, float configuredTorque,
            float configuredMaximumLinearSpeed, float configuredMaximumAngularSpeed)
        {
            mass = Mathf.Max(MinimumMass, configuredMass);
            activationImpulse = Mathf.Max(0f, configuredImpulse);
            activationTorque = Mathf.Max(0f, configuredTorque);
            maximumLinearSpeed = Mathf.Max(MinimumLinearSpeed, configuredMaximumLinearSpeed);
            maximumAngularSpeed = Mathf.Max(MinimumAngularSpeed, configuredMaximumAngularSpeed);
            CaptureAuthoredPose();
            EnsureKinematicBody();
        }

        private void Awake()
        {
            if (!hasAuthoredPose)
            {
                CaptureAuthoredPose();
            }

            EnsureKinematicBody();
        }

        /// <summary>
        /// Enables gravity and applies a moderate impulse and torque during Play Mode only.
        /// </summary>
        public void ActivateEarthquake()
        {
            if (!Application.isPlaying || !IsUnderGeneratedLevel() || earthquakeActive)
            {
                return;
            }

            EnsureKinematicBody();
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            earthquakeActive = true;

            Vector3 impulseDirection = new Vector3(
                Random.Range(-0.45f, 0.45f),
                Random.Range(0.25f, 0.65f),
                Random.Range(-0.45f, 0.45f)).normalized;
            body.AddForce(impulseDirection * activationImpulse, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * activationTorque, ForceMode.Impulse);
        }

        /// <summary>
        /// Stops motion and restores the prop to its authored local position and rotation.
        /// </summary>
        public void ResetToAuthoredPose()
        {
            if (!Application.isPlaying || !IsUnderGeneratedLevel())
            {
                return;
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            EnsureKinematicBody();
            earthquakeActive = false;
            body.isKinematic = true;
            transform.localPosition = authoredLocalPosition;
            transform.localRotation = authoredLocalRotation;
            body.position = transform.position;
            body.rotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            if (!earthquakeActive || body == null || body.isKinematic)
            {
                return;
            }

            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, maximumLinearSpeed);
            body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, maximumAngularSpeed);
        }

        private void CaptureAuthoredPose()
        {
            authoredLocalPosition = transform.localPosition;
            authoredLocalRotation = transform.localRotation;
            hasAuthoredPose = true;
        }

        private void EnsureKinematicBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.mass = mass;
            body.useGravity = true;
            body.isKinematic = true;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private bool IsUnderGeneratedLevel()
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name == GeneratedRootName)
                {
                    Transform house = current.parent;
                    if (house == null || house.name != "House")
                    {
                        return false;
                    }

                    string sceneName = gameObject.scene.name;
                    return gameObject.scene.IsValid() && (sceneName == "Level" || sceneName == "House");
                }

                current = current.parent;
            }

            return false;
        }
    }
}
