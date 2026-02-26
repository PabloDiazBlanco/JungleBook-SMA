using UnityEngine;

public class SensorVision : MonoBehaviour
{
    public float rangoVision = 10f;
    public float anguloVision = 45f;
    
    public LayerMask capaObjetivo; 
    public LayerMask capaObstaculos; 

    private MemoriaAgente memoria;

    void Start()
    {
        memoria = GetComponent<MemoriaAgente>();
    }

    void Update()
    {
        // El sensor solo se encarga de la Transducción:
        // Pasa de datos físicos a símbolos en la memoria.
        ActualizarPercepcion();
    }

    void ActualizarPercepcion()
    {
        Collider[] objetivosCercanos = Physics.OverlapSphere(transform.position, rangoVision, capaObjetivo);

        bool encontrado = false;
        Vector3 posicionDetectada = Vector3.zero;

        // Cambiado 'var' por el tipo explícito 'Collider'
        foreach (Collider colisionador in objetivosCercanos)
        {
            Transform objetivo = colisionador.transform;
            Vector3 direccionAlObjetivo = (objetivo.position - transform.position).normalized;

            // 1. Comprobación de ángulo
            if (Vector3.Angle(transform.forward, direccionAlObjetivo) < anguloVision / 2f)
            {
                float distancia = Vector3.Distance(transform.position, objetivo.position);

                Vector3 origenRayo = transform.position + Vector3.up * 0.2f;
                if (!Physics.Raycast(origenRayo, direccionAlObjetivo, distancia, capaObstaculos))
                {
                    encontrado = true;
                    posicionDetectada = objetivo.position;
                    break; // Salimos del bucle al encontrar al primer objetivo válido
                }
            }
        }

        // Informamos a la memoria (Creencias)
        memoria.ActualizarCreenciaVision(encontrado, posicionDetectada);
    }

    private void OnDrawGizmos()
    {
        // Dibujamos el área de visión para facilitar el ajuste en el editor de Unity
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoVision);
        
        Vector3 lineaIzquierda = Quaternion.Euler(0, -anguloVision / 2, 0) * transform.forward;
        Vector3 lineaDerecha = Quaternion.Euler(0, anguloVision / 2, 0) * transform.forward;
        
        Gizmos.DrawRay(transform.position, lineaIzquierda * rangoVision);
        Gizmos.DrawRay(transform.position, lineaDerecha * rangoVision);
    }
}