using UnityEngine;
using UnityEngine.Events; 
using UnityEngine.SceneManagement;

public class DetectorCaptura : MonoBehaviour
{
    public UnityEvent onCapture; 
    private bool capturado;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief") || other.CompareTag("LadronConFuego"))
        {
            if (onCapture != null) onCapture.Invoke();

            Invoke("ReiniciarEscena", 1.5f);
        }
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}