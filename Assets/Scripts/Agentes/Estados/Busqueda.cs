using UnityEngine;
using UnityEngine.AI;

public class Busqueda : GuardBehavior
{

    public float radioInspeccion = 5f; 
    private float tiempoProximoPunto = 0f;

    public override bool CanActivate()
    {
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