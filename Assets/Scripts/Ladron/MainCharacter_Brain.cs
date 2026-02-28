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
    public GameObject fuegoEnAntorcha;

    private CharacterController controller;
    private Animator anim;
    private float xRotation = 0f;
    private float yVelocity;

    private Vector2 moveInput;
    private Vector2 mouseDelta;
    private bool isGrounded;
    private bool isActionLocked; 
    private bool wantsToJump;
    private bool wantsToGather;
    
    private bool hayAntorchaCerca;
    private GameObject antorchaSuelo;
    private bool hayFuegoCerca;
    private GameObject hogueraDetectada;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        if (antorchaEnMano != null) antorchaEnMano.SetActive(false);
        if (fuegoEnAntorcha != null) fuegoEnAntorcha.SetActive(false);
    }

    void Update()
    {
        Percepcion();
        Next();
        Deliberar();
        Ejecutar();
    }

    void Percepcion()
    {
        float v = Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue();
        float h = Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue();
        moveInput = new Vector2(h, v);
        mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        wantsToJump = Keyboard.current.spaceKey.wasPressedThisFrame;
        wantsToGather = Keyboard.current.eKey.wasPressedThisFrame;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Antorcha"))
        {
            hayAntorchaCerca = true;
            antorchaSuelo = other.gameObject;
            Debug.Log("SENSOR: Detectada Antorcha.");
        }
        if (other.CompareTag("FuegoHoguera")) 
        {
            hayFuegoCerca = true;
            hogueraDetectada = other.gameObject;
            Debug.Log("SENSOR: Detectada Hoguera.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Antorcha")) { hayAntorchaCerca = false; antorchaSuelo = null; }
        if (other.CompareTag("FuegoHoguera")) { hayFuegoCerca = false; hogueraDetectada = null; }
    }

    void Next() { 
        isGrounded = controller.isGrounded; 
        if (anim != null) isActionLocked = anim.GetCurrentAnimatorStateInfo(0).IsName("Gathering"); 
    }

    void Deliberar() { 
        float multiplier = isActionLocked ? 0f : 1f; 
        moveInput *= multiplier; 
    }

    void Ejecutar()
    {
        transform.Rotate(Vector3.up * mouseDelta.x);
        xRotation = Mathf.Clamp(xRotation - mouseDelta.y, -80f, 80f);
        if (Camera.main != null) Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        Vector3 move = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y)) * moveSpeed;
        if (isGrounded && yVelocity < 0f) yVelocity = -2f;
        if (isGrounded && wantsToJump && !isActionLocked) {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            anim.SetTrigger("isJumping");
        }
        yVelocity += gravity * Time.deltaTime;
        move.y = yVelocity;
        controller.Move(move * Time.deltaTime);

        if (wantsToGather && isGrounded && !isActionLocked)
        {
            if (hayAntorchaCerca || hayFuegoCerca) anim.SetTrigger("isGathering");
        }

        ActualizarAnimaciones();
    }

    // --- EL ACTUADOR CON DEBUGS ---
    public void EjecutarRecogida()
    {
        Debug.Log("ACTUADOR: EjecutarRecogida disparado por el evento.");

        // Diagnóstico de Antorcha
        if (hayAntorchaCerca) {
            if (antorchaSuelo != null) {
                antorchaSuelo.SetActive(false);
                if (antorchaEnMano != null) antorchaEnMano.SetActive(true);
                Debug.Log("ACTUADOR: Antorcha recogida con éxito.");
            } else {
                Debug.LogError("ACTUADOR ERROR: hayAntorchaCerca es true pero antorchaSuelo es NULL.");
            }
        }

        // Diagnóstico de Hoguera
        if (hayFuegoCerca) {
            bool tieneAntorcha = antorchaEnMano != null && antorchaEnMano.activeSelf;
            Debug.Log("ACTUADOR: Cerca del fuego. ¿Tiene antorcha en mano?: " + tieneAntorcha);

            if (tieneAntorcha) {
                if (fuegoEnAntorcha != null) fuegoEnAntorcha.SetActive(true);
                if (hogueraDetectada != null) {
                    hogueraDetectada.SetActive(false);
                    Debug.Log("ACTUADOR: Hoguera apagada y antorcha encendida.");
                } else {
                    Debug.LogError("ACTUADOR ERROR: hayFuegoCerca es true pero hogueraDetectada es NULL.");
                }
            } else {
                Debug.LogWarning("ACTUADOR: El agente no puede encender el fuego porque no tiene la antorcha en la mano.");
            }
        }
    }

    void ActualizarAnimaciones() {
        if (anim == null) return;
        anim.SetBool("isMoving", moveInput.magnitude > 0.01f);
        anim.SetBool("isBack", moveInput.y < -0.1f);
        anim.SetBool("isRight", moveInput.x > 0.1f);
        anim.SetBool("isLeft", moveInput.x < -0.1f);
    }
}