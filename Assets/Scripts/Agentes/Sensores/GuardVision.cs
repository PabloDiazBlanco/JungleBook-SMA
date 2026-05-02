using UnityEngine;

public class GuardVision : MonoBehaviour
{
    public float distanciaVision = 15f;
    public float anguloVision = 45f;
    public LayerMask capaObstaculos;
    public LayerMask capaLadron; 
    
    private Vector3 ultimaPosicionDetectada;
    
    // NUEVO: Propiedades cinemáticas para la predicción BDI
    public bool ladronTieneFuego { get; private set; } = false;
    public float velocidadLadron { get; private set; } = 0f; 
    public Vector3 direccionLadron { get; private set; } = Vector3.zero; 

    private bool yaLogueadoFuego = false;

    public bool PuedeVerAlLadron()
    {
        ladronTieneFuego = false;

        Collider[] objetivosEnRango = Physics.OverlapSphere(
            transform.position, 
            distanciaVision, 
            capaLadron
        );

        foreach (Collider objetivo in objetivosEnRango)
        {
            Vector3 direccionHaciaObjetivo = (objetivo.transform.position - transform.position).normalized;
            float distanciaActual = Vector3.Distance(transform.position, objetivo.transform.position);

            if (Vector3.Angle(transform.forward, direccionHaciaObjetivo) < anguloVision)
            {
                if (!Physics.Raycast(transform.position, direccionHaciaObjetivo, distanciaActual, capaObstaculos))
                {
                    ultimaPosicionDetectada = objetivo.transform.position;

                    // EXTRACCIÓN CINEMÁTICA: Capturamos datos del CharacterController
                    CharacterController ccLadron = objetivo.GetComponent<CharacterController>();
                    if (ccLadron != null)
                    {
                        velocidadLadron = ccLadron.velocity.magnitude; 
                        direccionLadron = ccLadron.velocity.normalized; 
                    }

                    // Comprobación de tag para fuego
                    bool fuegoActivo = objetivo.CompareTag("LadronConFuego");
                    if (!fuegoActivo)
                    {
                        fuegoActivo = objetivo.GetComponentInChildren<Transform>() != null &&
                                      objetivo.transform.CompareTag("LadronConFuego");
                    }
 
                    if (fuegoActivo)
                    {
                        ladronTieneFuego = true;
                        if (!yaLogueadoFuego)
                        {
                            Debug.Log($"<color=orange>[VISION {gameObject.name}]: Veo al ladrón CON EL FUEGO a {velocidadLadron:F1} m/s</color>");
                            yaLogueadoFuego = true;
                        }
                    }
                    else
                    {
                        yaLogueadoFuego = false;
                    }

                    return true;
                }
            }
        }
 
        // Si no se detecta en este frame, mantenemos la última posición pero reseteamos inercia
        yaLogueadoFuego = false;
        return false;
    }

    public Vector3 UltimaPosicionDetectada()
    {
        return ultimaPosicionDetectada;
    }
}