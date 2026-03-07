using UnityEngine;

public class BloquearSalida : GuardBehavior
{
    public Transform puntoPuertaPueblo; 
    public override bool CanActivate()
    {
        return alarmaHogueraActiva;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = 10f;
        agent.SetDestination(puntoPuertaPueblo.position);
        
        Debug.Log("¡ALERTA! ¡Han robado la hoguera!");
    }
}