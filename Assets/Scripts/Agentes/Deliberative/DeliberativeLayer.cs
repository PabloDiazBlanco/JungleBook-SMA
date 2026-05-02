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

        // Solo inicia CNP si no tiene rol asignado y no hay ya uno en curso
        if (controller.AcabaDeVerAlLadron && !social.IsCnpIniciado() && creencias.rolActual == BeliefBase.RolCNP.Ninguno)
        {
            social.IniciarCNP();
        }

        // Solo predice en el frame exacto en que se pierde al ladrón (no acumula error cada frame)
        if (controller.blackboard.acabaDePerderAlLadron && creencias.posicionLadron.HasValue)
        {
            tactical.PredecirPosicionLadron();
        }

        social.Procesar();
        tactical.Procesar();
    }

    private void ActualizarCreencias()
    {
        creencias.ladronVisto = controller.VeAlLadron;
        creencias.ladronTieneFuego = controller.LadronTieneFuego;
        creencias.alarmaHogueraActiva = controller.AlarmaHogueraActiva;

        if (controller.VeAlLadron && controller.blackboard != null)
        {
            creencias.posicionLadron = controller.UltimaPosicionLadron;
            creencias.direccionLadron = controller.blackboard.direccionLadron; 
            creencias.velocidadLadron = controller.blackboard.velocidadLadron;
            creencias.timestampPosicionLadron = Time.time;
        }
        else if (creencias.posicionLadron.HasValue)
        {
            float tiempoDesdeUltimoAvistamiento = Time.time - creencias.timestampPosicionLadron;

            if (tiempoDesdeUltimoAvistamiento > creencias.tiempoVidaCreenciaLadron)
            {
                creencias.posicionLadron = null;
                creencias.direccionLadron = Vector3.zero;
                creencias.velocidadLadron = 0f;
                Debug.Log($"<color=gray>[DELIBERATIVA {communicator.nombreAgente}]: Creencia caducada. Objetivo perdido.</color>");
            }
        }
    }
}