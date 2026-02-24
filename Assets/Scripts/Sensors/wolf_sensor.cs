using UnityEngine;

namespace Sensors
{
    public class wolf_sensor : MonoBehaviour
    {
        public float rangoDeteccion = 10f; // Radio de detección en metros

        // Detecta si el lobo está cerca del ladrón
        public bool PuedeDetectarAlLadron(Transform ladron)
        {
            if (ladron == null) return false;

            float distancia = Vector3.Distance(transform.position, ladron.position); // Distancia entre el lobo y el ladrón
            return distancia <= rangoDeteccion; // Si está dentro del rango, lo detecta
        }
    }
}