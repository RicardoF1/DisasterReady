using System.Linq;
using UnityEngine;

namespace DisasterReady
{
    public sealed class LevelContent : MonoBehaviour
    {
        HouseBuilder house;
        Light sun;
        float sunIntensity;
        Color sunColor;
        bool started;
        GameObject debris;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(20, (root, isHouse) =>
        { if (isHouse) root.gameObject.AddComponent<LevelContent>().Build(); });
        void Build()
        {
            house = GetComponent<HouseBuilder>();
            var config = GameManager.Instance.Config;
            string[] ids = { "water", "flashlight", "firstaid", "radio", "batteries", "whistle", "documents", "food", "coat", "laptop", "console", "ornament" };
            string[] names = { "Agua", "Linterna", "Botiquín", "Radio a pilas", "Pilas", "Silbato", "Documentos en bolsa", "Alimentos no perecibles", "Abrigo", "Laptop", "Consola", "Adorno" };
            for (int i = 0; i < ids.Length; i++)
            {
                float x = -4.6f + (i % 4) * 1.4f;
                float z = i < 4 ? -5.5f : i < 8 ? 2.6f : 4.2f;
                Vector3 pos = new(x, 3.9f, z);
                house.Box("Soporte de suministro", pos - Vector3.up * .4f, new(.5f, .6f, .5f), new(.5f, .35f, .2f));
                var obj = house.Box(names[i], pos, new(.35f, .35f, .35f), i < 9 ? new(.3f, .75f, .65f) : new(.75f, .4f, .35f));
                DressItem(obj, ids[i]);
                int size = i == 0 || i == 2 || i == 7 ? 2 : i >= 9 ? 4 : 1;
                obj.AddComponent<LevelInteractable>().Item = new ItemData(ids[i], names[i], size,
                    i < 9 ? config.UsefulItemPoints : config.UselessItemPoints);
                house.Label(names[i], pos + Vector3.up * .35f, Color.white, .065f);
            }
            foreach (GameObject window in house.Windows)
            { window.AddComponent<LevelInteractable>().Kind = InteractionKind.Window; }
            Utility("gas", "Cerrar llave de gas", new(.9f, 1.3f, 5.7f), Color.yellow);
            Utility("power", "Bajar llave general", new(1.5f, 1.4f, -6.7f), Color.gray);
            for (int floor = 0; floor < 2; floor++)
            {
                float y = floor * 3.2f;
                for (int i = 0; i < 4; i++)
                    house.FallingFurniture.Add(house.Box("Libro", new(-4.9f, y + 2.3f, -.5f + i * .3f), new(.3f, .2f, .2f), Color.red));
                house.FallingFurniture.Add(house.Box("Lámpara", new(1, y + 2.7f, -4.5f), new(.4f, .3f, .4f), Color.yellow));
                house.FallingFurniture.Add(house.Box("Cuadro", new(-5.8f, y + 2, -5), new(.1f, .7f, .8f), Color.magenta));
            }
            foreach (GameObject obj in house.FallingFurniture) obj.AddComponent<FallingHazard>();
            // Bloquea el atajo oeste; el paso central y la escalera este permanecen transitables.
            debris = house.Box("Escombros: desvío", new(-2.6f, .6f, -1.3f), new(3, 1.2f, 1), Color.gray);
            debris.SetActive(false);
            house.Label("RUTA: PASO CENTRAL → SALIDA", new(0, 2.6f, -.5f), Color.green, .09f);
            foreach (Light light in gameObject.scene.GetRootGameObjects()
                .SelectMany(obj => obj.GetComponentsInChildren<Light>()).ToArray())
                if (light.type == LightType.Directional) { sun = light; sunIntensity = light.intensity; sunColor = light.color;
                    light.color = new Color(1f, .96f, .89f); light.intensity = 1.1f; break; }
            GameEvents.OnTremorChanged += Tremor;
            GameEvents.OnStateChanged += State;
        }
        void DressItem(GameObject item, string id)
        {
            void Part(string name, Vector3 pos, Vector3 scale, Color color, PrimitiveType type = PrimitiveType.Cube)
            {
                var child = GameObject.CreatePrimitive(type); child.name = name;
                child.transform.SetParent(item.transform, false);
                child.transform.localPosition = pos; child.transform.localScale = scale;
                var c = child.GetComponent<Collider>(); c.enabled = false; Destroy(c);
                var mat = new Material(item.GetComponent<Renderer>().sharedMaterial); mat.color = color;
                child.GetComponent<Renderer>().sharedMaterial = mat;
                itemMaterials.Add(mat);
            }
            Color dark = new(.08f,.13f,.16f), white = new(.92f,.94f,.9f);
            var renderer = item.GetComponent<Renderer>();
            if(id == "water")
            {
                renderer.enabled=false;
                Part("Botella",Vector3.zero,new(.65f,.7f,.65f),new(.2f,.6f,.8f),PrimitiveType.Cylinder);
                Part("Tapa",new(0,.78f,0),new(.38f,.12f,.38f),dark,PrimitiveType.Cylinder);
                Part("Etiqueta",new(0,0,-.33f),new(.45f,.5f,.03f),white);
            }
            else if(id == "firstaid")
            {
                Part("Cruz vertical",new(0,0,-.52f),new(.17f,.65f,.04f),white);
                Part("Cruz horizontal",new(0,0,-.54f),new(.65f,.17f,.04f),white);
            }
            else if(id == "radio" || id == "console" || id == "laptop")
            {
                Part("Pantalla",new(-.15f,.12f,-.52f),new(.55f,.4f,.04f),dark);
                for(int j=0;j<3;j++) Part("Control",new(.32f,-.25f+j*.2f,-.53f),new(.1f,.1f,.05f),white);
                if(id == "radio") Part("Antena",new(.32f,.95f,0),new(.04f,1,.04f),dark);
            }
            else if(id == "flashlight" || id == "batteries")
            {
                renderer.enabled=false;
                Part("Cuerpo",Vector3.zero,new(.5f,.8f,.5f),dark,PrimitiveType.Cylinder);
                Part("Extremo",new(0,.8f,0),new(.65f,.12f,.65f),white,PrimitiveType.Cylinder);
            }
            else
            {
                Part("Banda",new(0,0,-.52f),new(.9f,.23f,.03f),white);
                Part("Sello",new(.25f,.27f,-.53f),new(.18f,.18f,.04f),dark);
            }
        }
        readonly System.Collections.Generic.List<Material> itemMaterials = new();
        void Utility(string key, string label, Vector3 pos, Color color)
        {
            var obj = house.Box(label, pos, new(.45f, .45f, .25f), color);
            var interaction = obj.AddComponent<LevelInteractable>();
            interaction.Kind = InteractionKind.Utility; interaction.Key = key; interaction.Label = label;
            house.Label(label, pos + Vector3.up * .5f, Color.white, .075f);
        }
        void Tremor(bool shaking)
        {
            if (!shaking || started) return;
            started = true; debris.SetActive(true);
            foreach (Light light in house.HouseLights) light.enabled = false;
            if (sun != null) sun.intensity = .12f;
            foreach (GameObject window in house.Windows)
            {
                window.GetComponent<Renderer>().enabled = false;
                // El collider sigue detectando el intento peligroso de usar la ventana.
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = window.transform.position + Vector3.up * (.1f * i);
                    var shard = house.Box("Fragmento de vidrio", pos, new(.12f, .12f, .12f), Color.cyan);
                    shard.AddComponent<Rigidbody>().mass = .1f;
                    Destroy(shard, 4);
                }
            }
            GameEvents.Sound("glass");
        }
        void State(GameState state)
        { if (state == GameState.Evacuation) house.ExitGate.SetActive(false); }
        void Update()
        {
            var game = GameManager.Instance;
            if (game.Player == null || game.Paused) return;
            Vector3 p = game.Player.Position;
            if (Mathf.Abs(p.x) < 3 && Mathf.Abs(p.z + 22) < 3 && p.y < 1)
                game.TryEvacuate();
        }
        void OnDestroy()
        {
            foreach (var material in itemMaterials) Destroy(material);
            GameEvents.OnTremorChanged -= Tremor; GameEvents.OnStateChanged -= State;
            if (sun != null) { sun.intensity = sunIntensity; sun.color = sunColor; }
        }
    }
}
