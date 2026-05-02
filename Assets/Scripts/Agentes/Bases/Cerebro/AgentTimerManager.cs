using UnityEngine;

public class AgentTimerManager : MonoBehaviour
{
    [Header("Tiempos de configuración")]
    public float tiempoBusqueda              = 10f;
    public float tiempoGraciaPostComprobacion = 5f;
    public float tiempoCooldownInvestigacion  = 5f;
    public float tiempoCooldownPuerta         = 8f;

    // Cronómetros internos
    private float cronometroBusqueda       = 0f;
    private float cronometroLimiteBusqueda = 0f;
    private float cronometroGracia         = 0f;
    private float cronometroInvestigacion  = 0f;
    private float cronometroPuerta         = 0f;

    // Flags internos para evitar spam de logs
    private bool graciaLogueada                        = false;
    private bool cronometroLimiteBusquedaIniciadoLogueado = false;
    private bool puertaSupresionLogueada               = false;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    // ===================== TICK =====================

    public void Tick()
    {
        float dt = Time.deltaTime;

        if (cronometroGracia > 0f)
            cronometroGracia -= dt;

        if (cronometroInvestigacion > 0f)
        {
            cronometroInvestigacion -= dt;
            if (cronometroInvestigacion <= 0f && EsAldeanoPrincipal)
                Debug.Log($"[TIMERS {gameObject.name}]: Cooldown de sonido terminado.");
        }

        if (cronometroPuerta > 0f)
        {
            cronometroPuerta -= dt;
            if (cronometroPuerta <= 0f)
            {
                puertaSupresionLogueada = false;
                Debug.Log($"[TIMERS {gameObject.name}]: Cooldown de puerta terminado.");
            }
        }
    }

    public void TickLimiteBusqueda()
    {
        if (cronometroLimiteBusqueda <= 0f) return;

        if (EsAldeanoPrincipal && !cronometroLimiteBusquedaIniciadoLogueado)
        {
            Debug.Log($"[TIMERS {gameObject.name}]: Cronómetro de búsqueda iniciado — {cronometroLimiteBusqueda:F1}s restantes.");
            cronometroLimiteBusquedaIniciadoLogueado = true;
        }

        cronometroLimiteBusqueda -= Time.deltaTime;
    }

    // ===================== GETTERS LÓGICOS =====================

    public bool InvestigacionEnCooldown => cronometroInvestigacion > 0f;
    public bool PuertaEnCooldown        => cronometroPuerta > 0f;
    public bool GraciaActiva            => cronometroGracia > 0f;
    public bool LimiteBusquedaAgotado   => cronometroLimiteBusqueda <= 0f;
    public float CronometroBusqueda     => cronometroBusqueda;
    public bool PuertaSupresionLogueada
    {
        get => puertaSupresionLogueada;
        set => puertaSupresionLogueada = value;
    }
    public bool GraciaLogueada
    {
        get => graciaLogueada;
        set => graciaLogueada = value;
    }

    // ===================== CONTROL DE TIEMPOS =====================

    public void IniciarBusqueda()
    {
        cronometroBusqueda = tiempoBusqueda;
    }

    public void ReducirBusqueda()
    {
        if (cronometroBusqueda > 0f)
            cronometroBusqueda -= Time.deltaTime;
    }

    public void IniciarGracia()
    {
        cronometroGracia = tiempoGraciaPostComprobacion;
        graciaLogueada   = false;
    }

    public void IniciarCooldownSonido()
    {
        cronometroInvestigacion = tiempoCooldownInvestigacion;
    }

    public void ResetearSonido()
    {
        cronometroInvestigacion = 0f;
    }

    public void IniciarCooldownPuerta()
    {
        cronometroPuerta = tiempoCooldownPuerta;
    }

    public void SetCronometroLimite(float tiempo)
    {
        cronometroLimiteBusqueda               = tiempo;
        cronometroLimiteBusquedaIniciadoLogueado = false;
    }

    public void ResetearLimiteBusqueda()
    {
        cronometroLimiteBusquedaIniciadoLogueado = false;
    }

    public void ResetearCronometroBusqueda()
    {
        cronometroBusqueda = 0f;
    }
}
