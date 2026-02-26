using UnityEngine;
using UnityEngine.SceneManagement;

public class DetectorCaptura : MonoBehaviour
{
    public string etiquetaLadron = "Thief";

    // Este es el metodo que funcionara con "Is Trigger" marcado
    private void OnTriggerEnter(Collider other)
    {
        // Debug para ver en consola si alguien entra en el espacio del guardia
        Debug.Log("Algo ha entrado en el sensor del guardia: " + other.gameObject.name);

        if (other.gameObject.CompareTag(etiquetaLadron))
        {
            ReiniciarNivel();
        }
    }

    private void ReiniciarNivel()
    {
        Debug.Log("CAPTURA CONFIRMADA. Reiniciando escena...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}