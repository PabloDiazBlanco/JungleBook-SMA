using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Inteligent_Door_Houses : MonoBehaviour
{
    public Transform door;   
    public float slideDistance = 2f;
    public float speed = 2f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;
    private NavMeshObstacle obstacle;
    
    private List<Collider> agentesEnRango = new List<Collider>();

    void Start()
    {
        InicializarPuerta();
    }

    private void InicializarPuerta()
    {
        if (door != null)
        {
            closedPos = door.localPosition;
            openPos = closedPos + transform.forward * slideDistance;
            obstacle = door.GetComponent<NavMeshObstacle>();
            
            // Debug inicial para verificar componentes
            if (obstacle == null) Debug.LogWarning("PUERTA: No se encontró NavMeshObstacle en " + door.name);
        }
    }

    void Update()
    {
        ActualizarEstadoApertura();
        MoverPuerta();
        GestionarObstaculoNavMesh();
    }

    private void ActualizarEstadoApertura()
    {
        isOpen = agentesEnRango.Count > 0;
    }

    private void MoverPuerta()
    {
        if (door == null) return;
        Vector3 target = isOpen ? openPos : closedPos;
        door.localPosition = Vector3.MoveTowards(door.localPosition, target, speed * Time.deltaTime);
    }

    private void GestionarObstaculoNavMesh()
    {
        if (obstacle != null)
        {
            obstacle.enabled = !isOpen;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // DEBUG 1: ¿Algo está tocando el trigger?
        Debug.Log("COLISIÓN: Objeto '" + other.name + "' entró en el trigger de la puerta.");

        if (other.CompareTag("Thief") || other.CompareTag("Aldeano"))
        {
            // DEBUG 2: ¿El Tag es correcto?
            Debug.Log("PUERTA: Acceso concedido a " + other.tag);

            if (!agentesEnRango.Contains(other))
            {
                agentesEnRango.Add(other);
                Debug.Log("PUERTA: Agente añadido a la lista. Total: " + agentesEnRango.Count);
            }
        }
        else 
        {
            // DEBUG 3: El objeto no tiene el tag esperado
            Debug.Log("PUERTA: Objeto ignorado. Tag actual: " + other.tag);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Thief") || other.CompareTag("Aldeano"))
        {
            if (agentesEnRango.Contains(other))
            {
                agentesEnRango.Remove(other);
                Debug.Log("PUERTA: Agente salió. Quedan: " + agentesEnRango.Count);
            }
        }
    }
}