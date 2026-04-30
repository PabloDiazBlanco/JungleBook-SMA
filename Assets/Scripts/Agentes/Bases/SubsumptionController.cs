using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class SubsumptionController : MonoBehaviour
{

    [Header("Sensores")]
    public GuardVision sensorVision;
    public GuardHearing sensorOido;
    public SensorHogueraIndividual sensorHoguera;
    public SensorPercepcionObjetos sensorObjetos;

    [Header("Comportamientos")]
    public List<GuardBehavior> behaviors = new List<GuardBehavior>();

    [Header("Configuración")]
    public float tiempoBusqueda = 10f;

    [Header("Filtro hoguera")]
    public int framesParaAlarmaHoguera = 10;

    private NavMeshAgent agent;
    private GuardBehavior capaAnterior;

    private Busqueda busquedaCache;
    private ComprobarHoguera comprobarCache;

    private Vector3? ultimaPosicionLadron = null;
    private float cronometroBusqueda = 0f;
    private bool enAlerta = false;
    private bool ladronVisibleFrameAnterior = false;

    private float cronometroLimiteBusqueda = 0f;
    public bool busquedaAgotada = false;
    private bool cronometroLimiteBusquedaIniciadoLogueado = false;

    private bool alarmaHogueraActiva = false;

    private bool veAlLadron;
    private bool oyoAlgo;
    private Vector3? posicionRuido;
    private Vector3? posicionPuerta;
    private bool acabaDeVerAlLadron;
    private bool acabaDePerderAlLadron;

    // NUEVO: contador de frames sin ver la hoguera (filtro anti-oclusión)
    private int framesSinVerHoguera = 0;

    // Búsqueda rotatoria: cada 3 ciclos de ComprobarHoguera OK, el 3º usa radio amplio centrado en la hoguera
    [Header("Búsqueda rotatoria (cada 3 ciclos)")]
    public float radioBusquedaAmplia = 25f;
    public int ciclosBusquedaCompletados = 0;

    // Período de gracia tras ComprobarHoguera OK: bloquea EvaluarEstadoHoguera
    // mientras el agente aún está físicamente junto a la hoguera
    [Header("Gracia post-comprobación (segundos)")]
    public float tiempoGraciaPostComprobacion = 5f;
    private float cronometroGraciaPostComprobacion = 0f;
    private bool graciaLogueada = false;

    // Cooldown tras completar investigación de sonido: evita que InvestigarSonido
    // se reactive inmediatamente si el jugador sigue en rango de ruido
    [Header("Cooldown investigación sonido (segundos)")]
    public float tiempoCooldownInvestigacion = 5f;
    private float cronometroInvestigacion = 0f;
    public bool investigacionEnCooldown = false;

    // Cooldown tras investigar una puerta: evita que el agente vuelva a entrar en bucle
    [Header("Cooldown investigación puerta (segundos)")]
    public float tiempoCooldownPuerta = 8f;
    private float cronometroPuerta = 0f;
    private bool puertaEnCooldown = false;
    private bool puertaSupresionLogueada = false;
 
    // NUEVO: el sensor de visión detectó que el ladrón lleva el fuego
    private bool ladronTieneFuego = false;

    // NUEVO: guarda si el frame anterior el ladrón visible llevaba el fuego
    private bool ladronVisibleFrameAnteriorTeniaFuego = false;
 
    // NUEVO: helper para filtrar logs solo al Aldeano3
    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    private bool ladronPerdidoConFuego = false;
    private bool busquedaEsPorFuego = false;

    // Anti-bucle: fuerza ComprobarHoguera tras N interrupciones por visión del ladrón
    [Header("Anti-bucle persecución")]
    public int maxInterrupcionesCiclo = 3;
    private int contadorResetsCiclo = 0;
    private bool busquedaForzada = false;

    [Header("Capa Deliberativa")]
    public DeliberativeLayer deliberativa;

    // ===================== INICIALIZACIÓN =====================

    void Start()
    {
        InicializarComponentes();
        InicializarBehaviors();
    }

    private void InicializarComponentes()
    {
        agent = GetComponent<NavMeshAgent>();

        if (sensorVision == null) sensorVision = GetComponent<GuardVision>();
        if (sensorOido == null) sensorOido = GetComponent<GuardHearing>();
        if (sensorHoguera == null) sensorHoguera = GetComponent<SensorHogueraIndividual>();
        if (sensorObjetos == null) sensorObjetos = GetComponent<SensorPercepcionObjetos>();

        busquedaCache = GetComponent<Busqueda>();
        comprobarCache = GetComponent<ComprobarHoguera>();

        if (busquedaCache != null)
            cronometroLimiteBusqueda = busquedaCache.tiempoLimiteBusqueda;
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

    // ===================== UPDATE — Solo llamadas a métodos =====================

    void Update()
    {
        LeerSensores();
        DetectarTransiciones();
        ActualizarEstadoAlerta();
        GestionarResetsPorTransicion();
        AvanzarCronometroBusquedaLimitada();
        AvanzarGraciaPostComprobacion();
        AvanzarCooldownInvestigacion();
        AvanzarCooldownPuerta();
        EvaluarEstadoHoguera();
        RegistrarFrameAnterior();
        deliberativa?.Procesar();
        PropagarInformacionACapas();
        EjecutarDecision();
    }

    // ===================== 1. LECTURA DE SENSORES =====================

    private void LeerSensores()
    {
        veAlLadron = sensorVision != null && sensorVision.PuedeVerAlLadron();
        oyoAlgo = sensorOido != null && sensorOido.EscuchoAlgo();
        posicionRuido = oyoAlgo ? sensorOido.GetPosicionRuido() : (Vector3?)null;
        bool puertaFisicaDetectada = sensorObjetos != null && sensorObjetos.ultimaPuertaDetectada != null;
        if (puertaFisicaDetectada && puertaEnCooldown && !puertaSupresionLogueada)
        {
            Debug.Log($"[CEREBRO {gameObject.name}]: Puerta detectada físicamente pero suprimida por cooldown ({cronometroPuerta:F1}s restantes).");
            puertaSupresionLogueada = true;
        }
        if (!puertaEnCooldown) puertaSupresionLogueada = false;
        posicionPuerta = (!puertaEnCooldown && puertaFisicaDetectada)
            ? sensorObjetos.ultimaPuertaDetectada.position
            : (Vector3?)null;
        
        // NUEVO: leer si el ladrón lleva el fuego de la hoguera este frame
        ladronTieneFuego = veAlLadron && sensorVision.ladronTieneFuego;
    }

    // ===================== 2. DETECCIÓN DE TRANSICIONES =====================

    private void DetectarTransiciones()
    {
        acabaDeVerAlLadron = !ladronVisibleFrameAnterior && veAlLadron;
        acabaDePerderAlLadron = ladronVisibleFrameAnterior && !veAlLadron;
    }

    // ===================== 3. DECISIÓN: ESTADO DE ALERTA =====================

    private void ActualizarEstadoAlerta()
    {
        if (veAlLadron)
        {
            ActivarAlertaPorVision();
            return;
        }

        if (oyoAlgo)
        {
            ActivarAlertaPorSonido();
            return;
        }

        ReducirCronometroBusqueda();
    }

    private void ActivarAlertaPorVision()
    {
        enAlerta = true;
        ultimaPosicionLadron = sensorVision.UltimaPosicionDetectada();
        cronometroBusqueda = tiempoBusqueda;
    }

    private void ActivarAlertaPorSonido()
    {
        enAlerta = true;
        ultimaPosicionLadron = posicionRuido;
        cronometroBusqueda = tiempoBusqueda;
    }

    private void ReducirCronometroBusqueda()
    {
        if (cronometroBusqueda > 0)
            cronometroBusqueda -= Time.deltaTime;
    }

    // ===================== 4. DECISIÓN: RESETS POR TRANSICIÓN =====================

    private void GestionarResetsPorTransicion()
    {
        if (acabaDeVerAlLadron)
        {
            if (enAlerta) contadorResetsCiclo++;
            ladronPerdidoConFuego = false;
            busquedaEsPorFuego = false;
            investigacionEnCooldown = false;
            cronometroInvestigacion = 0f;
            ResetearCicloCompleto("Ladrón visible de nuevo. Ciclo reseteado.");
        }
        if (acabaDePerderAlLadron)
        {
            investigacionEnCooldown = false;
            cronometroInvestigacion = 0f;
            if (ladronVisibleFrameAnteriorTeniaFuego)
            {
                ladronPerdidoConFuego = true;
                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=red>[CEREBRO {gameObject.name}]: Perdí al ladrón CON el fuego. Manteniendo persecución hasta la última posición conocida.</color>");
            }
            else if (contadorResetsCiclo >= maxInterrupcionesCiclo)
            {
                contadorResetsCiclo = 0;
                busquedaForzada = true;
                busquedaAgotada = true;
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

    private void ResetearCicloCompleto(string motivo)
    {
        ciclosBusquedaCompletados = 0;
        ResetearBusqueda();
        ResetearComprobacion();
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: {motivo}");
    }

    // ===================== 5. DECISIÓN: BÚSQUEDA LIMITADA =====================

    private void AvanzarCronometroBusquedaLimitada()
    {
        if (!DebeAvanzarCronometroLimite()) return;

        if (EsAldeanoPrincipal && !cronometroLimiteBusquedaIniciadoLogueado)
        {
            Debug.Log($"[CEREBRO {gameObject.name}]: Cronómetro de búsqueda iniciado — {cronometroLimiteBusqueda:F1}s restantes.");
            cronometroLimiteBusquedaIniciadoLogueado = true;
        }

        cronometroLimiteBusqueda -= Time.deltaTime;

        if (cronometroLimiteBusqueda <= 0f)
            MarcarBusquedaAgotada();
    }

    private bool DebeAvanzarCronometroLimite()
    {
        if (!enAlerta) return false;
        if (veAlLadron) return false;
        if (oyoAlgo) return false;        // no consumir tiempo de búsqueda mientras se investiga un sonido
        if (posicionPuerta != null) return false; // no consumir tiempo de búsqueda mientras se investiga una puerta
        if (busquedaAgotada) return false;
        if (busquedaCache == null) return false;
        if (busquedaCache.tiempoLimiteBusqueda <= 0f) return false;
        return true;
    }

    private void AvanzarGraciaPostComprobacion()
    {
        if (cronometroGraciaPostComprobacion > 0f)
            cronometroGraciaPostComprobacion -= Time.deltaTime;
    }

    private void AvanzarCooldownInvestigacion()
    {
        if (cronometroInvestigacion > 0f)
        {
            cronometroInvestigacion -= Time.deltaTime;
            if (cronometroInvestigacion <= 0f)
            {
                investigacionEnCooldown = false;
                if (EsAldeanoPrincipal)
                    Debug.Log($"[CEREBRO {gameObject.name}]: Cooldown de investigación terminado. Puede volver a investigar sonidos.");
            }
        }
    }

    private void AvanzarCooldownPuerta()
    {
        if (cronometroPuerta > 0f)
        {
            cronometroPuerta -= Time.deltaTime;
            if (cronometroPuerta <= 0f)
            {
                puertaEnCooldown = false;
                Debug.Log($"[CEREBRO {gameObject.name}]: Cooldown de puerta terminado. Puede volver a detectar puertas.");
            }
        }
    }

    private void MarcarBusquedaAgotada()
    {
        busquedaAgotada = true;
        if (EsAldeanoPrincipal)
            Debug.Log($"[CEREBRO {gameObject.name}]: Tiempo de búsqueda agotado.");

        if (busquedaEsPorFuego)
            ActivarAlarmaHoguera();
    }

    public void NotificarLlegadaAUltimaPosicionConFuego()
    {
        ladronPerdidoConFuego = false;
        busquedaEsPorFuego = true;
        busquedaForzada = false;
        contadorResetsCiclo = 0;
        ResetearCicloCompleto("Llegué a última posición del ladrón con fuego. Iniciando búsqueda local.");
    }

    public void NotificarComprobacionHogueraCompletada()
    {
        // El agente está físicamente al lado de la hoguera: usamos OverlapSphere local
        // para una lectura fiable (el sensor de visión a distancia puede oscilar a 2m)
        bool hogueraPresente = ComprobarPresenciaFuegoLocal();

        if (hogueraPresente)
        {
            framesSinVerHoguera = 0;
            cronometroGraciaPostComprobacion = tiempoGraciaPostComprobacion;
            graciaLogueada = false;
            ciclosBusquedaCompletados++;
            busquedaForzada = false;
            contadorResetsCiclo = 0;
            ResetearBusqueda();
            ResetearComprobacion();
            bool cicloAmplio = ciclosBusquedaCompletados % 3 == 0;
            if (EsAldeanoPrincipal)
                Debug.Log($"[CEREBRO {gameObject.name}]: Comprobación OK — hoguera intacta. Gracia de {tiempoGraciaPostComprobacion}s activa. Ciclo {ciclosBusquedaCompletados} → búsqueda {(cicloAmplio ? "AMPLIA" : "normal")}.");
        }
        else
        {
            if (EsAldeanoPrincipal)
                Debug.Log($"<color=red>[CEREBRO {gameObject.name}]: Comprobación fallida — hoguera ausente. Activando alarma.</color>");
            ActivarAlarmaHoguera();
        }
    }

    private bool ComprobarPresenciaFuegoLocal()
    {
        if (sensorHoguera == null || sensorHoguera.posicionHogueraConocida == null) return false;

        Collider[] cols = Physics.OverlapSphere(
            sensorHoguera.posicionHogueraConocida.Value,
            sensorHoguera.radioDeteccion * 0.5f,
            sensorHoguera.capaHoguera
        );
        foreach (Collider col in cols)
        {
            if (col.CompareTag("FuegoHoguera")) return true;
        }
        return false;
    }

    // ===================== 6. DECISIÓN: ALARMA DE HOGUERA =====================

    private void EvaluarEstadoHoguera()
    {
        if (!PuedeEvaluarHoguera()) return;
        if (!EstaCercaDeLaHoguera()) return;
        
        if (!sensorHoguera.hogueraDetectada)
        {
            if (!sensorHoguera.hogueraEnCampoDeVision) return;
            if (veAlLadron && !ladronTieneFuego) { framesSinVerHoguera = 0; return; }

            framesSinVerHoguera++;

            if (EsAldeanoPrincipal && framesSinVerHoguera == 1)
                Debug.Log($"[CEREBRO {gameObject.name}]: Hoguera no visible. Iniciando contador ({framesParaAlarmaHoguera} frames para alarma)");
 
            if (framesSinVerHoguera >= framesParaAlarmaHoguera)
            {
                framesSinVerHoguera = 0;
                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=orange>[CEREBRO {gameObject.name}]: {framesParaAlarmaHoguera} frames sin ver hoguera. Yendo a comprobar físicamente.</color>");
                enAlerta = true;
                MarcarBusquedaAgotada();
            }
        }
        else
        {
            // NUEVO: si la vemos, resetear el contador
            if (framesSinVerHoguera > 0 && EsAldeanoPrincipal)
                Debug.Log($"<color=green>[CEREBRO {gameObject.name}]: Hoguera visible de nuevo. Reseteando contador (estaba en {framesSinVerHoguera}).</color>");

            framesSinVerHoguera = 0;
        }
    }

    private bool PuedeEvaluarHoguera()
    {
        if (alarmaHogueraActiva) return false;
        if (busquedaAgotada) return false;  // ComprobarHoguera ya está gestionando la situación
        if (cronometroGraciaPostComprobacion > 0f)
        {
            if (EsAldeanoPrincipal && !graciaLogueada)
            {
                Debug.Log($"[CEREBRO {gameObject.name}]: EvaluarHoguera bloqueada — gracia post-comprobación activa ({tiempoGraciaPostComprobacion:F1}s).");
                graciaLogueada = true;
            }
            return false;
        }
        if (sensorHoguera == null) return false;
        if (!sensorHoguera.haVistoHogueraAlgunaVez) return false;
        if (sensorHoguera.posicionHogueraConocida == null) return false;
        return true;
    }


    private bool EstaCercaDeLaHoguera()
    {
        float distancia = Vector3.Distance(
            transform.position,
            sensorHoguera.posicionHogueraConocida.Value
        );
        return distancia <= sensorHoguera.radioDeteccion;
    }

    private void ActivarAlarmaHoguera()
    {
        alarmaHogueraActiva = true;
        framesSinVerHoguera = 0;
        busquedaForzada = false;
        contadorResetsCiclo = 0;
        Debug.Log($"<color=red>[CEREBRO] {gameObject.name}: ¡ALARMA! La hoguera ha sido robada.</color>");
    }

    // ===================== 7. REGISTRO DE FRAME =====================

    private void RegistrarFrameAnterior()
    {
        ladronVisibleFrameAnterior = veAlLadron;
        ladronVisibleFrameAnteriorTeniaFuego = ladronTieneFuego;

    }

    // ===================== 8. PROPAGACIÓN A COMPORTAMIENTOS =====================

    private void PropagarInformacionACapas()
    {
        foreach (GuardBehavior capa in behaviors)
        {
            capa.RecibirInformacion(
                veAlLadron,
                ultimaPosicionLadron,
                oyoAlgo,
                posicionRuido,
                alarmaHogueraActiva,
                posicionPuerta,
                cronometroBusqueda,
                enAlerta,
                ladronTieneFuego,
                ladronPerdidoConFuego
            );
        }
    }

    // ===================== 9. DECISIÓN: SUBSUMPTION =====================

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
        puertaEnCooldown = true;
        cronometroPuerta = tiempoCooldownPuerta;
        Debug.Log($"[CEREBRO {gameObject.name}]: Puerta investigada. Cooldown {tiempoCooldownPuerta:F1}s activo. posicionPuerta se suprime hasta que expire.");
    }

    public void NotificarInvestigacionRuidoCompletada()
    {
        if (sensorOido != null)
        {
            sensorOido.ResetearAudicion();
            investigacionEnCooldown = true;
            cronometroInvestigacion = tiempoCooldownInvestigacion;
            if (EsAldeanoPrincipal)
                Debug.Log($"[CEREBRO {gameObject.name}]: Investigación de ruido completada. Cooldown {tiempoCooldownInvestigacion:F1}s activo.");
        }
    }

    // ===================== MÉTODOS PÚBLICOS DE RESET =====================

    public void ResetearBusqueda()
    {
        if (busquedaCache != null)
            cronometroLimiteBusqueda = busquedaCache.tiempoLimiteBusqueda;

        if (!busquedaForzada)
            busquedaAgotada = false;
        cronometroLimiteBusquedaIniciadoLogueado = false;
    }

    public void ResetearComprobacion()
    {
        if (comprobarCache != null)
            comprobarCache.ResetearComprobacion();
    }

    // ===================== PROPIEDADES PÚBLICAS (leídas por DeliberativeLayer) =====================

    public bool VeAlLadron           => veAlLadron;
    public bool AcabaDeVerAlLadron   => acabaDeVerAlLadron;
    public bool LadronTieneFuego     => ladronTieneFuego;
    public bool AlarmaHogueraActiva  => alarmaHogueraActiva;
    public bool EnAlerta             => enAlerta;
    public Vector3? UltimaPosicionLadron => ultimaPosicionLadron;

    // ===================== INYECCIÓN DESDE CAPA DELIBERATIVA =====================

    public void InyectarPosicionLadron(Vector3 pos)
    {
        enAlerta             = true;
        ultimaPosicionLadron = pos;
        cronometroBusqueda   = tiempoBusqueda;
    }

    public void InyectarAlarmaHoguera()
    {
        alarmaHogueraActiva = true;
    }

    public void InyectarCubrirHoguera()
    {
        enAlerta      = true;
        busquedaAgotada = true;
    }
}
