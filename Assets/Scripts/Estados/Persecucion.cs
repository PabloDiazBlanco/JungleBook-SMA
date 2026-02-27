using UnityEngine;

public class Persecucion : GuardBehavior
{
    // Referencia al sensor que hicimos antes
    public GuardVision sensor;

    void Start()
    {
        // Buscamos el sensor en este mismo objeto
        if (sensor == null)
        {
            sensor = GetComponent<GuardVision>();
        }
    }

    // PERCEPCION: ¿Se cumplen las condiciones?
    public override bool CanActivate()
    {
        // Esta capa solo quiere el control si el sensor ve al ladron
        if (sensor != null)
        {
            return sensor.PuedeVerAlLadron();
        }
        return false;
    }

    // ACCION: Que hace el guardia
    public override void Action()
    {
        // El agente se mueve directamente a la posicion del ladron
        // Como el ladron se mueve con WASD, el NavMesh actualiza la ruta constantemente
        agent.speed = 8.0f;
        agent.SetDestination(thief.position);
    }
}