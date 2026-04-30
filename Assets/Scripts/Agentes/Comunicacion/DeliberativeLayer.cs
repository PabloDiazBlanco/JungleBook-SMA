using UnityEngine;
using System.Collections.Generic;

public class DeliberativeLayer : MonoBehaviour
{
    private AgentCommunicator communicator;
    private SubsumptionController controller;
    private SensorHogueraIndividual sensorHoguera;

    public BeliefBase creencias = new BeliefBase();

    [Header("Contract Net Protocol")]
    public float tiempoEsperaPropuestas = 0.5f;
    private bool cnpIniciado;
    private float cronometroCNP;
    private string convIdCNP = "";
    private string tipoCNP = "";
    private List<FIPAMessage> propuestasCNP = new List<FIPAMessage>();
    private bool alarmaHogueraActivaFrameAnterior;

    void Awake()
    {
        communicator  = GetComponent<AgentCommunicator>();
        controller    = GetComponent<SubsumptionController>();
        sensorHoguera = GetComponent<SensorHogueraIndividual>();
    }

    // Llamado desde SubsumptionController.Update() tras RegistrarFrameAnterior()
    public void Procesar()
    {
        if (communicator == null) return;
        ActualizarCreencias();
        ProcesarMensajes();
        ComunicarEstado();
    }

    // ===================== BASE DE CREENCIAS =====================

    private void ActualizarCreencias()
    {
        creencias.ladronVisto         = controller.VeAlLadron;
        creencias.ladronTieneFuego    = controller.LadronTieneFuego;
        creencias.alarmaHogueraActiva = controller.AlarmaHogueraActiva;

        if (controller.VeAlLadron && controller.UltimaPosicionLadron.HasValue)
        {
            creencias.posicionLadron          = controller.UltimaPosicionLadron;
            creencias.timestampPosicionLadron = Time.time;
        }
    }

    // ===================== PROCESADO DE MENSAJES =====================

    private void ProcesarMensajes()
    {
        foreach (FIPAMessage msg in communicator.GetInbox())
        {
            switch (msg.performativa)
            {
                case FIPAPerformativa.INFORM:
                    ProcesarInform(msg);
                    break;
                case FIPAPerformativa.CFP:
                    ProcesarCFP(msg);
                    break;
                case FIPAPerformativa.PROPOSE:
                case FIPAPerformativa.REFUSE:
                    ProcesarPropuesta(msg);
                    break;
                case FIPAPerformativa.ACCEPT_PROPOSAL:
                    ProcesarAceptacion(msg);
                    break;
            }
        }
    }

    private void ProcesarInform(FIPAMessage msg)
    {
        if (msg.contenido != "alarma_hoguera") return;
        if (controller.AlarmaHogueraActiva) return;

        controller.InyectarAlarmaHoguera();
        Debug.Log($"<color=red>[DELIBERATIVA {gameObject.name}]: INFORM de '{msg.emisor}' — ¡ALARMA HOGUERA!</color>");
    }

    private void ProcesarCFP(FIPAMessage msg)
    {
        // Formato: "tipo|ladron:x,y,z|obj:x,y,z"
        string[] partes = msg.contenido.Split('|');
        if (partes.Length != 3) return;
        if (partes[0] != "hoguera" && partes[0] != "salida") return;
        if (!partes[1].StartsWith("ladron:") || !partes[2].StartsWith("obj:")) return;

        Vector3? posLadron = ParsearPosicion(partes[1].Substring("ladron:".Length));
        Vector3? posObj    = ParsearPosicion(partes[2].Substring("obj:".Length));
        if (posLadron == null || posObj == null) return;

        if (controller.EnAlerta || controller.AlarmaHogueraActiva)
        {
            FIPAMessage rechazo = new FIPAMessage(
                FIPAPerformativa.REFUSE, communicator.nombreAgente, msg.emisor,
                "ocupado", msg.conversationId, msg.conversationId);
            communicator.Enviar(rechazo, new List<string> { msg.emisor });
            Debug.Log($"[DELIBERATIVA {gameObject.name}]: REFUSE a '{msg.emisor}' — estoy ocupado.");
        }
        else
        {
            int dl   = Mathf.RoundToInt(Vector3.Distance(transform.position, posLadron.Value));
            int dobj = Mathf.RoundToInt(Vector3.Distance(transform.position, posObj.Value));
            FIPAMessage propuesta = new FIPAMessage(
                FIPAPerformativa.PROPOSE, communicator.nombreAgente, msg.emisor,
                $"dl:{dl},do:{dobj}", msg.conversationId, msg.conversationId);
            communicator.Enviar(propuesta, new List<string> { msg.emisor });
            Debug.Log($"[DELIBERATIVA {gameObject.name}]: PROPOSE a '{msg.emisor}' — dl:{dl}m, do:{dobj}m.");
        }
    }

