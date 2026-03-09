using UnityEngine;

public class ComprobarHoguera : GuardBehavior
{
    public Transform posicionHoguera;
    public float distanciaLlegada = 2f;
    public float velocidad = 7f;

    private bool haComprobado = false;

    public override bool CanActivate()
    {
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

    public void ResetearComprobacion()
    {
        haComprobado = false;
    }
}