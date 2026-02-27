using UnityEngine;

public class LoboIntentoAtaque : MonoBehaviour
{
    private LoboAnimationControllers animControl;

    void Start()
    {
        // Buscamos el controlador en el padre (el Lobo)
        animControl = GetComponentInParent<LoboAnimationControllers>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief"))
        {
            // El lobo intenta morder porque está cerca
            if (animControl != null) 
            {
                animControl.EjecutarAtaque();
            }
        }
    }
}