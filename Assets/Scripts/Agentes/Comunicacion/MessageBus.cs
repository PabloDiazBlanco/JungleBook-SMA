using UnityEngine;
using System.Collections.Generic;

public class MessageBus : MonoBehaviour
{
    public static MessageBus Instance { get; private set; }

    // Buzón de mensajes pendientes por agente
    private Dictionary<string, Queue<FIPAMessage>> buzones = new Dictionary<string, Queue<FIPAMessage>>();

    // Agentes registrados en el sistema
    private Dictionary<string, AgentCommunicator> agentesRegistrados = new Dictionary<string, AgentCommunicator>();

    // Historial completo de conversaciones indexado por conversationId
    private Dictionary<string, List<FIPAMessage>> historialConversaciones = new Dictionary<string, List<FIPAMessage>>();

    // ===================== SINGLETON =====================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MessageBus]: Ya existe una instancia en escena. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("<color=cyan>[MessageBus]: Sistema de mensajería inicializado y listo.</color>");
    }

    // ===================== REGISTRO DE AGENTES =====================

    public void Registrar(string nombre, AgentCommunicator communicator)
    {
        if (agentesRegistrados.ContainsKey(nombre))
        {
            Debug.LogWarning($"[MessageBus]: El agente '{nombre}' ya estaba registrado. Sobreescribiendo.");
        }

        agentesRegistrados[nombre] = communicator;
        buzones[nombre] = new Queue<FIPAMessage>();
        Debug.Log($"<color=cyan>[MessageBus]: Agente '{nombre}' registrado. Total agentes: {agentesRegistrados.Count} ({string.Join(", ", agentesRegistrados.Keys)})</color>");
    }

    public void Desregistrar(string nombre)
    {
        if (!agentesRegistrados.ContainsKey(nombre))
        {
            Debug.LogWarning($"[MessageBus]: Intento de desregistrar '{nombre}' pero no estaba registrado.");
            return;
        }

        agentesRegistrados.Remove(nombre);
        buzones.Remove(nombre);
        Debug.Log($"<color=cyan>[MessageBus]: Agente '{nombre}' desregistrado. Agentes restantes: {agentesRegistrados.Count}</color>");
    }

    // ===================== ENVÍO DE MENSAJES =====================

    public void Enviar(FIPAMessage mensaje, List<string> destinatarios)
    {
        if (destinatarios == null || destinatarios.Count == 0)
        {
            Debug.LogWarning($"[MessageBus]: '{mensaje.emisor}' intentó enviar un mensaje sin destinatarios. Ignorado.");
            return;
        }

        // Guardar en historial de conversaciones
        GuardarEnHistorial(mensaje);

        Debug.Log($"<color=yellow>[MessageBus]: Mensaje de '{mensaje.emisor}' " +
                  $"[{mensaje.performativa}] → {destinatarios.Count} destinatario(s): [{string.Join(", ", destinatarios)}] " +
                  $"| ConvID: '{mensaje.conversationId}' | Contenido: '{mensaje.contenido}'</color>");

        // Entregar a cada destinatario con foreach
        int entregados = 0;
        foreach (string destinatario in destinatarios)
        {
            if (destinatario == mensaje.emisor)
            {
                Debug.LogWarning($"[MessageBus]: '{mensaje.emisor}' intentó enviarse un mensaje a sí mismo. Ignorado.");
                continue;
            }

            if (!buzones.ContainsKey(destinatario))
            {
                Debug.LogWarning($"[MessageBus]: Destinatario '{destinatario}' no existe en el sistema. Mensaje no entregado.");
                continue;
            }

            buzones[destinatario].Enqueue(mensaje);
            entregados++;
            Debug.Log($"<color=yellow>[MessageBus]: ✓ Mensaje depositado en buzón de '{destinatario}'. " +
                      $"Mensajes pendientes en su buzón: {buzones[destinatario].Count}</color>");
        }

        if (entregados == 0)
            Debug.LogWarning($"[MessageBus]: El mensaje de '{mensaje.emisor}' no llegó a ningún destinatario.");
        else
            Debug.Log($"<color=yellow>[MessageBus]: Entrega completada — {entregados}/{destinatarios.Count} mensajes entregados.</color>");
    }

    // ===================== RECOGIDA DE MENSAJES =====================

    public List<FIPAMessage> Recoger(string nombre)
    {
        List<FIPAMessage> mensajesRecogidos = new List<FIPAMessage>();

        if (!buzones.ContainsKey(nombre))
        {
            Debug.LogWarning($"[MessageBus]: '{nombre}' intentó recoger mensajes pero no está registrado.");
            return mensajesRecogidos;
        }

        Queue<FIPAMessage> buzon = buzones[nombre];

        if (buzon.Count == 0)
            return mensajesRecogidos;

        Debug.Log($"<color=cyan>[MessageBus]: '{nombre}' recoge {buzon.Count} mensaje(s) de su buzón.</color>");

        while (buzon.Count > 0)
        {
            FIPAMessage msg = buzon.Dequeue();
            mensajesRecogidos.Add(msg);
            Debug.Log($"<color=cyan>[MessageBus]:   → [{msg.performativa}] de '{msg.emisor}' | ConvID: '{msg.conversationId}'</color>");
        }

        return mensajesRecogidos;
    }

    // ===================== HISTORIAL =====================

    private void GuardarEnHistorial(FIPAMessage mensaje)
    {
        if (string.IsNullOrEmpty(mensaje.conversationId))
        {
            Debug.LogWarning($"[MessageBus]: Mensaje de '{mensaje.emisor}' sin conversationId. No se guarda en historial.");
            return;
        }

        if (!historialConversaciones.ContainsKey(mensaje.conversationId))
        {
            historialConversaciones[mensaje.conversationId] = new List<FIPAMessage>();
            Debug.Log($"[MessageBus]: Nueva conversación registrada en historial: '{mensaje.conversationId}'");
        }

        historialConversaciones[mensaje.conversationId].Add(mensaje);
    }

    public List<FIPAMessage> GetHistorialConversacion(string conversationId)
    {
        if (!historialConversaciones.ContainsKey(conversationId))
        {
            Debug.LogWarning($"[MessageBus]: No existe historial para conversationId '{conversationId}'.");
            return new List<FIPAMessage>();
        }

        Debug.Log($"[MessageBus]: Historial de '{conversationId}' consultado — {historialConversaciones[conversationId].Count} mensaje(s).");
        return historialConversaciones[conversationId];
    }

    public Dictionary<string, List<FIPAMessage>> GetTodoElHistorial()
    {
        Debug.Log($"[MessageBus]: Historial completo consultado — {historialConversaciones.Count} conversación(es) registradas.");
        return historialConversaciones;
    }

    // ===================== CONSULTAS DE AGENTES =====================

    public List<string> GetTodosLosNombres()
    {
        return new List<string>(agentesRegistrados.Keys);
    }

    public List<string> GetNombresExcepto(string excluido)
    {
        List<string> resultado = new List<string>();
        foreach (string nombre in agentesRegistrados.Keys)
        {
            if (nombre != excluido)
                resultado.Add(nombre);
        }
        return resultado;
    }

    public bool EstaRegistrado(string nombre)
    {
        return agentesRegistrados.ContainsKey(nombre);
    }
}
