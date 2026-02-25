using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AgenteBase : MonoBehaviour
{
    public MemoriaAgente memoria;
    public List<Transform> puntosPatrulla; // Arrastra aquí objetos vacíos desde el Inspector
    private int indicePunto = 0;
    
    private EstadoAgente estadoActual;

    void Start()
    {
        memoria = GetComponent<MemoriaAgente>();
        // IMPORTANTE: El primer estado
        CambiarEstado(new EstadoPatrulla());
    }

    void Update()
    {
        if (estadoActual != null)
            estadoActual.Execute(this);
    }

    public void CambiarEstado(EstadoAgente nuevoEstado)
    {
        if (estadoActual != null) estadoActual.Exit(this);
        estadoActual = nuevoEstado;
        estadoActual.Enter(this);
    }

    public Vector3 ObtenerProximoPuntoPatrulla()
    {
        if (puntosPatrulla == null || puntosPatrulla.Count == 0) return transform.position;
        
        Vector3 destino = puntosPatrulla[indicePunto].position;
        indicePunto = (indicePunto + 1) % puntosPatrulla.Count;
        return destino;
    }
}