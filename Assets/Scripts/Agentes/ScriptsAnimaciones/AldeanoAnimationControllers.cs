using UnityEngine;
using UnityEngine.AI;

public class AldeanoAnimationControllers : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Umbrales de Velocidad")]
    public float umbralMovimiento = 0.2f;
    public float umbralCorrer = 4.0f; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (agent == null || anim == null)
        {
            this.enabled = false;
        }
    }

    void Update()
    {
        GestionarLogicaAnimaciones();
    }

    private void GestionarLogicaAnimaciones()
    {
        float velocidadActual = agent.velocity.magnitude;
        
        bool estaQuieto = velocidadActual < umbralMovimiento;
        bool estaCaminando = velocidadActual >= umbralMovimiento && velocidadActual < umbralCorrer;
        bool estaCorriendo = velocidadActual >= umbralCorrer;

        anim.SetBool("isIdle", estaQuieto);
        anim.SetBool("isWalking", estaCaminando);
        anim.SetBool("isRunning", estaCorriendo);
    }

    public void EjecutarAtaque()
    {
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