using UnityEngine;
using UnityEngine.InputSystem;

namespace DisasterReady
{
    /// <summary>
    /// Linterna (tecla F). Solo funciona si la linterna está en la mochila.
    /// Muy útil cuando se corta la luz durante el sismo.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        Light spot;
        Transform holder;
        bool on;
        float flicker;

        void Start()
        {
            holder = new GameObject("Linterna").transform;
            holder.SetParent(PlayerInfo.Camera != null ? PlayerInfo.Camera.transform : transform, false);
            holder.localPosition = new Vector3(0.18f, -0.12f, 0.1f);

            spot = holder.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 16f;
            spot.spotAngle = 62f;
            spot.innerSpotAngle = 28f;
            spot.intensity = 6f;
            spot.color = new Color(1f, 0.95f, 0.82f);
            spot.shadows = LightShadows.Soft;
            spot.enabled = false;

            GameEvents.InventoryChanged += OnInventoryChanged;
            GameEvents.StateChanged += OnStateChanged;
        }

        void OnDestroy()
        {
            GameEvents.InventoryChanged -= OnInventoryChanged;
            GameEvents.StateChanged -= OnStateChanged;
        }

        bool HasFlashlight
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.Backpack.Has("linterna");
            }
        }

        void OnInventoryChanged()
        {
            if (!HasFlashlight) SetOn(false);
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Evacuacion && HasFlashlight && !on)
                GameEvents.RaiseMessage("Está oscuro: pulsa F para encender la linterna.", MessageKind.Info);
        }

        void SetOn(bool value)
        {
            on = value;
            if (spot != null) spot.enabled = value;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var keyboard = Keyboard.current;
            if (keyboard == null || gm == null || !gm.Playing || gm.Paused) return;

            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (HasFlashlight)
                {
                    SetOn(!on);
                    GameEvents.PlaySound(SoundId.Switch, 0.5f);
                }
                else GameEvents.RaiseMessage("No tienes linterna. Búscala y guárdala en la mochila.", MessageKind.Warning);
            }

            // Pequeño parpadeo realista mientras tiembla
            if (on && spot != null)
            {
                flicker = Mathf.Lerp(flicker, Random.value, Time.deltaTime * 20f);
                spot.intensity = 6f - QuakeInfo.Intensity * flicker * 3f;
            }
        }
    }
}
