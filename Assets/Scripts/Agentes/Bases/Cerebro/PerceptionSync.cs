using UnityEngine;

public class PerceptionSync : MonoBehaviour
{
    private AgentBlackboard bb;
    private AgentTimerManager timers;
    private GuardVision sensorVision;
    private GuardHearing sensorOido;
    private SensorHogueraIndividual sensorHoguera;
    private SensorPercepcionObjetos sensorObjetos;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

    void Start()
    {
        bb            = GetComponent<AgentBlackboard>();
        timers        = GetComponent<AgentTimerManager>();
        sensorVision  = GetComponent<GuardVision>();
        sensorOido    = GetComponent<GuardHearing>();
        sensorHoguera = GetComponent<SensorHogueraIndividual>();
        sensorObjetos = GetComponent<SensorPercepcionObjetos>();
    }

    // Llamar al inicio del Update: lee sensores y detecta transiciones
    public void Sincronizar()
    {
        LeerSensores();
        DetectarTransiciones();
    }

    // Llamar al final de la fase de estado, antes de la capa deliberativa
    public void RegistrarFrame()
    {
        bb.ladronVisibleFrameAnterior = bb.veAlLadron;
        bb.ladronVisibleFrameAnteriorTeniaFuego = bb.ladronTieneFuego;
        bb.velocidadLadronFrameAnterior = bb.velocidadLadron;
    }

    // ===================== PRIVADO =====================

    private void LeerSensores()
    {
        bb.veAlLadron    = sensorVision != null && sensorVision.PuedeVerAlLadron();
        bb.oyoAlgo       = sensorOido   != null && sensorOido.EscuchoAlgo();
        bb.posicionRuido = bb.oyoAlgo ? sensorOido.GetPosicionRuido() : (Vector3?)null;

        if (bb.veAlLadron && sensorVision != null)
        {
            bb.velocidadLadron = sensorVision.velocidadLadron;
            bb.direccionLadron = sensorVision.direccionLadron;
        }
        else
        {
            bb.velocidadLadron = 0f;
        }

        bool puertaFisicaDetectada = sensorObjetos != null && sensorObjetos.ultimaPuertaDetectada != null;
        if (!timers.PuertaEnCooldown) timers.PuertaSupresionLogueada = false;

        bb.posicionPuerta = (!timers.PuertaEnCooldown && puertaFisicaDetectada)
            ? sensorObjetos.ultimaPuertaDetectada.position
            : (Vector3?)null;

        bb.ladronTieneFuego       = bb.veAlLadron && sensorVision.ladronTieneFuego;
        bb.hogueraDetectada       = sensorHoguera != null && sensorHoguera.hogueraDetectada;
        bb.hogueraEnCampoDeVision = sensorHoguera != null && sensorHoguera.hogueraEnCampoDeVision;
    }

    private void DetectarTransiciones()
    {
        bb.acabaDeVerAlLadron    = !bb.ladronVisibleFrameAnterior && bb.veAlLadron;
        bb.acabaDePerderAlLadron =  bb.ladronVisibleFrameAnterior && !bb.veAlLadron;
    }
}
