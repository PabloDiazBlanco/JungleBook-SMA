using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    private bool yaLogueado = false;
    private SubsumptionController controller;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<SubsumptionController>();
    }

    public override bool CanActivate()
    {
        return cronometroBusqueda > 0 && posicionPuerta != null;
    }

    public override void Action()
    {
        if (agent == null || posicionPuerta == null) return;

        agent.speed = 4.0f;
        agent.SetDestination(posicionPuerta.Value);

        if (!agent.pathPending && agent.remainingDistance < 1.5f)
        {
            yaLogueado = false;
            controller?.NotificarInvestigacionPuertaCompletada();
        }
        else if (!agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            if (!yaLogueado)
            {
                Debug.LogWarning($"[PUERTA {gameObject.name}]: Path PARCIAL — destino inalcanzable.");
                yaLogueado = true;
            }
        }
        else if (!agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[PUERTA {gameObject.name}]: Path INVÁLIDO — activando cooldown.");
            yaLogueado = false;
            controller?.NotificarInvestigacionPuertaCompletada();
        }
    }
}
