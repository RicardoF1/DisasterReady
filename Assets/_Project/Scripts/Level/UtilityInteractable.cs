using UnityEngine;

namespace DisasterReady
{
    /// <summary>Llave del gas o tablero eléctrico: solo se usan después del sismo.</summary>
    public class UtilityInteractable : Interactable
    {
        public string TaskId;
        public string Label;
        public string DoneMessage;
        public SoundId Sound = SoundId.Switch;
        public Transform Handle;          // pieza que gira al usarla
        public Vector3 HandleDoneEuler;

        bool done;

        public static UtilityInteractable Create(GameObject target, string taskId, string label, string doneMessage, SoundId sound,
                                                 Transform handle, Vector3 handleDoneEuler)
        {
            if (target.GetComponent<Collider>() == null) target.AddComponent<BoxCollider>();
            var u = target.AddComponent<UtilityInteractable>();
            u.TaskId = taskId;
            u.Label = label;
            u.DoneMessage = doneMessage;
            u.Sound = sound;
            u.Handle = handle;
            u.HandleDoneEuler = handleDoneEuler;
            return u;
        }

        public override string DisplayName => Label;

        public override bool CanInteract
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && !done && gm.State == GameState.Evacuacion && !gm.InAftershock && !gm.Paused;
            }
        }

        public override string Prompt
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return "";
                if (done) return Label + "  ·  hecho";
                switch (gm.State)
                {
                    case GameState.Preparacion: return Label + "  ·  recuerda: se usa DESPUÉS del sismo";
                    case GameState.Alerta:
                    case GameState.Sismo: return "¡Ahora no! Primero protégete";
                }
                if (gm.InAftershock) return "Espera a que pase la réplica";
                return Label;
            }
        }

        protected override Color HighlightColor => new Color(0.35f, 0.9f, 1f);

        public override void Interact()
        {
            var gm = GameManager.Instance;
            if (gm == null || done) return;
            if (!CanInteract)
            {
                gm.TryCompleteTask(TaskId, DoneMessage); // muestra el motivo
                return;
            }
            if (gm.TryCompleteTask(TaskId, DoneMessage))
            {
                done = true;
                GameEvents.PlaySoundAt(Sound, transform.position);
                if (Handle != null) Handle.localRotation = Quaternion.Euler(HandleDoneEuler);
            }
        }
    }
}
