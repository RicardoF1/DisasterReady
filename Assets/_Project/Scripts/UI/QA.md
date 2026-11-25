# QA manual — Abraham

Estado inicial: PENDIENTE. Anotar fecha, equipo, versión y evidencia por caso.
No marcar aprobado sin ejecutar la prueba en Unity o en el build indicado.

| Caso | Procedimiento | Resultado esperado |
|---|---|---|
| Importación | Abrir en Unity 6000.6.3f1 | Console sin errores de compilación |
| Arranque repetido | Play/Stop cinco veces con ambas recargas desactivadas | Una raíz, un jugador, un Canvas; puntaje 0, salud 100, mochila vacía |
| Menú | Usar todos los botones | Ayuda y créditos visibles; Salir finaliza Play/editor o ejecutable |
| House directa | Abrir House y Play | Casa, jugador, HUD y audio aparecen |
| Preparación | Recorrer los dos pisos | Escalera transitable en ambos sentidos; jugador comienza arriba |
| Mesa | Entrar con Ctrl y soltar Ctrl bajo tablero | Cabe agachado; no se levanta atravesando la mesa |
| Protección | Mantener Ctrl+H bajo mesa sin moverse | PROTEGIDO durante sismo/réplica; soltar H o salir produce EXPUESTO |
| Inventario | Recoger los nueve suministros | 12/12 espacios y 72 puntos con configuración predeterminada |
| Capacidad | Recoger primero laptop y luego suministros | Laptop resta 12; se rechaza lo que no cabe sin consumirlo |
| Unicidad | Pulsar E repetidamente sobre un recogible | Una sola incorporación y una sola recompensa |
| Oclusión | Intentar interactuar a través de una pared | No se recoge el objeto oculto |
| Fases | Esperar sin pausar | 90 s, 5 s, 24 s; después evacuación de 100 s |
| Alarma | Observar transición | Suena antes del sismo y se explica como simulacro |
| Peligros | Permanecer junto a ventana/mueble | Daño, caída de muebles, vidrios rotos y sonidos; no daño permanente de muebles inmóviles |
| Ventanas | Intentar usar con E varias veces | Penalización única de 15 puntos; consejo de salida segura |
| Carrera | Correr durante sismo | Movimiento dificultado y penalización de 3 puntos por segundo evaluado |
| Luz | Esperar sismo y pulsar F | Se apagan luces domésticas; linterna solo si fue recogida |
| Rutas | Evacuar con escombros activos | Atajo bloqueado, paso central y escalera este transitables |
| Suministros | Usar gas/tablero antes, durante y después | Solo se aceptan en evacuación sin temblor; una bonificación por dispositivo |
| Réplica | Permanecer en evacuación más de 8 s | Temblor de 6 s; protección vuelve a funcionar |
| Victoria | Llegar al cuadrado verde sin temblor | Resultado exitoso, puntaje, A/B/C y consejos desplazables |
| Derrota por tiempo | Esperar 100 s de evacuación | Derrota; no se confunde calificación con victoria |
| Derrota por salud | Acumular impactos/exposición | Salud llega a 0 y finaliza una sola vez |
| Pausa | Esc durante alarma/sismo/réplica | Tiempo, física, interacción y audio se detienen; cursor visible |
| Reintento | Reintentar desde pausa y resultado cinco veces | Estado inicial completo; sin suscripciones ni audio duplicados |
| Audio vacío | Probar sin clips externos | Se escucha respaldo; no hay excepciones |
| Audio externo | Añadir ambient.wav con ese nombre | Se utiliza el archivo importado |
| Resolución | 1280x720 y 1920x1080 | Texto legible; botones y resumen accesibles |
| Build Windows | Build and Run de MainMenu y House | Mismo flujo, entrada y audio; sin shader rosa |
| Integración Git | Abrir clon limpio tras todos los merges | Ninguna referencia ausente; escenas originales sin cambios |

Regresión mínima tras cada merge: importación, Play/Stop dos veces y función nueva.
Regresión final: todos los casos, incluyendo ejecutable Windows.
