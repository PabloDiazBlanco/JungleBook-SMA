using UnityEngine;
using UnityEngine.SceneManagement;

public class EstadoPersecucion : EstadoAgente
{
    private float tiempoParaRendirse = 1.5f; // Aumentado para que no te pierda tan rápido
    private float cronometroPerdida = 0f;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("<color=red>LOBO:</color> ¡Iniciando persecución cerrada!");
        AplicarModoCaza(agente);
        cronometroPerdida = 0f; 
    }

    public override void Execute(AgenteBase agente)
    {
        MoverHaciaLadron(agente);
        ValidarContinuidad(agente);
    }

    private void AplicarModoCaza(AgenteBase agente)
    {
        agente.nav.speed = 6.5f; 
        agente.nav.acceleration = 250f; 
        agente.nav.autoBraking = false;
        // Bajamos el stopping distance para que se pegue más físicamente
        agente.nav.stoppingDistance = 0.1f; 
    }

    private void MoverHaciaLadron(AgenteBase agente)
    {
        agente.nav.SetDestination(agente.memoria.ultimaPosicionConocida);
        ComprobarContactoFisico(agente);
    }

    private void ComprobarContactoFisico(AgenteBase agente)
    {
        float distanciaReal = Vector3.Distance(agente.transform.position, agente.memoria.ultimaPosicionConocida);
        
        // REDUCIDO: Ahora tiene que estar a 0.6 unidades (casi tocándote) para ganar
        if (distanciaReal < 0.6f) 
        {
            ReiniciarNivel();
        }
    }

    private void ValidarContinuidad(AgenteBase agente)
    {
        if (agente.memoria.veoAlLadron)
        {
            cronometroPerdida = 0f;
        }
        else
        {
            cronometroPerdida += Time.deltaTime;
            
            if (cronometroPerdida >= tiempoParaRendirse)
            {
                agente.CambiarEstado(new EstadoBusqueda());
            }
        }
    }

    private void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public override void Exit(AgenteBase agente) { }
}