using UnityEngine;
using System.Collections.Generic;

public class AgentCommunicator : MonoBehaviour
{
    [Header("Identidad del agente")]
    public string nombreAgente;

    // Mensajes recibidos en el frame actual, listos para que la capa deliberativa los procese
    private List<FIPAMessage> inbox = new List<FIPAMessage>();

    // Contador para generar conversationIds únicos
    private int contadorConversaciones = 0;

    // ===================== CICLO DE VIDA =====================

    void Start()
    {
        if (string.IsNullOrEmpty(nombreAgente))
        {
            Debug.LogError($"[AgentCommunicator] en '{gameObject.name}': nombreAgente no está asignado en el Inspector. El agente no funcionará.");
            return;
        }

        if (MessageBus.Instance == null)
        {
            Debug.LogError($"[AgentCommunicator] '{nombreAgente}': No se encontró MessageBus en la escena. Asegúrate de que existe un GameObject con MessageBus.");
            return;
        }

        MessageBus.Instance.Registrar(nombreAgente, this);
        Debug.Log($"<color=green>[AgentCommunicator] '{nombreAgente}': Registrado correctamente en el MessageBus.</color>");
    }

    void OnDestroy()
    {
        if (MessageBus.Instance != null && !string.IsNullOrEmpty(nombreAgente))
        {
            MessageBus.Instance.Desregistrar(nombreAgente);
            Debug.Log($"[AgentCommunicator] '{nombreAgente}': Desregistrado del MessageBus.");
        }
    }

    void Update()
    {
        RecogerMensajes();
    }

    // ===================== RECOGIDA DE MENSAJES =====================

    private void RecogerMensajes()
    {
        if (MessageBus.Instance == null) return;

        // Limpiar inbox del frame anterior
        inbox.Clear();

        // Recoger los nuevos mensajes del buzón
        List<FIPAMessage> nuevos = MessageBus.Instance.Recoger(nombreAgente);

        if (nuevos.Count > 0)
        {
            inbox.AddRange(nuevos);
            Debug.Log($"<color=green>[AgentCommunicator] '{nombreAgente}': {inbox.Count} mensaje(s) nuevo(s) en inbox este frame.</color>");

            foreach (FIPAMessage msg in inbox)
            {
                Debug.Log($"<color=green>[AgentCommunicator] '{nombreAgente}':   → [{msg.performativa}] de '{msg.emisor}' " +
                          $"| ConvID: '{msg.conversationId}' | Contenido: '{msg.contenido}'</color>");
            }
        }
    }

    // ===================== ENVÍO DE MENSAJES =====================

    public void Enviar(FIPAMessage mensaje, List<string> destinatarios)
    {
        if (MessageBus.Instance == null)
        {
            Debug.LogError($"[AgentCommunicator] '{nombreAgente}': No hay MessageBus. No se puede enviar.");
            return;
        }

        Debug.Log($"<color=orange>[AgentCommunicator] '{nombreAgente}': Enviando [{mensaje.performativa}] " +
                  $"a [{string.Join(", ", destinatarios)}] | ConvID: '{mensaje.conversationId}'</color>");

        MessageBus.Instance.Enviar(mensaje, destinatarios);
    }

    public void EnviarATodos(FIPAMessage mensaje)
    {
        if (MessageBus.Instance == null)
        {
            Debug.LogError($"[AgentCommunicator] '{nombreAgente}': No hay MessageBus. No se puede enviar.");
            return;
        }

        List<string> todosExceptoYo = MessageBus.Instance.GetNombresExcepto(nombreAgente);

        if (todosExceptoYo.Count == 0)
        {
            Debug.LogWarning($"[AgentCommunicator] '{nombreAgente}': EnviarATodos llamado pero no hay otros agentes registrados.");
            return;
        }

        Debug.Log($"<color=orange>[AgentCommunicator] '{nombreAgente}': EnviarATodos [{mensaje.performativa}] " +
                  $"→ {todosExceptoYo.Count} agente(s): [{string.Join(", ", todosExceptoYo)}]</color>");

        MessageBus.Instance.Enviar(mensaje, todosExceptoYo);
    }

    // ===================== CONSULTAS =====================

    public List<FIPAMessage> GetInbox()
    {
        return inbox;
    }

    public List<string> GetCompaneros()
    {
        if (MessageBus.Instance == null) return new List<string>();
        return MessageBus.Instance.GetNombresExcepto(nombreAgente);
    }

    public List<string> GetTodosLosAgentes()
    {
        if (MessageBus.Instance == null) return new List<string>();
        return MessageBus.Instance.GetTodosLosNombres();
    }

    public List<FIPAMessage> GetHistorialConversacion(string conversationId)
    {
        if (MessageBus.Instance == null) return new List<FIPAMessage>();
        return MessageBus.Instance.GetHistorialConversacion(conversationId);
    }

    // ===================== GENERADOR DE IDs =====================

    public string GenerarConversationId(string tipo)
    {
        contadorConversaciones++;
        string id = $"{nombreAgente}-{tipo}-{contadorConversaciones}-{Time.time:F1}";
        Debug.Log($"[AgentCommunicator] '{nombreAgente}': Nuevo conversationId generado: '{id}'");
        return id;
    }
}
