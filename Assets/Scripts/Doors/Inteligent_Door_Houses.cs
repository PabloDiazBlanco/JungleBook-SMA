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

    private List<Transform> agentesEnRango = new List<Transform>();

    // DEBUG: estado anterior para loguear solo cuando cambia
    private bool isOpenAnterior = false;

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

            if (obstacle == null) Debug.LogWarning("PUERTA: No se encontró NavMeshObstacle en " + door.name);
            else Debug.Log($"[PUERTA {gameObject.name}]: Inicializada. closedPos={closedPos} openPos={openPos}");
        }
    }

    void Update()
    {
        ActualizarEstadoApertura();
        MoverPuerta();
        GestionarObstaculoNavMesh();

        // DEBUG: loguear solo cuando cambia el estado abierto/cerrado
        if (isOpen != isOpenAnterior)
        {
            Debug.Log($"[PUERTA {gameObject.name}]: Estado → {(isOpen ? "ABIERTA" : "CERRADA")} | Agentes en rango: {agentesEnRango.Count}");
            isOpenAnterior = isOpen;
        }
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
        if (other.CompareTag("Thief") || other.CompareTag("Aldeano") || other.CompareTag("LadronConFuego"))
        {
            Transform raiz = other.transform.root;
            if (!agentesEnRango.Contains(raiz))
            {
                agentesEnRango.Add(raiz);
                Debug.Log($"[PUERTA {gameObject.name}]: OnTriggerEnter → {raiz.name} | posición={raiz.position} | Total en rango: {agentesEnRango.Count}");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Thief") || other.CompareTag("Aldeano") || other.CompareTag("LadronConFuego"))
        {
            Transform raiz = other.transform.root;
            if (agentesEnRango.Contains(raiz))
            {
                agentesEnRango.Remove(raiz);
                Debug.Log($"[PUERTA {gameObject.name}]: OnTriggerExit → {raiz.name} | Quedan en rango: {agentesEnRango.Count}");
            }
        }
    }
}
