using UnityEngine;
using UnityEngine.AI;

public class Busqueda : GuardBehavior
{
    public float radioInspeccion = 5f;

    [Header("Límite de Búsqueda (0 = sin límite)")]
    [Tooltip("Si es mayor que 0, el SubsumptionController contará este tiempo antes de pasar a ComprobarHoguera.")]
    public float tiempoLimiteBusqueda = 0f;

    private float tiempoProximoPunto = 0f;
    private SubsumptionController controller;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        // Si hay límite y se agotó, no activar
        if (tiempoLimiteBusqueda > 0f && controller != null && controller.busquedaAgotada) return false;

        if (enAlerta && posicionLadron != null && !veAlLadron) return true;
        return !veAlLadron && posicionLadron != null && cronometroBusqueda > 0;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = 6.0f;

        if (!agent.pathPending && agent.remainingDistance < 0.5f && Time.time >= tiempoProximoPunto)
        {
            CalcularSiguientePuntoInspeccion();
        }

        Debug.DrawLine(transform.position, agent.destination, Color.red);
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