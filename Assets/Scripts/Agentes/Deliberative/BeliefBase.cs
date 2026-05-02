using UnityEngine;
using System.Collections.Generic;

public class BeliefBase
{
    public bool ladronVisto;
    public bool ladronTieneFuego;
    public bool alarmaHogueraActiva;
    public Vector3? posicionLadron;
    public float timestampPosicionLadron;
    public float tiempoVidaCreenciaLadron = 10f;

    public List<Vector3> sectoresAsignados = new List<Vector3>();
    public List<Vector3> planBusqueda = new List<Vector3>();
    public int indiceSectorActual = 0;

    public bool TienePlanActivo => planBusqueda != null && indiceSectorActual < planBusqueda.Count;

    public Vector3? SectorActual => TienePlanActivo ? planBusqueda[indiceSectorActual] : (Vector3?)null;

    public void AvanzarSector() => indiceSectorActual++;

    public void CancelarPlan()
    {
        planBusqueda.Clear();
        sectoresAsignados.Clear();
        indiceSectorActual = 0;
    }
}

