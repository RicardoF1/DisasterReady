using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Base de todo lo que se usa con la tecla E (suministros, llave del gas, tablero eléctrico).
    /// Se registra en una lista global: el jugador elige el más cercano a su mira,
    /// así no hace falta apuntar con precisión de píxel a objetos pequeños.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> all = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => all;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        float highlight = -1f;

        /// <summary>Texto que aparece al mirar el objeto. Vacío = no se puede usar ahora.</summary>
        public abstract string Prompt { get; }

        /// <summary>Si es false, el objeto se muestra en gris (por ejemplo, fuera de su fase).</summary>
        public abstract bool CanInteract { get; }

        /// <summary>Nombre corto para la etiqueta flotante.</summary>
        public abstract string DisplayName { get; }

        public abstract void Interact();

        /// <summary>Punto al que se apunta (centro del objeto).</summary>
        public virtual Vector3 FocusPoint
        {
            get
            {
                var col = GetComponent<Collider>();
                return col != null ? col.bounds.center : transform.position;
            }
        }

        /// <summary>Color del brillo cuando el jugador lo mira.</summary>
        protected virtual Color HighlightColor => new Color(1f, 0.85f, 0.35f);

        protected virtual void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        protected virtual void OnDisable()
        {
            all.Remove(this);
            SetHighlight(0f);
        }

        /// <summary>Hace brillar el objeto (0 = normal, 1 = máximo).</summary>
        public void SetHighlight(float amount)
        {
            if (Mathf.Abs(amount - highlight) < 0.01f) return;
            highlight = amount;

            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            if (block == null) block = new MaterialPropertyBlock();

            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (amount <= 0.001f)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, HighlightColor * (amount * 0.9f));
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Vuelve a leer los renderers (si se añadieron piezas después).</summary>
        public void RefreshRenderers() => renderers = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();
    }
}
