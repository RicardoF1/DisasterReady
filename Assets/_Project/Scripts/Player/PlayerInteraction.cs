using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    /// <summary>
    /// Elige el objeto interactuable más cercano al centro de la vista (cono de 22°)
    /// con línea de visión libre, lo resalta y lo usa con E. Q saca el último objeto de la mochila.
    ///
    /// No depende de que un rayo toque exactamente un objeto pequeño: por eso siempre
    /// se puede recoger lo que se ve.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        public float Range = 2.6f;
        public float ConeAngle = 22f;

        Interactable focused;

        void Update()
        {
            var cam = PlayerInfo.Camera;
            var gm = GameManager.Instance;
            bool active = cam != null && (gm == null || (gm.Playing && !gm.Paused));

            Interactable best = active ? FindTarget(cam.transform) : null;
            SetFocus(best);

            if (!active) return;

            // Brillo pulsante en el objeto enfocado
            if (focused != null)
                focused.SetHighlight(focused.CanInteract ? 0.55f + 0.35f * Mathf.Sin(Time.time * 6f) : 0.15f);

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (focused != null) focused.Interact();
                else GameEvents.PlaySound(SoundId.Denied, 0.4f);
            }

            if (keyboard.qKey.wasPressedThisFrame && gm != null)
                gm.DropLastItem();
        }

        Interactable FindTarget(Transform view)
        {
            Vector3 origin = view.position;
            Vector3 forward = view.forward;
            Interactable best = null;
            float bestScore = float.MaxValue;

            var list = Interactable.All;
            for (int i = 0; i < list.Count; i++)
            {
                var candidate = list[i];
                if (candidate == null || !candidate.isActiveAndEnabled) continue;

                Vector3 toTarget = candidate.FocusPoint - origin;
                float distance = toTarget.magnitude;
                if (distance > Range || distance < 0.01f) continue;

                float angle = Vector3.Angle(forward, toTarget);
                // Objetos muy cerca: se permite un ángulo mayor
                float allowed = Mathf.Lerp(ConeAngle * 1.8f, ConeAngle, Mathf.InverseLerp(0.4f, Range, distance));
                if (angle > allowed) continue;

                if (!HasLineOfSight(origin, candidate, distance)) continue;

                float score = angle + distance * 4f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        static bool HasLineOfSight(Vector3 origin, Interactable target, float distance)
        {
            Vector3 direction = (target.FocusPoint - origin) / distance;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
                return true;
            if (hit.collider.GetComponentInParent<Interactable>() == target) return true;
            // Lo que bloquea está prácticamente pegado al objeto (por ejemplo, la repisa donde está apoyado)
            return hit.distance >= distance - 0.25f;
        }

        void SetFocus(Interactable target)
        {
            if (target == focused) return;
            if (focused != null) focused.SetHighlight(0f);
            focused = target;
            PlayerInfo.Focused = target;
            if (target != null) GameEvents.PlaySound(SoundId.Hover, 0.25f);
        }

        void OnDisable() => SetFocus(null);
    }
}
