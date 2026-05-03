using UnityEngine;
using System.Collections.Generic;

public class ModuleTactical : DeliberativeModule
{
    private SensorHogueraIndividual sensorHoguera;

    [Header("Configuración de Predicción")]
    public float tiempoProyeccionFutura = 2.0f;

    public override void Inicializar(BeliefBase b, AgentCommunicator comm, SubsumptionController ctrl)
    {
        base.Inicializar(b, comm, ctrl);
        sensorHoguera = controller.GetComponent<SensorHogueraIndividual>();
    }

    public override void Procesar()
    {
        if (creencias.TienePlanActivo)
        {
            Vector3 destinoActual = creencias.SectorActual.Value;
            float distanciaAlPunto = Vector3.Distance(controller.transform.position, destinoActual);

            if (distanciaAlPunto < 2.0f)
            {
                creencias.AvanzarSector();
                if (!creencias.TienePlanActivo)
                {
                    creencias.rolActual = BeliefBase.RolCNP.Ninguno;
                    Debug.Log($"[TACTICAL {communicator.nombreAgente}]: Plan de sectores completado. Rol liberado.");
                }
                else
                {
                    Debug.Log($"[TACTICAL {communicator.nombreAgente}]: Punto de sector registrado. Avanzando...");
                }
            }
            else
            {
                controller.InyectarPosicionLadron(destinoActual); 
            }
        }
    }

    public void PredecirPosicionLadron()
    {
        if (!creencias.posicionLadron.HasValue || creencias.velocidadLadron < 0.1f) return;

        Vector3 ultimaPos = creencias.posicionLadron.Value;
        Debug.Log($"[TACTICAL {communicator.nombreAgente}]: Iniciando predicción — ladrón en {ultimaPos:F1}");
        Vector3 direccion = creencias.direccionLadron.HasValue ? creencias.direccionLadron.Value : Vector3.zero;
        
        Vector3 posicionFutura = ultimaPos + (direccion * creencias.velocidadLadron * tiempoProyeccionFutura);

        creencias.posicionLadron = posicionFutura;
        
        Debug.DrawLine(ultimaPos, posicionFutura, Color.magenta, 1.0f);
    }

    // ===================== PLANIFICACIÓN A* DE SECTORES =====================

    public void GenerarPlanBusqueda()
    {
        List<Vector3> sectores = creencias.sectoresAsignados;
        if (sectores == null || sectores.Count == 0) return;

        Vector3 origen = controller.transform.position;
        List<Vector3> plan = AEstrellaSectores(origen, sectores);

        creencias.planBusqueda = plan;
        creencias.indiceSectorActual = 0;

        Debug.Log($"[TACTICAL {communicator.nombreAgente}]: Plan A* generado — {plan.Count} sectores.");
    }

    private List<Vector3> AEstrellaSectores(Vector3 inicio, List<Vector3> sectores)
    {
        int n = sectores.Count;
        // Cada nodo: (sectorActual=-1 es origen, visitados como bitmask, costeAcumulado, heuristica, padre)
        var abiertos = new SortedList<float, NodoSector>();
        var cerrados = new HashSet<string>();

        NodoSector raiz = new NodoSector(-1, 0, 0f, Heuristica(-1, 0, inicio, sectores), null, inicio);
        Insertar(abiertos, raiz);

        while (abiertos.Count > 0)
        {
            NodoSector actual = ExtraerMinimo(abiertos);
            string clave = actual.Clave();

            if (cerrados.Contains(clave)) continue;
            cerrados.Add(clave);

            if (actual.Visitados == (1 << n) - 1)
                return ReconstruirPlan(actual);

            for (int i = 0; i < n; i++)
            {
                if ((actual.Visitados & (1 << i)) != 0) continue;

                Vector3 posActual = actual.SectorIdx == -1 ? inicio : sectores[actual.SectorIdx];
                float coste = actual.Coste + Vector3.Distance(posActual, sectores[i]);
                int visitados = actual.Visitados | (1 << i);
                float h = Heuristica(i, visitados, sectores[i], sectores);

                NodoSector hijo = new NodoSector(i, visitados, coste, h, actual, sectores[i]);
                if (!cerrados.Contains(hijo.Clave()))
                    Insertar(abiertos, hijo);
            }
        }

        return new List<Vector3>(sectores);
    }