    private void ProcesarPropuesta(FIPAMessage msg)
    {
        if (msg.conversationId != convIdCNP) return;
        propuestasCNP.Add(msg);
        Debug.Log($"[DELIBERATIVA {gameObject.name}]: Respuesta CNP de '{msg.emisor}' [{msg.performativa}].");
    }

    private void ProcesarAceptacion(FIPAMessage msg)
    {
        if (msg.contenido.StartsWith("perseguir:"))
        {
            Vector3? pos = ParsearPosicion(msg.contenido.Substring("perseguir:".Length));
            if (pos == null) return;
            controller.InyectarPosicionLadron(pos.Value);
            Debug.Log($"<color=cyan>[DELIBERATIVA {gameObject.name}]: ACCEPT — voy a perseguir al ladrón.</color>");
        }
        else if (msg.contenido == "cubrir_hoguera")
        {
            controller.InyectarCubrirHoguera();
            Debug.Log($"<color=cyan>[DELIBERATIVA {gameObject.name}]: ACCEPT — voy a cubrir la hoguera.</color>");
        }
        else if (msg.contenido == "cubrir_salida")
        {
            controller.InyectarAlarmaHoguera();
            Debug.Log($"<color=cyan>[DELIBERATIVA {gameObject.name}]: ACCEPT — voy a cubrir la salida.</color>");
        }
    }

    // ===================== COMUNICAR ESTADO =====================

    private void ComunicarEstado()
    {
        if (controller.AcabaDeVerAlLadron && !cnpIniciado)
            IniciarCNP();

        if (cnpIniciado)
            AvanzarCNP();

        // Broadcast de alarma: solo la primera vez que se activa
        if (controller.AlarmaHogueraActiva && !alarmaHogueraActivaFrameAnterior)
        {
            FIPAMessage msg = new FIPAMessage(
                FIPAPerformativa.INFORM, communicator.nombreAgente, "broadcast",
                "alarma_hoguera", communicator.GenerarConversationId("alarma_hoguera"));
            communicator.EnviarATodos(msg);
            Debug.Log($"<color=red>[DELIBERATIVA {gameObject.name}]: INFORM 'alarma_hoguera' enviado a todos.</color>");
        }

        alarmaHogueraActivaFrameAnterior = controller.AlarmaHogueraActiva;
    }

    // ===================== CONTRACT NET PROTOCOL =====================

    private void IniciarCNP()
    {
        if (sensorHoguera == null || sensorHoguera.posicionHogueraConocida == null) return;
        if (!controller.UltimaPosicionLadron.HasValue) return;

        cnpIniciado = true;
        propuestasCNP.Clear();
        cronometroCNP = tiempoEsperaPropuestas;

        Vector3 posLadron = controller.UltimaPosicionLadron.Value;
        string ladronStr  = $"{Mathf.RoundToInt(posLadron.x)},{Mathf.RoundToInt(posLadron.y)},{Mathf.RoundToInt(posLadron.z)}";

        if (!controller.LadronTieneFuego)
        {
            tipoCNP   = "hoguera";
            convIdCNP = communicator.GenerarConversationId("cnp_hoguera");
            Vector3 posHoguera = sensorHoguera.posicionHogueraConocida.Value;
            string objStr = $"{Mathf.RoundToInt(posHoguera.x)},{Mathf.RoundToInt(posHoguera.y)},{Mathf.RoundToInt(posHoguera.z)}";
            FIPAMessage cfp = new FIPAMessage(FIPAPerformativa.CFP, communicator.nombreAgente, "broadcast",
                $"hoguera|ladron:{ladronStr}|obj:{objStr}", convIdCNP);
            communicator.EnviarATodos(cfp);
            Debug.Log($"<color=yellow>[DELIBERATIVA {gameObject.name}]: CFP — perseguir ladrón + cubrir hoguera.</color>");
        }
        else
        {
            tipoCNP   = "salida";
            convIdCNP = communicator.GenerarConversationId("cnp_salida");
            FIPAMessage cfp = new FIPAMessage(FIPAPerformativa.CFP, communicator.nombreAgente, "broadcast",
                $"salida|ladron:{ladronStr}|obj:{ladronStr}", convIdCNP);
            communicator.EnviarATodos(cfp);
            Debug.Log($"<color=yellow>[DELIBERATIVA {gameObject.name}]: CFP — perseguir ladrón + cubrir salida.</color>");
        }
    }

