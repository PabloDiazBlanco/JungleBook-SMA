# Resumen de Implementación: Sistema Multiagente (Práctica 2)

## 1. Arquitectura del Agente
Se ha implementado una **Arquitectura Híbrida Vertical** que combina reactividad inmediata con deliberación social:
- **Capa Deliberativa (`DeliberativeLayer`):** Gestiona el ciclo de vida de los mensajes y la base de creencias del agente.
- **Capa de Control (`SubsumptionController`):** Basada en la arquitectura de subsunción de Brooks, prioriza comportamientos mediante inhibición.
- **Capa Perceptiva:** Módulos especializados de visión, audición y detección de objetos (hoguera y puertas).

## 2. Comunicación y Protocolos (FIPA-ACL)
- **Infraestructura:** Sistema de mensajería peer-to-peer con registro de agentes y gestión de `conversationId`.
- **Protocolo Contract Net (CNP):** Capacidad de iniciar y responder a subastas de tareas (`CFP`, `PROPOSE`, `ACCEPT`).
- **Sincronización Social:** Uso de performativas `INFORM` para alertar al grupo sobre el robo de la hoguera o avistamientos del ladrón.

## 3. Catálogo de Comportamientos
- **Críticos:** Persecución activa (con ajuste de velocidad si hay fuego) y bloqueo de la salida del pueblo.
- **Investigación:** Inspección de ruidos (con imprecisión), verificación de puertas abiertas y comprobación física de la hoguera.
- **Rutina:** Patrullaje secuencial por puntos de control cuando no hay alertas activas.

## 4. Sensores Avanzados
- **Visión:** Detección por ángulo y distancia con validación de oclusión (Raycast).
- **Audición:** Captura de ruidos basada en la actividad del jugador (saltar/correr) con desplazamiento aleatorio para evitar omnisciencia.
- **Monitor de Hoguera:** Sensor dedicado para detectar la desaparición del objetivo principal.

# Plan de Trabajo y Pendientes: Hacia la Inteligencia Distribuida

Este documento detalla las tareas restantes para completar la Práctica 2, basándose en el análisis del enunciado (IA Distribuida, FIPA-ACL, Cooperación Emergente) y el documento de estrategia `vigilancia.md`.

## 1. Planificación de Vigilancia Dinámica
Actualmente, los agentes tienen destinos fijos (puntos de patrulla o la puerta). Para cumplir con una vigilancia real:
- **Reparto de Sectores:** En lugar de que todos vayan al mismo `puntoPuertaPueblo`, los agentes deben negociar quién cubre el flanco izquierdo, quién el derecho y quién se mantiene en retaguardia.
- **Sugerencia de Planificación:** Implementar un sistema de "Slots" de vigilancia. Al llegar a la fase de `ACCEPT_PROPOSAL`, el emisor puede asignar no solo la tarea, sino una posición específica calculada en función de la posición del ladrón.
- **Ejemplo:** Si el ladrón está en el Sur, los agentes calculan posiciones de interceptación en el Norte (salida) de forma repartida, no solapada.

## 2. Evolución del Control de Compromiso
Aunque la subsunción gestiona prioridades, el compromiso social requiere una **inhibición dinámica**:
- **Problema:** Un agente que ha aceptado el rol de "Bloqueador" no debería distraerse con ruidos menores (`InvestigarSonido`) si eso implica abandonar su posición crítica.
- **Implementación:** La `DeliberativeLayer` debe poder enviar una señal al `SubsumptionController` para desactivar o bajar la prioridad de ciertos sensores/comportamientos reactivos mientras una "Misión Crítica" (Intención BDI) esté activa.
- **Objetivo:** Que el agente mantenga su "promesa" al equipo por encima de sus instintos individuales temporales.

## 3. Cooperación Emergente y Roles Dinámicos
El sistema no debe tener roles fijos desde el inicio (`Start`), sino que deben surgir del `CNP`:
- **Subasta de Roles:** En el `CFP`, los agentes informarán de su estado (ej: "Estoy cerca de la hoguera", "Tengo visión clara del callejón").
- **Especialización:** El iniciador de la subasta asignará roles de **Perseguidor** (el más cercano al ladrón) y **Vigía** (el más cercano a la salida o puntos de interés), optimizando los recursos del grupo.
- **Sugerencia:** Añadir un parámetro `rolAsignado` en la base de creencias que modifique los parámetros del agente (ej: el vigía es más paciente, el perseguidor es más agresivo).

