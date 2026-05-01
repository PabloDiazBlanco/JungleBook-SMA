using UnityEngine;

namespace Agentes.Bases.Cerebro
{
    public class AgentTimerManager : MonoBehaviour
    {
        [Header("Configuración de Tiempos")]
        public float tiempoBusqueda = 10f;
        public float tiempoGraciaHoguera = 5f;
        public float cooldownSonido = 5f;
        public float cooldownPuerta = 8f;

        // Cronómetros internos
        public float CronometroBusqueda { get; private set; }
        public float CronometroGracia { get; private set; }
        public float CronometroInvestigacion { get; private set; }
        public float CronometroPuerta { get; private set; }
        public float CronometroLimiteBusqueda { get; private set; }

        private bool EsAldeanoPrincipal => gameObject.name == "Aldeano3";

        public void ActualizarTimers()
        {
            float dt = Time.deltaTime;

            if (CronometroBusqueda > 0) CronometroBusqueda -= dt;
            if (CronometroGracia > 0) CronometroGracia -= dt;
            
            if (CronometroInvestigacion > 0)
            {
                CronometroInvestigacion -= dt;
                if (CronometroInvestigacion <= 0 && EsAldeanoPrincipal)
                    Debug.Log($"[TIMERS] Cooldown de sonido terminado.");
            }

            if (CronometroPuerta > 0)
            {
                CronometroPuerta -= dt;
                if (CronometroPuerta <= 0 && EsAldeanoPrincipal)
                    Debug.Log($"[TIMERS] Cooldown de puerta terminado.");
            }
        }

        // Métodos de control
        public void IniciarBusqueda() => CronometroBusqueda = tiempoBusqueda;
        public void ReducirBusqueda() { if (CronometroBusqueda > 0) CronometroBusqueda -= Time.deltaTime; }
        
        public void IniciarGracia() => CronometroGracia = tiempoGraciaHoguera;
        
        public void IniciarCooldownSonido() => CronometroInvestigacion = cooldownSonido;
        public void ResetearSonido() => CronometroInvestigacion = 0;

        public void IniciarCooldownPuerta() => CronometroPuerta = cooldownPuerta;

        public void SetCronometroLimite(float tiempo) => CronometroLimiteBusqueda = tiempo;
        public void AvanzarLimite() { if (CronometroLimiteBusqueda > 0) CronometroLimiteBusqueda -= Time.deltaTime; }

        // Getters lógicos
        public bool InvestigacionEnCooldown => CronometroInvestigacion > 0;
        public bool PuertaEnCooldown => CronometroPuerta > 0;
        public bool GraciaActiva => CronometroGracia > 0;
    }
}