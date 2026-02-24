using UnityEngine;
using UnityEngine.AI;

namespace Actors
{
    public class wolf_actor : MonoBehaviour
    {
        private NavMeshAgent agente;

        void Start()
        {
            agente = GetComponent<NavMeshAgent>();
        }

        // Mueve al agente hacia un destino
        public void MoveTo(Vector3 destino)
        {
            agente.SetDestination(destino);
        }

        // Obtiene el NavMeshAgent para ser utilizado por otros scripts
        public NavMeshAgent GetAgent()
        {
            return agente;
        }
    }
}