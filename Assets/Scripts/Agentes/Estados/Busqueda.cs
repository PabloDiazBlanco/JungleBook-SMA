using UnityEngine;
using UnityEngine.AI;

public class Busqueda : GuardBehavior
{
    public float radioInspeccion = 5f;

    [Header("Límite de Búsqueda (0 = sin límite)")]
    [Tooltip("Si es mayor que 0, este agente solo buscará este tiempo antes de pasar al siguiente comportamiento.")]
    public float tiempoLimiteBusqueda = 0f;

    private float tiempoProximoPunto = 0f;
    private float cronometroLimiteBusqueda = 0f;
    private bool busquedaAgotada = false;

    // Guardamos si el ladrón era visible el frame anterior para detectar el momento en que se pierde
    private bool ladronVisibleFrameAnterior = false;

    public override bool CanActivate()
    {
        if (tiempoLimiteBusqueda > 0f && busquedaAgotada) return false;

        if (enAlerta && posicionLadron != null && !veAlLadron) return true;
        return !veAlLadron && posicionLadron != null && cronometroBusqueda > 0;
    }

    public override void Action()
    {
        if (agent == null) return;

        // Si acaba de perder de vista al ladrón este frame, reiniciar el cronómetro
        if (ladronVisibleFrameAnterior && !veAlLadron)
        {
            ResetearLimiteBusqueda();
        }
        ladronVisibleFrameAnterior = veAlLadron;

        // Gestionar cronómetro límite
        if (tiempoLimiteBusqueda > 0f)
        {
            if (cronometroLimiteBusqueda <= 0f)
                cronometroLimiteBusqueda = tiempoLimiteBusqueda;

            cronometroLimiteBusqueda -= Time.deltaTime;

            if (cronometroLimiteBusqueda <= 0f)
            {
                busquedaAgotada = true;
                return;
            }
        }

        agent.speed = 6.0f;

        if (!agent.pathPending && agent.remainingDistance < 0.5f && Time.time >= tiempoProximoPunto)
        {
            CalcularSiguientePuntoInspeccion();
        }

        Debug.DrawLine(transform.position, agent.destination, Color.red);
    }

    public void ResetearLimiteBusqueda()
    {
        busquedaAgotada = false;
        cronometroLimiteBusqueda = tiempoLimiteBusqueda;
    }

    private void CalcularSiguientePuntoInspeccion()
    {
        if (posicionLadron == null) return;

        Vector2 circuloAleatorio = Random.insideUnitCircle * radioInspeccion;
        Vector3 destinoAleatorio = posicionLadron.Value + new Vector3(circuloAleatorio.x, 0, circuloAleatorio.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destinoAleatorio, out hit, radioInspeccion, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            tiempoProximoPunto = Time.time + 1.5f;
        }
    }
}