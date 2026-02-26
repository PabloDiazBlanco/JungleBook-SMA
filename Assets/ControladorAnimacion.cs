using UnityEngine;
using UnityEngine.AI;

public class ControladorAnimacion : MonoBehaviour
{
    private NavMeshAgent agenteNav;
    private Animator animador;
    private string estadoActual;

    void Start()
    {
        agenteNav = GetComponent<NavMeshAgent>();
        animador = GetComponent<Animator>();
    }

    void Update()
    {
        float v = agenteNav.velocity.magnitude;

        // Decidimos qué animación toca según la velocidad
        if (v < 0.1f) {
            CambiarAnimacion("Idle");
        }
        else if (v > 0.1f && v < 3.5f) {
            CambiarAnimacion("Walk");
        }
        else if (v >= 3.5f) {
            CambiarAnimacion("Run");
        }
    }

    private void CambiarAnimacion(string nuevoEstado)
    {
        // Si ya estamos en esa animación, no hacemos nada
        if (estadoActual == nuevoEstado) return;

        // Forzamos el cambio con una transición suave de 0.2 segundos
        animador.CrossFade(nuevoEstado, 0.2f);
        estadoActual = nuevoEstado;
    }

    public void EjecutarGestoSentarse()
    {
        // Al sentarse, desactivamos el Update un momento o forzamos el estado
        estadoActual = "SitStart";
        animador.CrossFade("SitStart", 0.1f);
        // Podrías añadir un Invoke para pasar a SitIdle después
        Invoke("PasarASitIdle", 1.0f); 
    }

    void PasarASitIdle() {
        if (agenteNav.velocity.magnitude < 0.1f) {
            animador.CrossFade("SitIdle", 0.2f);
            estadoActual = "SitIdle";
        }
    }
}