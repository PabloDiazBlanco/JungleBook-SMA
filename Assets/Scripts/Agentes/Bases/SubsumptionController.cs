using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class SubsumptionController : MonoBehaviour
{
    public List<GuardBehavior> behaviors = new List<GuardBehavior>();

    public GuardVision sensorVision;
    public GuardHearing sensorOido;
    public SensorHogueraIndividual sensorHoguera;
    public SensorPercepcionObjetos sensorObjetos;

    private NavMeshAgent agent;
    private GuardBehavior capaAnterior;

    private Vector3? ultimaPosicionLadron = null;
    private float cronometroBusqueda = 0f;
    public float tiempoBusqueda = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (sensorVision == null) sensorVision = GetComponent<GuardVision>();
        if (sensorOido == null) sensorOido = GetComponent<GuardHearing>();
        if (sensorHoguera == null) sensorHoguera = GetComponent<SensorHogueraIndividual>();
        if (sensorObjetos == null) sensorObjetos = GetComponent<SensorPercepcionObjetos>();

        behaviors.AddRange(GetComponents<GuardBehavior>());
        behaviors.Sort(CompararPrioridades);
    }

    private int CompararPrioridades(GuardBehavior a, GuardBehavior b)
    {
        return a.priority.CompareTo(b.priority);
    }

    void Update()
    {
        bool veAlLadron = sensorVision != null && sensorVision.PuedeVerAlLadron();
        bool oyoAlgo = sensorOido != null && sensorOido.EscuchoAlgo();
        bool alarmaHoguera = sensorHoguera != null && sensorHoguera.alarmaRoboDetectada;

        Vector3? posicionRuido = oyoAlgo ? sensorOido.GetPosicionRuido() : (Vector3?)null;

        Vector3? posicionPuerta = (sensorObjetos != null && sensorObjetos.ultimaPuertaDetectada != null)
            ? sensorObjetos.ultimaPuertaDetectada.position
            : (Vector3?)null;

        if (veAlLadron)
        {
            ultimaPosicionLadron = sensorVision.UltimaPosicionDetectada();
            cronometroBusqueda = tiempoBusqueda;
        }
        else if (oyoAlgo)
        {
            ultimaPosicionLadron = posicionRuido;
            cronometroBusqueda = tiempoBusqueda;
        }
        else if (cronometroBusqueda > 0)
        {
            cronometroBusqueda -= Time.deltaTime;
            if (cronometroBusqueda <= 0)
                ultimaPosicionLadron = null;
        }

        foreach (GuardBehavior capa in behaviors)
        {
            capa.RecibirInformacion(
                veAlLadron,
                ultimaPosicionLadron,
                oyoAlgo,
                posicionRuido,
                alarmaHoguera,
                posicionPuerta,
                cronometroBusqueda
            );
        }

        EjecutarDecision();
    }

    public void EjecutarDecision()
    {
        foreach (GuardBehavior capa in behaviors)
        {
            if (capa.CanActivate())
            {
                if (capaAnterior != capa)
                {
                    if (agent != null) agent.ResetPath();
                    Debug.Log("Cambio de comportamiento a: " + capa.GetType().Name);
                    capaAnterior = capa;
                }

                capa.Action();
                return;
            }
        }
    }
}