using UnityEngine;
using UnityEngine.AI;

public abstract class GuardBehavior : MonoBehaviour
{
    // Prioridad de la capa: el numero mas bajo es mas importante
    public int priority; 
    
    protected NavMeshAgent agent;
    protected Transform thief;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        
        GameObject target = GameObject.FindGameObjectWithTag("Thief");
        if (target != null) thief = target.transform;
    }

    public abstract bool CanActivate();

    public abstract void Action();
}