using UnityEngine;

public abstract class EstadoAgente
{
    // Se ejecuta una vez al entrar en el estado
    public abstract void Enter(AgenteBase agente);
    
    // Se ejecuta en cada frame (como un Update privado)
    public abstract void Execute(AgenteBase agente);
    
    // Se ejecuta al salir del estado
    public abstract void Exit(AgenteBase agente);
}