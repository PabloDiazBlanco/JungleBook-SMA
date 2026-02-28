using UnityEngine;
using UnityEngine.AI;

public class AldeanoAnimationControllers : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Umbrales de Velocidad")]
    // Si va a 2 (Patrulla), es caminar.
    // Si va a 6 u 8 (Búsqueda/Persecución), es correr.
    public float umbralMovimiento = 0.2f;
    public float umbralCorrer = 4.0f; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent == null || anim == null) return;

        // Leemos la velocidad actual del NavMeshAgent
        float velocidadActual = agent.velocity.magnitude;

        // LOGICA DE ESTADOS:
        // 1. Idle: Velocidad casi cero.
        bool estaQuieto = velocidadActual < umbralMovimiento;
        
        // 2. Walking: Velocidad entre 0.2 y 4 (Cubre la Patrulla a 2.0)
        bool estaCaminando = velocidadActual >= umbralMovimiento && velocidadActual < umbralCorrer;
        
        // 3. Running: Velocidad mayor a 4 (Cubre Búsqueda a 6.0 y Persecución a 8.0)
        bool estaCorriendo = velocidadActual >= umbralCorrer;

        // Aplicamos al Animator según tu esquema
        anim.SetBool("isIdle", estaQuieto);
        anim.SetBool("isWalking", estaCaminando);
        anim.SetBool("isRunning", estaCorriendo);
    }

    // Actuador para el Puñetazo
    public void EjecutarAtaque()
    {
        // Según tu requisito, solo puede pegar desde el Sprint
        if (anim != null && anim.GetBool("isRunning"))
        {
            anim.SetTrigger("darPuñetazo");
            Debug.Log("ANIMACION: Ataque ejecutado (Condición de carrera cumplida).");
        }
        else
        {
            Debug.Log("ANIMACION: Intento de ataque fallido (El agente no está corriendo).");
        }
    }
}