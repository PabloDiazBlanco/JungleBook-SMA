using UnityEngine;
using UnityEngine.InputSystem;

public class MainCharacter_Brain : MonoBehaviour
{
    [Header("Ajustes del Agente")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public float gravity = -20f;
    public float jumpHeight = 2f;

    [Header("Referencias de Inventario")]
    public GameObject antorchaEnMano;

    private CharacterController controller;
    private Animator anim;
    private float xRotation = 0f;
    private float yVelocity;

    // --- BASE DE CREENCIAS (Estado Interno) ---
    private Vector2 moveInput;
    private Vector2 mouseDelta;
    private bool isGrounded;
    private bool isActionLocked; 
    private bool wantsToJump;
    private bool wantsToGather;
    
    private bool hayAntorchaCerca;
    private GameObject antorchaDetectada;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        
        // Estado inicial: mano vacía
        if (antorchaEnMano != null) antorchaEnMano.SetActive(false);
    }

    void Update()
    {
        Percepcion(); // Paso 1: Sensar entradas
        Next();       // Paso 2: Actualizar modelo interno
        Deliberar();  // Paso 3: Filtrar movimientos si hay bloqueo
        Ejecutar();   // Paso 4: Actuar
    }

    // CAPA DE PERCEPCIÓN: Recogida de datos sensoriales
    void Percepcion()
    {
        // 1. Inputs de movimiento y ratón
        float v = Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue();
        float h = Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue();
        moveInput = new Vector2(h, v);
        
        mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        
        wantsToJump = Keyboard.current.spaceKey.wasPressedThisFrame;
        wantsToGather = Keyboard.current.eKey.wasPressedThisFrame;
    }

    // --- SENSORES DE PROXIMIDAD (Trigger) ---
    // Actualizan la creencia simbólica "hayAntorchaCerca"
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Antorcha"))
        {
            hayAntorchaCerca = true;
            antorchaDetectada = other.gameObject;
            Debug.Log("SISTEMA: El agente percibe la antorcha al entrar en el área.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Antorcha"))
        {
            hayAntorchaCerca = false;
            antorchaDetectada = null;
            Debug.Log("SISTEMA: El agente pierde la percepción de la antorcha al salir.");
        }
    }

    void Next()
    {
        isGrounded = controller.isGrounded;
        if (anim != null)
        {
            // Verificamos compromiso con la acción actual
            isActionLocked = anim.GetCurrentAnimatorStateInfo(0).IsName("Gathering");
        }
    }

    void Deliberar()
    {
        // Si el agente está recogiendo, la intención de movimiento se anula
        float multiplier = isActionLocked ? 0f : 1f;
        moveInput *= multiplier;
    }

    void Ejecutar()
    {
        // 1. Rotación de cámara y cuerpo
        transform.Rotate(Vector3.up * mouseDelta.x);
        xRotation = Mathf.Clamp(xRotation - mouseDelta.y, -80f, 80f);
        if (Camera.main != null)
            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Movimiento físico
        Vector3 move = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y)) * moveSpeed;

        if (isGrounded)
        {
            if (yVelocity < 0f) yVelocity = -2f;
            if (wantsToJump && !isActionLocked)
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                anim.SetTrigger("isJumping");
            }
        }

        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;
        controller.Move(move * Time.deltaTime);

        // 3. Ejecución de Recogida (Sujeta a precondiciones de proximidad)
        if (wantsToGather)
        {
            Debug.Log("ACCIÓN: Intento de recogida. ¿Antorcha cerca?: " + hayAntorchaCerca);
            
            if (isGrounded && hayAntorchaCerca && !isActionLocked)
            {
                anim.SetTrigger("isGathering");
            }
        }

        ActualizarAnimaciones();
    }

    // ACTUADOR DE STRIPS: Se llama mediante el Animation Event en el frame elegido
    public void EjecutarRecogida()
    {
        if (antorchaDetectada != null)
        {
            // Lista de supresión (Mundo)
            antorchaDetectada.SetActive(false);
            
            // Lista de adición (Agente)
            if (antorchaEnMano != null) antorchaEnMano.SetActive(true);
            
            Debug.Log("STRIPS: Objeto suprimido del suelo y añadido a la mano.");
        }
    }

    void ActualizarAnimaciones()
    {
        if (anim == null) return;
        bool mov = moveInput.magnitude > 0.01f;
        anim.SetBool("isMoving", mov);
        anim.SetBool("isBack", moveInput.y < -0.1f);
        anim.SetBool("isRight", moveInput.x > 0.1f);
        anim.SetBool("isLeft", moveInput.x < -0.1f);
    }
}