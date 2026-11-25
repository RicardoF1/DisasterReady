# Disaster Ready

Prototipo educativo 3D para Windows, ambientado en una vivienda ficticia de dos pisos en Huancayo.
Unity 6000.6.3f1, URP 17.6, Input System 1.20 y uGUI 2.6.

## Arranque

1. Abrir este proyecto desde Unity Hub con la versión indicada.
2. Esperar a que se importen los scripts y comprobar Console.
3. Abrir Assets/_Project/Scenes/MainMenu.unity y pulsar Play.
4. Pulsar Jugar. No agregar componentes manualmente a las escenas.

Las escenas MainMenu y House deben permanecer habilitadas en Build Profiles.
El proyecto usa exclusivamente el nuevo Input System. No requiere TMP ni archivos de audio.
Los objetos se generan durante Play; no guardar esos objetos como cambios de escena.

## Controles

WASD: caminar. Ratón: mirar. Shift: correr. Ctrl: agacharse.
H: sujetarse estando quieto y agachado en una zona marcada.
E: interactuar. F: encender la linterna recogida. Esc: pausa.
No hay salto: las escaleras se recorren caminando.

## Arquitectura

Todos los contratos viven en Core, namespace DisasterReady.
Cada módulo registra su instalación en BeforeSceneLoad.
Core ejecuta los instaladores por orden y construye una sola raíz por escena.
SubsystemRegistration limpia las referencias estáticas y los eventos.
Level: prioridad 10 y 20. Player: 30 y 40. UI: 50. Audio: 60.
UI y Audio no conocen clases concretas de Player o Level.

## Configuración opcional

Create > Disaster Ready > Configuración permite crear un GameConfig.
Para cargarlo automáticamente guardarlo como:
Assets/_Project/Scripts/Core/Resources/DisasterConfig.asset
Sin ese archivo se utilizan valores de respaldo en memoria, sin modificar assets.

## Audio opcional

Colocar clips bajo Assets/_Project/Audio/Resources con estos nombres base:
ambient, alarm, quake, calm, pickup, impact, glass, switch.
Se admiten WAV/OGG importados por Unity; no incluir extensión en Resources.Load.
Sin clips, AudioClip.Create genera sonidos de respaldo.
Registrar autor, URL, licencia y modificaciones de cada audio externo.

## Reparto de commits

Keen 1: Contracts.cs y GameConfig.cs.
Keen 2: SceneBootstrap.cs, GameManager.cs y README.md.
Ricardo 1: FirstPersonPlayer.cs.
Ricardo 2: PlayerInteraction.cs.
Joshua 1: HouseBuilder.cs.
Joshua 2: LevelContent.cs, LevelInteractable.cs y FallingHazard.cs.
Abraham 1: GameUI.cs.
Abraham 2: AudioManager.cs y QA.md dentro de Scripts/UI.
Cada integrante incluye los archivos .meta de sus assets.

## Diseño educativo

Mochila: 12 espacios. Los nueve suministros útiles ocupan 12 en total.
Preparación: 90 s; alarma: 5 s; sismo: 24 s; evacuación: 100 s.
Réplica: comienza 8 s después del inicio de evacuación y dura 6 s.
La preparación y la alarma son partes de un simulacro, no predicción sísmica.
Las zonas estructurales son ficticias y se consideran previamente evaluadas.
Cerrar gas y electricidad solo se permite después del movimiento.
En una emergencia real, no operar interruptores si existe olor a gas.
No retrasar la salida ni exponerse para obtener una bonificación.

ODS 11.5: preparación para reducir consecuencias de desastres.
ODS 13.1: contribución educativa a la resiliencia.
Los sismos son geológicos; no los causa el cambio climático.
Referencias: https://www.gob.pe/1053-que-hacer-en-caso-de-sismo
https://www.gob.pe/institucion/indeci/noticias/576089-indeci-recomienda-medidas-de-preparacion-ante-sismos

## Validación

Consultar Assets/_Project/Scripts/UI/QA.md.
La revisión de generación no ejecutó Unity: compilar, probar Play y generar
un build Windows en el editor antes de considerar aprobada la entrega.
