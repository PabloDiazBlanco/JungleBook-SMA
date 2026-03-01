using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    public SensorPercepcionObjetos sensorObjetos;
    public Busqueda memoriaBusqueda; 

    public override bool CanActivate()
    {
        return memoriaBusqueda.cronometro > 0 && sensorObjetos.ultimaPuertaDetectada != null;
    }

    public override void Action()
    {
        if (agent == null || sensorObjetos.ultimaPuertaDetectada == null) return;

        agent.speed = 4.0f; 
        agent.SetDestination(sensorObjetos.ultimaPuertaDetectada.position);
        
        Debug.Log("COMPORTAMIENTO: Investigando puerta detectada.");
    }
}