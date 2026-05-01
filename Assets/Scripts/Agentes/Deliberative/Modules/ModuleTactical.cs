using UnityEngine;

public class ModuleTactical : DeliberativeModule
{
    private SensorHogueraIndividual sensorHoguera;

    public override void Inicializar(BeliefBase b, AgentCommunicator comm, SubsumptionController ctrl)
    {
        base.Inicializar(b, comm, ctrl);
        
        sensorHoguera = controller.GetComponent<SensorHogueraIndividual>();
    }

    public override void Procesar()
    {
    }


    public string GetPosicionLadron()
    {
        if (!creencias.posicionLadron.HasValue) return "0,0,0";
        Vector3 p = creencias.posicionLadron.Value;
        return $"{Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)}";
    }

    public string GetPosicionObjetivo(string tipo)
    {
        if (tipo == "hoguera" && sensorHoguera?.posicionHogueraConocida != null)
        {
            Vector3 p = sensorHoguera.posicionHogueraConocida.Value;
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
            if (pos.HasValue) controller.InyectarPosicionLadron(pos.Value);
        }
        else if (contenido == "cubrir_hoguera")
        {
            controller.InyectarCubrirHoguera();
        }
        else if (contenido == "cubrir_salida")
        {
            controller.InyectarAlarmaHoguera();
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