## 4. Robustez: Timeouts y Caducidad de Creencias
Para evitar que el sistema se bloquee o actúe con información obsoleta:
- **TTL (Time To Live):** Cada creencia en la `BeliefBase` (como la última posición del ladrón) debe expirar. Si un mensaje `INFORM` tiene más de 15 segundos, el agente debe borrar esa posición y volver a `Patrulla`.
- **Gestión de Fallos:** Si un agente acepta una propuesta pero no llega a su destino en X tiempo, debe enviar un mensaje de `FAILURE` para que el grupo reinicie la subasta.

## 5. Procesamiento de Conversaciones Completas
El enunciado pide "almacenar y procesar conversaciones":
- **Análisis de Historial:** Implementar una lógica en la que el agente consulte su `historialMensajes` para detectar patrones. 
- **Ejemplo:** "Si en los últimos 3 mensajes `INFORM` el ladrón se movía hacia la izquierda, mi plan de interceptación debe ser moverme hacia la salida izquierda del pueblo". Esto demuestra procesamiento de la información, no solo reacción.

## 6. Sugerencias Extra de Planificación (IA)
Para elevar la sofisticación del sistema multiagente y cumplir con la recomendación del enunciado sobre algoritmos de planificación, se proponen las siguientes implementaciones:

### A. Planificación de Interceptación Predictiva
En lugar de que el `Persecucion.cs` use la posición actual del ladrón, el agente puede estimar su trayectoria futura.
- **Lógica:** Si el ladrón mantiene una velocidad constante, el guardia calcula un punto de encuentro adelantado: `PuntoEncuentro = PosicionLadron + (VelocidadLadron * TiempoEstimadoLlegada)`.
- **Efecto:** Los guardias "cortan el paso" al jugador en lugar de seguir su rastro, lo que genera una sensación de inteligencia táctica muy superior.

### B. Mapas de Calor para Búsqueda Cooperativa (Stigmergy)
Implementar una memoria compartida o un sistema de marcas en el terreno para optimizar la búsqueda tras perder de vista al objetivo.
- **Zonas de Registro:** Dividir el pueblo en una rejilla virtual. Cuando un agente termina un comportamiento de `Busqueda.cs` en un área sin éxito, envía un `INFORM` con la zona despejada.
- **Planificación Eficiente:** Los demás agentes actualizan su `BeliefBase` y descartan esa zona para sus próximos puntos aleatorios, concentrando la presión en las áreas donde el ladrón realmente podría estar escondido.

### C. Formaciones Tácticas Dinámicas (Offsets de Flanqueo)
Evitar el "Efecto Conga" donde todos los guardias siguen la misma línea de NavMesh.
- **Asignación de Offsets:** Durante la resolución del `CNP`, la `DeliberativeLayer` asigna un "lado de persecución". El Guardia A persigue con un offset de -3 metros a la izquierda del ladrón, y el Guardia B con +3 metros a la derecha.
- **Resultado:** Los agentes rodean físicamente al jugador, dificultando las maniobras de esquiva lateral y forzando al jugador a retroceder.

### D. Planificación Basada en "Puntos de Conveniencia"
Modificar la patrulla y la búsqueda para que no sea puramente aleatoria o secuencial.
- **Análisis de Visibilidad:** Identificar puntos del mapa con mayor "valor de vigilancia" (esquinas con visión de dos callejones, zonas elevadas).
- **Priorización:** La capa deliberativa genera planes para que, en estado de alerta, los guardias se muevan entre estos puntos de alta visibilidad, maximizando el área total cubierta por el equipo de seguridad.

### E. Protocolo de Relevo (Handover)
Optimizar el consumo de "energía" o recursos del sistema.
- **Lógica de Relevo:** Si un Guardia A está persiguiendo al ladrón pero se queda sin "tiempo de búsqueda" o se aleja demasiado de su zona original, puede emitir un `CFP` de urgencia para que un Guardia B, que esté más cerca del ladrón en ese momento, tome el relevo de la persecución.
- **Uso de Performativas:** El Guardia A envía un `INFORM` de "Dejo Persecución" y el Guardia B asume el contrato social, permitiendo que el primero regrese a cubrir la hoguera o la puerta.

### F. Predicción de Objetivos (Goal Recognition)
Hacer que los agentes "entiendan" qué intenta hacer el ladrón.
- **Creencia Intencional:** Si el ladrón tiene el fuego y se mueve hacia el Norte, los agentes deducen que su intención es llegar a la puerta.
- **Plan de Bloqueo Proactivo:** Antes incluso de que el ladrón llegue a la vista de la puerta, los agentes en la zona Norte activan preventivamente `BloquearSalida.cs` basándose en esta deducción social.

