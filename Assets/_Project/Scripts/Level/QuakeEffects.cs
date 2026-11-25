using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    /// <summary>
    /// Consecuencias físicas del sismo en la vivienda: objetos que caen, muebles que se vuelcan,
    /// vidrios que estallan, trozos del techo que se desprenden, corte de luz, polvo
    /// y escombros que bloquean una ruta. Solo existe en la escena House.
    /// </summary>
    public class QuakeEffects : MonoBehaviour
    {
        readonly List<Fallable> pending = new List<Fallable>();
        readonly List<float> releaseTimes = new List<float>();
        Vector3 shakeForce;
        float shakeTimer;
        float quakeTime;
        ParticleSystem dustFloor1, dustFloor2, haze;
        Material plaster;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRegister() => SceneBoot.Register(GameConfig.HouseScene, Spawn);

        static void Spawn()
        {
            if (FindAnyObjectByType<QuakeEffects>() != null) return;
            new GameObject("Efectos del sismo").AddComponent<QuakeEffects>();
        }

        void Awake()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.TremorChanged += OnTremorChanged;
            GameEvents.Impact += OnImpact;
            plaster = Palette.Solid(new Color(0.85f, 0.83f, 0.8f), 0.05f);
        }

        void Start()
        {
            dustFloor1 = CreateDust("Polvo 1er piso", new Vector3(6f, 2.9f, 5f), new Vector3(11.5f, 0.1f, 9.5f));
            dustFloor2 = CreateDust("Polvo 2do piso", new Vector3(6f, 6.0f, 5f), new Vector3(11.5f, 0.1f, 9.5f));
            haze = CreateHaze();
        }

        void OnDestroy()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.TremorChanged -= OnTremorChanged;
            GameEvents.Impact -= OnImpact;
        }

        // ---------------------------------------------------------------------
        void OnStateChanged(GameState state)
        {
            if (state == GameState.Sismo) StartCoroutine(MainQuake());
            if (state == GameState.Evacuacion) SetRate(haze, 14f);
        }

        void OnTremorChanged(bool shaking)
        {
            var gm = GameManager.Instance;
            if (shaking && gm != null && gm.InAftershock) StartCoroutine(Aftershock());
        }

        IEnumerator MainQuake()
        {
            quakeTime = 0f;
            ScheduleFallables(GameConfig.QuakeSeconds * 0.7f);
            StartCoroutine(PowerCut());
            StartCoroutine(BreakWindows());
            StartCoroutine(CeilingDebris(GameConfig.QuakeSeconds - 4f));
            StartCoroutine(BlockRoute(9f));

            while (quakeTime < GameConfig.QuakeSeconds)
            {
                quakeTime += Time.deltaTime;
                ReleaseDue();
                yield return null;
            }
        }

        IEnumerator Aftershock()
        {
            // La réplica suelta lo que aún quedaba en pie y algunos trozos del techo
            ScheduleFallables(GameConfig.AftershockSeconds * 0.8f);
            quakeTime = 0f;
            StartCoroutine(CeilingDebris(GameConfig.AftershockSeconds - 1f, 0.5f));
            while (quakeTime < GameConfig.AftershockSeconds)
            {
                quakeTime += Time.deltaTime;
                ReleaseDue();
                yield return null;
            }
        }

        // ---------- Objetos que caen ----------

        void ScheduleFallables(float window)
        {
            pending.Clear();
            releaseTimes.Clear();
            foreach (var f in Fallable.All)
                if (f != null && !f.IsReleased) pending.Add(f);

            // Lo liviano cae primero; los muebles pesados se vuelcan cuando el movimiento es fuerte
            foreach (var f in pending)
            {
                float start = f.Heavy ? window * 0.25f : 0.6f;
                releaseTimes.Add(Random.Range(start, window));
            }
        }

        void ReleaseDue()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (quakeTime < releaseTimes[i]) continue;
                var f = pending[i];
                pending.RemoveAt(i);
                releaseTimes.RemoveAt(i);
                if (f != null && !f.IsReleased) Release(f);
            }
        }

        static void Release(Fallable f)
        {
            float mass = f.Body != null ? f.Body.mass : 1f;
            if (f.TipDirection != Vector3.zero)
            {
                Vector3 dir = f.TipDirection.normalized;
                float strength = f.Heavy ? 1.6f : 1.2f;
                f.Release(dir * mass * strength, f.TopPoint);
            }
            else
            {
                Vector2 r = Random.insideUnitCircle.normalized;
                f.Release(new Vector3(r.x, 0.4f, r.y) * mass * 0.9f);
            }
            if (f.Heavy) GameEvents.PlaySoundAt(SoundId.Creak, f.transform.position, 0.8f);
        }

        void FixedUpdate()
        {
            float q = QuakeInfo.Intensity;
            if (q < 0.05f) return;

            shakeTimer -= Time.fixedDeltaTime;
            if (shakeTimer <= 0f)
            {
                shakeTimer = Random.Range(0.08f, 0.18f);
                Vector2 r = Random.insideUnitCircle;
                shakeForce = new Vector3(r.x, Random.Range(-0.2f, 0.3f), r.y) * 9f;
            }

            foreach (var f in Fallable.All)
            {
                if (f == null || !f.IsReleased || f.Body == null || f.Body.isKinematic) continue;
                f.Body.AddForce(shakeForce * q, ForceMode.Acceleration);
            }
        }

        // ---------- Luz ----------

        IEnumerator PowerCut()
        {
            var house = HouseBuilder.Instance;
            float t = 0f;
            while (t < 3.2f)
            {
                bool on = Random.value > 0.45f;
                float factor = Random.Range(0.3f, 1f);
                if (house != null) house.SetLights(on, factor);
                float wait = Random.Range(0.05f, 0.22f);
                t += wait;
                yield return new WaitForSeconds(wait);
            }
            if (house != null) house.SetLights(false);
            QuakeInfo.PowerOn = false;
            GameEvents.PlaySound(SoundId.Switch, 0.6f);
            GameEvents.RaiseMessage("Se cortó la electricidad.", MessageKind.Info);
        }

        // ---------- Vidrios ----------

        IEnumerator BreakWindows()
        {
            var house = HouseBuilder.Instance;
            if (house == null) yield break;
            yield return new WaitForSeconds(3.5f);

            var order = new List<WindowInfo>(house.Windows);
            for (int i = 0; i < order.Count; i++)
            {
                int j = Random.Range(i, order.Count);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // Se rompen la mitad de las ventanas, y siempre las que están cerca del jugador
            for (int i = 0; i < order.Count; i++)
            {
                var w = order[i];
                bool nearPlayer = PlayerInfo.HasPlayer && Vector3.Distance(PlayerInfo.Position, w.Pane.transform.position) < 2.6f;
                if (i % 2 == 1 && !nearPlayer) continue;
                Shatter(w);
                yield return new WaitForSeconds(Random.Range(0.25f, 1.1f));
            }
        }

        void Shatter(WindowInfo w)
        {
            if (w.Broken || w.Pane == null) return;
            w.Broken = true;
            var bounds = w.Pane.GetComponent<Renderer>().bounds;
            // El vidrio desaparece pero el hueco sigue bloqueado (no se sale por la ventana)
            w.Pane.GetComponent<Renderer>().enabled = false;
            GameEvents.PlaySoundAt(SoundId.Glass, bounds.center, 1f);

            var glass = Palette.Glass(new Color(0.75f, 0.9f, 1f), 0.5f);
            Vector3 inward = w.Opening.InwardDirection;
            for (int i = 0; i < 10; i++)
            {
                Vector3 p = new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y),
                                        Random.Range(bounds.min.z, bounds.max.z)) + inward * 0.12f;
                var shard = Build.Box("Vidrio roto", p, new Vector3(Random.Range(0.06f, 0.2f), Random.Range(0.06f, 0.22f), 0.012f), glass, transform);
                shard.transform.rotation = Random.rotation;
                var body = shard.AddComponent<Rigidbody>();
                body.mass = 0.15f;
                var f = shard.AddComponent<Fallable>();
                f.Damage = 8;
                f.Release((inward * Random.Range(0.3f, 1.4f) + Random.insideUnitSphere * 0.4f) * body.mass);
                Destroy(shard, 12f);
            }
        }

        // ---------- Techo ----------

        IEnumerator CeilingDebris(float duration, float rate = 1f)
        {
            float t = 0f;
            float next = 2f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (t >= next && QuakeInfo.Intensity > 0.3f)
                {
                    next = t + Random.Range(2.2f, 3.8f) / rate;
                    if (PlayerInfo.HasPlayer && IsInside(PlayerInfo.Feet))
                    {
                        // Cae cerca del jugador: si está bajo una mesa, la mesa lo protege
                        Vector2 offset = Random.insideUnitCircle * 0.5f;
                        Vector3 p = PlayerInfo.Feet + new Vector3(offset.x, 0f, offset.y);
                        p.y = CeilingAbove(PlayerInfo.Feet) - 0.15f;
                        SpawnChunk(p, 12);
                    }
                    // Y otros en lugares aleatorios de la casa
                    SpawnChunk(new Vector3(Random.Range(0.5f, 11.5f), Random.value > 0.5f ? 2.85f : 5.95f, Random.Range(0.5f, 9.5f)), 10);
                }
                yield return null;
            }
        }

        static bool IsInside(Vector3 p) => p.x > 0f && p.x < 12f && p.z > 0f && p.z < 10f && p.y < 6f;

        static float CeilingAbove(Vector3 feet) => feet.y < 2.5f ? HouseLayout.WallHeight(0) : HouseLayout.FloorY(1) + HouseLayout.WallHeight(1);

        void SpawnChunk(Vector3 p, int damage)
        {
            var chunk = Build.Box("Trozo de techo", p, new Vector3(Random.Range(0.15f, 0.3f), 0.06f, Random.Range(0.15f, 0.3f)), plaster, transform);
            chunk.transform.rotation = Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), Random.Range(-20f, 20f));
            var body = chunk.AddComponent<Rigidbody>();
            body.mass = 2f;
            var f = chunk.AddComponent<Fallable>();
            f.Damage = damage;
            f.Heavy = false;
            f.Release(Vector3.down * 2f);
            BurstDust(p, 25);
            Destroy(chunk, 15f);
        }

        // ---------- Escombros que bloquean la puerta cocina - hall ----------

        IEnumerator BlockRoute(float delay)
        {
            yield return new WaitForSeconds(delay);
            Vector3 door = new Vector3(9.6f, 0f, 6f);
            var root = Build.Group("Escombros", door, transform);

            // Bloqueo físico de la puerta
            var blocker = Build.Block("Escombros (colisión)", new Vector3(8.9f, 0f, 5.7f), new Vector3(10.3f, 2.3f, 6.3f), plaster, root);
            blocker.GetComponent<Renderer>().enabled = false;

            var concrete = Palette.Textured("concrete", new Color(0.8f, 0.78f, 0.75f), Vector2.one);
            var beam = Build.Box("Viga caída", door + new Vector3(0f, 1.0f, 0f), new Vector3(1.8f, 0.25f, 0.25f), concrete, root, false);
            beam.transform.rotation = Quaternion.Euler(0f, 10f, 38f);
            for (int i = 0; i < 14; i++)
            {
                Vector3 p = door + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.1f, 0.9f), Random.Range(-0.5f, 0.5f));
                var rock = Build.Box("Escombro", p, Vector3.one * Random.Range(0.2f, 0.45f), i % 3 == 0 ? concrete : plaster, root, false);
                rock.transform.rotation = Random.rotation;
            }
            var brick = Palette.Textured("brick", Color.white, Vector2.one);
            for (int i = 0; i < 6; i++)
            {
                var b = Build.Box("Ladrillo", door + new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(0.1f, 0.6f), Random.Range(-0.6f, 0.6f)),
                                  new Vector3(0.24f, 0.09f, 0.12f), brick, root, false);
                b.transform.rotation = Random.rotation;
            }
            Build.Sign("RUTA\nBLOQUEADA", door + new Vector3(0f, 1.6f, 0.35f), Vector3.forward, new Vector2(0.7f, 0.35f), Palette.Danger, Color.white, 34, root);
            Build.Sign("RUTA\nBLOQUEADA", door + new Vector3(0f, 1.6f, -0.35f), Vector3.back, new Vector2(0.7f, 0.35f), Palette.Danger, Color.white, 34, root);

            GameEvents.PlaySoundAt(SoundId.ImpactHeavy, door + Vector3.up, 1f);
            BurstDust(door + Vector3.up, 120);
        }

        // ---------- Polvo ----------

        void OnImpact(Vector3 position, bool heavy) => BurstDust(position, heavy ? 50 : 15);

        void BurstDust(Vector3 position, int count)
        {
            if (haze == null) return;
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = false
            };
            for (int i = 0; i < count; i++)
            {
                emit.position = position + Random.insideUnitSphere * 0.4f;
                emit.velocity = Random.insideUnitSphere * 0.8f + Vector3.up * 0.3f;
                emit.startSize = Random.Range(0.3f, 0.9f);
                emit.startLifetime = Random.Range(2f, 4f);
                emit.startColor = new Color(0.82f, 0.78f, 0.72f, 0.35f);
                haze.Emit(emit, 1);
            }
        }

        ParticleSystem CreateDust(string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 2.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new Color(0.85f, 0.8f, 0.72f, 0.7f);
            main.gravityModifier = 0.25f;
            main.maxParticles = 800;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Palette.Particle(Color.white);
            ps.Play();
            return ps;
        }

        ParticleSystem CreateHaze()
        {
            var go = new GameObject("Polvo en suspensión");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(6f, 1.5f, 5f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 8f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startColor = new Color(0.85f, 0.82f, 0.76f, 0.08f);
            main.maxParticles = 1500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f; // se activa al empezar la evacuación

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(11f, 5f, 9f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Palette.Particle(Color.white);
            ps.Play();
            return ps;
        }

        void Update()
        {
            float q = QuakeInfo.Intensity;
            SetRate(dustFloor1, q * 120f);
            SetRate(dustFloor2, q * 120f);
        }

        static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.rateOverTime = rate;
        }
    }
}
