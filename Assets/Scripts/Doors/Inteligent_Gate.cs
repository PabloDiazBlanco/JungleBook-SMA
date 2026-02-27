using UnityEngine;
using UnityEngine.AI; // Necesario para controlar el NavMeshObstacle

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

    // Referencias a los obstáculos
    private NavMeshObstacle obstacleLeft;
    private NavMeshObstacle obstacleRight;

    void Start()
    {
        leftClosedPos = doorLeft.localPosition;
        rightClosedPos = doorRight.localPosition;

        leftOpenPos = leftClosedPos - doorLeft.right * slideDistance;
        rightOpenPos = rightClosedPos + doorRight.right * slideDistance;

        // Obtenemos los componentes de las hojas
        obstacleLeft = doorLeft.GetComponent<NavMeshObstacle>();
        obstacleRight = doorRight.GetComponent<NavMeshObstacle>();
        
        // Configuramos el Carving por código por seguridad
        if(obstacleLeft) obstacleLeft.carving = true;
        if(obstacleRight) obstacleRight.carving = true;
    }

    void Update()
    {
        Vector3 targetLeft = isOpen ? leftOpenPos : leftClosedPos;
        Vector3 targetRight = isOpen ? rightOpenPos : rightClosedPos;

        doorLeft.localPosition = Vector3.MoveTowards(doorLeft.localPosition, targetLeft, speed * Time.deltaTime);
        doorRight.localPosition = Vector3.MoveTowards(doorRight.localPosition, targetRight, speed * Time.deltaTime);

        // LÓGICA CLAVE: Si la puerta está casi cerrada, activamos el obstáculo.
        // Si se está abriendo, lo quitamos para que el lobo pueda entrar tras de ti.
// Calcula la distancia actual de la hoja izquierda respecto a su posición de cierre
        float distanciaActualAlCierre = Vector3.Distance(doorLeft.localPosition, leftClosedPos);

        // Si la puerta NO está abierta (isOpen es false), activamos los obstáculos
        // En cuanto el Thief entra al trigger (isOpen es true), se desactivan para dejar pasar
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