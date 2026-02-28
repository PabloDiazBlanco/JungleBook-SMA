using UnityEngine;

public class SensorHogueraIndividual : MonoBehaviour
{
    public float radioDeteccion = 12f;
    public LayerMask capaInteres;
    public float radioSeguridadAlarma = 15f; 

    private Vector3 posicionHogueraMemoria;
    private bool haInicializado = false;
    public bool alarmaRoboDetectada = false;
    public bool veHoguera = false;

    void Start()
    {
        // Solo buscamos el punto una vez al nacer para guardar la ubicación en memoria local
        GameObject punto = GameObject.Find("PuntoHogueraCentral");
        if (punto != null)
        {
            posicionHogueraMemoria = punto.transform.position;
            haInicializado = true;
            Debug.Log($"<color=cyan>MEMORIA: {gameObject.name} conoce la ubicación de la hoguera.</color>");
        }
    }

    void Update()
    {
        if (!haInicializado || alarmaRoboDetectada) return;

        float distanciaAlSitio = Vector3.Distance(transform.position, posicionHogueraMemoria);

        // SOLO si el aldeano está cerca del sitio, verifica la presencia física del fuego
        if (distanciaAlSitio < radioSeguridadAlarma)
        {
            bool fuegoEncontrado = false;
            Collider[] objetos = Physics.OverlapSphere(transform.position, radioDeteccion, capaInteres);

            foreach (Collider obj in objetos)
            {
                if (obj.CompareTag("FuegoHoguera"))
                {
                    fuegoEncontrado = true;
                    break;
                }
            }

            veHoguera = fuegoEncontrado;

            // Si está en el rango pero tras el escaneo no hay fuego: Alarma.
            if (!veHoguera)
            {
                alarmaRoboDetectada = true;
                Debug.LogError($"<color=red>¡ALERTA! {gameObject.name} ha visto que no hay fuego en la plaza.</color>");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = alarmaRoboDetectada ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioSeguridadAlarma);
    }
}