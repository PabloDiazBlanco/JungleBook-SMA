using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Transform door;   // Ahora solo una puerta

    public float slideDistance = 2f;
    public float speed = 2f;

    private Vector3 closedPos;
    private Vector3 openPos;

    private bool isOpen = false;

    void Start()
    {
        closedPos = door.localPosition;

        // Se mueve lateralmente en su eje local
        openPos = closedPos + transform.forward * slideDistance;

    }

    void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;

        door.localPosition = Vector3.MoveTowards(
            door.localPosition,
            target,
            speed * Time.deltaTime
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isOpen = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isOpen = false;
        }
    }
}
