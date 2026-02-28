using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    public SensorPercepcionObjetos sensorObjetos;
    public Busqueda memoriaBusqueda; // Para saber si estamos buscando al ladrón

    public override bool CanActivate()
    {
        // PRIORIDAD LÓGICA:
        // Si estamos en modo Búsqueda (memoriaBusqueda.cronometro > 0) 
        // Y el sensor detecta una puerta...
        return memoriaBusqueda.cronometro > 0 && sensorObjetos.ultimaPuertaDetectada != null;
    }

    public override void Action()
    {
        if (agent == null || sensorObjetos.ultimaPuertaDetectada == null) return;

        agent.speed = 4.0f; // Velocidad de investigación
        // El aldeano se dirige a la puerta porque "sabe" que es un punto de interés
        agent.SetDestination(sensorObjetos.ultimaPuertaDetectada.position);
        
        Debug.Log("COMPORTAMIENTO: Investigando puerta detectada.");
    }
}