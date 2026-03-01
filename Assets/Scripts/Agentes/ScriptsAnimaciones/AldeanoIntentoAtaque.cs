using UnityEngine;

public class AldeanoIntentoAtaque : MonoBehaviour
{
    private AldeanoAnimationControllers animControl;

    void Start()
    {
        animControl = GetComponentInParent<AldeanoAnimationControllers>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Thief"))
        {
            if (animControl != null) 
            {
                animControl.EjecutarAtaque();
            }
        }
    }
}