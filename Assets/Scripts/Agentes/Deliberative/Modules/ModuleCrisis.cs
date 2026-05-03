using UnityEngine;
using System.Collections.Generic;

public class ModuleCrisis : DeliberativeModule
{
    public override void Procesar()
    {
        foreach (FIPAMessage msg in communicator.GetInbox())
        {
            if (msg.performativa == FIPAPerformativa.INFORM)
            {
                ProcesarInform(msg);
            }
        }
    }

    private void ProcesarInform(FIPAMessage msg)
    {
        if (msg.contenido != "alarma_hoguera") return;
        if (controller.AlarmaHogueraActiva) return;

        controller.InyectarAlarmaHoguera();
        Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: INFORM de '{msg.emisor}' — ¡ALARMA HOGUERA!</color>");
    }
}