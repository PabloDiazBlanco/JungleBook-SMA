using UnityEngine;
using UnityEngine.AI;

public class EstadoBusqueda : EstadoAgente
{
    private float tiempoBusqueda = 8f; // Tiempo que se queda buscando
    private float cronometro = 0f;
    private NavMeshAgent nav;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("Te he perdido... buscando en la última posición.");
        nav = agente.GetComponent<NavMeshAgent>();
        cronometro = 0f;
        
        // Vamos al último sitio donde la memoria dice que estuviste
        nav.SetDestination(agente.memoria.ultimaPosicionConocida);
        nav.speed = 2.5f; // Camina cauteloso
    }

    public override void Execute(AgenteBase agente)
    {
        // REGLA REACTIVA: Si te vuelve a ver mientras busca, ¡A por ti!
        if (agente.memoria.veoAlLadron)
        {
            agente.CambiarEstado(new EstadoPersecucion());
            return;
        }

        // Si llega al sitio, empieza a contar el tiempo
        if (!nav.pathPending && nav.remainingDistance < 0.5f)
        {
            cronometro += Time.deltaTime;
            // Aquí podrías hacer que el guardia rote sobre sí mismo para "mirar"
            agente.transform.Rotate(Vector3.up, 200f * Time.deltaTime);
        }

        // Si pasa el tiempo y no te encuentra, vuelve a su vida normal
        if (cronometro >= tiempoBusqueda)
        {
            agente.CambiarEstado(new EstadoPatrulla());
        }
    }

    public override void Exit(AgenteBase agente)
    {
        agente.memoria.nivelDeAlerta = 0f;
    }
}