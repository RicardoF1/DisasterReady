# Disaster Ready

Simulación educativa 3D en primera persona sobre cómo actuar ante un sismo, en una vivienda
familiar de dos pisos en Huancayo. ODS 11 (meta 11.5) y ODS 13 (meta 13.1).

- Motor: **Unity 6000.6.3f1** (URP), Input System, uGUI. Todos deben usar esta versión.
- Escenas: `MainMenu` y `House` (ya están en Build Settings).
- Todo el contenido (casa, muebles, interfaz, efectos) se construye por código al dar Play,
  así las escenas casi no cambian y no hay conflictos de Git en los `.unity`.

## Cómo abrir

1. Instalar Unity 6000.6.3f1 desde Unity Hub.
2. Unity Hub › Add › Add project from disk › carpeta `DisasterReady`.
3. Esperar a que importe (la primera vez tarda unos minutos).
4. Abrir `Assets/_Project/Scenes/MainMenu.unity` y pulsar **Play**.

## Cómo se juega

| Fase | Qué hacer |
|---|---|
| 1 · Preparación (90 s) | Recorre los dos pisos y arma la **mochila de emergencia** (12 espacios). Los útiles no caben todos: hay que priorizar. Laptop, consola y trofeo restan puntos. |
| Alerta sísmica (6 s) | Suena la alarma: corre a una zona segura. |
| 2 · Sismo (25 s) | **Agáchate** (C) bajo una mesa firme o junto a una columna con señal ZONA SEGURA, **cúbrete** y **sujétate** (mantén H). Lejos de ventanas, estantes y roperos. |
| 3 · Evacuación (90 s) | Cierra el **balón de gas** (cocina), baja la **llave de luz** (hall), sigue las señales de SALIDA y llega al **punto de reunión**. Hay una ruta bloqueada por escombros y una **réplica**. |
| Resultado | Nota **A / B / C**, resumen de decisiones y consejos. |

**Controles:** WASD mover · Mouse mirar · Shift correr · C / Ctrl agacharse · H sujetarse ·
E recoger / usar · Q sacar el último objeto de la mochila · F linterna · Esc pausa.

## Estructura del código (namespace `DisasterReady`)

| Carpeta | Responsable | Contenido |
|---|---|---|
| `Scripts/Core` | Keen | Fases, tiempo, puntaje, salud, mochila, zonas, eventos, ajustes |
| `Scripts/Player` | Ricardo | Movimiento, agacharse/sujetarse, interacción (E/Q), linterna |
| `Scripts/Level` | Joshua | Casa de 2 pisos, mobiliario, objetos, zonas, exterior y efectos del sismo |
| `Scripts/UI` | Abraham | Menú, HUD, pausa, resultados, post-procesado |
| `Scripts/Audio` + `Audio/Resources` | Abraham | Música, ambientes y efectos |

Reglas: Player, Level, UI y Audio **solo dependen de Core** y se comunican con `GameEvents`.
Cada sistema se instala solo con `SceneBoot.Register(escena, crear)`.

## Cambiar valores o contenido

- Duraciones, puntajes y notas: `Scripts/Core/GameConfig.cs`.
- Suministros (nombre, espacios, puntos, consejo): `Scripts/Core/ItemCatalog.cs`.
- Plano de la casa, puertas, ventanas y ruta: `Scripts/Level/HouseLayout.cs`.
- Muebles y ubicación de los objetos: `Scripts/Level/Furnishing.cs`.
- Sonidos: reemplazar el `.wav` en `Assets/_Project/Audio/Resources/Audio/` con el **mismo nombre**.
- Modelos 3D: cambiar `Scripts/Level/ItemModels.cs` o los métodos de `Furnishing.cs` por modelos de Blender.

## Notas

- Los `.wav` usan Git LFS (`.gitattributes`). Instalar Git LFS una vez: `git lfs install`.
- Si se hace un build de Windows y el vidrio se ve opaco, agregar en
  *Project Settings › Graphics › Always Included Shaders* el shader `Universal Render Pipeline/Lit`.
- Los sonidos y la música se generaron con `Tools/generar_audio.py` (Python + numpy + scipy):
  `python Tools/generar_audio.py Assets/_Project/Audio/Resources/Audio`. Se pueden reemplazar por clips de Freesound o Pixabay editados en Audacity.
