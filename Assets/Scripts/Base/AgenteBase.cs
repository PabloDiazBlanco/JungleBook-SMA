using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AgenteBase : MonoBehaviour
{
    public MemoriaAgente memoria;
    public List<Transform> puntosPatrulla;
    public NavMeshAgent nav;
    public EstadoAgente estadoActual;

    void Start()
    {
        // Asignación directa y arranque
        memoria = GetComponent<MemoriaAgente>();
        nav = GetComponent<NavMeshAgent>();
        CambiarEstado(new EstadoPatrulla());
    }

    void Update()
    {
        EjecutarEstado();
    }

    private void EjecutarEstado()
    {
        estadoActual.Execute(this);
    }

    public void CambiarEstado(EstadoAgente nuevoEstado)
    {
        if (estadoActual != null && estadoActual.GetType() == nuevoEstado.GetType()) return;

        if (estadoActual != null) estadoActual.Exit(this);
        estadoActual = nuevoEstado;
        estadoActual.Enter(this);
    }
}