# Problemas detectados en la ejecución actual

---

## Problema 1 — Problema en investigar entorno.
No entiendo el motivo, pero los agentes ahora al investigar el entorno y entrar en las puertas de las casas, se quedan bugueados y no saben salir. No se me ocurre 
ninguna solución directa, lo único es poner algún mensaje de debug temporal que luego quitaremos porque solo es de diagnóstico que nos de información sobre lo que 
está pasando.

Creo que he ubicado un posible error, y es que simplemente la puerta no sabe abrirle al aldeano, no entiendo porque, pero pasa esto.

## Problema 2 — Bucle de comportamiento que impide llegar a ComprobarHoguera

**Síntoma observado en consola:**

Se producen secuencias repetidas del tipo:
```
Cambio a: Persecucion
Ladrón perdido → Cambio a: InvestigarSonido
[BUSQUEDA Aldeano3]: Nuevo destino → (...)
Cambio a: Persecucion
Ladrón perdido → Cambio a: InvestigarSonido
[BUSQUEDA Aldeano3]: Nuevo destino → (...)
Cambio a: Persecucion
...
```
Este patrón se repite muchas veces sin que el agente llegue nunca a `ComprobarHoguera`.

**Causa raíz:**

El sistema funciona mediante un cronómetro de búsqueda (`cronometroBusqueda`) que, cuando llega a 0, activa el flag `busquedaAgotada = true`. Solo cuando `busquedaAgotada` está activo, `ComprobarHoguera.CanActivate()` devuelve `true` y el agente va a verificar la hoguera.

El problema está en que cada vez que el agente vuelve a ver al ladrón (aunque sea por un instante), se llama a `ResetearCicloCompleto()`, que reinicia `cronometroBusqueda` a su valor inicial (por ejemplo, 5 segundos) y pone `busquedaAgotada = false`. Si el ladrón aparece y desaparece del campo visual con frecuencia suficiente (cada 3-4 segundos), el cronómetro nunca llega a 0. El agente entra en el ciclo:

1. Ve al ladrón → `Persecucion` activa
2. Pierde al ladrón → `InvestigarSonido` o `Busqueda` activa, cronómetro empieza a bajar
3. Antes de que el cronómetro expire, vuelve a ver al ladrón → `ResetearCicloCompleto()` → cronómetro vuelve al inicio
4. Vuelve a paso 1

El resultado es que `ComprobarHoguera` nunca se activa. El agente persigue al ladrón indefinidamente sin verificar nunca si la hoguera sigue en pie, lo que hace la ejecución rara y hace que el agente no cumpla su función de proteger la hoguera en escenarios donde el ladrón es ágil o aparece/desaparece repetidamente.

**Solución propuesta:**

Añadir un mecanismo que garantice que, después de un número mínimo de ciclos de persecución sin éxito, el agente se fuerce a ir a comprobar la hoguera independientemente de si el ladrón vuelve a aparecer. Opciones concretas:

- Llevar un contador de cuántas veces se ha ejecutado `ResetearCicloCompleto()` sin que el agente haya llegado a `ComprobarHoguera`. Cuando ese contador supera un umbral (por ejemplo, 3 veces), forzar `busquedaAgotada = true` en el siguiente ciclo de pérdida del ladrón, sin permitir que una nueva visión lo resetee.
- Alternativamente, añadir un temporizador global de "tiempo máximo sin comprobar hoguera". Si han pasado más de N segundos desde la última vez que el agente visitó la hoguera (o desde que comenzó a haber alerta), forzar la comprobación.

En cualquier caso, el cambio debe hacerse en `SubsumptionController`, específicamente en la lógica de `ResetearCicloCompleto()` o en la condición de activación de `ComprobarHoguera`, para que la hoguera siempre se verifique periódicamente incluso si el ladrón sigue visible.
