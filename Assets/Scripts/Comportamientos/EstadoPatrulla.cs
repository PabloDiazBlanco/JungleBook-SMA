using UnityEngine;
using UnityEngine.AI; // Necesario para el NavMesh

public class EstadoPatrulla : EstadoAgente
{
    private int indicePuntoActual = 0;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("Entrando en modo Patrulla");
        IrAlSiguientePunto(agente);
    }

    public override void Execute(AgenteBase agente)
    {
        // 1. REGLA REACTIVA: Si en la memoria veo al ladrón, cambio de estado
        if (agente.memoria.veoAlLadron)
        {
            agente.CambiarEstado(new EstadoPersecucion());
            return;
        }

        // PRIORIDAD 2: Oído (Investigación)
        if (agente.memoria.escuchoAlLadron)
        {
            // El lobo no sabe dónde estás exactamente para morderte, 
            // pero sabe que hay ruido y va a mirar.
            agente.CambiarEstado(new EstadoBusqueda());
            return;
        }

        // 2. LÓGICA DE PATRULLA: Si llegué al destino, voy al siguiente
        NavMeshAgent nav = agente.GetComponent<NavMeshAgent>();
        if (!nav.pathPending && nav.remainingDistance < 0.5f)
        {
            IrAlSiguientePunto(agente);
        }
    }

    public override void Exit(AgenteBase agente)
    {
        Debug.Log("Saliendo de modo Patrulla");
    }

    private void IrAlSiguientePunto(AgenteBase agente)
    {
        // Supongamos que el agente tiene una lista de puntos en un script de configuración
        // Por ahora, si no tienes puntos, se quedará quieto o irá a una posición fija
        Vector3 proximoDestino = agente.ObtenerProximoPuntoPatrulla();
        agente.GetComponent<NavMeshAgent>().SetDestination(proximoDestino);
    }
}