    private void AvanzarCNP()
    {
        cronometroCNP -= Time.deltaTime;
        if (cronometroCNP <= 0f)
        {
            ResolverCNP();
            cnpIniciado = false;
        }
    }

    private void ResolverCNP()
    {
        List<FIPAMessage> propuestas = new List<FIPAMessage>();
        foreach (FIPAMessage msg in propuestasCNP)
            if (msg.performativa == FIPAPerformativa.PROPOSE) propuestas.Add(msg);

        if (propuestas.Count == 0)
        {
            Debug.Log($"[DELIBERATIVA {gameObject.name}]: CNP sin candidatos — actúo solo.");
            return;
        }

        FIPAMessage perseguidor = null, cubridor = null;
        int minDl = int.MaxValue, minDo = int.MaxValue;

        foreach (FIPAMessage msg in propuestas)
        {
            string[] partes = msg.contenido.Split(',');
            if (partes.Length != 2) continue;
            if (!partes[0].StartsWith("dl:") || !partes[1].StartsWith("do:")) continue;
            if (!int.TryParse(partes[0].Substring(3), out int dl)) continue;
            if (!int.TryParse(partes[1].Substring(3), out int dobj)) continue;

            if (dl < minDl)   { minDl = dl;   perseguidor = msg; }
            if (dobj < minDo) { minDo = dobj; cubridor    = msg; }
        }

        string contenidoCubrir    = tipoCNP == "hoguera" ? "cubrir_hoguera" : "cubrir_salida";
        Vector3 posLadron         = controller.UltimaPosicionLadron.Value;
        string contenidoPerseguir = $"perseguir:{Mathf.RoundToInt(posLadron.x)},{Mathf.RoundToInt(posLadron.y)},{Mathf.RoundToInt(posLadron.z)}";

        if (perseguidor != cubridor)
        {
            EnviarAccept(perseguidor, contenidoPerseguir);
            EnviarAccept(cubridor,    contenidoCubrir);
        }
        else
        {
            // Un solo candidato: que cubra (yo ya persigo); si hay otro, también persigue
            EnviarAccept(cubridor, contenidoCubrir);
            foreach (FIPAMessage msg in propuestas)
                if (msg != cubridor) EnviarAccept(msg, contenidoPerseguir);
        }
    }

    private void EnviarAccept(FIPAMessage propuesta, string contenido)
    {
        FIPAMessage accept = new FIPAMessage(
            FIPAPerformativa.ACCEPT_PROPOSAL, communicator.nombreAgente, propuesta.emisor,
            contenido, convIdCNP, convIdCNP);
        communicator.Enviar(accept, new List<string> { propuesta.emisor });
        Debug.Log($"<color=cyan>[DELIBERATIVA {gameObject.name}]: ACCEPT → '{propuesta.emisor}' ({contenido}).</color>");
    }

    // ===================== UTILIDADES =====================

    private Vector3? ParsearPosicion(string coords)
    {
        string[] partes = coords.Split(',');
        if (partes.Length != 3) return null;
        if (int.TryParse(partes[0], out int x) &&
            int.TryParse(partes[1], out int y) &&
            int.TryParse(partes[2], out int z))
            return new Vector3(x, y, z);
        Debug.LogWarning($"[DELIBERATIVA {gameObject.name}]: No se pudo parsear posición: '{coords}'");
        return null;
    }
}
