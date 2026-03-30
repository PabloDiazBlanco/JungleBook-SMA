using UnityEngine;

public class InvestigarSonido : GuardBehavior
{
    public GuardHearing sensorOido;
    public float velocidadInvestigacion = 6.0f;
    public float distanciaLlegada = 1.5f;

    private SubsumptionController controller;


    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        if (controller != null && controller.investigacionEnCooldown) return false;
        if (controller != null && controller.busquedaAgotada) return false;
        return oyoAlgo;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = velocidadInvestigacion;
        agent.SetDestination(posicionRuido.Value);

        if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
        {
            NotificarLlegadaAlCerebro();
        }

        Debug.DrawLine(transform.position, posicionRuido.Value, Color.yellow);
    }

    private void NotificarLlegadaAlCerebro()
    {
        if (controller != null)
        {
            controller.NotificarInvestigacionRuidoCompletada();
        }

        Debug.Log($"<color=yellow>LOG: {gameObject.name} ha llegado al origen del ruido y no hay nada.</color>");
    }
}
