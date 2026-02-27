using UnityEngine;

public class GuardVision : MonoBehaviour
{
    // Distancia maxima a la que puede ver el guardia
    public float distanciaVision = 15f;
    // Medio angulo de vision (45 grados significa un cono total de 90)
    public float anguloVision = 45f;
    // Capa de Unity que representa las paredes (para que no vea a traves de ellas)
    public LayerMask capaObstaculos;
    
    // Referencia al ladron que buscamos
    public Transform objetivoLadron;

    void Awake()
    {
        // Buscamos al ladron por su etiqueta (Tag) al iniciar
        GameObject ladron = GameObject.FindGameObjectWithTag("Thief");
        if (ladron != null) objetivoLadron = ladron.transform;
    }

    // Metodo que consultaran las capas de comportamiento (Funcion Vision)
    public bool PuedeVerAlLadron()
    {
        if (objetivoLadron == null) return false;

        // 1. Calculamos la direccion y distancia hacia el ladron
        Vector3 direccion = (objetivoLadron.position - transform.position).normalized;
        float distanciaActual = Vector3.Distance(transform.position, objetivoLadron.position);

        // 2. Comprobamos si esta dentro del rango de distancia
        if (distanciaActual <= distanciaVision)
        {
            // 3. Comprobamos si esta dentro del cono frontal de vision
            if (Vector3.Angle(transform.forward, direccion) < anguloVision)
            {
                // 4. Lanzamos un Raycast para ver si hay una pared en medio
                // Si el rayo NO golpea nada en la capa de obstaculos, es que lo ve
                if (!Physics.Raycast(transform.position, direccion, distanciaActual, capaObstaculos))
                {
                    return true; // Percepcion positiva
                }
            }
        }
        return false; // No hay percepcion del ladron
    }
}