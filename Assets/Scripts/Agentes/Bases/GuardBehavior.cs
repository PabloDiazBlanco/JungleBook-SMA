using UnityEngine;
using UnityEngine.AI;

public abstract class GuardBehavior : MonoBehaviour
{
    public int priority;

    protected NavMeshAgent agent;

    protected bool veAlLadron;
    protected Vector3? posicionLadron;
    protected bool oyoAlgo;
    protected Vector3? posicionRuido;
    protected bool alarmaHogueraActiva;
    protected Vector3? posicionPuerta;
    protected float cronometroBusqueda;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void RecibirInformacion(
        bool veAlLadron,
        Vector3? posicionLadron,
        bool oyoAlgo,
        Vector3? posicionRuido,
        bool alarmaHogueraActiva,
        Vector3? posicionPuerta,
        float cronometroBusqueda)
    {
        this.veAlLadron = veAlLadron;
        this.posicionLadron = posicionLadron;
        this.oyoAlgo = oyoAlgo;
        this.posicionRuido = posicionRuido;
        this.alarmaHogueraActiva = alarmaHogueraActiva;
        this.posicionPuerta = posicionPuerta;
        this.cronometroBusqueda = cronometroBusqueda;
    }

    public abstract bool CanActivate();
    public abstract void Action();
}