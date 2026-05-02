using UnityEngine;

public class AgentBlackboard : MonoBehaviour
{
    [Header("Percepción del ladrón")]
    public bool veAlLadron;
    public Vector3? ultimaPosicionLadron;
    public bool ladronTieneFuego;
    public bool ladronPerdidoConFuego;

    [Header("Historial de frame anterior")]
    public bool ladronVisibleFrameAnterior;
    public bool ladronVisibleFrameAnteriorTeniaFuego;
    public bool acabaDeVerAlLadron;
    public bool acabaDePerderAlLadron;

    [Header("Percepción de entorno")]
    public bool oyoAlgo;
    public Vector3? posicionRuido;
    public Vector3? posicionPuerta;
    public bool hogueraDetectada;
    public bool hogueraEnCampoDeVision;
    public Vector3? posicionHogueraConocida;

    [Header("Estados lógicos")]
    public bool enAlerta;
    public bool alarmaHogueraActiva;
    public bool busquedaAgotada;
    public bool busquedaEsPorFuego;
    public bool busquedaForzada;

    [Header("Contadores")]
    public int framesSinVerHoguera;
    public int ciclosBusquedaCompletados;
    public int contadorResetsCiclo;

    public void ResetearRastroLadron()
    {
        veAlLadron           = false;
        ultimaPosicionLadron = null;
        ladronTieneFuego     = false;
        ladronPerdidoConFuego = false;
        enAlerta             = false;
    }
}
