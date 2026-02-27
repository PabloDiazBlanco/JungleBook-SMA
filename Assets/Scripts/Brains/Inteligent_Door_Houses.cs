using UnityEngine;
using UnityEngine.AI;

public class SlidingDoor : MonoBehaviour
{
    public Transform door;   
    public float slideDistance = 2f;
    public float speed = 2f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private bool isOpen = false;

    private NavMeshObstacle obstacle;

    void Start()
    {
        if (door != null)
        {
            closedPos = door.localPosition;
            openPos = closedPos + transform.forward * slideDistance;
            obstacle = door.GetComponent<NavMeshObstacle>();
            
            if (obstacle != null)
            {
                obstacle.carving = true;
            }
        }
    }

    void Update()
    {
        if (door == null) return;

        Vector3 target = isOpen ? openPos : closedPos;
        door.localPosition = Vector3.MoveTowards(door.localPosition, target, speed * Time.deltaTime);

        if (obstacle != null)
        {
            // El obstáculo se desactiva cuando la puerta está abierta para dejar pasar al lobo
            obstacle.enabled = !isOpen;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief"))
        {
            isOpen = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Thief"))
        {
            isOpen = false;
        }
    }
}