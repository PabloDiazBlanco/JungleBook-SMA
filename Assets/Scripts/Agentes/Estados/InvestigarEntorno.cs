using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    public SensorPercepcionObjetos sensorObjetos;
    public Busqueda memoriaBusqueda; 

    public override bool CanActivate()
    {
        return cronometroBusqueda > 0 && posicionPuerta != null;
    }

    public override void Action()
    {
        if (agent == null || posicionPuerta == null) return;

        agent.speed = 4.0f; 
        agent.SetDestination(posicionPuerta.Value);
        
        Debug.Log("COMPORTAMIENTO: Investigando puerta detectada.");
    }
}