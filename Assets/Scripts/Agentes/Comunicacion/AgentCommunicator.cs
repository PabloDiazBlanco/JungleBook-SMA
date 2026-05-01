using UnityEngine;
using System.Collections.Generic;

public class AgentCommunicator : MonoBehaviour
{
    [Header("Identidad del agente")]
    public string nombreAgente;

    // Registro global de todos los agentes (peer-to-peer, sin MessageBus)
    private static Dictionary<string, AgentCommunicator> registro = new Dictionary<string, AgentCommunicator>();

    // Mensajes encolados por otros agentes en frames anteriores
    private Queue<FIPAMessage> pendientes = new Queue<FIPAMessage>();

    // Mensajes listos para leer este frame
    private List<FIPAMessage> inbox = new List<FIPAMessage>();

    private int contadorConversaciones = 0;
    // Nueva lista para cumplir el requisito de almacenamiento persistente
    private List<FIPAMessage> historialMensajes = new List<FIPAMessage>();

    public List<FIPAMessage> GetHistorial() => historialMensajes;
    // ===================== CICLO DE VIDA =====================

    void Start()
    {
        if (string.IsNullOrEmpty(nombreAgente))
        {
            Debug.LogError($"[AgentCommunicator] en '{gameObject.name}': nombreAgente no asignado.");
            return;
        }

        registro[nombreAgente] = this;
        Debug.Log($"<color=cyan>[AgentCommunicator] '{nombreAgente}': registrado. Agentes activos: [{string.Join(", ", registro.Keys)}]</color>");
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(nombreAgente))
        {
            registro.Remove(nombreAgente);
            Debug.Log($"[AgentCommunicator] '{nombreAgente}': desregistrado.");
        }
    }

    void Update()
    {
        // Limpiamos el buzón del frame actual y procesamos los pendientes
        inbox.Clear();
        ProcesarMensajesPendientes();
    }

    private void ProcesarMensajesPendientes()
    {
        while (pendientes.Count > 0)
        {
            FIPAMessage msg = pendientes.Dequeue();
            
            // ALMACENAMIENTO: Guardamos en el histórico antes de pasar al inbox
            historialMensajes.Add(msg); 
            
            inbox.Add(msg);
            Debug.Log($"<color=green>[AgentCommunicator] '{nombreAgente}': inbox ← [{msg.performativa}] de '{msg.emisor}' | ConvID: '{msg.conversationId}'</color>");
        }
    }

    // ===================== RECEPCIÓN DIRECTA (llamada por el emisor) =====================

    public void RecibirMensaje(FIPAMessage msg)
    {
        pendientes.Enqueue(msg);
    }

    // ===================== ENVÍO =====================

    public void Enviar(FIPAMessage mensaje, List<string> destinatarios)
    {
        if (destinatarios == null || destinatarios.Count == 0) return;

        historialMensajes.Add(mensaje); // ALMACENAMIENTO: Guardamos el mensaje enviado en el histórico
        
        foreach (string dest in destinatarios)
        {
            if (dest == nombreAgente)
            {
                Debug.LogWarning($"[AgentCommunicator] '{nombreAgente}': intento de enviarse mensaje a sí mismo. Ignorado.");
                continue;
            }

            if (!registro.TryGetValue(dest, out AgentCommunicator target))
            {
                Debug.LogWarning($"[AgentCommunicator] '{nombreAgente}': destinatario '{dest}' no registrado. Mensaje no entregado.");
                continue;
            }

            target.RecibirMensaje(mensaje);
            Debug.Log($"<color=orange>[AgentCommunicator] '{nombreAgente}': [{mensaje.performativa}] → '{dest}' | ConvID: '{mensaje.conversationId}' | '{mensaje.contenido}'</color>");
        }
    }

    public void EnviarATodos(FIPAMessage mensaje)
    {
        List<string> compañero = Getcompañero();

        if (compañero.Count == 0)
        {
            Debug.LogWarning($"[AgentCommunicator] '{nombreAgente}': EnviarATodos sin otros agentes registrados.");
            return;
        }

        Debug.Log($"<color=orange>[AgentCommunicator] '{nombreAgente}': broadcast [{mensaje.performativa}] → [{string.Join(", ", compañero)}]</color>");
        Enviar(mensaje, compañero);
    }

    // ===================== CONSULTAS =====================

    public List<FIPAMessage> GetInbox() => inbox;

    public List<string> Getcompañero()
    {
        List<string> resultado = new List<string>();
        foreach (string nombre in registro.Keys)
            if (nombre != nombreAgente) resultado.Add(nombre);
        return resultado;
    }

    // ===================== GENERADOR DE IDs =====================

    public string GenerarConversationId(string tipo)
    {
        contadorConversaciones++;
        return $"{nombreAgente}-{tipo}-{contadorConversaciones}-{Time.time:F1}";
    }
}
