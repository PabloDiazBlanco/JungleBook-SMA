using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class SubsumptionController : MonoBehaviour
{
    [Header("Módulos del Cerebro")]
    public AgentBlackboard blackboard;
    public AgentTimerManager timers;
    public PerceptionSync perceptionSync;
    public FireAlarmMonitor fireAlarm;
    public AlertCycleManager alertCycle;

    [Header("Sensores")]
    public GuardVision sensorVision;
    public GuardHearing sensorOido;
    public SensorHogueraIndividual sensorHoguera;
    public SensorPercepcionObjetos sensorObjetos;

    [Header("Comportamientos")]
    public List<GuardBehavior> behaviors = new List<GuardBehavior>();

    [Header("Capa Deliberativa")]
    public DeliberativeLayer deliberativa;

    private NavMeshAgent agent;
    private GuardBehavior capaAnterior;

    private AgentBlackboard bb => blackboard;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    // API pública para behaviors que leen directamente del controller
    public bool busquedaAgotada          => bb.busquedaAgotada;
    public int  ciclosBusquedaCompletados => bb.ciclosBusquedaCompletados;
    public bool investigacionEnCooldown  => timers.InvestigacionEnCooldown;

    // ===================== INICIALIZACIÓN =====================

    void Start()
    {
        InicializarComponentes();
        InicializarBehaviors();
    }

    private void InicializarComponentes()
    {
        agent = GetComponent<NavMeshAgent>();

        if (blackboard == null)    blackboard    = GetComponent<AgentBlackboard>();
        if (timers == null)        timers        = GetComponent<AgentTimerManager>();
        if (perceptionSync == null) perceptionSync = GetComponent<PerceptionSync>();
        if (fireAlarm == null)     fireAlarm     = GetComponent<FireAlarmMonitor>();
        if (alertCycle == null)    alertCycle    = GetComponent<AlertCycleManager>();

        if (sensorVision == null)  sensorVision  = GetComponent<GuardVision>();
        if (sensorOido == null)    sensorOido    = GetComponent<GuardHearing>();
        if (sensorHoguera == null) sensorHoguera = GetComponent<SensorHogueraIndividual>();
        if (sensorObjetos == null) sensorObjetos = GetComponent<SensorPercepcionObjetos>();
    }

    private void InicializarBehaviors()
    {
        behaviors.AddRange(GetComponents<GuardBehavior>());
        behaviors.Sort(CompararPrioridades);
    }

    private int CompararPrioridades(GuardBehavior a, GuardBehavior b)
    {
        return a.priority.CompareTo(b.priority);
    }

    // ===================== UPDATE =====================

    void Update()
    {
        perceptionSync.Sincronizar();
        alertCycle.ActualizarEstadoAlerta();
        alertCycle.GestionarResetsPorTransicion();
        alertCycle.AvanzarCronometroBusquedaLimitada();
        timers.Tick();
        fireAlarm?.Evaluar();
        perceptionSync.RegistrarFrame();
        deliberativa?.Procesar();
        SincronizarConCapaDeliberativa();
        PropagarInformacionACapas();
        EjecutarDecision();
    }

    // ===================== MEDIADOR: HOGUERA =====================

    public void OnBusquedaAgotadaPorHoguera()
    {
        fireAlarm.ActivarAlarmaHoguera();
    }

    public void OnComprobacionHogueraExitosa()
    {
        alertCycle.ResetearBusqueda();
        alertCycle.ResetearComprobacion();
        bool cicloAmplio = bb.ciclosBusquedaCompletados % 3 == 0;
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: Ciclo {bb.ciclosBusquedaCompletados} → búsqueda {(cicloAmplio ? "AMPLIA" : "normal")}.");
    }

    // ===================== PROPAGACIÓN A COMPORTAMIENTOS =====================

    private void PropagarInformacionACapas()
    {
        foreach (GuardBehavior capa in behaviors)
        {
            capa.RecibirInformacion(
                bb.veAlLadron,
                bb.ultimaPosicionLadron,
                bb.oyoAlgo,
                bb.posicionRuido,
                bb.alarmaHogueraActiva,
                bb.posicionPuerta,
                timers.CronometroBusqueda,
                bb.enAlerta,
                bb.ladronTieneFuego,
                bb.ladronPerdidoConFuego
            );
        }
    }

    private void SincronizarConCapaDeliberativa()
    {
        if (deliberativa != null && deliberativa.creencias.posicionLadron == null)
        {
            // Si el agente tiene rol Perseguidor y aún tiene cronómetro de búsqueda activo,
            // no borrar la última posición: debe investigar el entorno donde perdió al ladrón.
            bool esPerseguidor = deliberativa.creencias.rolActual == BeliefBase.RolCNP.Perseguidor;
            bool busquedaViva  = timers.CronometroBusqueda > 0f;
            if (esPerseguidor && busquedaViva) return;

            if (!bb.veAlLadron && !bb.oyoAlgo && bb.ultimaPosicionLadron != null)
            {
                bb.ultimaPosicionLadron = null;
                bb.enAlerta             = false;
                timers.ResetearCronometroBusqueda();
                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=gray>[CEREBRO {gameObject.name}]: Sincronización TTL — Olvidando posición inyectada por obsolescencia.</color>");
            }
        }
    }

    // ===================== SUBSUMPTION =====================

    public void EjecutarDecision()
    {
        foreach (GuardBehavior capa in behaviors)
        {
            if (capa.CanActivate())
            {
                CambiarCapaSiNecesario(capa);
                capa.Action();
                return;
            }
        }
    }

    private void CambiarCapaSiNecesario(GuardBehavior nuevaCapa)
    {
        if (capaAnterior == nuevaCapa) return;
        if (agent != null) agent.ResetPath();
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: Cambio a: {nuevaCapa.GetType().Name}");
        capaAnterior = nuevaCapa;
    }

    // ===================== NOTIFICACIONES DESDE COMPORTAMIENTOS =====================

    public void NotificarInvestigacionPuertaCompletada()
    {
        timers.IniciarCooldownPuerta();
    }

    public void NotificarInvestigacionRuidoCompletada()
    {
        if (sensorOido != null)
        {
            sensorOido.ResetearAudicion();
            timers.IniciarCooldownSonido();
            if (EsAldeanoPrincipal)
                Debug.Log($"[CEREBRO {gameObject.name}]: Investigación de ruido completada. Cooldown {timers.tiempoCooldownInvestigacion:F1}s activo.");
        }
    }

    public void NotificarComprobacionHogueraCompletada()
    {
        fireAlarm.NotificarComprobacionHogueraCompletada();
    }

    public void NotificarLlegadaAUltimaPosicionConFuego()
    {
        alertCycle.NotificarLlegadaAUltimaPosicionConFuego();
    }

    // ===================== PROPIEDADES PÚBLICAS (leídas por DeliberativeLayer) =====================

    public bool VeAlLadron               => bb.veAlLadron;
    public bool AcabaDeVerAlLadron       => bb.acabaDeVerAlLadron;
    public bool LadronTieneFuego         => bb.ladronTieneFuego;
    public bool AlarmaHogueraActiva      => bb.alarmaHogueraActiva;
    public bool EnAlerta                 => bb.enAlerta;
    public Vector3? UltimaPosicionLadron => bb.ultimaPosicionLadron;

    // ===================== INYECCIÓN DESDE CAPA DELIBERATIVA =====================

    public void InyectarPosicionLadron(Vector3 pos)
    {
        bb.enAlerta             = true;
        bb.ultimaPosicionLadron = pos;
        timers.IniciarBusqueda();
        if (deliberativa != null)
        {
            deliberativa.creencias.posicionLadron          = pos;
            deliberativa.creencias.timestampPosicionLadron = Time.time;
        }
    }

    public void InyectarAlarmaHoguera()
    {
        bb.alarmaHogueraActiva = true;
    }

    public void InyectarCubrirHoguera()
    {
        bb.enAlerta        = true;
        bb.busquedaAgotada = true;
    }
}