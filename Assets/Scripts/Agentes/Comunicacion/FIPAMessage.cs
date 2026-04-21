public class FIPAMessage
{
    public FIPAPerformativa performativa;
    public string emisor;
    public string receptor;
    public string contenido;
    public string conversationId;
    public float timestamp;
    public string inReplyTo;

    public FIPAMessage(
        FIPAPerformativa performativa,
        string emisor,
        string receptor,
        string contenido,
        string conversationId,
        string inReplyTo = "")
    {
        this.performativa    = performativa;
        this.emisor          = emisor;
        this.receptor        = receptor;
        this.contenido       = contenido;
        this.conversationId  = conversationId;
        this.timestamp       = UnityEngine.Time.time;
        this.inReplyTo       = inReplyTo;
    }
}