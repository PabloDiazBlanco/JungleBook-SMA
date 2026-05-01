using UnityEngine;
using System.Collections.Generic;

public class DeliberativeLayer : MonoBehaviour
{
    public BeliefBase creencias = new BeliefBase();
    private AgentCommunicator communicator;
    private SubsumptionController controller;

    private ModuleSocial social;
    private ModuleCrisis crisis;
    private ModuleTactical tactical;

    private List<DeliberativeModule> modulos = new List<DeliberativeModule>();

    void Awake()
    {
        communicator = GetComponent<AgentCommunicator>();
        controller = GetComponent<SubsumptionController>();

        crisis = gameObject.AddComponent<ModuleCrisis>();
        social = gameObject.AddComponent<ModuleSocial>();
        tactical = gameObject.AddComponent<ModuleTactical>();

        crisis.Inicializar(creencias, communicator, controller);
        social.Inicializar(creencias, communicator, controller);
        tactical.Inicializar(creencias, communicator, controller);

        modulos.Add(crisis);
        modulos.Add(social);
        modulos.Add(tactical);
    }

    public void Procesar()
    {
        if (communicator == null) return;

        ActualizarCreencias();

        crisis.Procesar();

        if (controller.AcabaDeVerAlLadron && !social.IsCnpIniciado())
        {
            social.IniciarCNP();
        }

        social.Procesar();
        tactical.Procesar();
    }

    private void ActualizarCreencias()
    {
        creencias.ladronVisto = controller.VeAlLadron;
        creencias.ladronTieneFuego = controller.LadronTieneFuego;
        creencias.alarmaHogueraActiva = controller.AlarmaHogueraActiva;

        if (controller.VeAlLadron && controller.UltimaPosicionLadron.HasValue)
        {
            creencias.posicionLadron = controller.UltimaPosicionLadron;
            creencias.timestampPosicionLadron = Time.time;
        }
        else if (creencias.posicionLadron.HasValue)
        {
            float tiempoDesdeUltimoAvistamiento = Time.time - creencias.timestampPosicionLadron;

            if (tiempoDesdeUltimoAvistamiento > creencias.tiempoVidaCreenciaLadron)
            {
                creencias.posicionLadron = null;
                Debug.Log($"<color=gray>[DELIBERATIVA {communicator.nombreAgente}]: Creencia de posición caducada. Olvidando al ladrón.</color>");
            }
        }
    }
}