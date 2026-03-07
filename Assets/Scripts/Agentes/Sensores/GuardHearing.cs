using UnityEngine;

public class GuardHearing : MonoBehaviour
{
    [Header("Configuración de Audición")]
    public float agudezaAuditiva = 1.0f;
    public float radioImprecision = 5.0f;
    public LayerMask capaLadron;

    private Vector3 posicionRuidoDetectada;
    private bool haEscuchadoAlgo = false;

    void Update()
    {
        ProcesarAudicion();
    }

    private void ProcesarAudicion()
    {
        Collider[] objetivosEnRango = Physics.OverlapSphere(
            transform.position,
            50f, // Radio 
            capaLadron
        );

        foreach (Collider objetivo in objetivosEnRango)
        {
            if (!objetivo.CompareTag("Thief")) continue;

            MainCharacter_Brain cerebro = objetivo.GetComponent<MainCharacter_Brain>();
            if (cerebro == null) continue;

            float distanciaAlLadron = Vector3.Distance(transform.position, objetivo.transform.position);
            bool ruidoEnRango = distanciaAlLadron <= (cerebro.radioRuidoActual * agudezaAuditiva)
                                && cerebro.radioRuidoActual > 0;

            if (ruidoEnRango)
            {
                ActualizarMemoriaAcustica(objetivo.transform.position);
            }
        }
    }

    private void ActualizarMemoriaAcustica(Vector3 posicionReal)
    {
        Vector2 circuloAleatorio = Random.insideUnitCircle * radioImprecision;
        Vector3 desplazamiento = new Vector3(circuloAleatorio.x, 0, circuloAleatorio.y);
        posicionRuidoDetectada = posicionReal + desplazamiento;

        haEscuchadoAlgo = true;
        Debug.Log($"<color=orange>OÍDO: {gameObject.name} ha escuchado algo por la zona de {posicionRuidoDetectada}</color>");
    }

    public bool EscuchoAlgo() => haEscuchadoAlgo;
    public Vector3 GetPosicionRuido() => posicionRuidoDetectada;

    public void ResetearAudicion()
    {
        haEscuchadoAlgo = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 50f * agudezaAuditiva);
    }
}