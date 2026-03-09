using UnityEngine;

public class BloquearSalida : GuardBehavior
{
    public Transform puntoPuertaPueblo;

    [Tooltip("Dirección hacia la que mirará el agente al llegar (debería apuntar al interior del pueblo, dando la espalda a la puerta).")]
    public Transform direccionVigilancia;

    public float distanciaLlegada = 1.5f;
    public float velocidadGiro = 5f;

    private bool haLlegado = false;

    public override bool CanActivate()
    {
        return alarmaHogueraActiva;
    }

    public override void Action()
    {
        if (agent == null || puntoPuertaPueblo == null) return;

        if (!haLlegado)
        {
            agent.speed = 10f;
            agent.SetDestination(puntoPuertaPueblo.position);

            float distancia = Vector3.Distance(transform.position, puntoPuertaPueblo.position);
            if (!agent.pathPending && distancia <= distanciaLlegada)
            {
                haLlegado = true;
                agent.ResetPath(); // Dejar de moverse
                Debug.Log("¡ALERTA! Posición de bloqueo alcanzada. Vigilando interior.");
            }
        }
        else
        {
            // Girar para dar la espalda a la puerta y mirar al interior
            if (direccionVigilancia != null)
            {
                Vector3 direccion = (direccionVigilancia.position - transform.position).normalized;
                direccion.y = 0f;
                if (direccion != Vector3.zero)
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);
                }
            }
        }

        Debug.Log("¡ALERTA! ¡Han robado la hoguera!");
    }
}