using UnityEngine;

public class SensorPuertas : MonoBehaviour
{
    public float radioDeteccion = 12f;
    public LayerMask capaInteres;
    public Transform ultimaPuertaDetectada;

    void Update()
    {
        EscanearPuertas();
    }

    private void EscanearPuertas()
    {
        Collider[] objetos = Physics.OverlapSphere(transform.position, radioDeteccion, capaInteres);
        ultimaPuertaDetectada = null;

        foreach (Collider obj in objetos)
        {
            if (obj.CompareTag("Doors_houses"))
            {
                ultimaPuertaDetectada = obj.transform;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}