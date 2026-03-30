using UnityEngine;

public class Persecucion : GuardBehavior
{
    private SubsumptionController controller;
    public float distanciaLlegada = 1.5f;
    public float velocidadNormal = 8f;
    public float velocidadConFuego = 10f;
    
    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        if (veAlLadron) return true;
        return ladronPerdidoConFuego;
    }

    public override void Action()
    {
        if (posicionLadron == null) return;

        agent.speed = (ladronTieneFuego || ladronPerdidoConFuego) ? velocidadConFuego : velocidadNormal;
        agent.SetDestination(posicionLadron.Value);

        if (ladronPerdidoConFuego && !veAlLadron)
        {
            if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
            {
                controller?.NotificarLlegadaAUltimaPosicionConFuego();
            }
        }
    }
}