using UnityEngine;

public class InvestigarEntorno : GuardBehavior
{
    private bool yaLogueado = false;
    private SubsumptionController controller;

    // DEBUG: log de estado cada N segundos para no saturar consola
    private float timerDebug = 0f;
    private const float INTERVALO_DEBUG = 0.5f;

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

        if (!yaLogueado)
        {
            Debug.Log($"[PUERTA {gameObject.name}]: Iniciando investigación → destino {posicionPuerta.Value}");
            yaLogueado = true;
        }

        // DEBUG periódico de estado de navegación
        timerDebug -= Time.deltaTime;
        if (timerDebug <= 0f)
        {
            timerDebug = INTERVALO_DEBUG;
            Debug.Log($"[PUERTA {gameObject.name}]: remainingDistance={agent.remainingDistance:F2} " +
                      $"pathPending={agent.pathPending} " +
                      $"pathStatus={agent.pathStatus} " +
                      $"velocity={agent.velocity.magnitude:F2} " +
                      $"destino={posicionPuerta.Value}");
        }

        // Si llegamos a la puerta, notificar para que no volvamos a entrar en bucle
        if (!agent.pathPending && agent.remainingDistance < 1.5f)
        {
            Debug.Log($"[PUERTA {gameObject.name}]: LLEGUÉ a la puerta (remainingDistance={agent.remainingDistance:F2}). Activando cooldown.");
            yaLogueado = false;
            controller?.NotificarInvestigacionPuertaCompletada();
        }
        else if (!agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial)
        {
            Debug.LogWarning($"[PUERTA {gameObject.name}]: Path PARCIAL — no puedo llegar a la puerta. remainingDistance={agent.remainingDistance:F2}. Posiblemente bloqueado por obstáculo.");
        }
        else if (!agent.pathPending && agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            Debug.LogWarning($"[PUERTA {gameObject.name}]: Path INVÁLIDO — destino inalcanzable. Activando cooldown por seguridad.");
            yaLogueado = false;
            controller?.NotificarInvestigacionPuertaCompletada();
        }
    }
}
