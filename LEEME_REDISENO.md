# Disaster Ready — rediseño visual

## Instalación
Descomprime en una carpeta nueva. En Unity Hub agrega la carpeta DisasterReady y abre con Unity 6000.6.3f1. Abre Assets/_Project/Scenes/MainMenu.unity y pulsa Play. No sobrescribas tu repositorio sin revisar los cambios.

## Qué cambió
- Menú editorial oscuro con acentos verdes, instrucciones, créditos, pausa y resultados desplazables.
- HUD con tarjetas separadas de objetivo, reloj, salud y mochila; indicación contextual al mirar objetos.
- Eliminados los nombres gigantes flotantes y los textos que se veían a través de paredes.
- Señales físicas de protección y salida; detalles de muebles, camas, alfombra, cocina, ventanas y cruce exterior.
- Suministros con detalles geométricos reconocibles (botella, radio, botiquín, linterna).
- Colores más neutros e iluminación ambiental equilibrada; materiales compartidos para reducir duplicación.
- Preparación predeterminada de 150 s y evacuación de 120 s. Un DisasterConfig existente en Resources tiene prioridad sobre estos valores.
- Se conservan Core, controles, fases, réplica, mochila y audio del prototipo; no es una sustitución por modelos fotorrealistas.

## Validación
Se verificó la sintaxis C# de los 12 scripts con un parser, la integridad del ZIP y referencias de escenas. No se pudo compilar, ejecutar ni inspeccionar visualmente en Unity en este entorno. Quedan pendientes las pruebas del editor y del ejecutable.

## Pruebas en Unity
- Menú, Cómo jugar y Créditos legibles en 16:9.
- Jugar crea una casa y un jugador; no quedan textos flotantes.
- WASD y ratón; Ctrl + H bajo mesa; E recoger; F linterna; Esc pausa.
- Pausa detiene el reloj; continuar recupera el cursor del jugador.
- Fases, réplica, llegada al punto exterior, derrota y reintento.
- Revisa Console antes de subir tus cambios.

## Archivos modificados para integración
Scripts/UI/GameUI.cs, Scripts/Level/HouseBuilder.cs, Scripts/Level/LevelContent.cs, Scripts/Core/GameConfig.cs. Incluye además las correcciones del ZIP anterior en Core/SceneBootstrap, Player/FirstPersonPlayer y EditorSettings. Conserva los .meta existentes.
