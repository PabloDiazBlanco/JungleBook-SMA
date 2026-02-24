using UnityEngine;
using Actors;
using Sensors; // Añadido para poder usar el sensor de proximidad

namespace Brains
{
    public class Wolf_brain : MonoBehaviour
    {
        public Transform[] destinos; // Puntos de patrullaje
        private int indice = 0; // Índice del punto de patrullaje actual
        private wolf_actor actor;
        private wolf_sensor sensor; // Variable para el sensor de proximidad

        public float velocidadPatrullaje = 5f; // Velocidad del lobo
        public Transform ladron; // Transform del ladrón

        void Start()
        {
            actor = GetComponent<wolf_actor>(); // Obtiene el actor para mover al lobo
            sensor = GetComponent<wolf_sensor>(); // Obtiene el sensor
            actor.GetAgent().speed = velocidadPatrullaje; // Establece la velocidad
            MoverAlSiguienteDestino(); // Mueve al primer destino
        }

        void Update()
        {
            if (destinos.Length == 0) return;

            // Verifica si el lobo puede detectar al ladrón por proximidad
            bool detectaAlLadron = sensor.PuedeDetectarAlLadron(ladron);

            if (detectaAlLadron)
            {
                // Si detecta al ladrón, persigue
                actor.MoveTo(ladron.position);
                actor.GetAgent().speed = 10f; // Aumenta la velocidad al perseguir
            }
            else
            {
                // Si no detecta al ladrón, sigue patrullando
                if (!actor.GetAgent().pathPending && actor.GetAgent().remainingDistance <= 0.2f)
                {
                    indice = (indice + 1) % destinos.Length;
                    MoverAlSiguienteDestino();
                }
            }
        }

        // Mueve al lobo al siguiente destino
        private void MoverAlSiguienteDestino()
        {
            actor.MoveTo(destinos[indice].position);
        }
    }
}