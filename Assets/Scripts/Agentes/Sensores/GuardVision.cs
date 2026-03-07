using UnityEngine;

public class GuardVision : MonoBehaviour
{
    public float distanciaVision = 15f;
    public float anguloVision = 45f;
    public LayerMask capaObstaculos;
    public LayerMask capaLadron; 

    public bool PuedeVerAlLadron()
    {
        Collider[] objetivosEnRango = Physics.OverlapSphere(
            transform.position, 
            distanciaVision, 
            capaLadron
        );

        foreach (Collider objetivo in objetivosEnRango)
        {
            Vector3 direccion = (objetivo.transform.position - transform.position).normalized;
            float distanciaActual = Vector3.Distance(transform.position, objetivo.transform.position);

            if (Vector3.Angle(transform.forward, direccion) < anguloVision)
            {
                if (!Physics.Raycast(transform.position, direccion, distanciaActual, capaObstaculos))
                {
                    return true;
                }
            }
        }

        return false;
    }
}