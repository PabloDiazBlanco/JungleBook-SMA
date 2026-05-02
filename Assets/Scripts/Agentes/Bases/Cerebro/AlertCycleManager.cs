using UnityEngine;

public class AlertCycleManager : MonoBehaviour
{
    [Header("Anti-bucle persecución")]
    public int maxInterrupcionesCiclo = 3;

    [Header("Búsqueda rotatoria (cada 3 ciclos)")]
    public float radioBusquedaAmplia = 25f;

    private AgentBlackboard bb;
    private AgentTimerManager timers;
    private GuardVision sensorVision;
    private Busqueda busquedaCache;
    private ComprobarHoguera comprobarCache;
    private SubsumptionController mediador;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    void Start()
    {
        bb           = GetComponent<AgentBlackboard>();
        timers       = GetComponent<AgentTimerManager>();
        sensorVision = GetComponent<GuardVision>();
        busquedaCache  = GetComponent<Busqueda>();
        comprobarCache = GetComponent<ComprobarHoguera>();
        mediador       = GetComponent<SubsumptionController>();

        if (busquedaCache != null)
            timers.SetCronometroLimite(busquedaCache.tiempoLimiteBusqueda);
    }

    // ===================== LOOP PRINCIPAL =====================

    public void ActualizarEstadoAlerta()
    {
        if (bb.veAlLadron)
        {
            ActivarAlertaPorVision();
            return;
        }
        if (bb.oyoAlgo)
        {
            ActivarAlertaPorSonido();
            return;
        }
        timers.ReducirBusqueda();
    }

    public void GestionarResetsPorTransicion()
    {
        if (bb.acabaDeVerAlLadron)
        {
            if (bb.enAlerta) bb.contadorResetsCiclo++;
            bb.ladronPerdidoConFuego = false;
            bb.busquedaEsPorFuego    = false;
            timers.ResetearSonido();
            ResetearCicloCompleto("Ladrón visible de nuevo. Ciclo reseteado.");
        }
        if (bb.acabaDePerderAlLadron)
        {
            timers.ResetearSonido();
            if (bb.ladronVisibleFrameAnteriorTeniaFuego)
            {
                bb.ladronPerdidoConFuego = true;
                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=red>[CEREBRO {gameObject.name}]: Perdí al ladrón CON el fuego. Manteniendo persecución hasta la última posición conocida.</color>");
            }
            else if (bb.contadorResetsCiclo >= maxInterrupcionesCiclo)
            {
                bb.contadorResetsCiclo = 0;
                bb.busquedaForzada     = true;
                bb.busquedaAgotada     = true;
                ResetearComprobacion();
                if (EsAldeanoPrincipal)
                    Debug.Log($"[CEREBRO {gameObject.name}]: {maxInterrupcionesCiclo} interrupciones sin comprobar hoguera. Forzando ComprobarHoguera.");
            }
            else
            {
                ResetearCicloCompleto("Ladrón perdido. Iniciando ciclo búsqueda → comprobar.");
            }
        }
    }

    public void AvanzarCronometroBusquedaLimitada()
    {
        if (!DebeAvanzarCronometroLimite()) return;
        timers.TickLimiteBusqueda();
        if (timers.LimiteBusquedaAgotado)
            MarcarBusquedaAgotada();
    }

    // ===================== NOTIFICACIÓN DESDE BEHAVIOR =====================

    public void NotificarLlegadaAUltimaPosicionConFuego()
    {
        bb.ladronPerdidoConFuego = false;
        bb.busquedaEsPorFuego    = true;
        bb.busquedaForzada       = false;
        bb.contadorResetsCiclo   = 0;
        ResetearCicloCompleto("Llegué a última posición del ladrón con fuego. Iniciando búsqueda local.");
    }

    // ===================== RESETS PÚBLICOS =====================

    public void ResetearBusqueda()
    {
        if (busquedaCache != null)
            timers.SetCronometroLimite(busquedaCache.tiempoLimiteBusqueda);
        if (!bb.busquedaForzada)
            bb.busquedaAgotada = false;
        timers.ResetearLimiteBusqueda();
    }

    public void ResetearComprobacion()
    {
        if (comprobarCache != null)
            comprobarCache.ResetearComprobacion();
    }

    // ===================== PRIVADO =====================

    private void ActivarAlertaPorVision()
    {
        bb.enAlerta             = true;
        bb.ultimaPosicionLadron = sensorVision.UltimaPosicionDetectada();
        timers.IniciarBusqueda();
    }

    private void ActivarAlertaPorSonido()
    {
        bb.enAlerta             = true;
        bb.ultimaPosicionLadron = bb.posicionRuido;
        timers.IniciarBusqueda();
    }

    private void ResetearCicloCompleto(string motivo)
    {
        bb.ciclosBusquedaCompletados = 0;
        ResetearBusqueda();
        ResetearComprobacion();
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: {motivo}");
    }

    private bool DebeAvanzarCronometroLimite()
    {
        if (!bb.enAlerta)          return false;
        if (bb.veAlLadron)         return false;
        if (bb.oyoAlgo)            return false;
        if (bb.posicionPuerta != null) return false;
        if (bb.busquedaAgotada)    return false;
        if (busquedaCache == null) return false;
        if (busquedaCache.tiempoLimiteBusqueda <= 0f) return false;
        return true;
    }

    private void MarcarBusquedaAgotada()
    {
        bb.busquedaAgotada = true;
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: Tiempo de búsqueda agotado.");
        if (bb.busquedaEsPorFuego)
            mediador.OnBusquedaAgotadaPorHoguera();
    }
}
