using UnityEngine;

public class EstadoPatrulla : EstadoAgente
{
    private int indicePunto = 0;

    public override void Enter(AgenteBase agente)
    {
        Debug.Log("<color=cyan>LOBO:</color> Iniciando Patrulla.");
        EstablecerConfiguracionFisica(agente);
        IrADestino(agente);
    }

    public override void Execute(AgenteBase agente)
    {
        VerificarAvistamiento(agente);
        VerificarLlegadaADestino(agente);
    }

    private void EstablecerConfiguracionFisica(AgenteBase agente)
    {
        agente.nav.speed = 2.0f;
        agente.nav.acceleration = 15f;
        agente.nav.autoBraking = true;
    }

    private void VerificarAvistamiento(AgenteBase agente)
    {
        if (agente.memoria.veoAlLadron)
        {
            Debug.Log("<color=red>LOBO:</color> ¡Objetivo detectado! Iniciando persecución.");
            agente.CambiarEstado(new EstadoPersecucion());
        }
    }

    private void VerificarLlegadaADestino(AgenteBase agente)
    {
        if (!agente.nav.pathPending && agente.nav.remainingDistance < 0.5f)
        {
            ActualizarSiguientePunto(agente);
        }
    }

    private void ActualizarSiguientePunto(AgenteBase agente)
    {
        indicePunto = (indicePunto + 1) % agente.puntosPatrulla.Count;
        IrADestino(agente);
    }

    private void IrADestino(AgenteBase agente)
    {
        if (agente.puntosPatrulla.Count > 0)
        {
            agente.nav.SetDestination(agente.puntosPatrulla[indicePunto].position);
        }
    }

    public override void Exit(AgenteBase agente) {}
}