using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    private bool yaLogueado = false;

    public override bool CanActivate()
    {
        return cronometroBusqueda > 0 && posicionPuerta != null;
    }

    public override void Action()
    {
        if (agent == null || posicionPuerta == null) return;

        agent.speed = 4.0f;
        agent.SetDestination(posicionPuerta.Value);

        if (!yaLogueado)
        {
            Debug.Log("COMPORTAMIENTO: Investigando puerta detectada.");
            yaLogueado = true;
        }
    }
}