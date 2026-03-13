using UnityEngine;

public class SensorHogueraIndividual : MonoBehaviour
{
    [Header("Detección Visual")]
    public GuardVision sensorVision;
    public float radioDeteccion = 12f;
    public LayerMask capaInteres;

    [Header("Alarma")]
    public float radioSeguridadAlarma = 15f;

    [Tooltip("Segundos mirando hacia la hoguera sin verla para activar la alarma.")]
    public float tiempoConfirmacionAlarma = 2f;

    public bool alarmaRoboDetectada = false;
    public bool veHoguera = false;

    private float cronometroSinVer = 0f;
    private Vector3 posicionHogueraConocida;
    private bool posicionConocida = false;
    private bool haVistoFuegoAlMenos1Vez = true;

    void Start()
    {
        if (sensorVision == null)
            sensorVision = GetComponent<GuardVision>();

        if (sensorVision == null)
            Debug.LogError($"[SENSOR HOGUERA] {gameObject.name}: NO se encontró GuardVision.");
    }

    void Update()
    {
        VigilarHoguera();
    }

    private void VigilarHoguera()
    {
        if (alarmaRoboDetectada) return;

        // Buscar hoguera físicamente
        Collider[] objetos = Physics.OverlapSphere(transform.position, radioDeteccion, capaInteres);
        Transform hogueraEncontrada = null;

        foreach (Collider obj in objetos)
        {
            if (obj.CompareTag("FuegoHoguera"))
            {
                hogueraEncontrada = obj.transform;
                break;
            }
        }

        // Guardar posición en memoria si la detectamos físicamente
        if (hogueraEncontrada != null)
        {
            posicionHogueraConocida = hogueraEncontrada.position;
            posicionConocida = true;
        }

        if (!posicionConocida) return;

        float distanciaAlSitio = Vector3.Distance(transform.position, posicionHogueraConocida);

        // Solo vigilar dentro del radio de seguridad
        if (distanciaAlSitio > radioSeguridadAlarma)
        {
            cronometroSinVer = 0f;
            return;
        }

        // Comprobar si miramos hacia donde está/estaba la hoguera
        bool mirандоHaciaHoguera = EstaMirandoHaciaHoguera(posicionHogueraConocida);

        if (!mirандоHaciaHoguera)
        {
            // Está de espaldas o de lado — no podemos sacar conclusiones, ignorar
            cronometroSinVer = 0f;
            return;
        }

        // Está mirando hacia la hoguera — ahora sí comprobar si la ve
        if (hogueraEncontrada != null)
        {
            veHoguera = PuedeVerHoguera(hogueraEncontrada);
        }
        else
        {
            // El objeto no existe físicamente y estamos mirando hacia donde debería estar
            veHoguera = false;
        }

        if (veHoguera)
        {
            // Ve el fuego — recordar y resetear cronómetro
            haVistoFuegoAlMenos1Vez = true;
            cronometroSinVer = 0f;
        }
        else
        {
            // Solo contar si ya había visto el fuego antes (evita falsos positivos al inicio)
            if (haVistoFuegoAlMenos1Vez)
            {
                cronometroSinVer += Time.deltaTime;

                if (cronometroSinVer >= tiempoConfirmacionAlarma)
                {
                    alarmaRoboDetectada = true;
                    Debug.Log($"<color=red>[SENSOR HOGUERA] ¡ALERTA! {gameObject.name} confirma que no hay fuego.</color>");
                }
            }
        }
    }

    // Comprueba solo si el ángulo hacia la hoguera está dentro del campo de visión
    private bool EstaMirandoHaciaHoguera(Vector3 posicionHoguera)
    {
        Vector3 direccion = (posicionHoguera - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccion);
        return angulo < sensorVision.anguloVision;
    }

    // Comprueba ángulo + raycast (sin obstáculos entre medio)
    private bool PuedeVerHoguera(Transform hoguera)
    {
        if (sensorVision == null) return false;

        Vector3 origen = transform.position;
        Vector3 direccion = (hoguera.position - origen).normalized;
        float distancia = Vector3.Distance(origen, hoguera.position);

        if (Vector3.Angle(transform.forward, direccion) < sensorVision.anguloVision)
        {
            bool bloqueado = Physics.Raycast(origen, direccion, distancia, sensorVision.capaObstaculos);
            Debug.DrawRay(origen, direccion * distancia, bloqueado ? Color.red : Color.green);
            return !bloqueado;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = alarmaRoboDetectada ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioSeguridadAlarma);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}