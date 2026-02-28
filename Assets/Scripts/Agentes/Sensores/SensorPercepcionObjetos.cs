using UnityEngine;

public class SensorPercepcionObjetos : MonoBehaviour
{
    public float radioDeteccion = 10f;
    public LayerMask capaInteres;

    // Variables para almacenar lo que ve
    public Transform ultimaPuertaDetectada;
    public bool veHoguera = false;

    void Update()
    {
        EscanearEntorno();
    }

    private void EscanearEntorno()
    {
        Collider[] objetos = Physics.OverlapSphere(transform.position, radioDeteccion, capaInteres);
        
        // Reset de percepciones en cada escaneo
        ultimaPuertaDetectada = null;
        veHoguera = false;

        foreach (var obj in objetos)
        {
            if (obj.CompareTag("Doors_houses"))
            {
                ultimaPuertaDetectada = obj.transform;
            }
            if (obj.CompareTag("Hoguera"))
            {
                veHoguera = true;
            }
        }
    }
}