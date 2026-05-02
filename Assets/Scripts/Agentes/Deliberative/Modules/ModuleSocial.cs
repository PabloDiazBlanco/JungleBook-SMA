using UnityEngine;
using System.Collections.Generic;

public class ModuleSocial : DeliberativeModule
{
    // Configuración
    public float tiempoEsperaPropuestas = 0.5f;

    // Estado de la Negociación
    private bool cnpIniciado;
    private float cronometroCNP;
    private string convIdCNP = "";
    private string tipoCNP = "";
    private List<FIPAMessage> propuestasCNP = new List<FIPAMessage>();
    
    // Referencia al nuevo módulo táctico para delegar cálculos y acciones físicas
    private ModuleTactical tactical;

    public override void Procesar()
    {
        // 1. Gestión del cronómetro del CNP
        if (cnpIniciado)
        {
            cronometroCNP -= Time.deltaTime;
            if (cronometroCNP <= 0f)
            {
                ResolverCNP();
                cnpIniciado = false;
            }
        }

        // 2. Procesar mensajes del buzón que sean de interés social
        foreach (FIPAMessage msg in communicator.GetInbox())
        {
            if (EsMensajeSocial(msg.performativa))
            {
                ProcesarMensajeSocial(msg);
            }
        }
    }

    private bool EsMensajeSocial(FIPAPerformativa p)
    {
        return p == FIPAPerformativa.CFP || 
               p == FIPAPerformativa.PROPOSE || 
               p == FIPAPerformativa.REFUSE || 
               p == FIPAPerformativa.ACCEPT_PROPOSAL;
    }

    public void ProcesarMensajeSocial(FIPAMessage msg)
    {
        switch (msg.performativa)
        {
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

    // ===================== INICIADOR (Manager) =====================

    public void IniciarCNP()
    {
        if (tactical == null) tactical = controller.GetComponent<ModuleTactical>();
        
        string objStr = tactical.GetPosicionObjetivo(controller.LadronTieneFuego ? "salida" : "hoguera");
        if (string.IsNullOrEmpty(objStr)) return;

        cnpIniciado = true;
        propuestasCNP.Clear();
        cronometroCNP = tiempoEsperaPropuestas;

        string ladronStr = tactical.GetPosicionLadron();

        tipoCNP = controller.LadronTieneFuego ? "salida" : "hoguera";
        convIdCNP = communicator.GenerarConversationId("cnp_" + tipoCNP);
        
        FIPAMessage cfp = new FIPAMessage(FIPAPerformativa.CFP, communicator.nombreAgente, "broadcast",
            $"{tipoCNP}|ladron:{ladronStr}|obj:{objStr}", convIdCNP);
            
        communicator.EnviarATodos(cfp);
        Debug.Log($"<color=yellow>[SOCIAL {communicator.nombreAgente}]: CFP iniciado — tipo {tipoCNP.ToUpper()}.</color>");
    }

    private void ResolverCNP()
    {
        List<FIPAMessage> conversacion = communicator.GetHistorialPorConversacion(convIdCNP);
        int rechazos = conversacion.FindAll(m => m.performativa == FIPAPerformativa.REFUSE).Count;
        Debug.Log($"[SOCIAL {communicator.nombreAgente}]: CNP '{convIdCNP}' — {conversacion.Count} mensajes, {rechazos} rechazos.");

        List<FIPAMessage> propuestas = new List<FIPAMessage>();
        foreach (FIPAMessage msg in propuestasCNP)
            if (msg.performativa == FIPAPerformativa.PROPOSE) propuestas.Add(msg);

        if (propuestas.Count == 0)
        {
            Debug.Log($"[SOCIAL {communicator.nombreAgente}]: CNP sin candidatos — actúo solo.");
            return;
        }

        FIPAMessage perseguidor = null, cubridor = null;
        int minDl = int.MaxValue, minDo = int.MaxValue;

        foreach (FIPAMessage msg in propuestas)
        {
            string[] partes = msg.contenido.Split(',');
            if (partes.Length != 2) continue;
            if (!int.TryParse(partes[0].Substring(3), out int dl)) continue;
            if (!int.TryParse(partes[1].Substring(3), out int dobj)) continue;

            if (dl < minDl) { minDl = dl; perseguidor = msg; }
            if (dobj < minDo) { minDo = dobj; cubridor = msg; }
        }

        string contenidoCubrir = tipoCNP == "hoguera" ? "cubrir_hoguera" : "cubrir_salida";
        string contenidoPerseguir = tactical.GetComandoPersecucion();

        if (perseguidor != cubridor)
        {
            EnviarAccept(perseguidor, contenidoPerseguir);
            EnviarAccept(cubridor, contenidoCubrir);
        }
        else
        {
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
    }

    // ===================== PARTICIPANTE (Contractor) =====================

    private void ProcesarCFP(FIPAMessage msg)
    {
        if (tactical == null) tactical = controller.GetComponent<ModuleTactical>();

        if (controller.EnAlerta || controller.AlarmaHogueraActiva)
        {
            FIPAMessage rechazo = new FIPAMessage(
                FIPAPerformativa.REFUSE, communicator.nombreAgente, msg.emisor,
                "ocupado", msg.conversationId, msg.conversationId);
            communicator.Enviar(rechazo, new List<string> { msg.emisor });
        }
        else
        {
            string respuestaPropuesta = tactical.CalcularPropuesta(msg.contenido);
            if (string.IsNullOrEmpty(respuestaPropuesta)) return;

            FIPAMessage propuesta = new FIPAMessage(
                FIPAPerformativa.PROPOSE, communicator.nombreAgente, msg.emisor,
                respuestaPropuesta, msg.conversationId, msg.conversationId);
            communicator.Enviar(propuesta, new List<string> { msg.emisor });
        }
    }

    private void ProcesarPropuesta(FIPAMessage msg)
    {
        if (msg.conversationId != convIdCNP) return;
        propuestasCNP.Add(msg);
    }

    private void ProcesarAceptacion(FIPAMessage msg)
    {
        if (tactical == null) tactical = controller.GetComponent<ModuleTactical>();
        
        tactical.EjecutarAccionConfirmada(msg.contenido);
    }

    public bool IsCnpIniciado() => cnpIniciado;
}