# Propuesta de Evolución: Capa Deliberativa Modular

Para cumplir con los estándares de diseño de Sistemas Multiagente (SMA) y asegurar la escalabilidad del proyecto, se propone una reestructuración de la `DeliberativeLayer` actual.

## 1. Motivación del Cambio
La arquitectura actual centraliza toda la lógica de alto nivel en una única clase, lo que genera:
- **Acoplamiento excesivo:** La lógica de la hoguera se mezcla con el protocolo Contract Net y la gestión de ruido.
- **Dificultad de testeo:** No es posible probar la negociación social sin ejecutar toda la lógica sensorial.
- **Violación del principio de Responsabilidad Única:** La clase gestiona creencias, comunicación y planificación simultáneamente.

## 2. Nueva Arquitectura: Coordinador y Módulos Especializados
Se propone un modelo de **Coordinación de Módulos Deliberativos**. La `DeliberativeLayer` pasará a ser un contenedor que orquesta la ejecución de sub-cerebros independientes.

### Estructura Propuesta
- **DeliberativeLayer (Orquestador):** Clase base que mantiene el contexto compartido (`BeliefBase`, `AgentCommunicator`, `SubsumptionController`) y ejecuta cíclicamente los módulos.
- **Módulo Hoguera (`DeliberativeFire`):** Especializado en la vigilancia del fuego y alarmas de robo.
- **Módulo Persecución (`DeliberativeCombat`):** Gestiona la interceptación y el flanqueo coordinado.
- **Módulo Social (`DeliberativeSocial`):** Implementa el Contract Net Protocol y la gestión de la cola de mensajes FIPA.

## 3. Ventajas Técnicas y Académicas
- **Modularidad:** Cada módulo puede desarrollarse y depurarse de forma aislada.
- **Escalabilidad:** Añadir un comportamiento nuevo (ej. gestión de puertas sospechosas) solo requiere crear un nuevo script e inyectarlo en el coordinador.
- **Reutilización:** Un módulo de "Vigilancia de Objetos" podría reutilizarse en otros agentes sin llevarse consigo la lógica de combate.
- **Similitud con Modelos Reales:** Este diseño se aproxima a arquitecturas como **InteRRaP**, que divide el conocimiento en modelos del mundo, de planificación y sociales.

# Evolución de la Capa Deliberativa: Hacia la Modularidad Funcional

Tras analizar el crecimiento del sistema, se ha determinado que la `DeliberativeLayer` actual actúa como un cuello de botella para la escalabilidad. Antes de implementar nuevas tácticas de vigilancia o interceptación, se procederá a una refactorización estructural.

## 1. El Problema de la Clase Monolítica
Actualmente, la `DeliberativeLayer` gestiona:
- Actualización de creencias locales y sociales[cite: 10, 11].
- Procesamiento y filtrado de mensajes FIPA-ACL[cite: 11, 12].
- Lógica de la hoguera y control de alarmas[cite: 11, 23].
- Coordinación mediante el Contract Net Protocol (CNP)[cite: 11].

Esta acumulación de responsabilidades dificulta el mantenimiento y la implementación de sistemas de control de compromiso y timeouts.

## 2. Nueva Estructura: Descomposición por Módulos
Se transformará la capa deliberativa en un sistema basado en **Módulos Deliberativos Especializados**. La clase principal pasará a ser un **Orquestador** que distribuye el contexto shared (`BeliefBase`, `AgentCommunicator`, `SubsumptionController`) entre sub-clases autónomas.

### Arquitectura de Módulos
- **Modulo de Percepción Social:** Se encarga exclusivamente de transformar los mensajes entrantes en actualizaciones de la `BeliefBase`[cite: 11].
- **Módulo de Negociación (CNP):** Gestiona el estado de las subastas, el envío de propuestas y la selección de ganadores[cite: 11].
- **Módulo de Vigilancia Crítica:** Supervisa el estado de la hoguera y dispara planes de emergencia grupales[cite: 14, 23].
- **Módulo de Planificación Táctica:** Calcula interceptaciones predictivas y offsets de flanqueo para la capa de subsunción[cite: 15, 20].

## 3. Justificación del Cambio
- **Limpieza de Código:** Cumple con el principio de responsabilidad única de la ingeniería de software.
- **Robustez ante Timeouts:** Permitirá añadir temporizadores individuales a cada negociación sin interferir con la lógica de visión.
- **IA Distribuida Real:** Facilita que cada agente pueda tener módulos ligeramente distintos, permitiendo la especialización de roles que pide el enunciado[cite: 25].