using UnityEngine;
public class ComprobarHoguera : GuardBehavior
{
    public Transform posicionHoguera;
    public float distanciaLlegada = 2f;
    public float velocidad = 7f;

    private bool haComprobado = false;
    private SubsumptionController controller;
    private Busqueda busquedaCache;
    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
        busquedaCache = GetComponent<Busqueda>();
    }

    public override bool CanActivate()
    {
        if (!enAlerta) return false;
        if (veAlLadron) return false;
        if (alarmaHogueraActiva) return false;
        if (haComprobado) return false;
        if (!TieneBusquedaLimitadaAgotada()) return false;

        return true;
    }

    public override void Action()
    {
        if (agent == null || posicionHoguera == null) return;

        agent.speed = velocidad;
        agent.SetDestination(posicionHoguera.position);

        if (!agent.pathPending && agent.remainingDistance <= distanciaLlegada)
        {
            ProcesarLlegadaAHoguera();
        }
    }

    private bool TieneBusquedaLimitadaAgotada()
    {
        if (controller == null) return true;
        if (busquedaCache == null) return true;
        if (busquedaCache.tiempoLimiteBusqueda <= 0f) return true;

        return controller.busquedaAgotada;
    }

    private void ProcesarLlegadaAHoguera()
    {
        if (EsAldeanoPrincipal)
            Debug.Log($"[COMPROBAR {gameObject.name}]: Llegué a la hoguera. Verificando presencia...");
        haComprobado = true;
        if (controller != null)
            controller.NotificarComprobacionHogueraCompletada();
    }

    public void ResetearComprobacion()
    {
        haComprobado = false;
    }
}
