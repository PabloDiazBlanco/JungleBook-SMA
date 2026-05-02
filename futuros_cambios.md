# JungleBook-SMA — Documentación Completa del Sistema

## Índice

1. [Visión General del Proyecto](#1-visión-general-del-proyecto)
2. [Arquitectura Híbrida: Capa Reactiva + Capa Deliberativa](#2-arquitectura-híbrida-capa-reactiva--capa-deliberativa)
3. [La Capa Reactiva: Subsumption](#3-la-capa-reactiva-subsumption)
4. [La Capa Deliberativa: BDI](#4-la-capa-deliberativa-bdi)
5. [Comunicación entre Agentes: FIPA-ACL y CNP](#5-comunicación-entre-agentes-fipa-acl-y-cnp)
6. [Predicción e Intercepción Cinemática](#6-predicción-e-intercepción-cinemática)
7. [Planificación A* de Sectores](#7-planificación-a-de-sectores)
8. [Flujo Completo de una Situación de Crisis](#8-flujo-completo-de-una-situación-de-crisis)
9. [Fallo Pendiente: La Interrupción Reactiva del Rol Deliberativo](#9-fallo-pendiente-la-interrupción-reactiva-del-rol-deliberativo)

---

## 1. Visión General del Proyecto

**JungleBook-SMA** es una simulación Unity de un sistema multiagente donde 8 guardias (aldeanos/campesinos) deben capturar a un ladrón que puede robar fuego de una hoguera. El reto es que los agentes dejen de comportarse como una turba reactiva y actúen como un equipo coordinado con reparto inteligente de roles.

El sistema tiene dos escenarios principales:
- **Sin fuego**: el ladrón deambula y los guardias deben localizarlo y capturarlo.
- **Con fuego**: el ladrón ha robado fuego de la hoguera, lo que activa una alarma global y cambia el objetivo.

Los 8 agentes comparten el mismo cerebro (mismos scripts), pero actúan de forma independiente. No hay un coordinador central: la cooperación emerge del protocolo de negociación CNP.

---

## 2. Arquitectura Híbrida: Capa Reactiva + Capa Deliberativa

Cada agente tiene dos capas de inteligencia que coexisten y se comunican:

```
┌─────────────────────────────────────────────────────┐
│                  CAPA DELIBERATIVA (BDI)             │
│   DeliberativeLayer → ModuleCrisis                  │
│                     → ModuleSocial  (CNP, roles)    │
│                     → ModuleTactical (A*, predicción)│
│                     → BeliefBase   (creencias)      │
└────────────────────┬────────────────────────────────┘
                     │ Inyecta posiciones, roles, alarmas
                     ▼
┌─────────────────────────────────────────────────────┐
│              CAPA REACTIVA (Subsumption)             │
│   SubsumptionController                             │
│     → Persecucion  (prioridad alta)                 │
│     → BloquearSalida                                │
│     → ComprobarHoguera                              │
│     → InvestigarSonido                              │
│     → Busqueda                                      │
│     → InvestigarEntorno                             │
│     → Patrulla     (prioridad baja)                 │
└─────────────────────────────────────────────────────┘
```

La capa reactiva actúa como el "cuerpo" del agente: decide qué comportamiento ejecutar cada frame. La capa deliberativa actúa como el "cerebro": razona, negocia con otros agentes y le da órdenes a la capa reactiva mediante **inyección de datos** en el `AgentBlackboard`.

**La comunicación entre capas es unidireccional hacia abajo**: la deliberativa escribe en el blackboard, la reactiva lee del blackboard. La deliberativa nunca llama directamente a comportamientos específicos.

---

## 3. La Capa Reactiva: Subsumption

### SubsumptionController (`SubsumptionController.cs`)

Es el orquestador de la capa reactiva. Cada frame (`Update`):

1. **Sincroniza sensores** (`PerceptionSync.Sincronizar()`): lee todos los sensores y actualiza el `AgentBlackboard`.
2. **Actualiza estados de alerta** (`AlertCycleManager`): gestiona el ciclo alerta/búsqueda/reset.
3. **Avanza cronómetros** (`AgentTimerManager.Tick()`): actualiza cooldowns e internos.
4. **Evalúa alarma de hoguera** (`FireAlarmMonitor`).
5. **Registra el frame anterior** (`PerceptionSync.RegistrarFrame()`): guarda flags para detectar transiciones en el siguiente frame.
6. **Ejecuta la capa deliberativa** (`DeliberativeLayer.Procesar()`).
7. **Sincroniza con la capa deliberativa** (`SincronizarConCapaDeliberativa()`): si la creencia sobre el ladrón expiró, limpia la posición inyectada.
8. **Propaga información a behaviours** (`PropagarInformacionACapas()`).
9. **Ejecuta la decisión de subsumption** (`EjecutarDecision()`): recorre los behaviours por prioridad y activa el primero que puede.

### AgentBlackboard (`AgentBlackboard.cs`)

Pizarra de datos en tiempo real del agente. Es el punto de encuentro entre sensores, capa reactiva y capa deliberativa. Campos clave:

- `veAlLadron`, `ultimaPosicionLadron` — percepción visual del ladrón.
- `velocidadLadron`, `direccionLadron` — datos cinemáticos del ladrón (para predicción).
- `acabaDeVerAlLadron`, `acabaDePerderAlLadron` — **flags de transición** (true solo un frame). Son fundamentales para disparar eventos únicos sin acumulación de errores.
- `enAlerta`, `busquedaAgotada`, `alarmaHogueraActiva` — estados lógicos del agente.
- `ciclosBusquedaCompletados` — contador de ciclos de búsqueda (cada N ciclos se hace una búsqueda más amplia).

### PerceptionSync (`PerceptionSync.cs`)

Lee los sensores físicos y escribe en el blackboard. También calcula las **transiciones de frame**:

```csharp
bb.acabaDeVerAlLadron    = !bb.ladronVisibleFrameAnterior && bb.veAlLadron;
bb.acabaDePerderAlLadron =  bb.ladronVisibleFrameAnterior && !bb.veAlLadron;
```

Estos flags son `true` exactamente un frame: el frame en que el ladrón aparece o desaparece del campo visual. Son los disparadores de la lógica deliberativa (CNP, predicción).

### GuardVision (`GuardVision.cs`)

Sensor de visión en cono. Cuando detecta al ladrón, extrae datos cinemáticos directamente del `CharacterController` del ladrón:

```csharp
CharacterController ccLadron = objetivo.GetComponent<CharacterController>();
velocidadLadron = ccLadron.velocity.magnitude;
direccionLadron = ccLadron.velocity.normalized;
```

Estos datos se propagan al blackboard vía `PerceptionSync` y desde ahí a la `BeliefBase` deliberativa.

### Behaviours (Comportamientos Reactivos)

Ordenados de mayor a menor prioridad:

| Behaviour | Condición de activación |
|---|---|
| `Persecucion` | `veAlLadron == true` |
| `BloquearSalida` | `alarmaHogueraActiva && ladronPerdidoConFuego` |
| `ComprobarHoguera` | `enAlerta && busquedaAgotada && !haComprobado` |
| `InvestigarSonido` | `oyoAlgo && !investigacionEnCooldown` |
| `Busqueda` | `(enAlerta || cronometroBusqueda > 0) && posicionLadron != null && !busquedaAgotada` |
| `InvestigarEntorno` | condiciones menores de entorno |
| `Patrulla` | siempre (fallback) |

La arquitectura de subsumption significa que **el primer behaviour que puede activarse gana**. Si el ladrón está visible, siempre se ejecuta `Persecucion`, independientemente de lo que diga la capa deliberativa.

---

## 4. La Capa Deliberativa: BDI

### DeliberativeLayer (`DeliberativeLayer.cs`)

Punto de entrada de la deliberación. Se llama desde `SubsumptionController.Update()` cada frame. Orquesta los tres módulos:

```csharp
public void Procesar()
{
    ActualizarCreencias();          // sincroniza BeliefBase con el blackboard
    crisis.Procesar();              // mensajes INFORM, alarmas
    
    if (controller.AcabaDeVerAlLadron && !social.IsCnpIniciado()
        && creencias.rolActual == BeliefBase.RolCNP.Ninguno)
        social.IniciarCNP();        // lanza la subasta solo una vez, solo si sin rol

    if (controller.blackboard.acabaDePerderAlLadron && creencias.posicionLadron.HasValue)
        tactical.PredecirPosicionLadron(); // predicción cinemática, solo una vez

    social.Procesar();              // gestión CNP y mensajes sociales
    tactical.Procesar();            // ejecución del plan de sectores
}
```

**Condición para lanzar CNP**: el agente acaba de ver al ladrón **Y** no hay un CNP ya en curso **Y** el agente no tiene rol asignado. Esto evita que múltiples agentes lancen subastas simultáneas y que agentes ya coordinados relancen el proceso.

**Condición para predecir**: el ladrón acaba de perderse de vista **Y** hay una posición conocida. Esto garantiza que la predicción se hace exactamente una vez, en el momento en que se pierde al ladrón.

### BeliefBase (`BeliefBase.cs`)

La memoria semántica del agente. Almacena lo que el agente "cree" sobre el mundo:

- `posicionLadron` (nullable) — posición conocida o predicha del ladrón. Si es null, el agente no tiene objetivo.
- `direccionLadron`, `velocidadLadron` — datos cinemáticos para la predicción.
- `timestampPosicionLadron`, `tiempoVidaCreenciaLadron (30s)` — sistema de expiración de creencias: si el ladrón no se ve durante 30 segundos, la creencia se invalida.
- `rolActual` (enum `RolCNP`) — rol asignado por el CNP: `Ninguno`, `Perseguidor`, `BuscadorSectores`, `Bloqueador`.
- `sectoresAsignados` — lista de posiciones de sectores que el agente debe patrullar.
- `planBusqueda` — ruta A* ordenada de sectores a visitar.
- `indiceSectorActual` — puntero al sector actual del plan.
- `sectoresLimpios` — conjunto de IDs de sectores ya verificados (compartidos por mensajes INFORM).

Propiedades derivadas:
```csharp
public bool TienePlanActivo => planBusqueda != null && indiceSectorActual < planBusqueda.Count;
public Vector3? SectorActual => TienePlanActivo ? planBusqueda[indiceSectorActual] : null;
```

### ModuleCrisis (`ModuleCrisis.cs`)

Gestiona las emergencias de alarma de hoguera. Se ejecuta primero en cada frame deliberativo:

1. Lee mensajes `INFORM` del buzón y propaga la alarma de hoguera localmente si llega una.
2. Si la alarma acaba de activarse en este agente, emite un broadcast `INFORM alarma_hoguera` a todos los demás. Esto crea una propagación en cascada en un solo frame.

---

## 5. Comunicación entre Agentes: FIPA-ACL y CNP

### AgentCommunicator (`AgentCommunicator.cs`)

Sistema de mensajería peer-to-peer sin broker central. Cada agente se registra en un diccionario estático al inicializarse. Los mensajes se encolan en `pendientes` y se mueven a `inbox` al inicio del `Update` del receptor. Esto garantiza que los mensajes del frame N se procesan en el frame N+1 (no hay procesamiento inmediato que pudiera causar condiciones de carrera).

Funcionalidades:
- `Enviar(mensaje, listaDestinatarios)` — envío dirigido.
- `EnviarATodos(mensaje)` — broadcast a todos los agentes registrados excepto uno mismo.
- `GenerarConversationId(tipo)` — genera IDs únicos para identificar conversaciones CNP.
- `GetHistorial()` — acceso al histórico persistente de mensajes.

### FIPAMessage (`FIPAMessage.cs`)

Estructura de mensaje con campos:
- `performativa` — tipo de acto de habla (CFP, PROPOSE, REFUSE, ACCEPT_PROPOSAL, INFORM).
- `emisor`, `receptor` — identidades de los agentes.
- `contenido` — payload en texto plano (protocolo propio).
- `conversationId`, `inReplyTo` — trazabilidad de la conversación.

### ModuleSocial (`ModuleSocial.cs`) — Contract Net Protocol

El corazón de la coordinación grupal. Implementa el CNP completo:

#### Rol Manager (Iniciador)

El agente que primero detecta al ladrón (`AcabaDeVerAlLadron`) y no tiene rol activo lanza la subasta:

```
IniciarCNP()
  ├─ Se asigna a sí mismo como Perseguidor (cuenta como 1 de los 3)
  ├─ Calcula posición del ladrón y del objetivo (hoguera/salida)
  ├─ Genera un conversationId único
  └─ Emite CFP broadcast: "hoguera|ladron:X,Y,Z|obj:X,Y,Z"
```

Tras `tiempoEsperaPropuestas (0.5s)`, llama a `ResolverCNP()`:

```
ResolverCNP()
  ├─ Filtra solo mensajes PROPOSE (descarta REFUSE)
  ├─ Parsea cada propuesta: "dl:18,do:45" (distancia al ladrón, distancia al objetivo)
  ├─ Ordena candidatos por dl ascendente (más cercano al ladrón = mejor perseguidor)
  ├─ Asigna ACCEPT con "perseguir:X,Y,Z" a los 2 primeros (+ el iniciador = 3 perseguidores)
  └─ Distribuye sectores en round-robin al resto: "explorar_sector:0,3,6"
```

#### Rol Contractor (Participante)

Cuando un agente recibe un CFP:
- Si `rolActual != Ninguno`: envía REFUSE (ya está coordinado, no puede asumir más tareas).
- Si `rolActual == Ninguno`: calcula su aptitud y envía PROPOSE con `"dl:X,do:Y"`.

El criterio de rechazo es **exclusivamente el rol activo**, no el estado de alerta. Un agente asustado pero sin rol asignado participa en la subasta.

Cuando recibe un ACCEPT_PROPOSAL, delega en `ModuleTactical.EjecutarAccionConfirmada()`.

---

## 6. Predicción e Intercepción Cinemática

### El Problema que Resuelve

Sin predicción, todos los agentes persiguen la **posición actual** del ladrón. Cuando el ladrón se pierde de vista, los agentes se quedan quietos en el último punto visto. Con predicción, el agente calcula **dónde estará** el ladrón en el futuro inmediato y se dirige allí.

### Flujo Completo de la Predicción

**Paso 1 — Captura de datos cinemáticos (GuardVision)**

Mientras el agente ve al ladrón, cada frame extrae del `CharacterController` del ladrón:
```
velocidadLadron = ccLadron.velocity.magnitude   → escalar, m/s
direccionLadron = ccLadron.velocity.normalized  → vector unitario de movimiento
```

**Paso 2 — Propagación al blackboard (PerceptionSync)**

`PerceptionSync.LeerSensores()` copia estos valores al `AgentBlackboard` cada frame que el ladrón es visible.

**Paso 3 — Actualización de creencias (DeliberativeLayer → ActualizarCreencias)**

```csharp
if (controller.VeAlLadron && controller.blackboard != null)
{
    creencias.posicionLadron    = controller.UltimaPosicionLadron;
    creencias.direccionLadron   = controller.blackboard.direccionLadron;
    creencias.velocidadLadron   = controller.blackboard.velocidadLadron;
    creencias.timestampPosicionLadron = Time.time;
}
```

La `BeliefBase` mantiene siempre la posición y datos cinemáticos más recientes mientras el ladrón es visible.

**Paso 4 — Disparo de la predicción (exactamente un frame)**

```csharp
// En DeliberativeLayer.Procesar():
if (controller.blackboard.acabaDePerderAlLadron && creencias.posicionLadron.HasValue)
    tactical.PredecirPosicionLadron();
```

El flag `acabaDePerderAlLadron` es `true` solo el frame exacto en que el ladrón desaparece del campo visual. Esto garantiza que la predicción se hace **una sola vez**, con los últimos datos conocidos.

**Paso 5 — Cálculo del punto de intercepción (ModuleTactical)**

```csharp
public void PredecirPosicionLadron()
{
    if (!creencias.posicionLadron.HasValue || creencias.velocidadLadron < 0.1f) return;

    Vector3 ultimaPos = creencias.posicionLadron.Value;
    Vector3 direccion = creencias.direccionLadron.HasValue
                        ? creencias.direccionLadron.Value : Vector3.zero;

    Vector3 posicionFutura = ultimaPos + (direccion * creencias.velocidadLadron * tiempoProyeccionFutura);

    creencias.posicionLadron = posicionFutura;   // sobreescribe la creencia con la predicción
    Debug.DrawLine(ultimaPos, posicionFutura, Color.magenta, 1.0f);
}
```

Fórmula de cinemática uniforme: `posición_futura = última_pos + dirección × velocidad × tiempo_proyección`.

`tiempoProyeccionFutura` (configurable en Inspector, por defecto 2.0s) es el horizonte de predicción: cuántos segundos en el futuro se proyecta.

**Paso 6 — Uso de la predicción**

Una vez sobreescrita `creencias.posicionLadron` con el punto futuro, `ActualizarCreencias()` también actualiza el blackboard reactivo (`bb.ultimaPosicionLadron`) en el siguiente frame mediante `SincronizarConCapaDeliberativa`. Los behaviours reactivos de `Busqueda` se dirigen automáticamente a ese punto predicho.

Si el ladrón permanece sin verse durante más de `tiempoVidaCreenciaLadron (30s)`, la creencia se invalida y el agente pierde el objetivo.

### Por qué un solo disparo es crítico

Si la predicción se llamara cada frame mientras el ladrón no está visible, el resultado sería catastrófico:
- Frame 1: `posicion = A + dir*vel*2` → posición B
- Frame 2: `posicion = B + dir*vel*2` → posición C (acumulación de error)
- Frame N: el agente cree que el ladrón está a kilómetros de distancia

El flag `acabaDePerderAlLadron` resuelve este problema al garantizar exactamente un cálculo.

---

## 7. Planificación A* de Sectores

### SectorMap (`SectorMap.cs`)

Lista estática de 9 posiciones de sector, cargadas al inicio desde GameObjects con tag `"Sector"` en la escena Unity. Accesible globalmente como `SectorMap.Sectores[id]`.

### GenerarPlanBusqueda (ModuleTactical)

Cuando un agente recibe la orden `explorar_sector:0,3,6`, `EjecutarAccionConfirmada` llama a `GenerarPlanBusqueda()`, que ejecuta un A* sobre el subconjunto de sectores asignados para encontrar el **orden óptimo de visita** (problema similar al TSP para N pequeño).

### Algoritmo A* (`AEstrellaSectores`)

**Estado del nodo**: `(sectorActual, bitmask de visitados, coste acumulado, heurística)`

El bitmask es la clave: con N sectores, hay 2^N estados posibles. Cuando todos los bits están activos (`visitados == (1 << n) - 1`), se ha encontrado la ruta completa.

**Heurística**: distancia al sector no visitado más cercano desde la posición actual. Es admisible (nunca sobreestima) porque en el peor caso hay que ir al más cercano primero.

```
SortedList por f = g + h
├─ Nodo raíz: origen del agente, ningún sector visitado
├─ Expansión: para cada sector no visitado, crear hijo con:
│    coste += distancia(posActual → sectorSiguiente)
│    visitados |= (1 << i)
│    h = dist_min al no-visitado más cercano
└─ Meta: cuando visitados == (1<<n)-1, reconstruir plan
```

**Resultado**: `creencias.planBusqueda` = lista ordenada de Vector3, `creencias.indiceSectorActual = 0`.

### Ejecución del Plan (ModuleTactical.Procesar)

Cada frame deliberativo:

```csharp
public override void Procesar()
{
    if (creencias.TienePlanActivo)
    {
        Vector3 destinoActual = creencias.SectorActual.Value;
        float distanciaAlPunto = Vector3.Distance(controller.transform.position, destinoActual);

        if (distanciaAlPunto < 2.0f)
        {
            creencias.AvanzarSector();   // punto alcanzado → siguiente
        }
        else
        {
            controller.InyectarPosicionLadron(destinoActual);  // inyecta destino en capa reactiva
        }
    }
}
```

La inyección de la posición del sector en el blackboard hace que el behaviour `Busqueda` de la capa reactiva se dirija automáticamente a ese punto. No hay un behaviour especial de "ir a sector": se reutiliza el mecanismo existente de búsqueda.

---

## 8. Flujo Completo de una Situación de Crisis

A continuación, el flujo temporal completo desde que un guardia detecta al ladrón hasta que los roles están asignados:

```
Frame 0: GuardA detecta al ladrón → bb.acabaDeVerAlLadron = true

Frame 0 (deliberativa de GuardA):
  ├─ ActualizarCreencias: posicionLadron = posActualLadron, timestamp = now
  ├─ crisis.Procesar: sin INFORM relevante
  ├─ AcabaDeVerAlLadron && !cnpIniciado && rolActual==Ninguno → social.IniciarCNP()
  │    ├─ creencias.rolActual = Perseguidor  (GuardA se auto-asigna)
  │    ├─ genera CFP: "hoguera|ladron:X,Y,Z|obj:A,B,C"
  │    └─ EnviarATodos(CFP)  → 7 mensajes encolados en otros agentes
  └─ tactical.Procesar: sin plan activo aún

Frame 1 (cada GuardB..H procesa su inbox):
  ├─ ModuleSocial.ProcesarCFP(msg)
  │    ├─ rolActual == Ninguno → PROPOSE
  │    └─ calcula "dl:X,do:Y" con distancias reales
  └─ PROPOSE encolado en GuardA

Frame 1 (GuardA, cronómetro corriendo 0.5s):
  ├─ propuestasCNP.Add(cada PROPOSE recibido)
  └─ ...

Frame ~30 (0.5s después, cronómetro llega a 0):
  ResolverCNP()
    ├─ 7 propuestas recibidas (rol==Ninguno en todos)
    ├─ Ordena por dl ascendente
    ├─ GuardB (dl=18) → ACCEPT "perseguir:X,Y,Z"  → rol: Perseguidor
    ├─ GuardC (dl=50) → ACCEPT "perseguir:X,Y,Z"  → rol: Perseguidor
    ├─ GuardD        → ACCEPT "explorar_sector:0,3,6"
    ├─ GuardE        → ACCEPT "explorar_sector:1,4,7"
    ├─ GuardF        → ACCEPT "explorar_sector:2,5,8"
    ├─ GuardG        → ACCEPT "explorar_sector:0,3"   (round-robin)
    └─ GuardH        → ACCEPT "explorar_sector:1,4"

Frame ~31 (cada receptor procesa su ACCEPT):
  ModuleTactical.EjecutarAccionConfirmada()
    ├─ "perseguir:X,Y,Z" → rolActual=Perseguidor, InyectarPosicionLadron
    └─ "explorar_sector:..." → rolActual=BuscadorSectores, GenerarPlanBusqueda()
         └─ A* genera orden óptimo de sectores asignados

Frame ~31 en adelante:
  ├─ 3 Perseguidores: capa reactiva → Persecucion/Busqueda hacia posición del ladrón
  └─ 5 Buscadores: ModuleTactical.Procesar() inyecta sectores uno a uno
```

---

## 9. Fallo Pendiente: La Interrupción Reactiva del Rol Deliberativo

### Descripción del Problema

Cuando un agente recibe el rol `BuscadorSectores` y empieza a ejecutar su plan A*, el comportamiento reactivo `ComprobarHoguera` puede interrumpirlo periódicamente. En el log esto se ve como:

```
[CEREBRO Aldeano3]: Cambio a: Busqueda
[TACTICAL Aldeano3]: Punto de sector registrado. Avanzando...
[CEREBRO Aldeano3]: Cambio a: ComprobarHoguera    ← INTERRUPCIÓN
[COMPROBAR Aldeano3]: Llegué a la hoguera. Verificando presencia...
[CEREBRO Aldeano3]: Cambio a: Busqueda            ← reanuda
[TACTICAL Aldeano3]: Punto de sector registrado. Avanzando...
[CEREBRO Aldeano3]: Cambio a: ComprobarHoguera    ← INTERRUPCIÓN (de nuevo)
```

### Por qué ocurre

El behaviour `Busqueda` tiene un límite de tiempo (`tiempoLimiteBusqueda`) que, al agotarse, activa `bb.busquedaAgotada = true`. El behaviour `ComprobarHoguera` está esperando exactamente esa condición:

```csharp
// ComprobarHoguera.CanActivate():
if (enAlerta && !veAlLadron && !alarmaHogueraActiva && !haComprobado && busquedaAgotada)
    return true;
```

Cuando el timer de búsqueda expira, `ComprobarHoguera` se activa y el agente abandona su sector asignado para ir a verificar la hoguera. Una vez completada la comprobación, `AlertCycleManager.ResetearBusqueda()` resetea el ciclo y el agente vuelve a `Busqueda`. Cinco segundos después, el ciclo se repite.

### Por qué es un problema

El agente lleva a cabo su misión pero de forma intermitente e ineficiente. Pierde tiempo yendo y volviendo a la hoguera en cada ciclo, cuando su rol deliberativo le indica que debería estar patrullando sectores sin interrupciones hasta completarlos.

### La Solución (Pendiente de Implementar)

El método `CanActivate()` de `ComprobarHoguera` debería consultar el rol deliberativo del agente y bloquearse si el rol es `BuscadorSectores`:

```csharp
// En ComprobarHoguera.CanActivate(), añadir al inicio:
DeliberativeLayer deliberativa = GetComponent<DeliberativeLayer>();
if (deliberativa != null && deliberativa.creencias.rolActual == BeliefBase.RolCNP.BuscadorSectores)
    return false;
```

De esta forma, mientras el agente tenga una misión deliberativa activa, los reflejos reactivos de menor prioridad estratégica no pueden interrumpirla. El agente recuperará la capacidad de `ComprobarHoguera` cuando su rol vuelva a ser `Ninguno` (plan de sectores completado o rol cancelado por el CNP).

**Impacto esperado**: los buscadores completarán sus sectores sin desvíos, el mapa se cubrirá más rápido y los logs mostrarán una ejecución limpia del plan A* sin interrupciones de `ComprobarHoguera`.

---

*Documentación generada el 2026-05-02 para el estado actual del proyecto JungleBook-SMA.*
