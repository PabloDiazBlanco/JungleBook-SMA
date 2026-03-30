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
    protected bool enAlerta;
    protected bool ladronTieneFuego;
    protected bool ladronPerdidoConFuego;

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
        float cronometroBusqueda,
        bool enAlerta,
        bool ladronTieneFuego,
        bool ladronPerdidoConFuego)
    {
        this.veAlLadron = veAlLadron;
        this.posicionLadron = posicionLadron;
        this.oyoAlgo = oyoAlgo;
        this.posicionRuido = posicionRuido;
        this.alarmaHogueraActiva = alarmaHogueraActiva;
        this.posicionPuerta = posicionPuerta;
        this.cronometroBusqueda = cronometroBusqueda;
        this.enAlerta = enAlerta;
        this.ladronTieneFuego = ladronTieneFuego;
        this.ladronPerdidoConFuego = ladronPerdidoConFuego;
    }

    public abstract bool CanActivate();
    public abstract void Action();
}