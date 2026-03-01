using UnityEngine;

public class BloquearSalida : GuardBehavior
{
    public SensorHogueraIndividual sensor;
    public Transform puntoPuertaPueblo; 
    public override bool CanActivate()
    {
        return sensor != null && sensor.alarmaRoboDetectada;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = 10f;
        agent.SetDestination(puntoPuertaPueblo.position);
        
        Debug.Log("¡ALERTA! ¡Han robado la hoguera!");
    }
}