using UnityEngine;

public class SensorHogueraIndividual : MonoBehaviour
{
    [Header("Detección")]
    public float radioDeteccion = 12f;
    public float anguloVision = 120f;
    public LayerMask capaObstaculos;
    public LayerMask capaHoguera;

    [HideInInspector] public bool hogueraDetectada = false;
    [HideInInspector] public Vector3? posicionHogueraConocida = null;
    [HideInInspector] public bool haVistoHogueraAlgunaVez = false;
    // NUEVO: true cuando la hoguera está en rango Y en ángulo, aunque haya obstáculo
    // El cerebro usa esto para saber si tiene sentido incrementar el contador de frames
    [HideInInspector] public bool hogueraEnCampoDeVision = false;

    private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";
    private bool hogueraDetectadaAnterior = false;
    private bool visionBloqueadaLogueada = false;

    void Update()
    {
        EscanearConVision();

        if (EsAldeanoPrincipal && haVistoHogueraAlgunaVez && hogueraDetectada != hogueraDetectadaAnterior)
        {
            if (hogueraDetectada)
                Debug.Log($"<color=green>[SENSOR {gameObject.name}]: Hoguera visible. hogueraDetectada=true</color>");
            else
                Debug.Log($"<color=orange>[SENSOR {gameObject.name}]: Hoguera no detectada. hogueraDetectada=false</color>");
        }
        hogueraDetectadaAnterior = hogueraDetectada;
    }

    private void EscanearConVision()
    {
        if (!haVistoHogueraAlgunaVez)
            BuscarHogueraPorPrimeraVez();
        else
            ComprobarHogueraConRaycast();
    }

    private void BuscarHogueraPorPrimeraVez()
    {
        hogueraDetectada = false;
        hogueraEnCampoDeVision = false;

        Collider[] objetos = Physics.OverlapSphere(transform.position, radioDeteccion, capaHoguera);

        foreach (Collider obj in objetos)
        {
            if (!obj.CompareTag("FuegoHoguera")) continue;

            if (PuedoVerObjeto(obj.transform.position))
            {
                hogueraDetectada = true;
                hogueraEnCampoDeVision = true;
                posicionHogueraConocida = obj.transform.position;
                haVistoHogueraAlgunaVez = true;

                if (EsAldeanoPrincipal)
                    Debug.Log($"<color=green>[SENSOR {gameObject.name}]: Hoguera descubierta en {posicionHogueraConocida}.</color>");
                return;
            }
        }
    }

    private void ComprobarHogueraConRaycast()
    {
        hogueraDetectada = false;
        hogueraEnCampoDeVision = false;

        if (posicionHogueraConocida == null) return;

        Vector3 origen = transform.position + Vector3.up * 1.5f;
        Vector3 posicionObjetivo = posicionHogueraConocida.Value;
        float distancia = Vector3.Distance(origen, posicionObjetivo);

        if (distancia > radioDeteccion) return;

        Vector3 direccion = (posicionObjetivo - origen).normalized;
        float angulo = Vector3.Angle(transform.forward, direccion);

        if (angulo > anguloVision) return;

        // llegados aquí la hoguera está en rango y en ángulo, aunque haya obstáculo
        hogueraEnCampoDeVision = true;

        // RaycastAll para atravesar partes decorativas de la hoguera (Piedras, FireTrigger)
        // y detectar el fuego real aunque estén por delante
        RaycastHit[] hits = Physics.RaycastAll(origen, direccion, distancia + 1f, capaObstaculos | capaHoguera);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("FuegoHoguera"))
            {
                hogueraDetectada = true;
                visionBloqueadaLogueada = false;
                posicionHogueraConocida = hit.collider.transform.position;
                Debug.DrawLine(origen, hit.point, Color.green);
                return;
            }

            // Obstáculo real (pared, edificio) — visión bloqueada
            if (((1 << hit.collider.gameObject.layer) & capaObstaculos.value) != 0)
            {
                Debug.DrawLine(origen, hit.point, Color.red);
                if (EsAldeanoPrincipal && !visionBloqueadaLogueada)
                {
                    Debug.Log($"<color=red>[SENSOR {gameObject.name}]: Visión bloqueada por '{hit.collider.name}'</color>");
                    visionBloqueadaLogueada = true;
                }
                return;
            }

            // En capaHoguera pero sin tag FuegoHoguera (Piedras, FireTrigger, etc.) — ignorar y continuar
        }

        Debug.DrawLine(origen, origen + direccion * (distancia + 1f), Color.yellow);
    }

    private bool PuedoVerObjeto(Vector3 posicion)
    {
        Vector3 origen = transform.position + Vector3.up * 1.5f;
        float distancia = Vector3.Distance(origen, posicion);
        if (distancia > radioDeteccion) return false;

        Vector3 direccion = (posicion - origen).normalized;
        if (Vector3.Angle(transform.forward, direccion) > anguloVision) return false;

        if (!Physics.Raycast(origen, direccion, distancia, capaObstaculos))
            return true;

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}