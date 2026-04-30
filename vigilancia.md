# Mejoras del Sistema de Agentes

## 1. Planificación de Vigilancia Dinámica

Este punto se centra en dejar de usar posiciones fijas y empezar a calcular estrategias de posicionamiento. En lugar de que un agente vaya directo al centro de un objeto, la capa deliberativa generará "planes de situación" para que los guardias se repartan por el terreno. La idea es que el sistema decida dónde es mejor estar para vigilar sin estorbarse entre ellos, creando una red de vigilancia que no sea un muro físico infranqueable para el jugador.

## 2. Evolución de la Arquitectura Híbrida: Control de Compromiso

Significa darle a la Capa Deliberativa la capacidad de "filtrar" o priorizar qué instintos debe seguir el agente en cada momento. Se trata de que, cuando un agente acepta una misión importante por radio, sea capaz de ignorar distracciones menores que normalmente le harían abandonar su puesto. Es pasar de un agente que solo reacciona a lo que ve, a uno que mantiene un compromiso con lo que ha prometido hacer al grupo.

## 3. Cooperación Emergente Real: Formación de Equipos

Este concepto implica que los guardias no actúen siempre igual, sino que su organización nazca de la negociación en tiempo real. Los agentes intercambiarán información sobre su estado y su entorno para decidir quién asume cada rol de forma dinámica. La cooperación no está escrita de antemano; el equipo se forma, se disuelve o cambia sus funciones dependiendo de cómo se mueva el ladrón durante la partida.

## 4. Sistema de Control de Timeouts: Robustez del Protocolo

Se trata de gestionar los tiempos de espera y la caducidad de la información para que el sistema no se quede bloqueado. Esto asegura que los agentes sepan cuándo una oferta ha caducado, cuándo deben dejar de esperar una respuesta que no llega o cuándo una creencia (como la última posición del ladrón) es ya demasiado vieja para ser útil. Es, básicamente, añadir un reloj a la lógica social para que los agentes sepan cuándo desistir y volver a sus tareas normales.
