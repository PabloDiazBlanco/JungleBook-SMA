using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; 

public class SubsumptionController : MonoBehaviour
{
    public List<GuardBehavior> behaviors = new List<GuardBehavior>();
    private NavMeshAgent agent;
    private GuardBehavior capaAnterior;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        behaviors.AddRange(GetComponents<GuardBehavior>());
        behaviors.Sort(CompararPrioridades);
    }

    private int CompararPrioridades(GuardBehavior a, GuardBehavior b)
    {
        return a.priority.CompareTo(b.priority);
    }

    void Update()
    {
        EjecutarDecision();
    }

    public void EjecutarDecision()
    {
        foreach (GuardBehavior capa in behaviors)
        {
            if (capa.CanActivate())
            {
                if (capaAnterior != capa)
                {
                    if (agent != null) agent.ResetPath();
                    
                    Debug.Log("Cambio de comportamiento a: " + capa.GetType().Name);
                    capaAnterior = capa;
                }

                capa.Action();
                return; 
            }
        }
    }
}