using UnityEngine;
using System.Collections.Generic;

public class ModuleSocial : DeliberativeModule
{
    // Configuración
    public float tiempoEsperaPropuestas = 0.5f;
    public float cooldownCNPFallido = 3f;

    // Estado de la Negociación
    private bool cnpIniciado;
    private float tiempoUltimoCNPFallido = -999f;
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
        // El iniciador ya cuenta como perseguidor; solo asignamos 2 más desde propuestas
        const int MAX_PERSEGUIDORES_PROPUESTAS = 2;

        List<FIPAMessage> propuestas = new List<FIPAMessage>();
        foreach (FIPAMessage msg in propuestasCNP)
            if (msg.performativa == FIPAPerformativa.PROPOSE) propuestas.Add(msg);

        int rechazos = propuestasCNP.Count - propuestas.Count;
        Debug.Log($"[SOCIAL {communicator.nombreAgente}]: CNP '{convIdCNP}' — {propuestas.Count} propuestas, {rechazos} rechazos.");

        if (propuestas.Count == 0)
        {
            tiempoUltimoCNPFallido = Time.time;
            creencias.rolActual = BeliefBase.RolCNP.Perseguidor;
            creencias.rolAsignadoExternamente = false;
            Debug.Log($"[SOCIAL {communicator.nombreAgente}]: CNP sin candidatos — actúo solo.");
            return;
        }

        // El iniciador se asigna como perseguidor al confirmar que hay colaboradores
        creencias.rolActual = BeliefBase.RolCNP.Perseguidor;
        creencias.rolAsignadoExternamente = false;

        // Parsear y ordenar por distancia al ladrón (dl) ascendente
        var candidatos = new List<(FIPAMessage msg, int dl, int dobj)>();
        foreach (FIPAMessage msg in propuestas)
        {
            string[] partes = msg.contenido.Split(',');
            if (partes.Length != 2) continue;
            if (!int.TryParse(partes[0].Substring(3), out int dl)) continue;
            if (!int.TryParse(partes[1].Substring(3), out int dobj)) continue;
            candidatos.Add((msg, dl, dobj));
        }
        candidatos.Sort((a, b) => a.dl.CompareTo(b.dl));

        string contenidoPerseguir = tactical.GetComandoPersecucion();

        int numPerseguidores = Mathf.Min(MAX_PERSEGUIDORES_PROPUESTAS, candidatos.Count);

        // Si el CNP es de tipo hoguera, asignar el candidato más cercano a ella como Bloqueador
        int idxBloqueador = -1;
        if (tipoCNP == "hoguera" && candidatos.Count > numPerseguidores)
        {
            int minDobj = int.MaxValue;
            for (int i = numPerseguidores; i < candidatos.Count; i++)
            {
                if (candidatos[i].dobj < minDobj)
                {
                    minDobj = candidatos[i].dobj;
                    idxBloqueador = i;
                }
            }
        }

        // Preparar lista de IDs de sector disponibles para los buscadores
        int totalSectores = SectorMap.Sectores.Count;
        var idsDisponibles = new List<int>();
        for (int i = 0; i < totalSectores; i++) idsDisponibles.Add(i);

        int numBuscadores = candidatos.Count - numPerseguidores - (idxBloqueador >= 0 ? 1 : 0);

        int buscadorIdx = 0;
        for (int i = 0; i < candidatos.Count; i++)
        {
            if (i < numPerseguidores)
            {
                EnviarAccept(candidatos[i].msg, contenidoPerseguir);
                Debug.Log($"[SOCIAL {communicator.nombreAgente}]: → {candidatos[i].msg.emisor} PERSEGUIDOR (dl={candidatos[i].dl})");
            }
            else if (i == idxBloqueador)
            {
                EnviarAccept(candidatos[i].msg, "cubrir_hoguera");
                Debug.Log($"[SOCIAL {communicator.nombreAgente}]: → {candidatos[i].msg.emisor} BLOQUEADOR hoguera (dobj={candidatos[i].dobj})");
            }
            else
            {
                if (numBuscadores <= 0) continue;
                var sectoresAgente = new List<int>();
                for (int s = buscadorIdx; s < idsDisponibles.Count; s += numBuscadores)
                    sectoresAgente.Add(idsDisponibles[s]);

                string contenidoSector = "explorar_sector:" + string.Join(",", sectoresAgente);
                EnviarAccept(candidatos[i].msg, contenidoSector);
                Debug.Log($"[SOCIAL {communicator.nombreAgente}]: → {candidatos[i].msg.emisor} BUSCADOR sectores [{string.Join(",", sectoresAgente)}]");
                buscadorIdx++;
            }
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

        // Perseguidor y Bloqueador rechazan (misiones críticas); BuscadorSectores puede reasignarse
        if (creencias.rolActual == BeliefBase.RolCNP.Perseguidor || creencias.rolActual == BeliefBase.RolCNP.Bloqueador)
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
        if (creencias.alarmaHogueraActiva) return;
        if (tactical == null) tactical = controller.GetComponent<ModuleTactical>();

        creencias.rolAsignadoExternamente = true;
        tactical.EjecutarAccionConfirmada(msg.contenido);
    }

    public bool IsCnpIniciado() => cnpIniciado;
    public bool CooldownExpirado() => Time.time - tiempoUltimoCNPFallido >= cooldownCNPFallido;
}