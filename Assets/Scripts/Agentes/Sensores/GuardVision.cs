using UnityEngine;

public class GuardVision : MonoBehaviour
{
    public float distanciaVision = 15f;
    public float anguloVision = 45f;
    public LayerMask capaObstaculos;
    public LayerMask capaLadron; 
    private Vector3 ultimaPosicionDetectada;
    public bool ladronTieneFuego { get; private set; } = false;
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
            Vector3 direccion = (objetivo.transform.position - transform.position).normalized;
            float distanciaActual = Vector3.Distance(transform.position, objetivo.transform.position);

            if (Vector3.Angle(transform.forward, direccion) < anguloVision)
            {
                if (!Physics.Raycast(transform.position, direccion, distanciaActual, capaObstaculos))
                {
                    ultimaPosicionDetectada = objetivo.transform.position;
                    // NUEVO: buscar si el ladrón lleva el fuego activo (tag LadronConFuego en hijo)
                    // El tag lo pone MainCharacter_Brain al activar fuegoEnAntorcha
                    bool fuegoActivo = objetivo.CompareTag("LadronConFuego");
                    if (!fuegoActivo)
                    {
                        // Por si el collider detectado es el padre y el tag está en un hijo
                        fuegoActivo = objetivo.GetComponentInChildren<Transform>() != null &&
                                      objetivo.transform.CompareTag("LadronConFuego");
                    }
 
                    if (fuegoActivo)
                    {
                        ladronTieneFuego = true;
                        if (!yaLogueadoFuego)
                        {
                            Debug.Log($"<color=orange>[VISION {gameObject.name}]: Veo al ladrón CON EL FUEGO. ladronTieneFuego=true</color>");
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
 
        yaLogueadoFuego = false;
        return false;
    }

    public Vector3 UltimaPosicionDetectada()
    {
        return ultimaPosicionDetectada;
    }
}