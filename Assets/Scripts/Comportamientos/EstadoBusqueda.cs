using UnityEngine;

public class EstadoBusqueda : EstadoAgente
{
    private float tiempoBusqueda = 4f;
    private float cronometro = 0f;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("<color=orange>LOBO:</color> Investigando última zona conocida.");
        agente.nav.speed = 1.0f;
        agente.nav.SetDestination(agente.memoria.ultimaPosicionConocida);
    }

    public override void Execute(AgenteBase agente)
    {
        VerificarSiVuelveAVerlo(agente);
        GirarYEsperar(agente);
    }

    private void VerificarSiVuelveAVerlo(AgenteBase agente)
    {
        if (agente.memoria.veoAlLadron)
        {
            agente.CambiarEstado(new EstadoPersecucion());
        }
    }

    private void GirarYEsperar(AgenteBase agente)
    {
        if (agente.nav.remainingDistance < 0.5f)
        {
            cronometro += Time.deltaTime;
            agente.transform.Rotate(Vector3.up, 120f * Time.deltaTime);

            if (cronometro >= tiempoBusqueda)
            {
                Debug.Log("<color=cyan>LOBO:</color> Zona despejada. Retomando patrulla.");
                agente.CambiarEstado(new EstadoPatrulla());
            }
        }
    }

    public override void Exit(AgenteBase agente) {}
}