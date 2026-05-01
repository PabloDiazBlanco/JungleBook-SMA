# Especificación Técnica: Refactorización "Cerebro Único"
### Arquitectura Híbrida Vertical para Agentes en Unity

---

## 1. Visión y Propósito

El objetivo principal es desmantelar el script monolítico `SubsumptionController` para transformarlo en un **Sistema Multiagente (SMA)** eficiente y escalable. Actualmente, el controlador sufre de "acoplamiento fuerte", donde la percepción, la lógica temporal y la toma de decisiones compiten por los mismos recursos, dificultando la depuración y la expansión del comportamiento del guardia.

Se implementa una **Arquitectura Híbrida Vertical**:

- **Capa Reactiva (Subsunción):** Para respuestas inmediatas y supervivencia (evitar obstáculos, perseguir si ve al ladrón).
- **Capa Deliberativa (BDI):** Para razonamiento social y táctico (coordinación por mensajes, planes a largo plazo).

---

## 2. El Trípode de Control: Desglose de Ficheros

La lógica se redistribuye en tres pilares fundamentales que deben interactuar sin solaparse.

### A. `AgentBlackboard.cs` — La Memoria de Trabajo

Es el único depósito de la "verdad" del agente en el frame actual. Su lógica es **pasiva**: recibe datos y los expone.

| Categoría | Variables |
|---|---|
| Percepción de Ladrón | `veAlLadron`, `ultimaPosicionLadron`, `ladronTieneFuego` |
| Percepción de Entorno | `posicionRuido`, `posicionPuerta`, `hogueraDetectada` |
| Estados Lógicos | `enAlerta`, `alarmaHogueraActiva`, `busquedaAgotada` |
| Contadores de Filtro | `framesSinVerHoguera` (filtro de oclusión), `contadorResetsCiclo` (anti-bucle) |

### B. `AgentTimerManager.cs` — El Motor Temporal

Gestiona el flujo del tiempo. Su responsabilidad es absorber toda la lógica de `Time.deltaTime`.

- **Gestión de Cooldowns:** Contiene tanto el cronómetro (`float`) como el estado de bloqueo (`bool`) para la investigación de sonidos y puertas.
- **Lógica de Finalización:** Al llegar un cronómetro a cero, el propio manager resetea los booleanos y lanza los avisos por consola (Logs).
- **Cronómetros de Persistencia:** Controla cuánto tiempo dura el estado de `enAlerta` tras perder un rastro.
- **Período de Gracia:** Protege al agente de alarmas falsas tras realizar una comprobación física de la hoguera.

### C. `SubsumptionController.cs` — El Árbitro Estratégico

Se libera de la "contabilidad" para ser un **orquestador puro**. Su lógica se reduce a un ciclo de tres pasos:

1. **Sincronización:** Pide a los sensores que escriban en el Blackboard y al TimerManager que avance el tiempo.
2. **Consulta:** Pregunta al Blackboard "¿Qué veo?" y al TimerManager "¿Tengo permitido moverme por esta razón?".
3. **Arbitraje:** Evalúa la prioridad de los `GuardBehavior`. Si la capa deliberativa ha inyectado una orden de "Crisis" en el Blackboard, el controlador suprime la patrulla y ejecuta el bloqueo de salida.

---

## 3. Lógica de Interacción — Flujos Críticos

Para que el sistema funcione, la comunicación entre ficheros debe seguir estos protocolos:

### Flujo del Cooldown (Ejemplo: Sonido)

```
1. InvestigarSonido llega al destino → no encuentra nada
2. Notifica éxito al SubsumptionController
3. El Controlador ordena al TimerManager: "Inicia cooldown de investigación"
4. TimerManager → investigacionEnCooldown = true + inicia cuenta atrás
5. En frames siguientes, el Controlador consulta si hay cooldown activo
6. Si hay cooldown → ignora estímulos auditivos (evita que el guardia "baile")
```

### Flujo de la Hoguera (Filtro anti-oclusión)

```
1. El Cerebro detecta que el sensor no ve la hoguera
2. Consulta al Blackboard: ¿cuántos framesSinVerHoguera lleva?
3. Si el contador supera el límite:
   → Blackboard: busquedaAgotada = true
   → Orden: ir físicamente a la hoguera
```

---

## 4. Requisitos de la Capa Deliberativa (SMA)

Para cumplir con el enunciado de la **Práctica 2**, esta estructura permite la cooperación emergente:

1. El `AgentCommunicator` recibe un mensaje **FIPA** de un aliado.
2. La `DeliberativeLayer` procesa el mensaje y actualiza la `BeliefBase`.
3. Si se acuerda una acción táctica (CNP), la capa deliberativa escribe directamente en el **Blackboard** del agente.
4. El `SubsumptionController`, al ser reactivo, detecta el cambio en el Blackboard y cambia el comportamiento físico del guardia **inmediatamente**.

---

## 5. Conclusión de la Mejora

Con esta estructura, el guardia deja de ser un script de 600 líneas que intenta pensar, contar y ver al mismo tiempo. Se convierte en un **ente modular** donde cada fichero tiene una única razón para cambiar.

Esto garantiza que el comportamiento del agente sea:

- ✅ **Predecible** y fácil de depurar
- ✅ **Escalable** y sencillo de ajustar
- ✅ **Colaborativo**, capaz de trabajar en equipo mediante el intercambio de información en el Blackboard
