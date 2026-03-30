# Mejoras pendientes — Comportamiento de agentes guardias

Este documento recoge los bugs detectados y las mejoras de comportamiento planificadas para los agentes guardias del proyecto JungleBook-SMA. Los elementos tachados ya están implementados.

---

## Principio de diseño

La arquitectura sigue el flujo:

```
Sensor → Cerebro (SubsumptionController) → Comportamiento (GuardBehavior)
```

- Los **sensores** (`GuardVision`, `GuardHearing`, `SensorHogueraIndividual`) solo detectan y exponen datos. No toman decisiones.
- El **cerebro** (`SubsumptionController`) lee esos datos, actualiza su estado interno y lo propaga a los comportamientos.
- Los **comportamientos** (`GuardBehavior`) reciben la información ya procesada y solo deciden si activarse (`CanActivate()`) y qué acción ejecutar (`Action()`).

Cualquier cambio que se haga debe respetar estrictamente este flujo. Los comportamientos no deben leer sensores directamente ni modificar estado del cerebro salvo a través de métodos de notificación explícitos (como ya existe `NotificarInvestigacionRuidoCompletada()`).

---

## Bugs conocidos

### Bug 1 — Falsa alarma de hoguera por oclusión de geometría propia

**Fichero afectado:** `Assets/Scripts/Agentes/Sensores/SensorHogueraIndividual.cs` y `Assets/Scripts/Agentes/Bases/SubsumptionController.cs`

**Descripción del problema:**

`SensorHogueraIndividual.ComprobarHogueraConRaycast()` lanza un raycast desde la posición del agente hacia `posicionHogueraConocida`. Este raycast colisiona con la LayerMask `capaObstaculos | capaHoguera`. El problema es que la geometría propia de la hoguera (los objetos con nombre "Palos", que forman la estructura de madera de la hoguera) están en esa capa de obstáculos. Cuando el agente está cerca de la hoguera (y por tanto más cerca de los "Palos"), el raycast impacta primero en un "Palo" en vez de en el objeto `FuegoHoguera`, con lo que `hogueraDetectada = false` aunque el fuego siga ahí.

El cerebro (`SubsumptionController.EvaluarEstadoHoguera()`) cuenta los frames consecutivos en que `hogueraDetectada = false` mientras `hogueraEnCampoDeVision = true`. Al alcanzar el umbral `framesParaAlarmaHoguera`, activa `alarmaHogueraActiva = true`, lo que es irreversible y lanza al agente a bloquear la puerta.

**Condición exacta de disparo confirmada en consola:**

El falso positivo se produce específicamente mientras el agente está en `Persecucion` (`veAlLadron = true`, `ladronTieneFuego = false`). Al correr hacia el ladrón cerca de la hoguera, el raycast oscila entre detectar y no detectar el fuego por la geometría de los "Palos". Cuando acumula `framesParaAlarmaHoguera` frames sin verla, la alarma se dispara aunque el ladrón esté a la vista y sin el fuego — lo cual es físicamente imposible. La consecuencia es que cuando el agente pierde al ladrón a continuación, en lugar de iniciar `Busqueda → ComprobarHoguera`, salta directamente a `BloquearSalida` y no vuelve a buscar nunca.

**Solución propuesta:**

En `SubsumptionController.EvaluarEstadoHoguera()`, añadir una guarda antes de incrementar el contador: **si el ladrón es visible en este frame y no tiene el fuego** (`veAlLadron && !ladronTieneFuego`), no incrementar. Es imposible que la hoguera haya sido robada si el ladrón está a la vista sin el fuego, así que cualquier fallo del raycast en ese contexto es garantizadamente un falso positivo.

Adicionalmente, en `SensorHogueraIndividual`, se puede ajustar la LayerMask del raycast para que los "Palos" estén en su propia layer (por ejemplo `PropiedadHoguera`) separada de los obstáculos reales del mundo. El raycast entonces usaría solo `capaObstaculos` (paredes, edificios) ignorando la estructura interna de la hoguera, y la capa `capaHoguera` para detectar el fuego directamente.

---

### Bug 2 — `cronometroLimiteBusqueda` no se inicializa en `Start()`, causando búsqueda inmediatamente agotada

**Fichero afectado:** `Assets/Scripts/Agentes/Bases/SubsumptionController.cs`

**Descripción del problema:**

El campo `cronometroLimiteBusqueda` se declara como `private float cronometroLimiteBusqueda = 0f` y nunca se asigna en `Start()` ni en `InicializarComponentes()`. `ResetearBusqueda()` sí lo inicializa correctamente (`cronometroLimiteBusqueda = busquedaCache.tiempoLimiteBusqueda`), pero ese método solo se llama cuando ya hay un ciclo activo.

