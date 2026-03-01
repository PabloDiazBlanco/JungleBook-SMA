using UnityEngine;
using UnityEngine.AI;

public class LoboAnimationControllers : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Ajustes de Velocidad")]
    public float umbralCaminar = 0.1f;
    public float umbralCorrer = 4.5f; 

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
        ActualizarAnimacionesMovimiento();
    }

    private void ActualizarAnimacionesMovimiento()
    {
        float velocidadActual = agent.velocity.magnitude;

        bool moviendoseLento = velocidadActual > umbralCaminar && velocidadActual < umbralCorrer;
        bool moviendoseRapido = velocidadActual >= umbralCorrer;

        anim.SetBool("isWalking", moviendoseLento);
        anim.SetBool("isRunning", moviendoseRapido);
    }

    public void EjecutarAtaque()
    {
        if (anim != null && !anim.GetBool("isAttacking")) 
        {
            anim.SetBool("isAttacking", true);
            Invoke("DetenerAtaque", 0.75f);
        }
    }

    private void DetenerAtaque()
    {
        if (anim != null) anim.SetBool("isAttacking", false);
    }
}