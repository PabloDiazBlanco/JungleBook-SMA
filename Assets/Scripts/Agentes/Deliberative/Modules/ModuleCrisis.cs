using UnityEngine;
using System.Collections.Generic;

public class ModuleCrisis : DeliberativeModule
{
    private bool alarmaHogueraActivaFrameAnterior;

    public override void Procesar()
    {
        // 1. Revisar mensajes INFORM del buzón
        foreach (FIPAMessage msg in communicator.GetInbox())
        {
            if (msg.performativa == FIPAPerformativa.INFORM)
            {
                ProcesarInform(msg);
            }
        }

        // 2. Gestionar la difusión de nuestra propia alarma
        GestionarDifusionAlarma();
    }

    private void ProcesarInform(FIPAMessage msg)
    {
        if (msg.contenido != "alarma_hoguera") return;
        if (controller.AlarmaHogueraActiva) return;

        controller.InyectarAlarmaHoguera();
        Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: INFORM de '{msg.emisor}' — ¡ALARMA HOGUERA!</color>");
    }

    private void GestionarDifusionAlarma()
    {
        if (controller.AlarmaHogueraActiva && !alarmaHogueraActivaFrameAnterior)
        {
            FIPAMessage msg = new FIPAMessage(
                FIPAPerformativa.INFORM, 
                communicator.nombreAgente, 
                "broadcast",
                "alarma_hoguera", 
                communicator.GenerarConversationId("alarma_hoguera")
            );

            communicator.EnviarATodos(msg);
            Debug.Log($"<color=red>[CRISIS {communicator.nombreAgente}]: INFORM 'alarma_hoguera' enviado a todos.</color>");
        }

        alarmaHogueraActivaFrameAnterior = controller.AlarmaHogueraActiva;
    }
}