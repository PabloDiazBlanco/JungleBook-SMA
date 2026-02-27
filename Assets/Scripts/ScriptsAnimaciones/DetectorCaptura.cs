using UnityEngine;
using UnityEngine.Events; // Necesario para los eventos
using UnityEngine.SceneManagement;

public class DetectorCaptura : MonoBehaviour
{
    // Este evento aparecerá en el Inspector como un recuadro donde puedes arrastrar cosas
    public UnityEvent onCapture; 
    private bool capturado = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief") && !capturado)
        {
            capturado = true;

            // 1. Lanzamos el evento. "Avisamos" a quien esté escuchando.
            if (onCapture != null) onCapture.Invoke();

            // 2. Reiniciamos la escena con un pequeño retraso
            Invoke("ReiniciarEscena", 1.5f);
        }
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}