    private float Heuristica(int sectorActual, int visitados, Vector3 posActual, List<Vector3> sectores)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < sectores.Count; i++)
        {
            if ((visitados & (1 << i)) == 0)
                minDist = Mathf.Min(minDist, Vector3.Distance(posActual, sectores[i]));
        }
        return minDist == float.MaxValue ? 0f : minDist;
    }

    private List<Vector3> ReconstruirPlan(NodoSector nodo)
    {
        List<Vector3> plan = new List<Vector3>();
        NodoSector actual = nodo;
        while (actual != null && actual.SectorIdx != -1)
        {
            plan.Insert(0, actual.Posicion);
            actual = actual.Padre;
        }
        return plan;
    }

    private void Insertar(SortedList<float, NodoSector> lista, NodoSector nodo)
    {
        float prioridad = nodo.Coste + nodo.H;
        while (lista.ContainsKey(prioridad)) prioridad += 0.0001f;
        lista.Add(prioridad, nodo);
    }

    private NodoSector ExtraerMinimo(SortedList<float, NodoSector> lista)
    {
        NodoSector nodo = lista.Values[0];
        lista.RemoveAt(0);
        return nodo;
    }

    private class NodoSector
    {
        public int SectorIdx;
        public int Visitados;
        public float Coste;
        public float H;
        public NodoSector Padre;
        public Vector3 Posicion;

        public NodoSector(int idx, int visitados, float coste, float h, NodoSector padre, Vector3 pos)
        {
            SectorIdx = idx; Visitados = visitados; Coste = coste;
            H = h; Padre = padre; Posicion = pos;
        }

        public string Clave() => $"{SectorIdx}_{Visitados}";
    }


    public string GetPosicionLadron()
    {
        if (!creencias.posicionLadron.HasValue) return "0,0,0";
        Vector3 p = creencias.posicionLadron.Value;
        return $"{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)}";
    }

    private static readonly Vector3 PosicionSalida = new Vector3(40.06f, 0.7621841f, 10.15f);

    public string GetPosicionObjetivo(string tipo)
    {
        if (tipo == "hoguera" && sensorHoguera?.posicionHogueraConocida != null)
        {
            Vector3 p = sensorHoguera.posicionHogueraConocida.Value;
            return $"{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)}";
        }

        if (tipo == "salida")
        {
            Vector3 p = PosicionSalida;
            return $"{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)}";
        }

        return GetPosicionLadron();
    }

    public string GetComandoPersecucion()
    {
        return $"perseguir:{GetPosicionLadron()}";
    }


    public string CalcularPropuesta(string contenidoCFP)
    {
        string[] partes = contenidoCFP.Split('|');

        // Formato crisis: "bloqueo|puerta:{x},{y},{z}"
        if (partes.Length == 2 && partes[0] == "bloqueo")
        {
            Vector3? posPuerta = ParsearPosicion(partes[1].Substring("puerta:".Length));
            if (!posPuerta.HasValue) return "";
            int dp = Mathf.RoundToInt(Vector3.Distance(controller.transform.position, posPuerta.Value));
            return $"dp:{dp}";
        }

        // Formato normal: "{tipo}|ladron:{x},{y},{z}|obj:{x},{y},{z}"
        if (partes.Length != 3) return "";

        Vector3? posLadron = ParsearPosicion(partes[1].Substring("ladron:".Length));
        Vector3? posObj = ParsearPosicion(partes[2].Substring("obj:".Length));

        if (!posLadron.HasValue || !posObj.HasValue) return "";

        int dl = Mathf.RoundToInt(Vector3.Distance(controller.transform.position, posLadron.Value));
        int dobj = Mathf.RoundToInt(Vector3.Distance(controller.transform.position, posObj.Value));

        return $"dl:{dl},do:{dobj}";
    }

    public void EjecutarAccionConfirmada(string contenido)
    {
        if (contenido.StartsWith("perseguir:"))
        {
            Vector3? pos = ParsearPosicion(contenido.Substring("perseguir:".Length));
            if (pos.HasValue)
            {
                creencias.planBusqueda.Clear();
                creencias.indiceSectorActual = 0;
                creencias.rolActual = BeliefBase.RolCNP.Perseguidor;
                controller.InyectarPosicionLadron(pos.Value);
            }
        }
        else if (contenido == "cubrir_hoguera")
        {
            creencias.rolActual = BeliefBase.RolCNP.Bloqueador;
            controller.InyectarCubrirHoguera();
        }
        else if (contenido == "comprobar_hoguera")
        {
            creencias.rolActual = BeliefBase.RolCNP.Vigilante;
            controller.InyectarCubrirHoguera();
        }
        else if (contenido == "cubrir_salida")
        {
            creencias.rolActual = BeliefBase.RolCNP.Bloqueador;
            controller.InyectarAlarmaHoguera();
        }
        else if (contenido.StartsWith("explorar_sector:"))
        {
            string idsStr = contenido.Substring("explorar_sector:".Length);
            string[] ids = idsStr.Split(',');

            creencias.sectoresAsignados.Clear();
            foreach (string idStr in ids)
            {
                if (int.TryParse(idStr.Trim(), out int id) && id < SectorMap.Sectores.Count)
                    creencias.sectoresAsignados.Add(SectorMap.Sectores[id]);
            }

            if (creencias.sectoresAsignados.Count > 0)
            {
                creencias.rolActual = BeliefBase.RolCNP.BuscadorSectores;
                GenerarPlanBusqueda();
                Debug.Log($"[TACTICAL {communicator.nombreAgente}]: Sector(es) asignados y plan A* generado.");
            }
        }
    }


    private Vector3? ParsearPosicion(string coords)
    {
        string[] partes = coords.Split(',');
        if (partes.Length != 3) return null;

        if (int.TryParse(partes[0], out int x) &&
            int.TryParse(partes[1], out int y) &&
            int.TryParse(partes[2], out int z))
        {
            return new Vector3(x, y, z);
        }
        return null;
    }
}