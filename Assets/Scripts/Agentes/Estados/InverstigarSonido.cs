using UnityEngine;

public class InvestigarSonido : GuardBehavior
{
    public GuardHearing sensorOido;
    public float velocidadInvestigacion = 6.0f;
    public float distanciaLlegada = 1.5f;

    void Start()
    {
        if (sensorOido == null) sensorOido = GetComponent<GuardHearing>();
    }

    public override bool CanActivate()
    {
        return oyoAlgo;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = velocidadInvestigacion;
        agent.SetDestination(posicionRuido.Value);

        if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
        {
            if (sensorOido != null) sensorOido.ResetearAudicion();
            Debug.Log($"<color=yellow>LOG: {gameObject.name} ha llegado al origen del ruido y no hay nada.</color>");
        }

        Debug.DrawLine(transform.position, posicionRuido.Value, Color.yellow);
    }
}