El efecto es que la primera vez que el agente entra en alerta sin ver al ladrón (por ejemplo, al escuchar un ruido), `AvanzarCronometroBusquedaLimitada()` decrementa desde 0 a negativo en el primer frame y llama a `MarcarBusquedaAgotada()` inmediatamente. El comportamiento `Busqueda` queda bloqueado antes de haber ejecutado un solo frame, y el agente salta directamente a `ComprobarHoguera` o `InvestigarSonido` sin haber buscado en ningún sitio.

Confirmado en consola: aparece `Tiempo de búsqueda agotado` justo después de `OÍDO: ha escuchado algo`, sin ningún frame de búsqueda entre medias.

**Solución propuesta:**

En `InicializarComponentes()` (o al final de `Start()`), inicializar el cronómetro:

```csharp
if (busquedaCache != null)
    cronometroLimiteBusqueda = busquedaCache.tiempoLimiteBusqueda;
```

---

## Mejoras de comportamiento

### Mejora 1 — Nuevo comportamiento: `BusquedaActiva`

**Motivación:**

Actualmente, cuando el agente completa el ciclo `Busqueda → ComprobarHoguera` y encuentra la hoguera intacta, el cerebro llama a `ResetearBusqueda()` y reinicia el ciclo. Como `enAlerta` sigue siendo `true` y `posicionLadron` tiene valor, `Busqueda.CanActivate()` vuelve a devolver `true` y el agente repite la búsqueda en la misma zona. Pero si el `cronometroBusqueda` ha caído a 0 y `enAlerta` ya no está activo, el agente cae a `Patrulla`, que lo hace ignorar completamente al ladrón y rondar plácidamente como si nada. Esto es comportamiento incorrecto: una vez que el agente ha visto al ladrón, no tiene sentido que vuelva a la patrulla tranquila.

**Descripción del nuevo comportamiento:**

`BusquedaActiva` es un `GuardBehavior` nuevo que reemplaza a `Patrulla` como comportamiento de "fondo" una vez el agente ha tenido contacto con el ladrón. Su lógica es la siguiente:

- Elige un punto aleatorio en un radio grande (configurable, por ejemplo 20-30 unidades) alrededor de la posición del agente.
- Navega hasta ese punto.
- Cuando llega, navega de vuelta a la posición de la hoguera para hacer un "check visual" pasando por ella.
- Cuando llega a la hoguera, elige un nuevo punto aleatorio y repite el ciclo indefinidamente.

Este comportamiento actúa como una patrulla activa e inteligente: el agente nunca para, cubre zona, y siempre pasa por la hoguera periódicamente para verificarla. Se interrumpe en cuanto cualquier comportamiento de mayor prioridad se activa (Persecución, Búsqueda normal, ComprobarHoguera, BloquearSalida).

**Condición de activación (`CanActivate()`):**

```
haVistoAlLadronAlgunaVez == true
&& !alarmaHogueraActiva
&& !veAlLadron
&& !enAlerta  (o cronometroBusqueda == 0)
```

Es decir: se activa cuando el agente ya tuvo contacto con el ladrón en algún momento de la partida, pero ahora mismo no hay ninguna alerta activa ni ve al ladrón.

**Prioridad en el sistema de subsumption:**

Debe tener prioridad mayor que `Patrulla` pero menor que todos los demás comportamientos:

```
Persecucion (1)        ← máxima prioridad
BloquearSalida (2)
InvestigarSonido (3)
Busqueda (4)
ComprobarHoguera (5)
BusquedaActiva (6)     ← nueva
Patrulla (7)           ← mínima prioridad (ya casi nunca se activa)
```

---

### Mejora 2 — Flag `haVistoAlLadronAlgunaVez` en `SubsumptionController`

**Motivación:**

Para que `BusquedaActiva` sepa cuándo activarse, el cerebro necesita recordar si en algún momento de la partida el agente vio al ladrón. Este dato no se puede derivar de los sensores en el frame actual (el ladrón puede estar fuera del campo visual).

**Implementación:**

Añadir en `SubsumptionController`:
- Un campo privado `bool haVistoAlLadronAlgunaVez = false`.
- En `ActualizarEstadoAlerta()` o en `LeerSensores()`, cuando `veAlLadron == true`, poner `haVistoAlLadronAlgunaVez = true`. Una vez activado, nunca se resetea.
- Propagar este flag a los comportamientos a través de `RecibirInformacion()`, igual que el resto de campos del estado del cerebro. Esto implica añadir el parámetro `bool haVistoAlLadronAlgunaVez` tanto en `GuardBehavior.RecibirInformacion()` como en `GuardBehavior` como campo protegido.

**Impacto en `Patrulla`:**

La condición de activación de `Patrulla` debe añadir `!haVistoAlLadronAlgunaVez`. De esta forma, si el agente ya vio al ladrón, `Patrulla` nunca vuelve a activarse y es `BusquedaActiva` quien cubre ese rol.

---

### Mejora 3 — Notificación desde `ComprobarHoguera` al cerebro

**Motivación:**

