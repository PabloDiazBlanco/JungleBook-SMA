using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainCharacter_Brain : MonoBehaviour
{
    [Header("Ajustes del Agente")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f;
    public float gravity = -20f;
    public float jumpHeight = 1f;

    [Header("Ajustes de Sonido")]
    public float radioRuidoActual = 0f;
    public float radioCorriendo = 10f;
    public float radioSaltando = 15f;

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
    private bool wantsToDeposit;

    private bool hayAntorchaCerca;
    private GameObject antorchaSuelo;
    private bool hayFuegoCerca;
    private GameObject hogueraDetectada;
    private string tagOriginal;

    // --- Hoguera destino ---
    private bool hayHogueraDestinoCerca;
    private GameObject hogueraDestinoDetectada;

    // --- Flag para distinguir recogida de depósito en el evento de animación ---
    private bool depositoPendiente = false;

    // --- Victoria ---
    private bool juegoTerminado = false;
    private bool mostrarMenuVictoria = false;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        if (antorchaEnMano != null) antorchaEnMano.SetActive(false);
        if (fuegoEnAntorcha != null) fuegoEnAntorcha.SetActive(false);

        tagOriginal = gameObject.tag;
    }

    void Update()
    {
        if (juegoTerminado) return;

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
        wantsToDeposit = Keyboard.current.fKey.wasPressedThisFrame;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Antorcha"))
        {
            hayAntorchaCerca = true;
            antorchaSuelo = other.gameObject;
            Debug.Log("SENSOR: Antorcha detectada.");
        }
        if (other.CompareTag("FuegoHoguera"))
        {
            hayFuegoCerca = true;
            hogueraDetectada = other.gameObject;
            Debug.Log("SENSOR: Hoguera detectada.");
        }
        if (other.CompareTag("HogueraDestino"))
        {
            hayHogueraDestinoCerca = true;
            hogueraDestinoDetectada = other.gameObject;
            Debug.Log("SENSOR: Hoguera destino detectada.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Antorcha")) { hayAntorchaCerca = false; antorchaSuelo = null; }
        if (other.CompareTag("FuegoHoguera")) { hayFuegoCerca = false; hogueraDetectada = null; }
        if (other.CompareTag("HogueraDestino")) { hayHogueraDestinoCerca = false; hogueraDestinoDetectada = null; }
    }

    void Next()
    {
        isGrounded = controller.isGrounded;
        if (anim != null) isActionLocked = anim.GetCurrentAnimatorStateInfo(0).IsName("Gathering");
    }

    void Deliberar()
    {
        float multiplier = isActionLocked ? 0f : 1f;
        moveInput *= multiplier;

        if (!isGrounded)
            radioRuidoActual = radioSaltando;
        else if (moveInput.magnitude > 0.01f)
            radioRuidoActual = radioCorriendo;
        else
            radioRuidoActual = 0f;
    }

    void Ejecutar()
    {
        transform.Rotate(Vector3.up * mouseDelta.x);
        xRotation = Mathf.Clamp(xRotation - mouseDelta.y, -80f, 80f);
        if (Camera.main != null) Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        Vector3 move = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y)) * moveSpeed;
        if (isGrounded && yVelocity < 0f) yVelocity = -2f;
        if (isGrounded && wantsToJump && !isActionLocked)
        {
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

        // Depositar fuego con tecla F
        if (wantsToDeposit && isGrounded && !isActionLocked && hayHogueraDestinoCerca)
        {
            bool tieneFuego = fuegoEnAntorcha != null && fuegoEnAntorcha.activeSelf;
            if (tieneFuego)
            {
                // Misma animación de agacharse que al recoger
                if (anim != null) anim.SetTrigger("isGathering");
                // Marcamos que el próximo evento de animación debe depositar, no recoger
                depositoPendiente = true;
            }
            else
            {
                Debug.LogWarning("ACTUADOR: No tienes fuego en la antorcha para depositar.");
            }
        }

        ActualizarAnimaciones();
    }

    // Llamado por el evento de animación Gathering
    public void EjecutarRecogida()
    {
        Debug.Log("ACTUADOR: EjecutarRecogida disparado por el evento.");

        // Si hay depósito pendiente, ejecutamos eso en lugar de recoger
        if (depositoPendiente)
        {
            depositoPendiente = false;
            EjecutarDeposito();
            return;
        }

        if (hayAntorchaCerca)
        {
            if (antorchaSuelo != null)
            {
                antorchaSuelo.SetActive(false);
                if (antorchaEnMano != null) antorchaEnMano.SetActive(true);
                Debug.Log("ACTUADOR: Antorcha recogida con éxito.");
            }
            else
            {
                Debug.LogError("ACTUADOR ERROR: hayAntorchaCerca es true pero antorchaSuelo es NULL.");
            }
        }

        if (hayFuegoCerca)
        {
            bool tieneAntorcha = antorchaEnMano != null && antorchaEnMano.activeSelf;
            Debug.Log("ACTUADOR: Cerca del fuego. ¿Tiene antorcha en mano?: " + tieneAntorcha);

            if (tieneAntorcha)
            {
                if (fuegoEnAntorcha != null) fuegoEnAntorcha.SetActive(true);

                GameObject hogueraAApagar = hogueraDetectada;
                hayFuegoCerca = false;
                hogueraDetectada = null;

                if (hogueraAApagar != null)
                {
                    hogueraAApagar.SetActive(false);
                    Debug.Log("ACTUADOR: Hoguera apagada y antorcha encendida.");
                }

                gameObject.tag = "LadronConFuego";
                Debug.Log("<color=orange>ACTUADOR: Tag del ladrón cambiado a 'LadronConFuego'.</color>");
            }
            else
            {
                Debug.LogWarning("ACTUADOR: No puede encender el fuego sin antorcha en mano.");
            }
        }
    }

    private void EjecutarDeposito()
    {
        // Apagar fuego de la antorcha y restaurar tag
        fuegoEnAntorcha.SetActive(false);
        gameObject.tag = tagOriginal;

        // Encender el VFX de la hoguera destino
        Transform raiz = hogueraDestinoDetectada.transform.parent;
        if (raiz != null)
        {
            ParticleSystem[] particulas = raiz.GetComponentsInChildren<ParticleSystem>(true);
            if (particulas.Length > 0)
            {
                particulas[0].gameObject.SetActive(true);
                Debug.Log("<color=green>ACTUADOR: Fuego depositado. ¡Victoria en 2s!</color>");
            }
        }

        // Bloquear input y mostrar victoria tras 2 segundos
        juegoTerminado = true;
        Invoke("MostrarVictoria", 2f);
    }

    private void MostrarVictoria()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mostrarMenuVictoria = true;
        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (!mostrarMenuVictoria) return;

        float anchoPanel = 400f;
        float altoPanel = 200f;
        float x = (Screen.width - anchoPanel) / 2f;
        float y = (Screen.height - altoPanel) / 2f;

        GUI.Box(new Rect(x, y, anchoPanel, altoPanel), "");

        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label);
        estiloTitulo.fontSize = 28;
        estiloTitulo.alignment = TextAnchor.MiddleCenter;
        estiloTitulo.normal.textColor = Color.yellow;

        GUIStyle estiloSubtitulo = new GUIStyle(GUI.skin.label);
        estiloSubtitulo.fontSize = 16;
        estiloSubtitulo.alignment = TextAnchor.MiddleCenter;

        GUIStyle estiloBoton = new GUIStyle(GUI.skin.button);
        estiloBoton.fontSize = 20;

        GUI.Label(new Rect(x, y + 20f, anchoPanel, 60f), "¡VICTORIA!", estiloTitulo);
        GUI.Label(new Rect(x, y + 75f, anchoPanel, 35f), "Has llevado el fuego a su destino.", estiloSubtitulo);

        if (GUI.Button(new Rect(x + 40f, y + 130f, 140f, 45f), "Reiniciar", estiloBoton))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (GUI.Button(new Rect(x + 220f, y + 130f, 140f, 45f), "Salir", estiloBoton))
        {
            Time.timeScale = 1f;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    public void ApagarFuegoAntorcha()
    {
        if (fuegoEnAntorcha != null) fuegoEnAntorcha.SetActive(false);
        gameObject.tag = tagOriginal;
        Debug.Log($"<color=yellow>ACTUADOR: Fuego apagado. Tag restaurado a '{tagOriginal}'.</color>");
    }

    void ActualizarAnimaciones()
    {
        if (anim == null) return;
        anim.SetBool("isMoving", moveInput.magnitude > 0.01f);
        anim.SetBool("isBack", moveInput.y < -0.1f);
        anim.SetBool("isRight", moveInput.x > 0.1f);
        anim.SetBool("isLeft", moveInput.x < -0.1f);
    }
}