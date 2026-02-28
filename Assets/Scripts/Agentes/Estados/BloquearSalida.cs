using UnityEngine;

public class BloquearSalida : GuardBehavior
{
    public SensorHogueraIndividual sensor;
    public Transform puntoPuertaPueblo; // Referencia al destino de bloqueo
    public override bool CanActivate()
    {
        // Solo se activa para este individuo si ÉL detectó el robo
        return sensor != null && sensor.alarmaRoboDetectada;
    }

    public override void Action()
    {
        if (agent == null) return;

        // ¡Corre a la puerta!
        agent.speed = 10f; // Velocidad de pánico
        agent.SetDestination(puntoPuertaPueblo.position);
        
        Debug.Log("¡ALERTA! ¡Han robado la hoguera!");
    }
}