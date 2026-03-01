using UnityEngine;

public class GuardHearing : MonoBehaviour
{
    [Header("Configuración de Audición")]
    public float agudezaAuditiva = 1.0f;
    public float radioImprecision = 5.0f; 
    private MainCharacter_Brain mainCharacter;
    private Vector3 posicionRuidoDetectada;
    private bool haEscuchadoAlgo = false;

    void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Thief");
        if (player != null) mainCharacter = player.GetComponent<MainCharacter_Brain>();
        if (mainCharacter == null) this.enabled = false;
    }

    void Update()
    {
        ProcesarAudicion();
    }

    private void ProcesarAudicion()
    {
        float radioRuidoLadrón = mainCharacter.radioRuidoActual;
        float distanciaAlLadrón = Vector3.Distance(transform.position, mainCharacter.transform.position);

        bool ruidoEnRango = distanciaAlLadrón <= (radioRuidoLadrón * agudezaAuditiva);

        ActualizarMemoriaAcustica(ruidoEnRango && radioRuidoLadrón > 0);
    }

    private void ActualizarMemoriaAcustica(bool detectado)
    {
        if (detectado)
        {            
            Vector2 circuloAleatorio = Random.insideUnitCircle * radioImprecision;
            Vector3 desplazamiento = new Vector3(circuloAleatorio.x, 0, circuloAleatorio.y);
            posicionRuidoDetectada = mainCharacter.transform.position + desplazamiento;
            
            haEscuchadoAlgo = true;
            Debug.Log($"<color=orange>OÍDO: {gameObject.name} ha escuchado algo por la zona de {posicionRuidoDetectada}</color>");
        }
    }

    public bool EscuchoAlgo() => haEscuchadoAlgo;
    public Vector3 GetPosicionRuido() => posicionRuidoDetectada;

    public void ResetearAudicion()
    {
        haEscuchadoAlgo = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (mainCharacter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(mainCharacter.transform.position, mainCharacter.radioRuidoActual * agudezaAuditiva);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mainCharacter.transform.position, radioImprecision);
        }
    }
}