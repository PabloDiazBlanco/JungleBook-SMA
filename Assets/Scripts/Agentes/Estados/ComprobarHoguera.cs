using UnityEngine;

public class ComprobarHoguera : GuardBehavior
{
    public Transform posicionHoguera;
    public float distanciaLlegada = 2f;
    public float velocidad = 7f;

    private bool haComprobado = false;
    private SubsumptionController controller;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        // Solo activar cuando la búsqueda se ha agotado
        if (controller != null)
        {
            Busqueda busqueda = GetComponent<Busqueda>();
            if (busqueda != null && busqueda.tiempoLimiteBusqueda > 0f && !controller.busquedaAgotada)
                return false;
        }

        return enAlerta && !veAlLadron && !alarmaHogueraActiva && !haComprobado;
    }

    public override void Action()
    {
        if (agent == null || posicionHoguera == null) return;

        agent.speed = velocidad;
        agent.SetDestination(posicionHoguera.position);

        if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
        {
            haComprobado = true;
            Debug.Log($"{gameObject.name}: He comprobado la hoguera. Fuego presente: {!alarmaHogueraActiva}");

            if (!alarmaHogueraActiva && controller != null)
            {
                // Fuego sigue ahí — reiniciar ciclo búsqueda → comprobar
                controller.ResetearBusqueda();
                haComprobado = false;
                Debug.Log($"{gameObject.name}: Fuego a salvo. Reiniciando búsqueda cerca de la hoguera.");
            }
            // Si alarmaHogueraActiva es true, BloquearSalida tomará el control
        }
    }

    public void ResetearComprobacion()
    {
        haComprobado = false;
    }
}