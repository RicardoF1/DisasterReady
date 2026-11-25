# Disaster Ready — paquete corregido para Unity 6000.6.3f1

## Abrir y probar
1. Descomprime en una carpeta nueva; no sobrescribas tu repositorio de trabajo.
2. En Unity Hub agrega la carpeta DisasterReady (contiene Assets, Packages y ProjectSettings).
3. Abre con Unity 6000.6.3f1 y espera la importación de paquetes.
4. Abre Assets/_Project/Scenes/MainMenu.unity y pulsa Play.
5. Comprueba Jugar, movimiento WASD, interacción E, pausa Esc, fases, evacuación y reinicio.

## Cambios
- SceneBootstrap: HashSet<ulong> y GetRawData(); libera el registro al descargar escenas.
- GameUI: FindAnyObjectByType en lugar de FindFirstObjectByType.
- FirstPersonPlayer y LevelContent: recorren componentes de la escena sin FindObjectsSortMode obsoleto.
- EditorSettings: recarga normal de dominio y escena al entrar en Play para reiniciar el estado.
- No se incluye el panel temporal de diagnóstico de GameManager.

## Alcance de validación
Revisión de los scripts, estructura, referencias GUID y archivo ZIP. No se dispone aquí del editor Unity para compilar o ejecutar el juego. Las pruebas de ejecución quedan pendientes.
El mensaje «Layout update failed to stabilize after 10 iterations» no se pudo reproducir ni diagnosticar sin la traza completa del editor. No se considera corregido ni se oculta.
Si persiste en una apertura limpia, guarda, reinicia el editor y proporciona la traza completa del mensaje.

Este paquete completo incluye Core, Player, Level, UI y Audio. Para contribuir por ramas, copia solo los archivos acordados y conserva los .meta. Las correcciones de Core y EditorSettings deben coordinarse con el encargado de integración.
