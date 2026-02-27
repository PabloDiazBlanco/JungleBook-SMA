using UnityEngine;
using UnityEngine.AI;

public class LoboAnimationControllers : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Ajustes de Velocidad")]
    public float umbralCaminar = 0.1f;
    public float umbralCorrer = 4.5f; // Por encima de 4.5 (Búsqueda y Persecución) corre

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent == null || anim == null) return;

        float velocidadActual = agent.velocity.magnitude;

        // Lógica de animaciones basada en la velocidad real
        bool moviendoseLento = velocidadActual > umbralCaminar && velocidadActual < umbralCorrer;
        bool moviendoseRapido = velocidadActual >= umbralCorrer;

        anim.SetBool("isWalking", moviendoseLento);
        anim.SetBool("isRunning", moviendoseRapido);
    }

    public void EjecutarAtaque()
    {
        if (anim != null && !anim.GetBool("isAttacking")) // Solo si no está ya atacando
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