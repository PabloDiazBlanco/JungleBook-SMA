using UnityEngine;

public class LoboIntentoAtaque : MonoBehaviour
{
    private LoboAnimationControllers animControl;

    void Start()
    {
        animControl = GetComponentInParent<LoboAnimationControllers>();
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