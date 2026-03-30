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
    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        if (BusquedaLimitadaAgotada()) return false;
        if (veAlLadron) return false;
        if (posicionLadron == null) return false;

        return enAlerta || cronometroBusqueda > 0;
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

    private bool BusquedaLimitadaAgotada()
    {
        if (tiempoLimiteBusqueda <= 0f) return false;
        return controller != null && controller.busquedaAgotada;
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
            if (EsAldeanoPrincipal)
                Debug.Log($"[BUSQUEDA {gameObject.name}]: Nuevo destino → {hit.position:F1}");
        }
        else
        {
            if (EsAldeanoPrincipal)
                Debug.Log($"<color=orange>[BUSQUEDA {gameObject.name}]: NavMesh.SamplePosition falló cerca de {destinoAleatorio:F1}</color>");
        }
    }
}