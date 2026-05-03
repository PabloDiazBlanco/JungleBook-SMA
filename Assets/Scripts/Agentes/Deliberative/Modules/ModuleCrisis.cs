using UnityEngine;
using System.Collections.Generic;

public class ModuleCrisis : DeliberativeModule
{
    public float tiempoEsperaCrisis = 0.5f;

    private bool cnpCrisisIniciado;
    private float cronometroCrisis;
    private string convIdCrisis = "";
    private List<FIPAMessage> propuestasCrisis = new List<FIPAMessage>();
    private Vector3? _posPuerta;

    private bool alarmaActivaFrameAnterior;
    private bool recibimoInformEsteFrame;

    public override void Procesar()
    {
        if (cnpCrisisIniciado)
        {
            cronometroCrisis -= Time.deltaTime;
            if (cronometroCrisis <= 0f)
            {
                ResolverCNPCrisis();
                cnpCrisisIniciado = false;
            }
        }

        bool alarmaAntes = alarmaActivaFrameAnterior;
        recibimoInformEsteFrame = false;

        // Procesar mensajes ANTES de evaluar la transición de alarma,
        // para que recibimoInformEsteFrame esté correcto cuando se compruebe abajo.
        foreach (FIPAMessage msg in communicator.GetInbox())
        {
            if (msg.performativa == FIPAPerformativa.INFORM)
            {
                ProcesarInform(msg); // setea recibimoInformEsteFrame internamente
            }
            else if (cnpCrisisIniciado && msg.conversationId == convIdCrisis
                     && (msg.performativa == FIPAPerformativa.PROPOSE || msg.performativa == FIPAPerformativa.REFUSE))
                propuestasCrisis.Add(msg);
        }

        bool alarmaAhora = controller.AlarmaHogueraActiva;

        // Si la alarma acaba de activarse SIN haber recibido un INFORM → somos el detector → manager del CNP
        if (alarmaAhora && !alarmaAntes && !recibimoInformEsteFrame && !cnpCrisisIniciado)
            IniciarCNPCrisis();

        alarmaActivaFrameAnterior = alarmaAhora;
    }

    private void ProcesarInform(FIPAMessage msg)
    {
        if (msg.contenido != "alarma_hoguera") return;
        if (controller.AlarmaHogueraActiva) return;

        // Solo inyectar la alarma localmente. NO lanzar CNP crisis aquí.
        // El CNP lo inicia únicamente el agente que detectó la ausencia (FireAlarmMonitor),
        // identificado porque su alarma se activa sin haber recibido un INFORM previo.
        controller.InyectarAlarmaHoguera();
        recibimoInformEsteFrame = true; // marcar para que no se confunda con detección propia
        Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: INFORM de '{msg.emisor}' — alarma inyectada (sin CNP receptor).</color>");
    }

    private void IniciarCNPCrisis()
    {
        Vector3? posPuerta = GetPosPuerta();
        if (!posPuerta.HasValue)
        {
            Debug.LogWarning($"[CRISIS {communicator.nombreAgente}]: Bloqueo_Puerta no encontrado — CNP crisis cancelado.");
            return;
        }

        cnpCrisisIniciado = true;
        propuestasCrisis.Clear();
        cronometroCrisis = tiempoEsperaCrisis;
        convIdCrisis = communicator.GenerarConversationId("cnp_bloqueo");

        Vector3 p = posPuerta.Value;
        string contenido = $"bloqueo|puerta:{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)}";

        FIPAMessage cfp = new FIPAMessage(
            FIPAPerformativa.CFP, communicator.nombreAgente, "broadcast",
            contenido, convIdCrisis);
        communicator.EnviarATodos(cfp);

        Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: CNP bloqueo iniciado — puerta {p:F0}.</color>");
    }

    private void ResolverCNPCrisis()
    {
        var propuestas = propuestasCrisis.FindAll(m => m.performativa == FIPAPerformativa.PROPOSE);
        int rechazos = propuestasCrisis.Count - propuestas.Count;
        Debug.Log($"[CRISIS {communicator.nombreAgente}]: CNP bloqueo — {propuestas.Count} propuestas, {rechazos} rechazos.");

        if (propuestas.Count == 0)
        {
            Debug.Log($"[CRISIS {communicator.nombreAgente}]: CNP bloqueo sin candidatos.");
            return;
        }

        // Seleccionar el más cercano a Bloqueo_Puerta (mínimo dp)
        FIPAMessage mejor = null;
        int minDp = int.MaxValue;
        foreach (FIPAMessage msg in propuestas)
        {
            if (!msg.contenido.StartsWith("dp:")) continue;
            if (!int.TryParse(msg.contenido.Substring(3), out int dp)) continue;
            if (dp < minDp) { minDp = dp; mejor = msg; }
        }

        if (mejor == null) return;

        FIPAMessage accept = new FIPAMessage(
            FIPAPerformativa.ACCEPT_PROPOSAL, communicator.nombreAgente, mejor.emisor,
            "cubrir_salida", convIdCrisis, convIdCrisis);
        communicator.Enviar(accept, new List<string> { mejor.emisor });

        Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: → {mejor.emisor} CUBRIR SALIDA (dp={minDp})</color>");
    }

    private Vector3? GetPosPuerta()
    {
        if (_posPuerta.HasValue) return _posPuerta;
        GameObject obj = GameObject.Find("Bloqueo_Puerta");
        if (obj == null) return null;
        _posPuerta = obj.transform.position;
        return _posPuerta;
    }
}