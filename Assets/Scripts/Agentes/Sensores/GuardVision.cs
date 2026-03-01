using UnityEngine;

public class GuardVision : MonoBehaviour
{
    public float distanciaVision = 15f;
    public float anguloVision = 45f;
    public LayerMask capaObstaculos;
    
    public Transform objetivoLadron;

    void Awake()
    {
        GameObject ladron = GameObject.FindGameObjectWithTag("Thief");
        if (ladron != null) objetivoLadron = ladron.transform;
    }

    public bool PuedeVerAlLadron()
    {
        if (objetivoLadron == null) return false;

        Vector3 direccion = (objetivoLadron.position - transform.position).normalized;
        float distanciaActual = Vector3.Distance(transform.position, objetivoLadron.position);

        if (distanciaActual <= distanciaVision)
        {
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