using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady.Level
{
    /// <summary>
    /// Breaks an authored primitive pane into a small number of capped physical fragments on explicit activation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelBreakableWindow : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Level - DisasterReady";
        private const int MaximumFragmentCount = 9;
        private const float MaximumFragmentSpeed = 3f;
        private const float MaximumFragmentAngularSpeed = 4f;

        [SerializeField, Range(1, MaximumFragmentCount)] private int fragmentCount = MaximumFragmentCount;
        [SerializeField] private Renderer paneRenderer;
        [SerializeField] private Collider paneCollider;
        [SerializeField] private Transform fragmentParent;
        [SerializeField] private Material glassMaterial;

        private readonly List<GameObject> fragments = new List<GameObject>();
        private readonly List<Rigidbody> fragmentBodies = new List<Rigidbody>();
        private bool isBroken;

        /// <summary>
        /// Stores the generated structure as the fragment parent and captures the pane's existing glass material.
        /// </summary>
        public void Configure(Transform generatedStructure)
        {
            fragmentParent = generatedStructure;
            paneRenderer = GetComponent<Renderer>();
            paneCollider = GetComponent<Collider>();
            glassMaterial = paneRenderer != null ? paneRenderer.sharedMaterial : null;
            fragmentCount = Mathf.Clamp(fragmentCount, 1, MaximumFragmentCount);
            isBroken = false;
            if (paneRenderer != null)
            {
                paneRenderer.enabled = true;
            }

            if (paneCollider != null)
            {
                paneCollider.enabled = true;
            }
        }

        private void Awake()
        {
            if (paneRenderer == null)
            {
                paneRenderer = GetComponent<Renderer>();
            }

            if (paneCollider == null)
            {
                paneCollider = GetComponent<Collider>();
            }

            if (glassMaterial == null && paneRenderer != null)
            {
                glassMaterial = paneRenderer.sharedMaterial;
            }

            if (fragmentParent == null)
            {
                fragmentParent = transform.parent;
            }

            if (paneRenderer != null)
            {
                paneRenderer.enabled = true;
            }

            if (paneCollider != null)
            {
                paneCollider.enabled = true;
            }

            fragmentCount = Mathf.Clamp(fragmentCount, 1, MaximumFragmentCount);
        }

        /// <summary>
        /// Hides the intact pane and emits a capped set of primitive fragments during Play Mode only.
        /// </summary>
        public void Break()
        {
            if (!Application.isPlaying || !IsUnderGeneratedLevel() || isBroken)
            {
                return;
            }

            if (paneRenderer == null || paneCollider == null || glassMaterial == null)
            {
                return;
            }

            isBroken = true;
            paneRenderer.enabled = false;
            paneCollider.enabled = false;
            EmitFragments();
        }

        /// <summary>
        /// Restores the intact pane and removes all fragments created by this component.
        /// </summary>
        public void ResetWindow()
        {
            if (!Application.isPlaying || !IsUnderGeneratedLevel())
            {
                return;
            }

            isBroken = false;
            if (paneRenderer != null)
            {
                paneRenderer.enabled = true;
            }

            if (paneCollider != null)
            {
                paneCollider.enabled = true;
            }

            ClearFragments();
        }

        private void FixedUpdate()
        {
            for (int bodyIndex = fragmentBodies.Count - 1; bodyIndex >= 0; bodyIndex--)
            {
                Rigidbody fragmentBody = fragmentBodies[bodyIndex];
                if (fragmentBody == null)
                {
                    fragmentBodies.RemoveAt(bodyIndex);
                    continue;
                }

                fragmentBody.linearVelocity = Vector3.ClampMagnitude(fragmentBody.linearVelocity, MaximumFragmentSpeed);
                fragmentBody.angularVelocity = Vector3.ClampMagnitude(fragmentBody.angularVelocity, MaximumFragmentAngularSpeed);
            }
        }

        private void EmitFragments()
        {
            Transform parent = fragmentParent != null ? fragmentParent : transform.parent;
            if (parent == null)
            {
                return;
            }

            Vector3 paneScale = transform.localScale;
            bool widthAlongLocalX = paneScale.x >= paneScale.z;
            float localWidth = widthAlongLocalX ? paneScale.x : paneScale.z;
            float localHeight = paneScale.y;
            float pieceWidth = localWidth / 3f * 0.9f;
            float pieceHeight = localHeight / 3f * 0.9f;
            float paneThickness = Mathf.Max(0.025f, Mathf.Min(paneScale.x, paneScale.z));
            Vector3 paneNormal = widthAlongLocalX ? transform.forward : transform.right;

            for (int fragmentIndex = 0; fragmentIndex < fragmentCount; fragmentIndex++)
            {
                int column = fragmentIndex % 3;
                int row = fragmentIndex / 3;
                float widthOffset = (column - 1f) * localWidth / 3f;
                float heightOffset = (row - 1f) * localHeight / 3f;
                Vector3 localOffset = widthAlongLocalX
                    ? new Vector3(widthOffset, heightOffset, 0f)
                    : new Vector3(0f, heightOffset, widthOffset);

                GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragment.name = name + " - Glass Fragment " + (fragmentIndex + 1);
                fragment.transform.SetParent(parent, true);
                fragment.transform.SetPositionAndRotation(transform.TransformPoint(localOffset) + paneNormal * 0.025f,
                    transform.rotation);
                fragment.transform.localScale = widthAlongLocalX
                    ? new Vector3(pieceWidth, pieceHeight, paneThickness)
                    : new Vector3(paneThickness, pieceHeight, pieceWidth);

                Renderer renderer = fragment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = glassMaterial;
                }

                Rigidbody fragmentBody = fragment.AddComponent<Rigidbody>();
                fragmentBody.mass = 0.04f;
                fragmentBody.useGravity = true;
                fragmentBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                fragmentBody.interpolation = RigidbodyInterpolation.Interpolate;
                Vector3 direction = (paneNormal * Random.Range(0.35f, 0.7f) + Random.insideUnitSphere * 0.35f).normalized;
                fragmentBody.AddForce(direction * 0.06f, ForceMode.Impulse);
                fragmentBody.AddTorque(Random.insideUnitSphere * 0.012f, ForceMode.Impulse);
                fragments.Add(fragment);
                fragmentBodies.Add(fragmentBody);
            }
        }

        private void ClearFragments()
        {
            for (int fragmentIndex = fragments.Count - 1; fragmentIndex >= 0; fragmentIndex--)
            {
                GameObject fragment = fragments[fragmentIndex];
                if (fragment == null)
                {
                    continue;
                }

                fragment.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(fragment);
                }
                else
                {
                    DestroyImmediate(fragment);
                }
            }

            fragments.Clear();
            fragmentBodies.Clear();
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
