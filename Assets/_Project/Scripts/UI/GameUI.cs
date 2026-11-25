using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DisasterReady
{
    // Interfaz adaptable a la ventana mediante un lienzo de referencia 1600 x 900.
    public sealed class GameUI : MonoBehaviour
    {
        readonly Color ink = new(.035f,.065f,.085f,.96f);
        readonly Color mint = new(.48f,.88f,.74f);
        readonly Color muted = new(.66f,.74f,.77f);
        Canvas canvas;
        Font font;
        GameObject overlay, headsUp;
        Text phase, clock, status, bag, objective, hint, toast, safe;
        Image healthBar;
        float messageUntil;
        GameManager Game => GameManager.Instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => SceneBootstrap.Register(50,(root,house)=>root.gameObject.AddComponent<GameUI>().Build());

        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h, Vector2 anchor)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=anchor; r.pivot=anchor;
            r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); return r;
        }
        Image Box(Transform parent,string name,float x,float y,float w,float h,Color color, Vector2 anchor)
        {
            var image=Rect(parent,name,x,y,w,h,anchor).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false; return image;
        }
        Text Label(Transform parent,string value,int size,float x,float y,float w,float h,Color color,TextAnchor align=TextAnchor.UpperLeft)
        {
            var t=Rect(parent,"Texto",x,y,w,h,new Vector2(0,1)).gameObject.AddComponent<Text>();
            t.font=font; t.fontSize=size; t.text=value; t.color=color; t.alignment=align;
            t.raycastTarget=false; t.horizontalOverflow=HorizontalWrapMode.Wrap;
            t.verticalOverflow=VerticalWrapMode.Truncate; return t;
        }
        void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go=new GameObject("Disaster Ready UI",typeof(RectTransform)); go.transform.SetParent(transform,false);
            canvas=go.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=go.AddComponent<CanvasScaler>(); scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution=new Vector2(1600,900); scale.matchWidthOrHeight=.5f;
            go.AddComponent<GraphicRaycaster>();
            var events=Object.FindAnyObjectByType<EventSystem>();
            if(events==null) { var e=new GameObject("EventSystem"); e.transform.SetParent(transform,false); events=e.AddComponent<EventSystem>(); }
            foreach(var old in events.GetComponents<BaseInputModule>()) old.enabled=false;
            var input=events.GetComponent<InputSystemUIInputModule>();
            if(input==null) input=events.gameObject.AddComponent<InputSystemUIInputModule>();
            input.enabled=true; input.AssignDefaultActions();
            headsUp=Rect(canvas.transform,"HUD",0,0,1600,900,new Vector2(.5f,.5f)).gameObject;
            var top=Box(headsUp.transform,"Fase",32,-30,400,110,ink,new Vector2(0,1));
            Box(top.transform,"Acento",0,0,4,110,mint,new Vector2(0,1));
            phase=Label(top.transform,"PREPARACIÓN",20,22,-18,330,30,mint);
            objective=Label(top.transform,"Prepara tu mochila",18,22,-57,350,48,Color.white);
            var timer=Box(headsUp.transform,"Tiempo",-32,-30,180,110,ink,new Vector2(1,1));
            Label(timer.transform,"TIEMPO RESTANTE",13,16,-15,150,22,muted);
            clock=Label(timer.transform,"01:30",36,16,-40,150,50,Color.white);
            var vitals=Box(headsUp.transform,"Estado",32,32,260,92,ink,Vector2.zero);
            status=Label(vitals.transform,"SALUD 100  /  0 PTS",16,18,-14,230,28,Color.white);
            Box(vitals.transform,"Fondo salud",18,-55,224,5,new Color(.2f,.27f,.29f),new Vector2(0,1));
            healthBar=Box(vitals.transform,"Salud",18,-55,224,5,mint,new Vector2(0,1));
            var inventory=Box(headsUp.transform,"Mochila",-32,32,300,112,ink,new Vector2(1,0));
            bag=Label(inventory.transform,"MOCHILA  0 / 12",16,18,-14,265,90,Color.white);
            var center=Rect(headsUp.transform,"Indicaciones",0,48,700,76,new Vector2(.5f,0));
            hint=Label(center,"",20,0,0,700,76,Color.white,TextAnchor.MiddleCenter);
            var dot=Box(headsUp.transform,"Mira",0,0,5,5,new Color(1,1,1,.8f),new Vector2(.5f,.5f));
            var notice=Rect(headsUp.transform,"Avisos",0,-160,700,60,new Vector2(.5f,1));
            toast=Label(notice,"",20,0,0,700,60,mint,TextAnchor.MiddleCenter);
            var protection=Rect(headsUp.transform,"Protección",0,-32,450,44,new Vector2(.5f,1));
            safe=Label(protection,"",18,0,0,450,44,mint,TextAnchor.MiddleCenter);
            GameEvents.OnStateChanged+=State; GameEvents.OnMessage+=Message; State(Game.State);
        }
        void ClearPanel() { if(overlay!=null) { overlay.SetActive(false); Destroy(overlay); } }
        Transform Panel(string eyebrow,string title,string subtitle)
        {
            ClearPanel();
            var shade=Box(canvas.transform,"Pantalla",0,0,0,0,ink,new Vector2(.5f,.5f));
            var sr=shade.rectTransform; sr.anchorMin=Vector2.zero; sr.anchorMax=Vector2.one; sr.offsetMin=sr.offsetMax=Vector2.zero;
            shade.raycastTarget=true; overlay=shade.gameObject;
            var content=Rect(overlay.transform,"Contenido",0,0,1320,760,new Vector2(.5f,.5f));
            Label(content,eyebrow,16,0,-20,1200,35,mint);
            Label(content,title,68,0,-80,1280,180,Color.white);
            Label(content,subtitle,22,4,-260,720,85,muted);
            Box(content,"Separador",0,-355,1320,1,new Color(.2f,.29f,.31f),new Vector2(0,1));
            Label(content,"DISASTER READY   /   HUANCAYO, PERÚ",13,0,-725,850,25,muted);
            return content;
        }
        void Button(Transform parent,string text,float x,float y,UnityEngine.Events.UnityAction action,bool primary=false)
        {
            var image=Box(parent,text,x,y,340,58,primary?mint:new Color(.10f,.16f,.19f),new Vector2(0,1));
            image.raycastTarget=true; var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image;
            var colors=b.colors; colors.highlightedColor=new Color(.8f,.95f,.9f); colors.pressedColor=new Color(.6f,.8f,.75f); b.colors=colors;
            b.onClick.AddListener(action);
            Label(image.transform,text,20,20,-10,300,38,primary?ink:Color.white,TextAnchor.MiddleLeft);
        }
        void MainMenu()
        {
            var p=Panel("SIMULACIÓN EDUCATIVA  /  01","La calma también\nse prepara.","Explora tu hogar. Elige lo esencial.\nAprende a actuar antes, durante y después de un sismo.");
            Button(p,"Comenzar simulacro  →",0,-395,Game.Play,true);
            Button(p,"Cómo jugar",0,-470,Help); Button(p,"Créditos",0,-545,Credits); Button(p,"Salir",0,-620,Quit);
            string[] numbers={"01","02","03"}; string[] titles={"PREPARA","PROTÉGETE","EVACÚA"};
            string[] descriptions={"Organiza los suministros esenciales.","Agáchate, cúbrete y sujétate.","Sigue la ruta al punto de reunión."};
            for(int i=0;i<3;i++)
            {
                float y=-400-i*95; Label(p,numbers[i],32,700,y,80,55,mint);
                Label(p,titles[i],19,800,y,480,30,Color.white);
                Label(p,descriptions[i],17,800,y-34,480,48,muted);
            }
        }
        void Help()
        {
            var p=Panel("ANTES DE COMENZAR","Decisiones que importan.","Una experiencia de práctica. Identifica zonas seguras reales con asesoramiento competente.");
            Label(p,"CONTROLES\n\nWASD   Caminar\nRATÓN   Mirar\nCTRL + H   Agacharte y sujetarte\nE   Recoger / interactuar\nF   Linterna   ·   ESC   Pausa",21,0,-385,600,265,Color.white);
            Label(p,"TU MISIÓN\n\nPrepara la mochila antes del movimiento.\nBajo una mesa firme, mantén Ctrl + H sin moverte.\nTras el sismo, baja por la escalera y sal por la puerta.\nDurante una réplica, vuelve a protegerte.\nNo uses las ventanas como salida.",21,680,-385,630,265,muted);
            Button(p,"Volver",0,-660,MainMenu,true);
        }
        void Credits()
        {
            var p=Panel("EL EQUIPO","Aprender para actuar.","Proyecto universitario de preparación ante sismos.");
            Label(p,"KEEN   /   Dirección y Core\nRICARDO   /   Jugador e interacción\nJOSHUA   /   Vivienda y entorno\nABRAHAM   /   Interfaz, audio y QA",24,0,-390,670,230,Color.white);
            Label(p,"ODS 11 · Comunidades resilientes\nODS 13 · Educación y preparación\n\nLos sismos tienen origen geológico.\nEste simulador no evalúa la seguridad de una vivienda real.",21,720,-390,590,230,muted);
            Button(p,"Volver",0,-660,MainMenu,true);
        }
        void State(GameState state)
        {
            ClearPanel();
            if(state==GameState.Menu) MainMenu();
            if(state==GameState.Result)
            {
                var p=Panel("RESUMEN DEL SIMULACRO",Game.Won?"Llegaste a un lugar seguro.":"Cada intento enseña.",$"CALIFICACIÓN {Game.Grade}     /     {Game.Score} PUNTOS     /     SALUD {Game.Health:0}");
                var viewport=Box(p,"Resultados",0,-385,1300,230,new Color(.07f,.11f,.14f),new Vector2(0,1));
                viewport.raycastTarget=true; viewport.gameObject.AddComponent<RectMask2D>();
                var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport.rectTransform; scroll.horizontal=false;
                scroll.movementType=ScrollRect.MovementType.Clamped;
                var t=Label(viewport.transform,string.Join("\n\n",Game.Notes.Select(n=>"• "+n)),20,20,-12,1250,230,Color.white);
                t.rectTransform.sizeDelta=new Vector2(1250,Mathf.Max(230,t.preferredHeight+24)); scroll.content=t.rectTransform;
                Button(p,"Volver a intentarlo",0,-650,Game.Play,true); Button(p,"Menú principal",370,-650,Game.Menu);
            }
        }
        void Pause()
        {
            var p=Panel("UN MOMENTO DE CALMA","Simulacro en pausa.","Continúa cuando estés listo.");
            Button(p,"Continuar",0,-410,()=>{Game.SetPause(false);ClearPanel();},true);
            Button(p,"Reiniciar",0,-490,Game.Play); Button(p,"Menú principal",0,-570,Game.Menu);
        }
        void Message(string value) { toast.text=value; messageUntil=Time.unscaledTime+4; }
        void Update()
        {
            if(Game==null) return;
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame && Game.Playing)
            { Game.SetPause(!Game.Paused); if(Game.Paused) Pause(); else ClearPanel(); }
            headsUp.SetActive(Game.Playing&&!Game.Paused);
            if(Time.unscaledTime>messageUntil) toast.text="";
            if(!Game.Playing || Game.Paused) return;
            phase.text=Game.State switch {GameState.Preparation=>"01 / PREPARACIÓN",GameState.Alarm=>"AVISO DEL SIMULACRO",GameState.Earthquake=>"02 / SISMO",GameState.Evacuation=>Game.Shaking?"RÉPLICA":"03 / EVACUACIÓN",_=>""};
            objective.text=Game.State switch {GameState.Preparation=>"Elige los suministros esenciales.",GameState.Alarm=>"Acércate a la mesa firme.",GameState.Earthquake=>"Agáchate, cúbrete y sujétate.",_=>Game.Shaking?"Protégete hasta que termine.":"Escalera → puerta → punto exterior."};
            int seconds=Mathf.CeilToInt(Game.Remaining); clock.text=$"{seconds/60:00}:{seconds%60:00}";
            clock.color=seconds<=10?new Color(1,.6f,.4f):Color.white;
            status.text=$"SALUD {Game.Health:0}    /    {Game.Score} PTS";
            healthBar.rectTransform.sizeDelta=new Vector2(224*Mathf.Clamp01(Game.Health/100),5);
            var backpack=Game.Player?.Backpack;
            bag.text=backpack==null?"MOCHILA\nEsperando jugador":$"MOCHILA   {backpack.Used} / {backpack.Capacity}\n"+string.Join(", ",backpack.Items.Select(i=>i.Name));
            hint.text=string.IsNullOrEmpty(Game.InteractionHint)?"E  INTERACTUAR     ·     ESC  PAUSA":Game.InteractionHint;
            safe.text=Game.Shaking?(Game.Protected?"●  ESTÁS PROTEGIDO":"CTRL + H  ·  BUSCA PROTECCIÓN"):"";
        }
        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void OnDestroy() { GameEvents.OnStateChanged-=State; GameEvents.OnMessage-=Message; }
    }
}
