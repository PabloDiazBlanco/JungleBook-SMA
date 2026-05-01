using UnityEngine;
using System.Collections.Generic;

public abstract class DeliberativeModule : MonoBehaviour
{
    // Contexto compartido que la DeliberativeLayer les entregará
    protected BeliefBase creencias;
    protected AgentCommunicator communicator;
    protected SubsumptionController controller;

    // Método para que la capa principal les pase las referencias
    public virtual void Inicializar(BeliefBase b, AgentCommunicator comm, SubsumptionController ctrl)
    {
        creencias = b;
        communicator = comm;
        controller = ctrl;
    }

    // El orquestador llamará a esto en cada Update
    public abstract void Procesar();
}