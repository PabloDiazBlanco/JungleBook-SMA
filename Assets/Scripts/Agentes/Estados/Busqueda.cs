using UnityEngine;
using UnityEngine.AI;

public class Busqueda : GuardBehavior
{
    public GuardVision sensor;
    public Vector3 ultimaPosicionConocida;
    public bool tieneRastro = false;
    public float tiempoBusqueda = 10f; 
    public float cronometro = 0f;

    public float radioInspeccion = 5f; 
    private float tiempoProximoPunto = 0f;

    [Header("Ajustes de Inteligencia")]
    // Si es 0, el comportamiento es el de siempre (ideal para el Lobo)
    // Si es > 0, el agente "imagina" que avanzaste esa distancia al perderte de vista
    public float impulsoInercia = 0f; 

    void Start()
    {
        if (sensor == null) sensor = GetComponent<GuardVision>();
    }

    void Update()
    {
        ActualizarEstadoRastro();
    }

    private void ActualizarEstadoRastro()
    {
        if (sensor != null && sensor.PuedeVerAlLadron())
        {
            ultimaPosicionConocida = thief.position;
            tieneRastro = true;
            cronometro = tiempoBusqueda;
        }
        else if (tieneRastro && cronometro > 0)
        {
            // Detectamos el MOMENTO JUSTO en el que perdemos la visión
            // Si el cronómetro está al máximo, es que acabamos de perder el contacto
            if (cronometro == tiempoBusqueda && impulsoInercia > 0)
            {
                AplicarInerciaAlRastro();
            }

            cronometro -= Time.deltaTime;
        }
    }

    private void AplicarInerciaAlRastro()
    {
        // Calculamos la dirección en la que se movía el ladrón respecto al guardia
        Vector3 direccionHuida = (thief.position - transform.position).normalized;
        // Proyectamos la posición un poco más adelante para que el guardia "entre"
        ultimaPosicionConocida += direccionHuida * impulsoInercia;
        
        Debug.Log(gameObject.name + ": He perdido de vista al ladrón, supongo que sigue hacia adelante.");
    }

    public override bool CanActivate()
    {
        if (sensor != null && !sensor.PuedeVerAlLadron() && tieneRastro && cronometro > 0)
        {
            return true;
        }

        if (tieneRastro && cronometro <= 0)
        {
            tieneRastro = false;
        }

        return false;
    }

    public override void Action()
    {
        if (agent == null) return;

        agent.speed = 6.0f;

        if (!agent.pathPending && agent.remainingDistance < 0.5f && Time.time >= tiempoProximoPunto)
        {
            CalcularSiguientePuntoInspeccion();
        }

        Debug.DrawLine(transform.position, agent.destination, Color.red);
    }

    private void CalcularSiguientePuntoInspeccion()
    {
        Vector2 circuloAleatorio = Random.insideUnitCircle * radioInspeccion;
        Vector3 destinoAleatorio = ultimaPosicionConocida + new Vector3(circuloAleatorio.x, 0, circuloAleatorio.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destinoAleatorio, out hit, radioInspeccion, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            tiempoProximoPunto = Time.time + 1.5f; 
        }
    }
}