Actualmente, cuando `ComprobarHoguera` llega a la hoguera y la encuentra intacta, llama directamente a `controller.ResetearBusqueda()` y pone `haComprobado = false`. Esto hace que el ciclo se reinicie, pero el cerebro no sabe explícitamente que la comprobación ha terminado con resultado "hoguera presente". Cuando se implemente `BusquedaActiva`, el cerebro necesita saber este resultado para decidir si pasar a `BusquedaActiva` (hoguera OK) o a `BloquearSalida` (hoguera robada).

**Implementación:**

Añadir en `SubsumptionController` un método público:

```
public void NotificarComprobacionHogueraCompletada(bool hogueraPresente)
```

`ComprobarHoguera.ProcesarLlegadaAHoguera()` llamará a este método en lugar de llamar directamente a `ResetearBusqueda()`.

Dentro de `NotificarComprobacionHogueraCompletada()`:
- Si `hogueraPresente == true`: llamar a `ResetearBusqueda()` y `ResetearComprobacion()`. El agente pasará a `BusquedaActiva` porque `haVistoAlLadronAlgunaVez == true` y no hay alerta.
- Si `hogueraPresente == false`: activar `alarmaHogueraActiva = true`. El agente pasará a `BloquearSalida`.

Nota: el comportamiento `ComprobarHoguera` no decide qué ocurre después, solo informa al cerebro. El cerebro decide. Esto respeta estrictamente el flujo Sensor → Cerebro → Comportamiento.

---

### ~~Mejora 4 — Corrección de spam en `BloquearSalida`~~ ✅ YA IMPLEMENTADA

`BloquearSalida` ya tiene el flag `yaLogueado` que limita el `Debug.Log` a una única emisión. No requiere cambios.

---

## Resumen de ficheros a crear o modificar

| Fichero | Tipo de cambio |
|---|---|
| `Assets/Scripts/Agentes/Bases/SubsumptionController.cs` | **Bug 1**: guarda en `EvaluarEstadoHoguera()` para `veAlLadron && !ladronTieneFuego`; **Bug 2**: inicializar `cronometroLimiteBusqueda` en `Start()`; añadir `haVistoAlLadronAlgunaVez`, `NotificarComprobacionHogueraCompletada()`, propagación de nuevos campos |
| `Assets/Scripts/Agentes/Bases/GuardBehavior.cs` | Añadir campo protegido `haVistoAlLadronAlgunaVez`; ampliar firma de `RecibirInformacion()` |
| `Assets/Scripts/Agentes/Estados/Patrulla.cs` | `CanActivate()`: añadir `&& !haVistoAlLadronAlgunaVez` |
| `Assets/Scripts/Agentes/Estados/ComprobarHoguera.cs` | `ProcesarLlegadaAHoguera()`: sustituir lógica directa por llamada a `NotificarComprobacionHogueraCompletada()` |
| `Assets/Scripts/Agentes/Estados/BloquearSalida.cs` | ~~Eliminar `Debug.Log` por frame~~ ✅ Ya hecho |
| `Assets/Scripts/Agentes/Estados/BusquedaActiva.cs` | **Fichero nuevo** — comportamiento del bucle punto aleatorio ↔ hoguera |

---

## Flujo de comportamiento completo tras las mejoras

```
[Sin incidentes — agente nunca ha visto al ladrón]
Patrulla (ronda alrededor de la hoguera)

[Ve o escucha al ladrón por primera vez → haVistoAlLadronAlgunaVez = true]
↓
Si lo ve:   Persecucion
Si lo oye:  InvestigarSonido → si llega a la zona, puede ver al ladrón → Persecucion

[Pierde al ladrón SIN fuego]
↓
Busqueda (va a última posición conocida, ~10 segundos)
↓
ComprobarHoguera (corre a la hoguera a verificar)
  ├── Hoguera presente → NotificarComprobacionHogueraCompletada(true) → BusquedaActiva
  └── Hoguera ausente → NotificarComprobacionHogueraCompletada(false) → BloquearSalida

[En BusquedaActiva — bucle indefinido]
  Punto aleatorio (radio grande) → vuelve a hoguera → punto aleatorio → ...
  Se interrumpe si ve/oye al ladrón (Persecucion/InvestigarSonido toman prioridad)
  Se interrumpe si la hoguera desaparece (ComprobarHoguera → BloquearSalida)

[Ve al ladrón CON fuego y lo pierde → ladronPerdidoConFuego = true]
↓
Persecucion sigue activa → va a la última posición conocida (velocidad elevada)
  ├── Lo vuelve a ver por el camino → Persecucion normal (ladronPerdidoConFuego se resetea)
  └── Llega y no está → NotificarLlegadaAUltimaPosicionConFuego() → Busqueda local
        ├── Lo encuentra → Persecucion
        └── Búsqueda agotada (busquedaEsPorFuego = true) → alarmaHogueraActiva = true → BloquearSalida
```
