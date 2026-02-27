using UnityEngine;
using UnityEngine.InputSystem;

public class MainCharacter_Brain : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public float gravity = -20f;          // un poco más fuerte que -9.81 para que se note
    public float slideSpeed = 6f;         // velocidad al deslizar en pendiente

    private CharacterController controller;
    private float xRotation = 0f;
    private float yVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Look();
        MoveWithGravityAndSlide();
    }

    void MoveWithGravityAndSlide()
    {
        // WASD
        Vector3 input = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) input += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) input += Vector3.back;
        if (Keyboard.current.aKey.isPressed) input += Vector3.left;
        if (Keyboard.current.dKey.isPressed) input += Vector3.right;

        Vector3 move = (transform.TransformDirection(input.normalized) * moveSpeed);

        // Gravedad
        if (controller.isGrounded && yVelocity < 0f)
            yVelocity = -2f;

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;

        // Slide en pendientes (si está grounded)
        if (controller.isGrounded)
        {
            // Raycast hacia abajo para obtener la normal del "suelo"
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 1.5f))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);

                // Si es pendiente "notable", empuja hacia abajo por la pendiente
                if (angle > 5f)
                {
                    Vector3 slideDir = new Vector3(hit.normal.x, -hit.normal.y, hit.normal.z);
                    move += slideDir * slideSpeed;
                }
            }
        }

        controller.Move(move * Time.deltaTime);
    }

    void Look()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseDelta.x);

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (Camera.main != null)
            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}
