using UnityEngine;
using UnityEngine.Events; 
using UnityEngine.SceneManagement;

public class DetectorCaptura : MonoBehaviour
{
    public UnityEvent onCapture; 
    private bool capturado = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief") && !capturado)
        {
            capturado = true;

            if (onCapture != null) onCapture.Invoke();

            Invoke("ReiniciarEscena", 1.5f);
        }
    }

    void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}