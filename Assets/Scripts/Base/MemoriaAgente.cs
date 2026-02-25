using UnityEngine;

public class MemoriaAgente : MonoBehaviour
{
    // Creencias sobre el entorno (Beliefs)
    public bool veoAlLadron;
    public bool escuchoAlLadron; // Nueva creencia basada en el sonido
    public Vector3 ultimaPosicionConocida;
    public float nivelDeAlerta; // De 0 a 1
    
    // Esto es el "Mundo simbólico" que menciona el Tema 3a
    public void ActualizarCreenciaVision(bool avistado, Vector3 posicion)
    {
        veoAlLadron = avistado;
        if (avistado)
            ultimaPosicionConocida = posicion;
    }
}