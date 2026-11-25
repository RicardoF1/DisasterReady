using System.Collections.Generic;
using UnityEngine;

namespace DisasterReady
{
    public sealed class HouseBuilder : MonoBehaviour, IWorldQuery
    {
        readonly List<Bounds> shelters = new();
        readonly List<Bounds> glassZones = new();
        readonly List<Material> materials = new();
        readonly Dictionary<Color, Material> palette = new();
        public readonly List<GameObject> Windows = new();
        public readonly List<Light> HouseLights = new();
        public readonly List<GameObject> FallingFurniture = new();
        public GameObject ExitGate { get; private set; }
        public bool IsSheltered(Vector3 feet) => shelters.Exists(b => b.Contains(feet + Vector3.up * .4f));
        public bool NearGlass(Vector3 feet) => glassZones.Exists(b => b.Contains(feet + Vector3.up));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(10, (root, house) =>
        { if (house) root.gameObject.AddComponent<HouseBuilder>().Build(); });

        public GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(transform);
            obj.transform.position = position; obj.transform.localScale = scale;
            if (!palette.TryGetValue(color, out Material material))
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.color = color; material.SetFloat("_Smoothness", .18f);
                materials.Add(material); palette[color] = material;
            }
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        // Los nombres se muestran solo al mirar un objeto; no hay texto atravesando paredes.
        public void Label(string text, Vector3 position, Color color, float size = .15f) { }
        GameObject Detail(string name, Vector3 position, Vector3 size, Color color)
        {
            var obj = Box(name, position, size, color);
            var collider = obj.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            return obj;
        }
        void Decorate()
        {
            Color oak = new(.42f, .28f, .17f), linen = new(.88f, .85f, .76f);
            Color teal = new(.12f, .32f, .34f), brass = new(.67f, .49f, .25f);
            for (int level = 0; level < 2; level++)
            {
                float y = level * 3.2f;
                // Juntas discretas del piso, zócalos y marcos arquitectónicos.
                for (int i = 0; i < 14; i++)
                    Detail("Junta de madera", new(-1.25f,y+.012f,-6.5f+i), new(9.35f,.012f,.018f), oak);
                foreach(float x in new[]{-5.86f,5.86f})
                    Detail("Zócalo",new(x,y+.08f,0),new(.06f,.16f,13.8f),linen);
                Detail("Zócalo norte",new(0,y+.08f,6.85f),new(11.8f,.16f,.06f),linen);
                Detail("Alfombra tejida",new(-3,y+.025f,-3.5f),new(3.5f,.025f,3.1f),teal);
                for(int i=0;i<7;i++)
                    Detail("Trama alfombra",new(-4.4f+i*.45f,y+.042f,-3.5f),new(.025f,.012f,2.9f),linen);
                // Lámpara de techo con pantalla y núcleo luminoso.
                Detail("Pantalla techo",new(0,y+2.88f,-3),new(.85f,.16f,.85f),linen);
                Detail("Soporte techo",new(0,y+3.02f,-3),new(.07f,.2f,.07f),brass);
                Detail("Luz difusora",new(0,y+2.78f,-3),new(.65f,.025f,.65f),new(1,.91f,.7f));
                foreach(float x in new[]{-5.83f,5.83f})
                    foreach(float z in new[]{-2.5f,2.5f})
                    {
                        Detail("Marco ventana superior",new(x,y+2.3f,z),new(.14f,.1f,2.1f),linen);
                        Detail("Marco ventana inferior",new(x,y+1,z),new(.14f,.1f,2.1f),linen);
                        Detail("Montante ventana",new(x,y+1.65f,z),new(.14f,1.3f,.06f),linen);
                    }
                // Señal física de protección: placa verde con símbolo de mesa.
                Detail("Placa zona segura",new(-3,y+2.05f,-6.83f),new(.55f,.48f,.035f),teal);
                Detail("Icono mesa",new(-3,y+2.1f,-6.8f),new(.34f,.04f,.025f),Color.white);
                foreach(float x in new[]{-3.13f,-2.87f})
                    Detail("Icono pata",new(x,y+1.99f,-6.8f),new(.035f,.2f,.025f),Color.white);
            }
            // Sofá con respaldo, brazos y cojines.
            Box("Respaldo sofá",new(-4,.9f,-6.08f),new(2.5f,1.05f,.2f),teal);
            foreach(float x in new[]{-5.2f,-2.8f}) Box("Brazo sofá",new(x,.65f,-5.7f),new(.18f,.7f,1),teal);
            foreach(float x in new[]{-4.65f,-4f,-3.35f})
                Detail("Cojín sofá",new(x,.95f,-5.65f),new(.58f,.18f,.7f),linen);
            foreach(Vector3 bed in new[]{new Vector3(-4,3.2f,-.1f),new Vector3(-.2f,3.2f,5.8f)})
            {
                Box("Cabecero",bed+new Vector3(0,.65f,.65f),new(2,.95f,.12f),oak);
                Detail("Colcha",bed+new Vector3(0,.62f,-.12f),new(1.95f,.1f,.95f),teal);
                foreach(float dx in new[]{-.5f,.5f}) Detail("Almohada",bed+new Vector3(dx,.68f,.43f),new(.7f,.16f,.35f),linen);
            }
            // Cocina: frentes, tiradores y fregadero.
            for(int i=0;i<4;i++)
            {
                Detail("Puerta cocina",new(-1.1f+i*.73f,.5f,5.79f),new(.68f,.88f,.035f),linen);
                Detail("Tirador",new(-1.1f+i*.73f,.8f,5.75f),new(.23f,.025f,.03f),brass);
            }
            Detail("Fregadero",new(-.5f,1.01f,6.3f),new(.8f,.025f,.6f),new(.14f,.19f,.2f));
            // Punto de reunión con postes y pavimento visible.
            foreach(float x in new[]{-3.2f,3.2f})
                Box("Poste exterior",new(x,.6f,-22),new(.12f,1.2f,.12f),linen);
            for(int i=0;i<6;i++)
                Detail("Paso peatonal",new(0,-.055f,-11.5f-i),new(2,.015f,.45f),linen);
            Detail("Placa salida",new(0,2.5f,-6.83f),new(1.1f,.4f,.04f),teal);
            Detail("Flecha salida",new(0,2.5f,-6.79f),new(.65f,.06f,.025f),Color.white);
            var arrow=Detail("Punta flecha",new(.26f,2.5f,-6.77f),new(.17f,.17f,.025f),Color.white);
            arrow.transform.rotation=Quaternion.Euler(0,0,45);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.56f,.61f,.65f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=false;
        }
        void Table(float x, float floor, float z)
        {
            Color wood = new(.36f, .20f, .10f);
            Box("Mesa firme", new(x, floor + 1.22f, z), new(2.8f, .16f, 2), wood);
            foreach (float dx in new[] { -1.22f, 1.22f })
                foreach (float dz in new[] { -.82f, .82f })
                    Box("Pata", new(x + dx, floor + .57f, z + dz), new(.16f, 1.14f, .16f), wood);
            shelters.Add(new Bounds(new(x, floor + .5f, z), new(2, 1, 1.35f)));
            Label("MESA FIRME\nCtrl + H bajo la mesa", new(x, floor + 1.7f, z), Color.green, .09f);
        }
        void Window(float x, float floor, float z)
        {
            var pane = Box("Vidrio", new(x, floor + 1.65f, z), new(.12f, 1.2f, 2), new(.45f, .8f, .95f));
            Windows.Add(pane);
            glassZones.Add(new Bounds(new(x, floor + 1, z), new(2.2f, 2.5f, 3)));
        }
        void Build()
        {
            GameManager.Instance.World = this;
            Color wall = new(.82f, .81f, .75f), floor = new(.49f, .36f, .24f);
            Box("Terreno", new(0, -.3f, -7), new(50, .3f, 60), new(.25f, .4f, .22f));
            Box("Piso 1", new(0, -.12f, 0), new(12, .24f, 14), floor);
            // Hueco real de escalera: x=3.5..6, z=-5..3.
            Box("Piso 2 oeste", new(-1.25f, 3.08f, 0), new(9.5f, .24f, 14), floor);
            Box("Descanso superior", new(4.75f, 3.08f, 5), new(2.5f, .24f, 4), floor);
            Box("Piso 2 sur", new(4.75f, 3.08f, -6), new(2.5f, .24f, 2), floor);
            Box("Techo", new(0, 6.5f, 0), new(12.4f, .2f, 14.4f), new(.85f, .84f, .79f));
            for (int i = 0; i < 20; i++)
            {
                float h = (i + 1) * .16f;
                Box("Escalón " + (i + 1), new(4.75f, h / 2, -4.8f + i * .4f), new(2, h, .4f), floor);
            }
            // Barandilla lateral continua; extremos abiertos para entrar y salir.
            Box("Baranda hueco", new(3.55f, 3.65f, -1), new(.12f, .9f, 8), wall);
            for (int level = 0; level < 2; level++)
            {
                float y = level * 3.2f;
                Box("Pared norte", new(0, y + 1.6f, 7), new(12, 3.2f, .2f), wall);
                Box("Fachada izquierda", new(-3.5f, y + 1.6f, -7), new(5, 3.2f, .2f), wall);
                Box("Fachada derecha", new(3.5f, y + 1.6f, -7), new(5, 3.2f, .2f), wall);
                if (level == 1) Box("Fachada superior centro", new(0, y + 1.6f, -7), new(2, 3.2f, .2f), wall);
                else Box("Dintel puerta", new(0, 2.85f, -7), new(2, .7f, .2f), wall);
                foreach (float x in new[] { -6f, 6f })
                {
                    Box("Antepecho", new(x, y + .5f, 0), new(.2f, 1, 14), wall);
                    Box("Dintel lateral", new(x, y + 2.75f, 0), new(.2f, .9f, 14), wall);
                    foreach (float z in new[] { -5f, 0f, 5f })
                        Box("Machón", new(x, y + 1.65f, z), new(.2f, 1.3f, 4), wall);
                    Window(x, y, -2.5f); Window(x, y, 2.5f);
                    // Cierra los huecos alrededor del vidrio, sin crear salidas utilizables.
                    Box("Marco central", new(x, y + 1.65f, 0), new(.2f, 1.3f, 3), wall);
                }
                foreach (float x in new[] { -5.7f, 5.7f })
                    foreach (float z in new[] { -6.7f, 6.7f })
                        Box("Columna estructural", new(x, y + 1.6f, z), new(.4f, 3.2f, .4f), Color.gray);
                // División dormitorios/sala con paso central de dos metros.
                Box("Tabique oeste", new(-3.5f, y + 1.5f, 1), new(5, 3, .15f), wall);
                Box("Tabique este", new(2.25f, y + 1.5f, 1), new(2.5f, 3, .15f), wall);
                Label(level == 0 ? "SALA / COMEDOR" : "DORMITORIO 1", new(-2, y + 2.6f, -5), Color.white);
                Label(level == 0 ? "COCINA" : "DORMITORIO 2", new(-2, y + 2.6f, 5.8f), Color.white);
                Table(-3, y, -3.5f);
                // Zona estructural solo válida en esta vivienda ficticia previamente evaluada.
                shelters.Add(new Bounds(new(-4.6f, y + .5f, 6), new(1.3f, 1, 1.1f)));
                Label("ZONA EVALUADA\nCtrl + H", new(-4.6f, y + 1.6f, 6), Color.green, .10f);
                var shelf = Box("Estante", new(-5.1f, y + 1.1f, 0), new(.65f, 2.2f, 1.5f), new(.3f, .18f, .1f));
                FallingFurniture.Add(shelf);
                var wardrobe = Box("Ropero", new(-2.7f, y + 1.2f, 6), new(1.4f, 2.4f, .7f), floor);
                FallingFurniture.Add(wardrobe);
                var lightObject = new GameObject("Luz cálida"); lightObject.transform.SetParent(transform);
                lightObject.transform.position = new(0, y + 2.6f, 0);
                Light light = lightObject.AddComponent<Light>(); light.type = LightType.Point;
                light.range = 13; light.intensity = 1.8f; light.color = new(1, .83f, .61f); HouseLights.Add(light);
            }
            Box("Sofá", new(-4, .45f, -5.7f), new(2.5f, .9f, 1), new(.28f, .42f, .5f));
            Box("Encimera", new(0, .5f, 6.3f), new(3, 1, 1), Color.gray);
            Box("Cama 1", new(-4, 3.5f, -.1f), new(2, .6f, 1.4f), new(.3f, .55f, .75f));
            Box("Cama 2", new(-.2f, 3.5f, 5.8f), new(2, .6f, 1.4f), new(.5f, .6f, .3f));
            Box("Baño pared", new(2, 1.4f, 4.7f), new(.15f, 2.8f, 4.4f), wall);
            Box("Baño frente", new(2.4f, 1.4f, 2.5f), new(.8f, 2.8f, .15f), wall);
            Box("Inodoro", new(2.8f, .35f, 6.3f), new(.6f, .7f, .8f), Color.white);
            Label("BAÑO", new(2.9f, 2.3f, 3), Color.white, .1f);
            Label("ESCALERA\nBajar después del sismo", new(4.5f, 4.5f, 3.7f), Color.green, .09f);
            Label("SALIDA", new(0, 2.4f, -6.8f), Color.green);
            ExitGate = Box("Puerta: espera fin del sismo", new(0, 1.1f, -7), new(2, 2.2f, .2f), floor);
            Box("Vereda", new(0, -.08f, -9), new(20, .16f, 3.5f), Color.gray);
            Box("Calle", new(0, -.12f, -14), new(40, .1f, 6), new(.16f, .17f, .18f));
            Box("Punto de reunión", new(0, -.08f, -22), new(6, .16f, 6), new(.1f, .55f, .25f));
            Label("PUNTO DE REUNIÓN\nHUANCAYO", new(0, 2, -22), Color.white, .2f);
            Decorate();
        }
        void OnDestroy() { foreach (Material material in materials) Destroy(material); }
    }
}
