using UnityEngine;

public class ComprobarHoguera : GuardBehavior
{
    public Transform posicionHoguera;
    public float distanciaLlegada = 2f;
    public float velocidad = 7f;

    private bool haComprobado = false;

    public override bool CanActivate()
    {
        // Solo si ha visto al ladrón, lo ha perdido de vista, y la hoguera aún no ha sido robada
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
            Debug.Log($"{gameObject.name}: He comprobado la hoguera.");
        }
    }
}