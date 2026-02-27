using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // Importante para que reconozca el NavMeshAgent

public class SubsumptionController : MonoBehaviour
{
    public List<GuardBehavior> behaviors = new List<GuardBehavior>();
    
    // Referencias internas para la gestion de capas
    private NavMeshAgent agent;
    private GuardBehavior capaAnterior;

    void Start()
    {
        // 1. Obtenemos el componente de navegacion del guardia
        agent = GetComponent<NavMeshAgent>();

        // 2. Buscamos todos los componentes que heredan de GuardBehavior
        behaviors.AddRange(GetComponents<GuardBehavior>());

        // 3. Ordenamos la lista de forma tradicional por prioridad
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
                // Si la capa que gana ahora es distinta a la de hace un momento
                if (capaAnterior != capa)
                {
                    // Limpiamos la ruta anterior para que no haya conflictos
                    // Esto es lo que garantiza que la Busqueda tome el mando real
                    if (agent != null) agent.ResetPath();
                    
                    Debug.Log("Cambio de comportamiento a: " + capa.GetType().Name);
                    capaAnterior = capa;
                }

                // Ejecutamos la accion de la capa ganadora
                capa.Action();
                return; 
            }
        }
    }
}