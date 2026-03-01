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
        return sensorOido != null && sensorOido.EscuchoAlgo();
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = velocidadInvestigacion;
        Vector3 destinoRuido = sensorOido.GetPosicionRuido();
        agent.SetDestination(destinoRuido);

        if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
        {
            sensorOido.ResetearAudicion();
            Debug.Log($"<color=yellow>LOG: {gameObject.name} ha llegado al origen del ruido y no hay nada.</color>");
        }

        Debug.DrawLine(transform.position, destinoRuido, Color.yellow);
    }
}