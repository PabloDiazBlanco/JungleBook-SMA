using UnityEngine;
using System.Collections.Generic;

public class SectorMap : MonoBehaviour
{
    private static List<Vector3> sectores = new List<Vector3>();

    public static IReadOnlyList<Vector3> Sectores => sectores;

    void Awake()
    {
        sectores.Clear();
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Sector");
        foreach (GameObject obj in objs)
            sectores.Add(obj.transform.position);

        Debug.Log($"[SectorMap]: {sectores.Count} sectores cargados.");
    }
}
