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
    private bool alarmaAnterior = false;

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

        // Solo BuscadorSectores abandona su misión al ver al ladrón; Perseguidor y Bloqueador la mantienen
        if (controller.AcabaDeVerAlLadron && creencias.rolAsignadoExternamente
            && creencias.rolActual == BeliefBase.RolCNP.BuscadorSectores)
        {
            creencias.CancelarPlan();
            Debug.Log($"[DELIBERATIVA {communicator.nombreAgente}]: BuscadorSectores libera rol al ver al ladrón — relanzando CNP.");
        }

        // Al activarse la alarma de hoguera (transición), liberar roles para participar en el CNP de salida
        bool alarmaActiva = controller.AlarmaHogueraActiva;
        if (alarmaActiva && !alarmaAnterior && creencias.rolActual != BeliefBase.RolCNP.Ninguno)
        {
            creencias.CancelarPlan();
            Debug.Log($"[DELIBERATIVA {communicator.nombreAgente}]: Rol liberado por alarma de hoguera — disponible para CNP salida.");
        }
        alarmaAnterior = alarmaActiva;

        // Solo inicia CNP si no tiene rol asignado, no hay uno en curso y el cooldown expiró
        if (controller.AcabaDeVerAlLadron && !social.IsCnpIniciado()
            && creencias.rolActual == BeliefBase.RolCNP.Ninguno && social.CooldownExpirado())
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
                if (creencias.rolActual == BeliefBase.RolCNP.BuscadorSectores ||
                    (creencias.rolActual == BeliefBase.RolCNP.Perseguidor && !creencias.rolAsignadoExternamente))
                {
                    creencias.CancelarPlan();
                    Debug.Log($"<color=gray>[DELIBERATIVA {communicator.nombreAgente}]: Plan cancelado por creencia caducada.</color>");
                }
                Debug.Log($"<color=gray>[DELIBERATIVA {communicator.nombreAgente}]: Creencia caducada. Objetivo perdido.</color>");
            }
        }
    }
}