using UnityEngine;

public class FireAlarmMonitor : MonoBehaviour
{
    [Header("Filtro hoguera")]
    public int framesParaAlarmaHoguera = 10;

    private AgentBlackboard bb;
    private AgentTimerManager timers;
    private SensorHogueraIndividual sensorHoguera;
    private SubsumptionController mediador;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    void Start()
    {
        bb            = GetComponent<AgentBlackboard>();
        timers        = GetComponent<AgentTimerManager>();
        sensorHoguera = GetComponent<SensorHogueraIndividual>();
        mediador      = GetComponent<SubsumptionController>();
    }

    // ===================== LOOP PRINCIPAL =====================

    public void Evaluar()
    {
        if (!PuedeEvaluar())    return;
        if (!EstaCercaDeLaHoguera()) return;

        if (!bb.hogueraDetectada)
        {
            if (!bb.hogueraEnCampoDeVision) return;
            if (bb.veAlLadron && !bb.ladronTieneFuego) { bb.framesSinVerHoguera = 0; return; }

            bb.framesSinVerHoguera++;

            if (EsAldeanoPrincipal && bb.framesSinVerHoguera == 1)
                Debug.Log($"[CEREBRO {gameObject.name}]: Hoguera no visible. Iniciando contador ({framesParaAlarmaHoguera} frames para alarma)");

            if (bb.framesSinVerHoguera >= framesParaAlarmaHoguera)
            {
                bb.framesSinVerHoguera = 0;
                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=orange>[CEREBRO {gameObject.name}]: {framesParaAlarmaHoguera} frames sin ver hoguera. Yendo a comprobar físicamente.</color>");
                bb.enAlerta = true;
                mediador.OnBusquedaAgotadaPorHoguera();
            }
        }
        else
        {
            if (bb.framesSinVerHoguera > 0 && EsAldeanoPrincipal)
                Debug.Log($"<color=green>[CEREBRO {gameObject.name}]: Hoguera visible de nuevo. Reseteando contador (estaba en {bb.framesSinVerHoguera}).</color>");
            bb.framesSinVerHoguera = 0;
        }
    }

    // ===================== NOTIFICACIÓN DESDE BEHAVIOR =====================

    public void NotificarComprobacionHogueraCompletada()
    {
        bool hogueraPresente = ComprobarPresenciaFuegoLocal();
        if (hogueraPresente)
        {
            bb.framesSinVerHoguera = 0;
            timers.IniciarGracia();
            bb.ciclosBusquedaCompletados++;
            bb.busquedaForzada     = false;
            bb.contadorResetsCiclo = 0;
            mediador.OnComprobacionHogueraExitosa();
            if (EsAldeanoPrincipal)
                Debug.Log($"[CEREBRO {gameObject.name}]: Comprobación OK — hoguera intacta. Gracia de {timers.tiempoGraciaPostComprobacion}s activa. Ciclo {bb.ciclosBusquedaCompletados}.");
        }
        else
        {
            if (EsAldeanoPrincipal)
                Debug.Log($"<color=red>[CEREBRO {gameObject.name}]: Comprobación fallida — hoguera ausente. Activando alarma.</color>");
            ActivarAlarmaHoguera();
        }
    }

    // ===================== API PÚBLICA (mediador) =====================

    public void ActivarAlarmaHoguera()
    {
        bb.alarmaHogueraActiva = true;
        bb.framesSinVerHoguera = 0;
        bb.busquedaForzada     = false;
        bb.contadorResetsCiclo = 0;
        Debug.Log($"<color=red>[CEREBRO] {gameObject.name}: ¡ALARMA! La hoguera ha sido robada.</color>");
    }

    // ===================== PRIVADO =====================

    private bool PuedeEvaluar()
    {
        if (bb.alarmaHogueraActiva)  return false;
        if (bb.busquedaAgotada)      return false;
        if (timers.GraciaActiva)
        {
            if (EsAldeanoPrincipal && !timers.GraciaLogueada)
            {
                Debug.Log($"[CEREBRO {gameObject.name}]: EvaluarHoguera bloqueada — gracia post-comprobación activa ({timers.tiempoGraciaPostComprobacion:F1}s).");
                timers.GraciaLogueada = true;
            }
            return false;
        }
        if (sensorHoguera == null)                        return false;
        if (!sensorHoguera.haVistoHogueraAlgunaVez)       return false;
        if (sensorHoguera.posicionHogueraConocida == null) return false;
        return true;
    }

    private bool EstaCercaDeLaHoguera()
    {
        float distancia = Vector3.Distance(transform.position, sensorHoguera.posicionHogueraConocida.Value);
        return distancia <= sensorHoguera.radioDeteccion;
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
}
