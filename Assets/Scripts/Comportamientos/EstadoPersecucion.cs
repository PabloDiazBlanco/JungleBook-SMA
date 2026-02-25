using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // <-- IMPORTANTE: Para poder reiniciar la escena

public class EstadoPersecucion : EstadoAgente
{
    private NavMeshAgent nav;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("¡Te he visto! Iniciando persecución.");
        nav = agente.GetComponent<NavMeshAgent>();
        nav.ResetPath(); 
        nav.speed = 5.0f; 
        agente.memoria.nivelDeAlerta = 1.0f;
    }

    public override void Execute(AgenteBase agente)
    {
        if (agente.memoria.veoAlLadron)
        {
            nav.SetDestination(agente.memoria.ultimaPosicionConocida);

            // CONDICIÓN DE CAPTURA
            // Si el guardia está muy cerca de ti y te ve... ¡Game Over!
            float distanciaAlLadron = Vector3.Distance(agente.transform.position, agente.memoria.ultimaPosicionConocida);
            
            if (distanciaAlLadron < 1.5f)
            {
                CapturarJugador();
            }
        }
        else
        {
            agente.CambiarEstado(new EstadoBusqueda());
        }
    }

    private void CapturarJugador()
    {
        Debug.Log("¡ATRAPADO! Reiniciando nivel...");
        
        // Reinicia la escena actual
        Scene escenaActiva = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActiva.name);
    }

    public override void Exit(AgenteBase agente)
    {
        nav.speed = 3.5f;
    }
}