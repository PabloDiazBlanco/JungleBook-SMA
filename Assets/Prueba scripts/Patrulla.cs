using UnityEngine;
using System.Collections.Generic;

public class Patrulla : GuardBehavior
{
    // Lista de puntos por los que pasara el guardia
    public List<Transform> puntosControl = new List<Transform>();
    
    // Indice para saber a que punto vamos
    public int indiceActual = 0;
    
    // Distancia para considerar que hemos llegado al punto
    public float distanciaUmbral = 1.5f;

    // PERCEPCION: Siempre puede activarse si no hay nada mas prioritario
    public override bool CanActivate()
    {
        // Esta es la capa base (Nivel mas bajo de Brooks)
        // Solo se ejecutara si Persecucion y Busqueda dicen que NO pueden actuar
        return puntosControl.Count > 0;
    }

    // ACCION: Ir al siguiente punto de la lista
    public override void Action()
    {
        if (puntosControl.Count == 0) return;

        agent.speed = 2.0f;
        // Aseguramos que el agente esté "despierto" para recibir la nueva orden de patrulla
        if (agent.isStopped) agent.isStopped = false;

        agent.SetDestination(puntosControl[indiceActual].position);

        if (!agent.pathPending && agent.remainingDistance <= distanciaUmbral)
        {
            indiceActual = (indiceActual + 1) % puntosControl.Count;
        }
    }
}