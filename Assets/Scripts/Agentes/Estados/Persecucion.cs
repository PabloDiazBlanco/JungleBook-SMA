using UnityEngine;

public class Persecucion : GuardBehavior
{
    public override bool CanActivate()
    {
        return veAlLadron;
    }

    public override void Action()
    {
        if (posicionLadron == null) return;
        agent.speed = 8.0f;
        agent.SetDestination(posicionLadron.Value);
    }
}