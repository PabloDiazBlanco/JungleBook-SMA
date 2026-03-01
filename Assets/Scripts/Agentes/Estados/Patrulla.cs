using UnityEngine;
using System.Collections.Generic;

public class Patrulla : GuardBehavior
{
    public List<Transform> puntosControl = new List<Transform>();
    public int indiceActual = 0;
    public float distanciaUmbral = 1.5f;
    
    public override bool CanActivate()
    {
        return puntosControl.Count > 0;
    }

    public override void Action()
    {
        if (puntosControl.Count == 0) return;

        agent.speed = 2.0f;
        if (agent.isStopped) agent.isStopped = false;

        agent.SetDestination(puntosControl[indiceActual].position);

        if (!agent.pathPending && agent.remainingDistance <= distanciaUmbral)
        {
            indiceActual = (indiceActual + 1) % puntosControl.Count;
        }
    }
}