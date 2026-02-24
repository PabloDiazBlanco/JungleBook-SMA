using UnityEngine;
using UnityEngine.AI;

namespace Actors
{
    public class wolf_actor : MonoBehaviour
    {
        private NavMeshAgent agente;
        private Animator anim; // <--- Nuevo: Referencia al Animator

        void Start()
        {
            agente = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>(); // <--- Nuevo: Buscamos el Animator
        }

        void Update() 
        {
            // <--- Nuevo: Sincronizamos la velocidad con el Animator
            // Usamos magnitude para saber qué tan rápido va (0 = parado, >0 = moviéndose)
            float velocidadActual = agente.velocity.magnitude;
            anim.SetFloat("Speed", velocidadActual);
        }

        public void MoveTo(Vector3 destino)
        {
            agente.SetDestination(destino);
        }

        public NavMeshAgent GetAgent()
        {
            return agente;
        }
    }
}