using UnityEngine;
using System.Collections.Generic;

public class BeliefBase
{
    public bool ladronVisto;
    public bool ladronTieneFuego;
    public bool alarmaHogueraActiva;
    
    public Vector3? posicionLadron;
    public Vector3? direccionLadron; 
    public float velocidadLadron;    
    public float timestampPosicionLadron;
    public float tiempoVidaCreenciaLadron = 30f;

    public enum RolCNP { Ninguno, Perseguidor, BuscadorSectores, Bloqueador, Vigilante }
    public RolCNP rolActual = RolCNP.Ninguno;
    public bool rolAsignadoExternamente = false;

    public List<Vector3> sectoresAsignados = new List<Vector3>();
    public List<Vector3> planBusqueda = new List<Vector3>();
    public int indiceSectorActual = 0;
    
    public HashSet<int> sectoresLimpios = new HashSet<int>(); 

    public bool TienePlanActivo => planBusqueda != null && indiceSectorActual < planBusqueda.Count;
    public Vector3? SectorActual => TienePlanActivo ? planBusqueda[indiceSectorActual] : (Vector3?)null;

    public void AvanzarSector() => indiceSectorActual++;

    public void CancelarPlan()
    {
        planBusqueda.Clear();
        sectoresAsignados.Clear();
        indiceSectorActual = 0;
        rolActual = RolCNP.Ninguno;
        rolAsignadoExternamente = false;
    }

    public string GetSectoresPendientesStr()
    {
        List<int> pendientes = new List<int>();
        for (int i = 0; i < 9; i++) 
        {
            if (!sectoresLimpios.Contains(i)) pendientes.Add(i);
        }
        return string.Join(",", pendientes);
    }

    public List<int> GetSectoresNoLimpiosIndices()
    {
        List<int> resultado = new List<int>();
        for (int i = 0; i < 9; i++)
        {
            if (!sectoresLimpios.Contains(i)) resultado.Add(i);
        }
        return resultado;
    }

    public void MarcarSectorComoLimpio(int id)
    {
        if (!sectoresLimpios.Contains(id))
        {
            sectoresLimpios.Add(id);
            Debug.Log($"[BELIEF]: Sector {id} marcado como LIMPIO. Sincronizando rotación...");
        }
    }

    public void AsignarSectorUnico(int id)
    {
        if (id >= SectorMap.Sectores.Count) return;
        rolActual = RolCNP.BuscadorSectores;
        sectoresAsignados.Clear();
        sectoresAsignados.Add(SectorMap.Sectores[id]);
    }
}