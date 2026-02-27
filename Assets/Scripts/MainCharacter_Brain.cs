using UnityEngine;
using UnityEngine.InputSystem;

public class MainCharacter_Brain : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public float gravity = -20f;
    public float slideSpeed = 6f;
    public float jumpHeight = 2f; 

    private CharacterController controller;
    private Animator anim;               
    private float xRotation = 0f;
    private float yVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Si el modelo 3D con el Animator es un hijo del objeto principal, usa GetComponentInChildren
        anim = GetComponentInChildren<Animator>(); 
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Look();
        MoveWithGravityAndSlide();
        UpdateAnimations(); 
    }

    void MoveWithGravityAndSlide()
    {
        Vector3 input = Vector3.zero;

        // Lectura de WASD con New Input System
        if (Keyboard.current.wKey.isPressed) input += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) input += Vector3.back;
        if (Keyboard.current.aKey.isPressed) input += Vector3.left;
        if (Keyboard.current.dKey.isPressed) input += Vector3.right;

        Vector3 move = (transform.TransformDirection(input.normalized) * moveSpeed);

        // Lógica de Suelo y Gravedad
        if (controller.isGrounded)
        {
            if (yVelocity < 0f) yVelocity = -2f;

            // Salto (Espacio)
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                // Impulso físico
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                // Disparar Trigger en Animator
                if (anim != null) anim.SetTrigger("isJumping");
            }
        }

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;

        // Deslizamiento en pendientes
        if (controller.isGrounded)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 1.5f))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > 5f)
                {
                    Vector3 slideDir = new Vector3(hit.normal.x, -hit.normal.y, hit.normal.z);
                    move += slideDir * slideSpeed;
                }
            }
        }

        controller.Move(move * Time.deltaTime);
    }

    void UpdateAnimations()
    {
        if (anim == null) return;

        float h = 0f;
        float v = 0f;

        // Detectamos dirección para alimentar los parámetros del Animator
        if (Keyboard.current.wKey.isPressed) v += 1f;
        if (Keyboard.current.sKey.isPressed) v -= 1f;
        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;

        // 1. isMoving: True si pulsas cualquier tecla de movimiento
        bool moving = (h != 0 || v != 0);
        anim.SetBool("isMoving", moving);

        // 2. VerticalSpeed: 1 para W, -1 para S (Correr_Atras)
        anim.SetFloat("VerticalSpeed", v);

        // 3. Direction: -1 para A (StrafeLeft), 1 para D (StrafeRight)
        anim.SetFloat("Direction", h);
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