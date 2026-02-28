using UnityEngine;

public class AldeanoIntentoAtaque : MonoBehaviour
{
    private AldeanoAnimationControllers animControl;

    void Start()
    {
        // Buscamos el controlador en el objeto padre (el Aldeano)
        animControl = GetComponentInParent<AldeanoAnimationControllers>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Si el ladrón entra en este radio de "peligro"
        if (other.CompareTag("Thief"))
        {
            if (animControl != null) 
            {
                // Intentamos ejecutar la animación
                animControl.EjecutarAtaque();
            }
        }
    }
}