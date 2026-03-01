using UnityEngine;
using UnityEngine.AI; 

public class SlidingGate : MonoBehaviour
{
    public Transform doorLeft;
    public Transform doorRight;

    public float slideDistance = 2f;
    public float speed = 2f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool isOpen = false;

    private NavMeshObstacle obstacleLeft;
    private NavMeshObstacle obstacleRight;

    void Start()
    {
        leftClosedPos = doorLeft.localPosition;
        rightClosedPos = doorRight.localPosition;

        leftOpenPos = leftClosedPos - doorLeft.right * slideDistance;
        rightOpenPos = rightClosedPos + doorRight.right * slideDistance;

        obstacleLeft = doorLeft.GetComponent<NavMeshObstacle>();
        obstacleRight = doorRight.GetComponent<NavMeshObstacle>();
        
        if(obstacleLeft) obstacleLeft.carving = true;
        if(obstacleRight) obstacleRight.carving = true;
    }

    void Update()
    {
        MoverPuertas();
        ActualizarObstaculosNavMesh();
    }

    private void MoverPuertas()
    {
        Vector3 targetLeft = isOpen ? leftOpenPos : leftClosedPos;
        Vector3 targetRight = isOpen ? rightOpenPos : rightClosedPos;

        doorLeft.localPosition = Vector3.MoveTowards(doorLeft.localPosition, targetLeft, speed * Time.deltaTime);
        doorRight.localPosition = Vector3.MoveTowards(doorRight.localPosition, targetRight, speed * Time.deltaTime);
    }

    private void ActualizarObstaculosNavMesh()
    {
        if (obstacleLeft) obstacleLeft.enabled = !isOpen;
        if (obstacleRight) obstacleRight.enabled = !isOpen;
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