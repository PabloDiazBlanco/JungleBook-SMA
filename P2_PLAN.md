# P2 — Plan de Implementación: Arquitectura Híbrida con SMA

## Índice
1. [Qué hay ahora y qué hay que añadir](#1-qué-hay-ahora-y-qué-hay-que-añadir)
2. [Visión general de la arquitectura híbrida](#2-visión-general-de-la-arquitectura-híbrida)
3. [Pieza 1 — Sistema de mensajería FIPA-ACL](#3-pieza-1--sistema-de-mensajería-fipa-acl)
4. [Pieza 2 — Base de Creencias (BeliefBase)](#4-pieza-2--base-de-creencias-beliefbase)
5. [Pieza 3 — Capa Deliberativa](#5-pieza-3--capa-deliberativa)
6. [Integración en SubsumptionController](#6-integración-en-subsumptioncontroller)
7. [Comportamientos grupales](#7-comportamientos-grupales)
8. [Flujo completo de un frame](#8-flujo-completo-de-un-frame)
9. [Ficheros a crear](#9-ficheros-a-crear)
10. [Orden de implementación](#10-orden-de-implementación)

---

## 1. Qué hay ahora y qué hay que añadir

### Lo que existe (no se toca)
- **7 capas de subsunción** por guardia: `BloquearSalida`, `InverstigarSonido`, `InvestigarEntorno`, `Persecucion`, `ComprobarHoguera`, `Busqueda`, `Patrulla`
- **4 sensores** por guardia: `GuardVision`, `GuardHearing`, `SensorHogueraIndividual`, `SensorPercepcionObjetos`
- **`SubsumptionController`**: orquesta sensores → estado → subsunción. Tiene el `Update()` principal.
- **`GuardBehavior`**: clase base abstracta de todas las capas. Recibe información mediante `RecibirInformacion()`.
- El estado interno del controlador: `veAlLadron`, `ultimaPosicionLadron`, `enAlerta`, `alarmaHogueraActiva`, `ladronTieneFuego`, `ladronPerdidoConFuego`, etc.

### El problema fundamental
Los 3 guardias son completamente ciegos el uno al otro. Cada uno actúa como si fuera el único agente del mundo. No comparten información, no se coordinan, no saben dónde están los demás.

### Lo que hay que añadir
Tres piezas nuevas que se montan **encima** de lo existente:
1. **Sistema de mensajería FIPA-ACL** — la infraestructura de comunicación
2. **Base de Creencias (BeliefBase)** — lo que cada agente cree sobre el mundo (sensores propios + mensajes recibidos)
3. **Capa Deliberativa** — razona sobre las creencias y decide qué hacer con esa información y qué mensajes enviar

---

## 2. Visión general de la arquitectura híbrida

```
┌──────────────────────────────────────────────────────┐
│                CAPA DELIBERATIVA                      │
│  Lee inbox → actualiza BeliefBase → toma decisiones  │
│  de coordinación → envía mensajes FIPA-ACL            │
├──────────────────────────────────────────────────────┤
│              BASE DE CREENCIAS (BeliefBase)           │
│  Sensores propios + mensajes recibidos fusionados     │
├──────────────────────────────────────────────────────┤
│         COMUNICACIÓN FIPA-ACL (AgentCommunicator)    │
│  Inbox / Outbox / Historial de conversaciones        │
├──────────────────────────────────────────────────────┤
│         SUBSUNCIÓN REACTIVA (sin cambios)             │
│  BloquearSalida > InvSonido > ... > Patrulla          │
├──────────────────────────────────────────────────────┤
│              SENSORES (sin cambios)                   │
│  Vision / Oído / Hoguera / Objetos                    │
└──────────────────────────────────────────────────────┘
```

La clave del diseño: la capa deliberativa **no sustituye** a la subsunción. Lo que hace es enriquecer los inputs que la subsunción ya consume. Cuando un guardia recibe un mensaje diciendo que el ladrón está en posición X, la deliberativa actualiza `ultimaPosicionLadron` y `enAlerta` en el controlador, exactamente igual que si el propio sensor hubiera detectado esa información. La subsunción reacciona de forma natural sin saber de dónde vino el dato.

---

## 3. Pieza 1 — Sistema de mensajería FIPA-ACL

### 3.1 Qué es FIPA-ACL

FIPA-ACL (Foundation for Intelligent Physical Agents — Agent Communication Language) es un estándar para que agentes se comuniquen. Define el formato de los mensajes y el vocabulario de **performativas** (el tipo de acto que representa el mensaje).

Un mensaje FIPA-ACL no es solo datos: es un **acto comunicativo**. No es lo mismo decir "el ladrón está aquí" (`INFORM`) que "¿el ladrón está ahí?" (`QUERY-IF`) que "¿alguien puede perseguir?" (`CFP`).

### 3.2 Performativas necesarias para este proyecto

| Performativa | Significado | Cuándo se usa aquí |
|---|---|---|
| `INFORM` | "Te comunico un hecho que creo verdadero" | Avisar posición del ladrón, avisar hoguera robada, informar posición propia |
| `CFP` (Call For Proposals) | "¿Alguien puede hacer X?" | Pedir que alguien cubra la hoguera mientras yo persigo |
| `PROPOSE` | "Yo puedo hacer X" | Responder a un CFP aceptando una tarea |
| `ACCEPT_PROPOSAL` | "Acepto tu propuesta" | Confirmar la asignación de rol |
| `REFUSE` | "No puedo hacer X" | Rechazar un CFP porque ya tengo tarea urgente |
| `INFORM` con contenido `FIRE_STOLEN` | Alarma de hoguera robada | Broadcast desde BloquearSalida |

### 3.3 Estructura de un mensaje FIPA-ACL

Cada mensaje tiene estos campos:

- **`performativa`** — tipo de acto (`INFORM`, `CFP`, `PROPOSE`, etc.)
- **`emisor`** — nombre del agente que envía (p.ej. `"Aldeano1"`)
- **`receptor`** — nombre del agente destino, o `"BROADCAST"` para todos
- **`contenido`** — string serializado con los datos (posición, tipo de alerta, etc.)
- **`conversationId`** — string que vincula mensajes de una misma negociación (p.ej. `"coord-persecucion-001"`)
- **`timestamp`** — momento en el que se envió (en segundos de juego, `Time.time`)
- **`inReplyTo`** — conversationId del mensaje al que responde (para hilos de conversación)

El contenido puede ser un JSON simple serializado a mano o con `JsonUtility`. Ejemplos:
- `INFORM thiefPosition`: `{"tipo":"AVISTAMIENTO","x":12.3,"y":0,"z":-5.1,"confianza":1.0}`
- `INFORM fireStolen`: `{"tipo":"ALARMA_HOGUERA"}`
- `CFP coordinator`: `{"tipo":"CFP_COBERTURA","posicionHoguera":{"x":0,"y":0,"z":0}}`
- `PROPOSE`: `{"tipo":"PROPUESTA_COBERTURA","distanciaHoguera":8.5}`

### 3.4 MessageBus — El bus de mensajes

Es un **singleton pasivo** en la escena. Solo tiene una responsabilidad: recibir mensajes y entregarlos al destinatario (o a todos si es broadcast).

No toma ninguna decisión. Es como una red física. Su comportamiento:

- Los agentes llaman a `MessageBus.Instance.Send(mensaje)` para enviar
- Cada frame, cada agente llama a `MessageBus.Instance.Receive(nombreAgente)` para recoger sus mensajes pendientes
- Internamente mantiene un `Dictionary<string, Queue<FIPAMessage>>` — una cola de mensajes por agente
- También guarda un historial de todas las conversaciones (requerimiento explícito del enunciado): `Dictionary<string, List<FIPAMessage>>` indexado por `conversationId`

**Por qué no es centralización**: el bus no sabe qué significan los mensajes, no decide quién hace qué, no tiene estado del juego. Es exactamente igual que un router de red o un servidor de correo. La inteligencia y las decisiones siguen siendo exclusivamente de cada agente individual.

### 3.5 AgentCommunicator — El componente por agente

Se añade como componente a cada guardia (en el mismo GameObject que `SubsumptionController`).

Responsabilidades:
- Tiene el **nombre único** del agente (p.ej. `"Aldeano1"`)
- Método `Send(FIPAMessage)` — deposita el mensaje en el MessageBus
- Método `List<FIPAMessage> Receive()` — recoge todos los mensajes pendientes del bus y los añade al inbox local
- Lista interna `inbox` de mensajes recibidos en el frame actual
- Método `GetConversationHistory(conversationId)` — devuelve todos los mensajes de una conversación
- Método `GetAllConversations()` — devuelve el historial completo (el bus lo tiene, el communicator hace de proxy)

El inbox se vacía al principio de cada frame, después de que la capa deliberativa lo haya procesado.

---

## 4. Pieza 2 — Base de Creencias (BeliefBase)

### 4.1 Qué es y para qué sirve

La BeliefBase es la memoria de trabajo de cada agente. Representa **lo que el agente cree que es verdad** sobre el mundo en este momento, combinando:
- Lo que sus propios sensores detectaron este frame
- Lo que otros agentes le comunicaron (con su timestamp, para saber si es reciente)

Es una clase de datos simple, sin lógica de decisión.

### 4.2 Campos que necesita

**Sobre el ladrón:**
- `posicionLadronCreida` — Vector3 de la mejor estimación de dónde está el ladrón
- `fuentePosicionLadron` — de dónde vino ese dato: `SENSOR_PROPIO` o `MENSAJE_RECIBIDO`
- `timestampPosicionLadron` — cuándo se recibió/detectó (para descartar datos viejos)
- `ladronTieneFuegoCreido` — bool
- `fuegoRobadoCreido` — bool (si cualquier agente activó alarma)
- `ladronVisto` — bool (si el propio sensor lo ve ahora mismo)

**Sobre los otros guardias:**
- `posicionesGuardias` — `Dictionary<string, (Vector3 posicion, float timestamp)>` — posición conocida de cada compañero
- `rolGuardias` — `Dictionary<string, string>` — rol que está ejecutando cada compañero: `"PERSIGUIENDO"`, `"CUBRIENDO_HOGUERA"`, `"PATRULLANDO"`, etc.

**Sobre la coordinación activa:**
- `hayConversacionActiva` — bool
- `conversacionActivaId` — string
- `rolActualPropio` — string: `"PERSIGUIENDO"`, `"CUBRIENDO_HOGUERA"`, `"LIBRE"`, etc.

### 4.3 Cómo se actualiza

La BeliefBase se actualiza en dos momentos cada frame:

1. **Después de leer sensores**: el `SubsumptionController` actualiza los campos de sensor propio en la BeliefBase (posición ladrón si se ve, ladronTieneFuego, etc.)
2. **Después de procesar mensajes**: la capa deliberativa interpreta los mensajes del inbox y actualiza los campos de creencia compartida

Los datos de mensajes tienen un **TTL (time-to-live)**: si un dato de mensaje tiene más de N segundos de antigüedad, se considera obsoleto y no se usa para tomar decisiones. Esto evita que un guardia actúe sobre información muy antigua.

---

## 5. Pieza 3 — Capa Deliberativa

### 5.1 Qué hace

Es el "cerebro social" del agente. Se ejecuta cada frame, antes de que la subsunción tome su decisión. Tiene dos responsabilidades:

**A) Procesar mensajes entrantes:**
- Lee el inbox del `AgentCommunicator`
- Para cada mensaje, interpreta su contenido y actualiza la `BeliefBase`
- Si el mensaje requiere respuesta (p.ej. un CFP), prepara esa respuesta

**B) Tomar decisiones de coordinación:**
- Mira el estado actual (BeliefBase + estado reactivo)
- Decide si debe enviar algún mensaje (avisar un avistamiento, proponer una tarea, responder a un CFP)
- Si recibió información útil de otro agente, la **inyecta en el estado del `SubsumptionController`** (esto es el puente entre las dos capas)

### 5.2 La inyección de creencias en la subsunción

Este es el mecanismo central del diseño híbrido. `SubsumptionController` expone métodos públicos para que la capa deliberativa pueda modificar su estado:

- `InyectarPosicionLadron(Vector3 pos)` → sobreescribe `ultimaPosicionLadron` y activa `enAlerta`
- `InyectarAlarmaHoguera()` → activa `alarmaHogueraActiva` (aunque el sensor propio no lo haya detectado)
- `InyectarAlerta()` → activa `enAlerta` y reinicia `cronometroBusqueda`

Con esto, un guardia que recibe un `INFORM` con la posición del ladrón actúa exactamente igual que si su propio sensor lo hubiera visto. La subsunción no distingue el origen del dato.

### 5.3 Ciclo de la capa deliberativa cada frame

```
1. Recoger mensajes del inbox (AgentCommunicator.Receive())
2. Para cada mensaje:
   a. Actualizar BeliefBase con la información del mensaje
   b. Si es INFORM de avistamiento → InyectarPosicionLadron() si el dato es más fresco que el propio
   c. Si es INFORM de alarma de hoguera → InyectarAlarmaHoguera()
   d. Si es CFP de cobertura → evaluar si puedo y debo responder, preparar PROPOSE o REFUSE
   e. Si es ACCEPT_PROPOSAL → comprometerse con el rol, actualizar rolActualPropio
3. Evaluar si debo enviar mensajes proactivos:
   a. ¿Acabo de ver al ladrón? → enviar INFORM de avistamiento
   b. ¿Acabo de activar BloquearSalida? → enviar INFORM de alarma
   c. ¿Acabo de perder al ladrón y estoy en búsqueda? → enviar CFP de cobertura de zonas
4. Limpiar inbox procesado
```

---

## 6. Integración en SubsumptionController

### 6.1 El nuevo Update()

El `Update()` de `SubsumptionController` queda así (los pasos en **negrita** son los nuevos):

```
1. LeerSensores()                          — existente
2. DetectarTransiciones()                  — existente
3. ActualizarEstadoAlerta()                — existente
4. GestionarResetsPorTransicion()          — existente
5. [avanzar cronómetros]                   — existente
6. EvaluarEstadoHoguera()                  — existente
7. RegistrarFrameAnterior()                — existente
8. **ActualizarBeliefBaseConSensores()**   — NUEVO: BeliefBase ← sensores propios
9. **capadeliberativa.Procesar()**         — NUEVO: inbox → BeliefBase → inyección → mensajes salientes
10. PropagarInformacionACapas()            — existente (ahora con datos potencialmente enriquecidos)
11. EjecutarDecision()                     — existente
```

El orden importa: primero los sensores propios alimentan el estado y la BeliefBase, luego la deliberativa procesa mensajes y enriquece ese estado, y solo entonces la subsunción toma su decisión con toda la información disponible.

### 6.2 Referencias necesarias en SubsumptionController

El controlador necesita referencias a los tres nuevos componentes:
- `AgentCommunicator communicator`
- `BeliefBase beliefs`
- `DeliberativeLayer deliberativa`

Y necesita los métodos de inyección públicos mencionados en 5.2.

### 6.3 Qué NO cambia

- Las 7 capas de subsunción: ni una línea
- La clase `GuardBehavior` y `RecibirInformacion()`: sin cambios
- Los 4 sensores: sin cambios
- Toda la lógica de cooldowns, timers, búsqueda rotatoria, anti-bucle: sin cambios

---

## 7. Comportamientos grupales

### 7.1 Compartir avistamientos (`INFORM` de posición)

**Disparador**: Un guardia pasa de no ver al ladrón a verlo (`acabaDeVerAlLadron == true`)

**Acción**: Envía `INFORM` broadcast con la posición del ladrón y si tiene fuego.

**Recepción**: Los demás guardias reciben el mensaje. Si la posición informada es más reciente que la que tienen, inyectan esa posición en su controlador → activan `enAlerta` aunque sus sensores no detecten nada.

**Efecto observable**: Los tres guardias convergen hacia el ladrón aunque solo uno lo haya visto. La coordinación emerge del broadcast individual, no de ningún coordinador.

**Protocolo FIPA**: mensaje único `INFORM`, sin respuesta esperada. Conversación de un solo mensaje.

---

### 7.2 Alarma de hoguera robada (`INFORM` de alarma)

**Disparador**: Un guardia activa `BloquearSalida` (es decir, `alarmaHogueraActiva` pasa a true).

**Acción**: Envía `INFORM` broadcast con contenido `FIRE_STOLEN`.

**Recepción**: Los demás llaman a `InyectarAlarmaHoguera()` en su controlador → activan `alarmaHogueraActiva` → todos entran en `BloquearSalida` de forma sincronizada.

**Efecto observable**: Actualmente los guardias detectan el robo de forma independiente y con retraso. Con este comportamiento, en cuanto uno lo detecta, los demás reaccionan en el siguiente frame.

**Protocolo FIPA**: mensaje único `INFORM`, sin respuesta esperada. El más simple de implementar.

---

### 7.3 Negociación perseguir / cubrir hoguera (`CFP` → `PROPOSE` → `ACCEPT_PROPOSAL`)

**Escenario**: Guardia A ve al ladrón con fuego (`ladronTieneFuego == true`). Sabe que necesita perseguir pero la hoguera queda desprotegida.

**Flujo completo**:

1. **Guardia A** genera un `conversationId` único y envía `CFP` broadcast con tipo `SOLICITAR_COBERTURA_HOGUERA` y la posición de la hoguera. Actualiza su `rolActualPropio = "PERSIGUIENDO"`.

2. **Guardias B y C** reciben el CFP. Cada uno evalúa de forma independiente:
   - ¿Tengo yo al ladrón visible? Si sí → envío `REFUSE` (tengo tarea más urgente)
   - ¿Estoy en alarma activa? Si sí → envío `REFUSE`
   - Si no → calculo mi distancia a la hoguera y envío `PROPOSE` con esa distancia como métrica

3. **Guardia A** recibe las propuestas. Acepta la de menor distancia a la hoguera. Envía `ACCEPT_PROPOSAL` a ese guardia y `REFUSE` (o simplemente ignora) al otro.

4. **Guardia aceptado** recibe `ACCEPT_PROPOSAL`. Actualiza `rolActualPropio = "CUBRIENDO_HOGUERA"`. La capa deliberativa inyecta en su controlador una instrucción de ir a la hoguera: activa `busquedaAgotada = true` y `alarmaHogueraActiva = false` pero con destino forzado a la posición de la hoguera.

**Consideración importante**: La cooperación surge en ejecución. El "equipo" no existía antes del CFP. Si Guardia A pierde visión del ladrón durante la negociación, puede cancelar la conversación enviando un `INFORM` de tipo `CANCELAR_CFP` con el mismo `conversationId`.

**Protocolo FIPA**: conversación multi-mensaje con `conversationId` compartido. Requiere almacenar el historial de la conversación para saber a qué CFP corresponde cada respuesta.

---

### 7.4 División de zonas de búsqueda

**Escenario**: Dos o más guardias están en estado de búsqueda simultáneamente y han perdido al ladrón. Si no se coordinan, buscan en el mismo sitio.

**Flujo**:

1. Guardia A entra en `Busqueda`. Envía `INFORM` broadcast con `rolActualPropio = "BUSCANDO"` y su posición actual.

2. Guardia B recibe ese INFORM. Sabe que A está buscando y dónde está. B también está buscando. B calcula un punto de búsqueda que esté lo más lejos posible de A (p.ej. el punto antipodal respecto a la última posición conocida del ladrón).

3. Cada guardia no necesita respuesta. Con los `INFORM` de posición propios que ya emiten periódicamente (ver 7.5), cada uno puede ajustar su zona de búsqueda para no solaparse.

**Diferencia con 7.3**: Aquí no hay negociación explícita. Cada agente toma su decisión unilateralmente basándose en la información de posición de los demás. Es más simple y también emergente.

**Implementación en la deliberativa**: cuando el agente está en búsqueda, en lugar de ir a un punto aleatorio puro, calcula un punto aleatorio dentro del radio de búsqueda pero sesgado hacia la zona más alejada de los guardias conocidos.

---

### 7.5 Broadcast periódico de posición propia

**Disparador**: Cada N segundos (p.ej. cada 2s), independientemente del estado.

**Acción**: Envía `INFORM` broadcast con `{"tipo":"POSICION_PROPIA","rol":"PATRULLANDO"}` y las coordenadas actuales.

**Para qué sirve**: Permite que todos los guardias mantengan en su BeliefBase una imagen actualizada de dónde están los compañeros. Esto es el sustrato que hace posibles los comportamientos 7.3 y 7.4. Sin saber dónde están los demás, no se puede coordinar zona de búsqueda ni evaluar quién está más cerca de la hoguera.

---

## 8. Flujo completo de un frame

Ejemplo concreto: Aldeano1 acaba de ver al ladrón con fuego.

```
Frame N:
  SubsumptionController.Update() de Aldeano1:
    1. LeerSensores()
       → veAlLadron = true, ladronTieneFuego = true, posición = (12, 0, -5)
    2. ActualizarEstadoAlerta()
       → enAlerta = true, ultimaPosicionLadron = (12, 0, -5)
    3. ActualizarBeliefBaseConSensores()
       → beliefs.ladronVisto = true, beliefs.ladronTieneFuegoCreido = true
       → beliefs.posicionLadronCreida = (12, 0, -5), beliefs.fuentePosicion = SENSOR_PROPIO
    4. deliberativa.Procesar():
       a. Inbox vacío (nadie le escribió este frame)
       b. Detección: acabaDeVerAlLadron && ladronTieneFuego
          → Generar conversationId "coord-001"
          → Enviar CFP broadcast "SOLICITAR_COBERTURA_HOGUERA"
          → beliefs.rolActualPropio = "PERSIGUIENDO"
    5. PropagarInformacionACapas() → igual que siempre
    6. EjecutarDecision() → Persecucion.CanActivate() = true → persigue

Frame N:
  SubsumptionController.Update() de Aldeano2 (misma iteración de Unity):
    1. LeerSensores() → veAlLadron = false
    2-7. Estados normales → sigue en Patrulla o Busqueda
    8. ActualizarBeliefBaseConSensores() → beliefs.ladronVisto = false
    9. deliberativa.Procesar():
       a. Inbox: tiene el CFP de Aldeano1 ("coord-001")
       b. Evalúa: ¿tengo tarea urgente? No. ¿distancia a hoguera? 8.5m
          → Enviar PROPOSE "coord-001" con distanciaHoguera=8.5
    ...

Frame N+1:
  Aldeano1 recibe PROPOSE de Aldeano2 (distancia 8.5) y de Aldeano3 (distancia 12.0):
    → Acepta Aldeano2 (más cercano)
    → Envía ACCEPT_PROPOSAL a Aldeano2
    → beliefs.rolGuardias["Aldeano2"] = "CUBRIENDO_HOGUERA"

Frame N+2:
  Aldeano2 recibe ACCEPT_PROPOSAL:
    → beliefs.rolActualPropio = "CUBRIENDO_HOGUERA"
    → deliberativa inyecta en controlador: InyectarAlerta() con destino hoguera
    → SubsumptionController entra en ComprobarHoguera por inyección
```

---

## 9. Ficheros a crear

Todos van en `Assets/Scripts/MAS/`:

| Fichero | Tipo | Descripción |
|---|---|---|
| `FIPAMessage.cs` | struct/clase de datos | Campos del mensaje: performativa, emisor, receptor, contenido, conversationId, timestamp, inReplyTo |
| `FIPAPerformativa.cs` | enum | `INFORM`, `CFP`, `PROPOSE`, `ACCEPT_PROPOSAL`, `REFUSE`, `FAILURE` |
| `MessageBus.cs` | MonoBehaviour singleton | Cola de mensajes por agente + historial de conversaciones |
| `AgentCommunicator.cs` | MonoBehaviour por agente | Nombre del agente, Send(), Receive(), GetConversationHistory() |
| `BeliefBase.cs` | clase de datos por agente | Campos de creencia: posición ladrón, fuego robado, posiciones guardias, rol propio, etc. |
| `DeliberativeLayer.cs` | MonoBehaviour por agente | Procesa inbox, actualiza BeliefBase, inyecta en controlador, envía mensajes |

Modificaciones en ficheros existentes:

| Fichero | Cambio |
|---|---|
| `SubsumptionController.cs` | Añadir referencias a los 3 nuevos componentes, añadir `ActualizarBeliefBaseConSensores()` en Update(), añadir métodos de inyección públicos, llamar a `deliberativa.Procesar()` en Update() |

---

## 10. Orden de implementación

### Fase 1 — Infraestructura (sin esto nada funciona)
1. `FIPAPerformativa.cs` y `FIPAMessage.cs` — solo estructuras de datos
2. `MessageBus.cs` — singleton con cola y historial
3. `AgentCommunicator.cs` — componente básico, Send y Receive
4. Verificar que los mensajes llegan: enviar un INFORM desde Aldeano1 y loguearlo en Aldeano2

### Fase 2 — BeliefBase y conexión con sensores
5. `BeliefBase.cs` — estructura de datos
6. Modificar `SubsumptionController` para crear BeliefBase y llamar a `ActualizarBeliefBaseConSensores()` en Update()
7. Verificar que la BeliefBase refleja correctamente el estado del sensor

### Fase 3 — Capa deliberativa básica
8. `DeliberativeLayer.cs` con el ciclo de procesado de inbox
9. Implementar `INFORM` de avistamiento (7.1) — el más simple
10. Implementar `INFORM` de alarma de hoguera (7.2)
11. Verificar que los guardias reaccionan a información que no detectaron sus sensores

### Fase 4 — Comportamientos de coordinación
12. Broadcast periódico de posición propia (7.5) — sustrato de los demás
13. Negociación perseguir/cubrir hoguera (7.3) — el más complejo
14. División de zonas de búsqueda (7.4)

### Fase 5 — Pulido y memoria
15. TTL de creencias (descartar datos viejos)
16. Historial de conversaciones accesible
17. Gestión de conversaciones interrumpidas (ladrón desaparece durante negociación)
18. Limpieza de roles cuando la situación cambia

---

## Notas finales

**Sobre la no-centralización**: el `MessageBus` podría parecer centralización pero no lo es — es infraestructura pasiva (como TCP/IP). Lo que prohíbe el enunciado es un agente que tome decisiones por los demás. Ningún fichero de los propuestos hace eso.

**Sobre la emergencia**: los comportamientos grupales propuestos en la sección 7 emergen de reglas locales de cada agente. Ningún agente tiene una visión global del sistema. La coordinación surge porque cada agente transmite lo que sabe y reacciona racionalmente a lo que recibe.

**Sobre los algoritmos de planificación**: el enunciado los menciona explícitamente. La forma más natural de introducirlos es en el comportamiento 7.3, donde el guardia que acepta cubrir la hoguera podría planificar una ruta de interceptación en lugar de simplemente moverse en línea recta hacia ella. También se podría usar A* o navegación por waypoints para que el guardia perseguidor no siga al ladrón sino que prediga su destino y vaya a interceptarle.
