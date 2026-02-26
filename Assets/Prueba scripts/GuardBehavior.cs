using UnityEngine;
using UnityEngine.AI;

public abstract class GuardBehavior : MonoBehaviour
{
    // Prioridad de la capa: el numero mas bajo es mas importante
    public int priority; 
    
    // Referencias para que las usen los scripts hijos
    protected NavMeshAgent agent;
    protected Transform thief;

    protected virtual void Awake()
    {
        // Obtenemos el componente de navegacion
        agent = GetComponent<NavMeshAgent>();
        
        // Buscamos al ladron por su Tag
        GameObject target = GameObject.FindGameObjectWithTag("Thief");
        if (target != null) thief = target.transform;
    }

    // Funcion de PERCEPCION: Decide si la capa quiere el control [cite: 1131, 1133]
    public abstract bool CanActivate();

    // Funcion de ACCION: Lo que hace el guardia si gana esta capa [cite: 1130]
    public abstract void Action();
}